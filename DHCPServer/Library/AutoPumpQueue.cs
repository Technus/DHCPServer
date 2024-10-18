using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace DHCP.Server.Library;

public class AutoPumpQueue<T> : IDisposable
{
    public delegate void DataDelegate(AutoPumpQueue<T> sender, T data);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentQueue<T> _queue = [];
    private readonly ILogger _logger;
    private readonly DataDelegate _dataDelegate;
    private bool _disposed;

    /// <summary>
    /// Constructor
    /// </summary>
    public AutoPumpQueue(ILogger logger, DataDelegate dataDelegate)
    {
        _logger = logger ?? NullLogger.Instance;
        _dataDelegate = dataDelegate;
    }

    private void WaitCallback(object? state)
    {
        _semaphore.Wait();
        try
        {
            while(_queue.TryDequeue(out var data))
            {
                try
                {
                    _dataDelegate(this, data);
                }
                catch(Exception e)
                {
                    _logger.LogError(e, "While awaiting Queue.");
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Enqueue(T data)
    {
        bool queueWasEmpty = _queue.IsEmpty;
        _queue.Enqueue(data);

        if(queueWasEmpty)
            ThreadPool.QueueUserWorkItem(WaitCallback);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposed)
        {
            if(disposing)
            {
                _semaphore.Wait();
                try
                {
                    while(_queue.TryDequeue(out var data))
                    {
                        try
                        {
                            _dataDelegate(this, data);
                        }
                        catch(Exception e)
                        {
                            _logger.LogError(e, "While awaiting Queue.");
                        }
                    }
                }
                finally
                {
                    _semaphore.Dispose();
                }
            }

            _queue.Clear();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
