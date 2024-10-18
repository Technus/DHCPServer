using System.Collections.Concurrent;
using System.Net;
using GitHub.JPMikkers.DHCP.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GitHub.JPMikkers.DHCP;

public class DHCPServer : IDHCPServer
{
    private const int s_clientInformationWriteRetries = 10;
    private IUDPSocket _socket = default!;
    private readonly ILogger _logger;
    private readonly string? _clientInfoPath;
    private readonly IUDPSocketFactory _udpSocketFactory;
    private readonly ConcurrentDictionary<DHCPClient, DHCPClient> _clients = new();
    private TimeSpan _leaseTime = TimeSpan.FromDays(1);
    private int _minimumPacketSize = 576;
    private readonly AutoPumpQueue<int> _updateClientInfoQueue;
    private readonly Random _random = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _mainTask;

    #region IDHCPServer Members

    public event Action<IDHCPServer, DHCPStopEventArgs?>? OnStatusChange;
    public event Action<IDHCPServer, string?>? OnTrace;

    public IPEndPoint EndPoint { get; set; } = new(IPAddress.Loopback, 67);

    public IPAddress SubnetMask { get; set; } = IPAddress.Any;

    public IPAddress PoolStart { get; set; } = IPAddress.Any;

    public IPAddress PoolEnd { get; set; } = IPAddress.Broadcast;

    public TimeSpan OfferExpirationTime { get; set; } = TimeSpan.FromSeconds(30.0);

    public TimeSpan LeaseTime
    {
        get => _leaseTime;
        set => _leaseTime = Utils.SanitizeTimeSpan(value);
    }

    public int MinimumPacketSize
    {
        get => _minimumPacketSize;
        set => _minimumPacketSize = Math.Max(value, 312);
    }

    public string HostName { get; }

    public IList<DHCPClient> Clients => new List<DHCPClient>(_clients.Keys.Select(x => x.Clone()));

    public bool Active { get; private set; } = false;

    public List<OptionItem> Options { get; set; } = [];

    public List<IDHCPMessageInterceptor> Interceptors { get; set; } = [];

    public List<ReservationItem> Reservations { get; set; } = [];

    private void OnUpdateClientInfo(AutoPumpQueue<int> sender, int data)
    {
        if(Active)
        {
            try
            {
                if(_clientInfoPath != null)
                {
                    var clientInformation = new DHCPClientInformation();
                    clientInformation.Clients.AddRange(Clients);

                    for(var t = 0; t < s_clientInformationWriteRetries; t++)
                    {
                        try
                        {
                            clientInformation.Write(_clientInfoPath);
                            break;
                        }
                        catch
                        {
                            if(t >= s_clientInformationWriteRetries - 1)
                                Trace("Could not update client information, data might be stale");
                            Thread.Sleep(_random.Next(500, 1000));
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Trace($"Exception in OnUpdateClientInfo : {e}");
            }
        }
    }

    public DHCPServer(ILogger logger, IUDPSocketFactory udpSocketFactory) : this(logger, null, udpSocketFactory)
    { }

    public DHCPServer(ILogger logger, string? clientInfoPath, IUDPSocketFactory udpSocketFactory)
    {
        _updateClientInfoQueue = new AutoPumpQueue<int>(logger, OnUpdateClientInfo);
        _logger = logger ?? NullLogger.Instance;
        _clientInfoPath = clientInfoPath;
        _udpSocketFactory = udpSocketFactory;
        HostName = Environment.MachineName;
    }

    public void Start()
    {
        try
        {
            var clientInformation = string.IsNullOrWhiteSpace(_clientInfoPath) ? 
                new DHCPClientInformation() : 
                DHCPClientInformation.Read(_clientInfoPath);

            foreach(DHCPClient client in clientInformation.Clients
                .Where(c => c.State != DHCPClient.TState.Offered && 
                        IsIPAddressInPoolRange(c.IPAddress))) // Forget offered clients and clients no longer in ip range.
            {
                _ = _clients.TryAdd(client, client);
            }
        }
        catch
        {
            //Hmmm...
        }

        if(!Active)
        {
            try
            {
                Trace($"Starting DHCP server '{EndPoint}'");
                Active = true;
                _socket = _udpSocketFactory.Create(EndPoint, 2048, true, 10);
                _mainTask = Task.Run(() => MainTask(_cancellationTokenSource.Token));
                Trace("DHCP Server start succeeded");
            }
            catch(Exception e)
            {
                Trace($"DHCP Server start failed, reason '{e}'");
                Active = false;
                throw;
            }
        }

        HandleStatusChange(null);
    }

    #endregion

    #region Dispose pattern

    ~DHCPServer()
    {
        try
        {
            Dispose(false);
        }
        catch
        {
            // never let any exception escape the finalizer, or else your process will be killed.
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if(disposing)
        {
            Stop();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    #endregion

    private void HandleStatusChange(DHCPStopEventArgs? data)
    {
        _updateClientInfoQueue.Enqueue(0);
        OnStatusChange?.Invoke(this, data);
    }

    internal void Trace(string msg)
    {
        OnTrace?.Invoke(this, msg);
        _logger?.LogInformation(msg);
    }

    public void Stop() => Stop(null);

    private void Stop(Exception? reason)
    {
        bool notify = false;

        if(Active)
        {
            Trace($"Stopping DHCP server '{EndPoint}'");
            Active = false;
            notify = true;
            _cancellationTokenSource.Cancel();

            if(_mainTask!= null)
            {
                try
                {
                    _mainTask.GetAwaiter().GetResult();
                }
                catch(Exception ex)
                {
                    _logger?.LogError(ex, $"Exception during {nameof(Stop)}");
                }
                _mainTask = null;
            }

            _socket.Dispose();
            Trace("Stopped");
        }

        if(notify)
        {
            HandleStatusChange(new() { Reason = reason });
        }
    }

    private void CheckLeaseExpiration()
    {
        bool modified = false;

        foreach(var client in _clients.Keys.ToList())
        {
            if((client.State == DHCPClient.TState.Offered && (DateTime.Now - client.OfferedTime) > OfferExpirationTime) ||
                (client.State == DHCPClient.TState.Assigned && (DateTime.Now > client.LeaseEndTime)))
            {
                RemoveClient(client);
                modified = true;
            }
        }

        if(modified)
        {
            HandleStatusChange(null);
        }
    }

    private void RemoveClient(DHCPClient client)
    {
        if(_clients.TryRemove(client, out _))
        {
            Trace($"Removed client '{client}' from client table");
        }
    }

    private async Task SendMessage(DHCPMessage msg, IPEndPoint endPoint)
    {
        Trace($"==== Sending response to {endPoint} ====");
        Trace(Utils.PrefixLines(msg.ToString(), "s->c "));

        try
        {
            await using var m = new MemoryStream();
            msg.ToStream(m, _minimumPacketSize);
            await _socket.Send(endPoint, m.ToArray(), _cancellationTokenSource.Token);
        }
        catch(Exception e)
        {
            // treat any send failures like a lost udp packet, we don't want any badly behaving DHCP clients to kill the server
            Trace($"{nameof(SendMessage)} failed: {e.Message}");
        }
    }

    private void AppendConfiguredOptions(DHCPMessage sourceMsg, DHCPMessage targetMsg)
    {
        foreach(var optionItem in Options)
        {
            if((optionItem.Mode == OptionMode.Force || sourceMsg.IsRequestedParameter(optionItem.Option.OptionType)) && 
                targetMsg.GetOption(optionItem.Option.OptionType) == null)
            {
                targetMsg.Options.Add(optionItem.Option);
            }
        }

        foreach(IDHCPMessageInterceptor interceptor in Interceptors)
        {
            interceptor.Apply(sourceMsg, targetMsg);
        }
    }

    private async Task SendOFFER(DHCPMessage sourceMsg, IPAddress offeredAddress, TimeSpan leaseTime)
    {
        //Field      DHCPOFFER            
        //-----      ---------            
        //'op'       BOOTREPLY            
        //'htype'    (From "Assigned Numbers" RFC)
        //'hlen'     (Hardware address length in octets)
        //'hops'     0                    
        //'xid'      'xid' from client DHCPDISCOVER message              
        //'secs'     0                    
        //'ciaddr'   0                    
        //'yiaddr'   IP address offered to client            
        //'siaddr'   IP address of next bootstrap server     
        //'flags'    'flags' from client DHCPDISCOVER message              
        //'giaddr'   'giaddr' from client DHCPDISCOVER message              
        //'chaddr'   'chaddr' from client DHCPDISCOVER message              
        //'sname'    Server host name or options           
        //'file'     Client boot file name or options      
        //'options'  options              
        var response = new DHCPMessage();
        response.Opcode = DHCPMessage.TOpcode.BootReply;
        response.HardwareType = sourceMsg.HardwareType;
        response.Hops = 0;
        response.XID = sourceMsg.XID;
        response.Secs = 0;
        response.ClientIPAddress = IPAddress.Any;
        response.YourIPAddress = offeredAddress;
        response.NextServerIPAddress = IPAddress.Any;
        response.BroadCast = sourceMsg.BroadCast;
        response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
        response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
        response.MessageType = TDHCPMessageType.OFFER;

        //Option                    DHCPOFFER    
        //------                    ---------    
        //Requested IP address      MUST NOT     : ok
        //IP address lease time     MUST         : ok                                               
        //Use 'file'/'sname' fields MAY          
        //DHCP message type         DHCPOFFER    : ok
        //Parameter request list    MUST NOT     : ok
        //Message                   SHOULD       
        //Client identifier         MUST NOT     : ok
        //Vendor class identifier   MAY          
        //Server identifier         MUST         : ok
        //Maximum message size      MUST NOT     : ok
        //All others                MAY          

        response.Options.Add(new DHCPOptionIPAddressLeaseTime(leaseTime));
        response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
        if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) 
            response.Options.Add(new DHCPOptionSubnetMask(SubnetMask));
        AppendConfiguredOptions(sourceMsg, response);
        await SendOfferOrAck(sourceMsg, response);
    }

    private async Task SendNAK(DHCPMessage sourceMsg)
    {
        //Field      DHCPNAK
        //-----      -------
        //'op'       BOOTREPLY
        //'htype'    (From "Assigned Numbers" RFC)
        //'hlen'     (Hardware address length in octets)
        //'hops'     0
        //'xid'      'xid' from client DHCPREQUEST message
        //'secs'     0
        //'ciaddr'   0
        //'yiaddr'   0
        //'siaddr'   0
        //'flags'    'flags' from client DHCPREQUEST message
        //'giaddr'   'giaddr' from client DHCPREQUEST message
        //'chaddr'   'chaddr' from client DHCPREQUEST message
        //'sname'    (unused)
        //'file'     (unused)
        //'options'  
        var response = new DHCPMessage();
        response.Opcode = DHCPMessage.TOpcode.BootReply;
        response.HardwareType = sourceMsg.HardwareType;
        response.Hops = 0;
        response.XID = sourceMsg.XID;
        response.Secs = 0;
        response.ClientIPAddress = IPAddress.Any;
        response.YourIPAddress = IPAddress.Any;
        response.NextServerIPAddress = IPAddress.Any;
        response.BroadCast = sourceMsg.BroadCast;
        response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
        response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
        response.MessageType = TDHCPMessageType.NAK;
        response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
        if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) 
            response.Options.Add(new DHCPOptionSubnetMask(SubnetMask));

        if(!sourceMsg.RelayAgentIPAddress.Equals(IPAddress.Any))
        {
            // If the 'giaddr' field in a DHCP message from a client is non-zero,
            // the server sends any return messages to the 'DHCP server' port on the
            // BOOTP relay agent whose address appears in 'giaddr'.
            await SendMessage(response, new IPEndPoint(sourceMsg.RelayAgentIPAddress, 67));
        }
        else
        {
            // In all cases, when 'giaddr' is zero, the server broadcasts any DHCPNAK
            // messages to 0xffffffff.
            await SendMessage(response, new IPEndPoint(IPAddress.Broadcast, 68));
        }
    }

    private async Task SendACK(DHCPMessage sourceMsg, IPAddress assignedAddress, TimeSpan leaseTime)
    {
        //Field      DHCPACK             
        //-----      -------             
        //'op'       BOOTREPLY           
        //'htype'    (From "Assigned Numbers" RFC)
        //'hlen'     (Hardware address length in octets)
        //'hops'     0                   
        //'xid'      'xid' from client DHCPREQUEST message             
        //'secs'     0                   
        //'ciaddr'   'ciaddr' from DHCPREQUEST or 0
        //'yiaddr'   IP address assigned to client
        //'siaddr'   IP address of next bootstrap server
        //'flags'    'flags' from client DHCPREQUEST message             
        //'giaddr'   'giaddr' from client DHCPREQUEST message             
        //'chaddr'   'chaddr' from client DHCPREQUEST message             
        //'sname'    Server host name or options
        //'file'     Client boot file name or options
        //'options'  options
        var response = new DHCPMessage();
        response.Opcode = DHCPMessage.TOpcode.BootReply;
        response.HardwareType = sourceMsg.HardwareType;
        response.Hops = 0;
        response.XID = sourceMsg.XID;
        response.Secs = 0;
        response.ClientIPAddress = sourceMsg.ClientIPAddress;
        response.YourIPAddress = assignedAddress;
        response.NextServerIPAddress = IPAddress.Any;
        response.BroadCast = sourceMsg.BroadCast;
        response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
        response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
        response.MessageType = TDHCPMessageType.ACK;

        //Option                    DHCPACK            
        //------                    -------            
        //Requested IP address      MUST NOT           : ok
        //IP address lease time     MUST (DHCPREQUEST) : ok
        //Use 'file'/'sname' fields MAY                
        //DHCP message type         DHCPACK            : ok
        //Parameter request list    MUST NOT           : ok
        //Message                   SHOULD             
        //Client identifier         MUST NOT           : ok
        //Vendor class identifier   MAY                
        //Server identifier         MUST               : ok
        //Maximum message size      MUST NOT           : ok  
        //All others                MAY                

        response.Options.Add(new DHCPOptionIPAddressLeaseTime(leaseTime));
        response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
        if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) 
            response.Options.Add(new DHCPOptionSubnetMask(SubnetMask));
        AppendConfiguredOptions(sourceMsg, response);
        await SendOfferOrAck(sourceMsg, response);
    }

    private async Task SendINFORMACK(DHCPMessage sourceMsg)
    {
        // The server responds to a DHCPINFORM message by sending a DHCPACK
        // message directly to the address given in the 'ciaddr' field of the
        // DHCPINFORM message.  The server MUST NOT send a lease expiration time
        // to the client and SHOULD NOT fill in 'yiaddr'.  The server includes
        // other parameters in the DHCPACK message as defined in section 4.3.1.

        //Field      DHCPACK             
        //-----      -------             
        //'op'       BOOTREPLY           
        //'htype'    (From "Assigned Numbers" RFC)
        //'hlen'     (Hardware address length in octets)
        //'hops'     0                   
        //'xid'      'xid' from client DHCPREQUEST message             
        //'secs'     0                   
        //'ciaddr'   'ciaddr' from DHCPREQUEST or 0
        //'yiaddr'   IP address assigned to client
        //'siaddr'   IP address of next bootstrap server
        //'flags'    'flags' from client DHCPREQUEST message             
        //'giaddr'   'giaddr' from client DHCPREQUEST message             
        //'chaddr'   'chaddr' from client DHCPREQUEST message             
        //'sname'    Server host name or options
        //'file'     Client boot file name or options
        //'options'  options
        var response = new DHCPMessage();
        response.Opcode = DHCPMessage.TOpcode.BootReply;
        response.HardwareType = sourceMsg.HardwareType;
        response.Hops = 0;
        response.XID = sourceMsg.XID;
        response.Secs = 0;
        response.ClientIPAddress = sourceMsg.ClientIPAddress;
        response.YourIPAddress = IPAddress.Any;
        response.NextServerIPAddress = IPAddress.Any;
        response.BroadCast = sourceMsg.BroadCast;
        response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
        response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
        response.MessageType = TDHCPMessageType.ACK;

        //Option                    DHCPACK            
        //------                    -------            
        //Requested IP address      MUST NOT              : ok
        //IP address lease time     MUST NOT (DHCPINFORM) : ok
        //Use 'file'/'sname' fields MAY                
        //DHCP message type         DHCPACK               : ok
        //Parameter request list    MUST NOT              : ok
        //Message                   SHOULD             
        //Client identifier         MUST NOT              : ok
        //Vendor class identifier   MAY                
        //Server identifier         MUST                  : ok
        //Maximum message size      MUST NOT              : ok
        //All others                MAY                

        response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
        if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) 
            response.Options.Add(new DHCPOptionSubnetMask(SubnetMask));
        AppendConfiguredOptions(sourceMsg, response);
        await SendMessage(response, new IPEndPoint(sourceMsg.ClientIPAddress, 68));
    }

    private async Task SendOfferOrAck(DHCPMessage request, DHCPMessage response)
    {
        // RFC2131.txt, 4.1, paragraph 4

        // DHCP messages broadcast by a client prior to that client obtaining
        // its IP address must have the source address field in the IP header
        // set to 0.

        if(!request.RelayAgentIPAddress.Equals(IPAddress.Any))
        {
            // If the 'giaddr' field in a DHCP message from a client is non-zero,
            // the server sends any return messages to the 'DHCP server' port on the
            // BOOTP relay agent whose address appears in 'giaddr'.
            await SendMessage(response, new IPEndPoint(request.RelayAgentIPAddress, 67));
        }
        else
        {
            if(!request.ClientIPAddress.Equals(IPAddress.Any))
            {
                // If the 'giaddr' field is zero and the 'ciaddr' field is nonzero, then the server
                // unicasts DHCPOFFER and DHCPACK messages to the address in 'ciaddr'.
                await SendMessage(response, new IPEndPoint(request.ClientIPAddress, 68));
            }
            else
            {
                // If 'giaddr' is zero and 'ciaddr' is zero, and the broadcast bit is
                // set, then the server broadcasts DHCPOFFER and DHCPACK messages to
                // 0xffffffff. If the broadcast bit is not set and 'giaddr' is zero and
                // 'ciaddr' is zero, then the server unicasts DHCPOFFER and DHCPACK
                // messages to the client's hardware address and 'yiaddr' address.  
                await SendMessage(response, new IPEndPoint(IPAddress.Broadcast, 68));
            }
        }
    }

    private bool ServerIdentifierPrecondition(DHCPMessage msg)
    {
        var result = false;
        var dhcpOptionServerIdentifier = msg.FindOption<DHCPOptionServerIdentifier>();

        if(dhcpOptionServerIdentifier != null)
        {
            if(dhcpOptionServerIdentifier.IPAddress.Equals(EndPoint.Address))
            {
                result = true;
            }
            else
            {
                Trace($"Client sent message with non-matching server identifier '{dhcpOptionServerIdentifier.IPAddress}' -> ignored");
            }
        }
        else
        {
            Trace("Client sent message without filling required ServerIdentifier option -> ignored");
        }
        return result;
    }

    private bool IsIPAddressInRange(IPAddress address, IPAddress start, IPAddress end)
    {
        var adr32 = Utils.IPAddressToUInt32(address);
        return adr32 >= Utils.IPAddressToUInt32(SanitizeHostRange(start)) && 
                adr32 <= Utils.IPAddressToUInt32(SanitizeHostRange(end));
    }

    /// <summary>
    /// Checks whether the given IP address falls within the known pool ranges.
    /// </summary>
    /// <param name="address">IP address to check</param>
    /// <returns>true when the ip address matches one of the known pool ranges</returns>
    private bool IsIPAddressInPoolRange(IPAddress address) => 
        IsIPAddressInRange(address, PoolStart, PoolEnd) || 
        Reservations.Exists(r => IsIPAddressInRange(address, r.PoolStart, r.PoolEnd));

    private bool IPAddressIsInSubnet(IPAddress address) => 
        (Utils.IPAddressToUInt32(address) & Utils.IPAddressToUInt32(SubnetMask)) ==
            (Utils.IPAddressToUInt32(EndPoint.Address) & Utils.IPAddressToUInt32(SubnetMask));

    private bool IPAddressIsFree(IPAddress address, bool reuseReleased)
    {
        if(!IPAddressIsInSubnet(address)) 
            return false;

        if(address.Equals(EndPoint.Address)) 
            return false;

        var released = true;

        foreach(var client in _clients.Keys.Where(x=>x.IPAddress.Equals(address)))
        {
            if(reuseReleased && client.State == DHCPClient.TState.Released)
                client.IPAddress = IPAddress.Any;
            else
                released = false;
        }

        return released;
    }

    private IPAddress SanitizeHostRange(IPAddress startend) => Utils.UInt32ToIPAddress(
            (Utils.IPAddressToUInt32(EndPoint.Address) & Utils.IPAddressToUInt32(SubnetMask)) |
            (Utils.IPAddressToUInt32(startend) & ~Utils.IPAddressToUInt32(SubnetMask))
        );

    private IPAddress AllocateIPAddress(DHCPMessage dhcpMessage)
    {
        var dhcpOptionRequestedIPAddress = dhcpMessage.FindOption<DHCPOptionRequestedIPAddress>();

        var reservation = Reservations.Find(x => x.Match(dhcpMessage));

        if(reservation is not null)
        {
            // the client matches a reservation.. find the first free address in the reservation block
            for(var host = Utils.IPAddressToUInt32(SanitizeHostRange(reservation.PoolStart)); 
                host <= Utils.IPAddressToUInt32(SanitizeHostRange(reservation.PoolEnd)); 
                host++)
            {
                var testIPAddress = Utils.UInt32ToIPAddress(host);

                // I don't see the point of avoiding released addresses for reservations (yet)
                if(IPAddressIsFree(testIPAddress, true))
                    return testIPAddress;

                // if Preempt is true, return the first address of the reservation range. Preempt should ONLY ever be used if the range is a single address, and you're 100% sure you'll 
                // _always_ have just a single device in your network that matches the reservation MAC or name.
                if(reservation.Preempt)
                    return testIPAddress;
            }
        }

        
        if(dhcpOptionRequestedIPAddress is not null && 
                IPAddressIsFree(dhcpOptionRequestedIPAddress.IPAddress, true)) // there is a requested IP address. Is it in our subnet and free?
            return dhcpOptionRequestedIPAddress.IPAddress;

        // first try to find a free address without reusing released ones
        for(var host = Utils.IPAddressToUInt32(SanitizeHostRange(PoolStart)); 
            host <= Utils.IPAddressToUInt32(SanitizeHostRange(PoolEnd)); 
            host++)
        {
            var testIPAddress = Utils.UInt32ToIPAddress(host);
            if(IPAddressIsFree(testIPAddress, false))
                return testIPAddress;
        }

        // nothing found.. now start allocating released ones as well
        for(var host = Utils.IPAddressToUInt32(SanitizeHostRange(PoolStart)); 
            host <= Utils.IPAddressToUInt32(SanitizeHostRange(PoolEnd)); 
            host++)
        {
            var testIPAddress = Utils.UInt32ToIPAddress(host);
            if(IPAddressIsFree(testIPAddress, true))
                return testIPAddress;
        }

        // still nothing: report failure
        return IPAddress.Any;
    }

    private async Task OfferClient(DHCPMessage dhcpMessage, DHCPClient client)
    {
        client.State = DHCPClient.TState.Offered;
        client.OfferedTime = DateTime.Now;
        _ = _clients.TryAdd(client, client);
        await SendOFFER(dhcpMessage, client.IPAddress, _leaseTime);
    }

    private async Task MainTask(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        Task<(IPEndPoint,ReadOnlyMemory<byte>)>? receiveTask = default;
        Task? timerTask = default;

        _logger?.LogInformation("Maintask has started");

        try
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                receiveTask ??= _socket.Receive(cancellationToken);
                timerTask ??= timer.WaitForNextTickAsync(cancellationToken).AsTask();

                var completedTask = await Task.WhenAny(receiveTask, timerTask);

                if(completedTask == receiveTask)
                {
                    try
                    {
                        (var ipEndPoint, var data) = await receiveTask;
                        await OnReceive(ipEndPoint, data);
                    }
                    catch(UDPSocketException ex) when (!ex.IsFatal)
                    {
                        // udp socket says something non-fatal happened, ignore and try receiving the next packet
                    }
                    finally
                    {
                        receiveTask = null;
                    }
                }
                else if(completedTask == timerTask)
                {
                    try
                    {
                        await timerTask;
                        CheckLeaseExpiration();
                    }
                    finally
                    {
                        timerTask = null;
                    }
                }
            }
            // this ensures a normal exit is always handled via the catch clause of OperationCanceledException
            cancellationToken.ThrowIfCancellationRequested();   
        }
        catch(OperationCanceledException)
        {
            // OperationCanceledException is the normal way of exiting MainTask
            _logger?.LogInformation("Maintask has stopped gracefully");
        }
        catch(Exception e)
        {
            _logger?.LogError(e, $"fatal exception in {nameof(MainTask)}");
            // TODO jmik: throw to report ??
        }
        finally
        {
            // make sure the two marker tasks don't cause unobserved exceptions, because it's
            // possible one of them is still in the proces of canceling
            // thanks to https://github.com/Nuklon for reporting this issue!
            receiveTask?.IgnoreExceptions();
            timerTask?.IgnoreExceptions();
        }
    }

    private DHCPClient? GetKnownClient(DHCPClient client) => _clients.GetValueOrDefault(client);

    private async Task OnReceive(IPEndPoint endPoint, ReadOnlyMemory<byte> data)
    {
        try
        {
            Trace("Incoming packet - parsing DHCP Message");

            // translate array segment into a DHCPMessage
            var dhcpMessage = DHCPMessage.FromMemory(data);
            Trace(Utils.PrefixLines(dhcpMessage.ToString(), "c->s "));

            // only react to messages from client to server. Ignore other types.
            if(dhcpMessage.Opcode == DHCPMessage.TOpcode.BootRequest)
            {
                var client = DHCPClient.CreateFromMessage(dhcpMessage);
                Trace($"Client {client} sent {dhcpMessage.MessageType}");
                // is it a known client?
                var knownClient = GetKnownClient(client);

                switch(dhcpMessage.MessageType)
                {
                    // DHCPDISCOVER - client to server
                    // broadcast to locate available servers
                    case TDHCPMessageType.DISCOVER:

                        if(knownClient is null)
                        {
                            Trace("Client is not known yet");
                            // client is not known yet.
                            // allocate new address, add client to client table in Offered state
                            client.IPAddress = AllocateIPAddress(dhcpMessage);
                            // allocation ok ?
                            if(!client.IPAddress.Equals(IPAddress.Any))
                            {
                                await OfferClient(dhcpMessage, client);
                                break;
                            }

                            Trace("No more free addresses. Don't respond to discover");
                            break;
                        }

                        Trace($"Client is known, in state {knownClient.State}");

                        if(knownClient.State == DHCPClient.TState.Offered || knownClient.State == DHCPClient.TState.Assigned)
                        {
                            Trace("Client sent DISCOVER but we already offered, or assigned -> repeat offer with known address");
                            await OfferClient(dhcpMessage, knownClient);
                            break;
                        }

                        Trace("Client is known but released");
                        // client is known but released or dropped. Use the old address or allocate a new one
                        if(!knownClient.IPAddress.Equals(IPAddress.Any))
                        {
                            await OfferClient(dhcpMessage, knownClient);
                            break;
                        }

                        knownClient.IPAddress = AllocateIPAddress(dhcpMessage);

                        if(!knownClient.IPAddress.Equals(IPAddress.Any))
                        {
                            await OfferClient(dhcpMessage, knownClient);
                            break;
                        }

                        Trace("No more free addresses. Don't respond to discover");
                        break;

                    // DHCPREQUEST - client to server
                    // Client message to servers either 
                    // (a) requesting offered parameters from one server and implicitly declining offers from all others.
                    // (b) confirming correctness of previously allocated address after e.g. system reboot, or
                    // (c) extending the lease on a particular network address
                    case TDHCPMessageType.REQUEST:

                        // is there a server identifier?
                        var dhcpOptionServerIdentifier = dhcpMessage.FindOption<DHCPOptionServerIdentifier>();
                        var dhcpOptionRequestedIPAddress = dhcpMessage.FindOption<DHCPOptionRequestedIPAddress>();

                        if(dhcpOptionServerIdentifier is null)
                        {
                            // no server identifier: the message is a request to verify or extend an existing lease
                            // Received REQUEST without server identifier, client is INIT-REBOOT, RENEWING or REBINDING

                            Trace("Received REQUEST without server identifier, client state is INIT-REBOOT, RENEWING or REBINDING");

                            if(!dhcpMessage.ClientIPAddress.Equals(IPAddress.Any))
                            {
                                Trace("REQUEST client IP is filled in -> client state is RENEWING or REBINDING");

                                // see : http://www.tcpipguide.com/free/t_DHCPLeaseRenewalandRebindingProcesses-2.htm

                                if(knownClient != null &&
                                    knownClient.State == DHCPClient.TState.Assigned &&
                                    knownClient.IPAddress.Equals(dhcpMessage.ClientIPAddress))
                                {
                                    // known, assigned, and IP address matches administration. ACK
                                    knownClient.LeaseStartTime = DateTime.Now;
                                    knownClient.LeaseDuration = _leaseTime;
                                    await SendACK(dhcpMessage, dhcpMessage.ClientIPAddress, knownClient.LeaseDuration);
                                    break;
                                }

                                // not known, or known but in some other state. Just dump the old one.
                                if(knownClient != null)
                                    RemoveClient(knownClient);

                                // check if client IP address is marked free
                                if(!IPAddressIsFree(dhcpMessage.ClientIPAddress, false))
                                {
                                    Trace("Renewing client IP address already in use. Oops..");
                                    break;
                                }

                                // it's free. send ACK
                                client.IPAddress = dhcpMessage.ClientIPAddress;
                                client.State = DHCPClient.TState.Assigned;
                                client.LeaseStartTime = DateTime.Now;
                                client.LeaseDuration = _leaseTime;
                                _ = _clients.TryAdd(client, client);
                                await SendACK(dhcpMessage, dhcpMessage.ClientIPAddress, client.LeaseDuration);
                            }
                            else
                            {
                                Trace("REQUEST client IP is empty -> client state is INIT-REBOOT");

                                if(dhcpOptionRequestedIPAddress == null)
                                {
                                    Trace("Client sent apparent INIT-REBOOT REQUEST but with an empty 'RequestedIPAddress' option. Oops..");
                                    break;
                                }

                                if(knownClient == null || knownClient.State != DHCPClient.TState.Assigned)
                                {
                                    // client not known, or known but in some other state.
                                    // send NAK so client will drop to INIT state where it can acquire a new lease.
                                    // see also: http://tcpipguide.com/free/t_DHCPGeneralOperationandClientFiniteStateMachine.htm
                                    Trace("Client attempted INIT-REBOOT REQUEST but server has no lease for this client -> NAK");
                                    await SendNAK(dhcpMessage);
                                    if(knownClient is not null)
                                        RemoveClient(knownClient);
                                    break;
                                }

                                if(!knownClient.IPAddress.Equals(dhcpOptionRequestedIPAddress.IPAddress))
                                {
                                    Trace($"Client sent request for IP address '{dhcpOptionRequestedIPAddress.IPAddress}', but it does not match cached address '{knownClient.IPAddress}' -> NAK");
                                    await SendNAK(dhcpMessage);
                                    RemoveClient(knownClient);
                                    break;
                                }

                                Trace("Client request matches cached address -> ACK");
                                // known, assigned, and IP address matches administration. ACK
                                knownClient.LeaseStartTime = DateTime.Now;
                                knownClient.LeaseDuration = _leaseTime;
                                await SendACK(dhcpMessage, dhcpOptionRequestedIPAddress.IPAddress, knownClient.LeaseDuration);
                            }
                        }
                        else
                        {
                            // there is a server identifier: the message is in response to a DHCPOFFER
                            if(!dhcpOptionServerIdentifier.IPAddress.Equals(EndPoint.Address))
                            {
                                Trace($"Client requests IP address that was offered by another DHCP server at '{dhcpOptionServerIdentifier.IPAddress}' -> drop offer");
                                // it's a response to another DHCP server.
                                // if we sent an OFFER to this client earlier, remove it.
                                if(knownClient != null)
                                    RemoveClient(knownClient);
                                break;
                            }

                            // it's a response to OUR offer.
                            // but did we actually offer one?
                            if(knownClient is null || knownClient.State != DHCPClient.TState.Offered)
                            {
                                // we don't have an outstanding offer!
                                Trace("Client requested IP address from this server, but we didn't offer any. -> NAK");
                                await SendNAK(dhcpMessage);
                                break;
                            }

                            // yes.
                            // the requested IP address MUST be filled in with the offered address
                            if(dhcpOptionRequestedIPAddress is null)
                            {
                                Trace("Client sent request without filling the RequestedIPAddress option -> NAK");
                                await SendNAK(dhcpMessage);
                                RemoveClient(knownClient);
                                break;
                            }

                            if(!knownClient.IPAddress.Equals(dhcpOptionRequestedIPAddress.IPAddress))
                            {
                                Trace($"Client sent request for IP address '{dhcpOptionRequestedIPAddress.IPAddress}', but it does not match the offered address '{knownClient.IPAddress}' -> NAK");
                                await SendNAK(dhcpMessage);
                                RemoveClient(knownClient);
                                break;
                            }

                            Trace("Client request matches offered address -> ACK");
                            knownClient.State = DHCPClient.TState.Assigned;
                            knownClient.LeaseStartTime = DateTime.Now;
                            knownClient.LeaseDuration = _leaseTime;
                            await SendACK(dhcpMessage, knownClient.IPAddress, knownClient.LeaseDuration);
                        }
                        break;

                    case TDHCPMessageType.DECLINE:
                        // If the server receives a DHCPDECLINE message, the client has
                        // discovered through some other means that the suggested network
                        // address is already in use.  The server MUST mark the network address
                        // as not available and SHOULD notify the local system administrator of
                        // a possible configuration problem.
                        if(!ServerIdentifierPrecondition(dhcpMessage))
                            break;

                        if(knownClient is null)
                        {
                            Trace("Client not found in client table -> decline ignored.");
                            break;
                        }

                        Trace("Found client in client table.");
                        RemoveClient(client);

                        // the network address that should be marked as not available MUST be 
                        // specified in the RequestedIPAddress option.
                        dhcpOptionRequestedIPAddress = dhcpMessage.FindOption<DHCPOptionRequestedIPAddress>();
                        if(dhcpOptionRequestedIPAddress is not null &&
                            dhcpOptionRequestedIPAddress.IPAddress.Equals(knownClient.IPAddress))
                        {
                            Trace($"Error: Client declined address {dhcpOptionRequestedIPAddress.IPAddress} as it might be already in use.");
                        }
                        break;

                    case TDHCPMessageType.RELEASE:
                        // relinguishing network address and cancelling remaining lease.
                        // Upon receipt of a DHCPRELEASE message, the server marks the network
                        // address as not allocated.  The server SHOULD retain a record of the
                        // client's initialization parameters for possible reuse in response to
                        // subsequent requests from the client.
                        if(!ServerIdentifierPrecondition(dhcpMessage))
                            break;

                        if(knownClient is null /*|| knownClient.State != DHCPClient.TState.Assigned*/)
                        {
                            Trace("Client not found in client table, release ignored.");
                            break;
                        }

                        if(!dhcpMessage.ClientIPAddress.Equals(knownClient.IPAddress))
                        {
                            Trace("IP address in RELEASE doesn't match known client address. Mark this client as released with unknown IP");
                            knownClient.IPAddress = IPAddress.Any;
                            knownClient.State = DHCPClient.TState.Released;
                            break;
                        }

                        Trace("Found client in client table, marking as released");
                        knownClient.State = DHCPClient.TState.Released;
                        break;

                    // DHCPINFORM - client to server
                    // client asking for local configuration parameters, client already has externally configured
                    // network address.
                    case TDHCPMessageType.INFORM:
                        // The server responds to a DHCPINFORM message by sending a DHCPACK
                        // message directly to the address given in the 'ciaddr' field of the
                        // DHCPINFORM message.  The server MUST NOT send a lease expiration time
                        // to the client and SHOULD NOT fill in 'yiaddr'.  The server includes
                        // other parameters in the DHCPACK message as defined in section 4.3.1.
                        await SendINFORMACK(dhcpMessage);
                        break;

                    default:
                        Trace($"Invalid `{dhcpMessage.MessageType}` message from client, ignored");
                        break;
                }

                HandleStatusChange(null);
            }
        }
        catch(Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
            System.Diagnostics.Debug.WriteLine(e.StackTrace);
        }
    }
}
