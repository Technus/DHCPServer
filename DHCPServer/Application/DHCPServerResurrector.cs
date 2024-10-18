using GitHub.JPMikkers.DHCP;
using System.Diagnostics;
using System.Net;
using Timer = System.Threading.Timer;

namespace DHCPServerApp
{
    public class DHCPServerResurrector : IDisposable
    {
        private const int RetryTime = 30000;
        private readonly SemaphoreSlim _semaphore = new(1,1);
        private bool _disposed;
        private readonly DHCPServerConfiguration _config;
        private readonly EventLog _eventLog;

        private DHCPServer? _server;
        private readonly Timer _retryTimer;

        public DHCPServerResurrector(DHCPServerConfiguration config, EventLog eventLog)
        {
            _disposed = false;
            _config = config;
            _eventLog = eventLog;
            _retryTimer = new Timer(new TimerCallback(x=>Resurrect()));
            Resurrect();
        }

        ~DHCPServerResurrector()
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

        private void Resurrect()
        {
            _semaphore.Wait();
            try
            {
                if(!_disposed)
                {
                    try
                    {
                        _server = new DHCPServer(default, Program.GetClientInfoPath(_config.Name, _config.Address), default);//TODO
                        _server.EndPoint = new IPEndPoint(IPAddress.Parse(_config.Address), 67);
                        _server.SubnetMask = IPAddress.Parse(_config.NetMask);
                        _server.PoolStart = IPAddress.Parse(_config.PoolStart);
                        _server.PoolEnd = IPAddress.Parse(_config.PoolEnd);
                        _server.LeaseTime = (_config.LeaseTime > 0) ? TimeSpan.FromSeconds(_config.LeaseTime) : Utils.InfiniteTimeSpan;
                        _server.OfferExpirationTime = TimeSpan.FromSeconds(Math.Max(1, _config.OfferTime));
                        _server.MinimumPacketSize = _config.MinimumPacketSize;

                        var options = new List<OptionItem>();
                        foreach(var optionConfiguration in _config.Options)
                        {
                            options.Add(optionConfiguration.ConstructOptionItem());
                        }
                        _server.Options = options;

                        var reservations = new List<ReservationItem>();
                        foreach(var reservationConfiguration in _config.Reservations)
                        {
                            reservations.Add(reservationConfiguration.ConstructReservationItem());
                        }
                        _server.Reservations = reservations;

                        _server.OnStatusChange += server_OnStatusChange;
                        _server.OnTrace += server_OnTrace;
                        _server.Start();
                    }
                    catch(Exception)
                    {
                        CleanupAndRetry();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Log(EventLogEntryType entryType, string msg)
        {
            _eventLog.WriteEntry($"{_config.Name} : {msg}", entryType);
        }

        private void server_OnTrace(IDHCPServer server, string? trace)
        {
            Log(EventLogEntryType.Information, trace ?? "");
        }

        private void server_OnStatusChange(IDHCPServer server, DHCPStopEventArgs? e)
        {
            if(server.Active)
            {
                //Log(EventLogEntryType.Information, string.Format("{0} transfers in progress", server.ActiveTransfers));
            }
            else
            {
                if(e.Reason != null)
                {
                    Log(EventLogEntryType.Error, $"Stopped, reason: {e.Reason}");
                }
                CleanupAndRetry();
            }
        }

        private void CleanupAndRetry()
        {
            _semaphore.Wait();
            try
            {
                if(!_disposed)
                {
                    // stop server
                    if(_server is not null)
                    {
                        _server.OnStatusChange -= server_OnStatusChange;
                        _server.OnTrace -= server_OnTrace;
                        _server.Dispose();
                        _server = null;
                    }
                    // initiate retry timer
                    _retryTimer.Change(RetryTime, Timeout.Infinite);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                _semaphore.Wait();
                try
                {
                    if(!_disposed)
                    {
                        _disposed = true;

                        _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _retryTimer.Dispose();

                        if(_server is not null)
                        {
                            _server.OnStatusChange -= server_OnStatusChange;
                            _server.Dispose();
                            _server.OnTrace -= server_OnTrace;
                            _server = null;
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        #endregion
    }
}
