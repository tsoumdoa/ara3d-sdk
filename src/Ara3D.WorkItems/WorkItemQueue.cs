using System.Diagnostics;
using System.Threading.Channels;

namespace Ara3D.WorkItems
{
    public sealed class WorkItemQueue : IWorkItemQueue
    {
        public string Name { get; }
        public IWorkItemListener Listener;

        private readonly Channel<WorkItem> _channel;
        private readonly Thread? _thread;

        // Two purposes, two CTS:
        private readonly CancellationTokenSource _shutdownCts = new();
        private CancellationTokenSource _preemptCts = new();

        private volatile bool _disposed;

        public WorkItemQueue(
            string name,
            IWorkItemListener listener,
            ThreadPriority priority,
            bool threaded,
            int capacity)
        {
            Name = name;
            Listener = listener;

            _channel = capacity > 0
                ? Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(capacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest // OK if you truly want newest-first
                })
                : Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });

            if (threaded)
            {
                _thread = new Thread(RunLoop)
                {
                    IsBackground = true,
                    Name = name,
                    Priority = priority
                };
                _thread.Start();
            }
        }

        public void Enqueue(WorkItem item)
        {
            if (!_channel.Writer.TryWrite(item))
                throw new InvalidOperationException($"Failed to enqueue work item {item.Name}");
        }

        public void ProcessAllPendingWork()
        {
            var reader = _channel.Reader;
            var shutdown = _shutdownCts.Token;
            ProcessAllPendingWork(reader, shutdown);
        }

        public void ProcessAllPendingWork(ChannelReader<WorkItem> reader, CancellationToken shutdown)
        {
            // Drain the batch
            while (reader.TryRead(out var work))
            {
                // Linked token = shutdown OR preempt
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(shutdown, _preemptCts.Token);
                var token = linked.Token;

                try
                {
                    try { Listener?.OnWorkStarted(this, work); }
                    catch { Debug.Assert(false, "Listener OnWorkStarted should not throw"); }

                    work.Action(token); // MUST honor token and return promptly on cancel

                    try { Listener?.OnWorkCompleted(this, work); }
                    catch { Debug.Assert(false, "Listener OnWorkCompleted should not throw"); }
                }
                catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
                {
                    // Shutdown cancel: exit loop immediately
                    return;
                }
                catch (OperationCanceledException)
                {
                    // Preempt cancel: skip to next item
                    try { Listener?.OnWorkCompleted(this, work); } catch { }
                }
                catch (Exception ex)
                {
                    try { Listener?.OnWorkError(this, work, ex); }
                    catch { Debug.Assert(false, "Listener OnWorkError should not throw"); }
                }
            }
        }

        public void ClearAllPendingWork()
        {
            var r = _channel.Reader;
            while (r.TryRead(out _)) { /* drop */ }
        }

        /// <summary>
        /// Preempt whatever is running, and drop queued work.
        /// Consumer thread keeps running and will accept new items.
        /// </summary>
        public void CancelCurrentAndClearPending()
        {
            // Cancel any in-flight work
            _preemptCts.Cancel();

            // Fresh CTS for future items
            _preemptCts.Dispose();
            _preemptCts = new CancellationTokenSource();

            // Drop queued work
            ClearAllPendingWork();
        }

        private void RunLoop(object? _)
        {
            var reader = _channel.Reader;
            var shutdown = _shutdownCts.Token;

            try
            {
                while (!shutdown.IsCancellationRequested)
                {
                    // Wait until there is data or the channel is completed.
                    // IMPORTANT: only bind to the shutdown token here; preemption should not break the loop.
                    var channelStillOpen = reader.WaitToReadAsync(shutdown).AsTask().GetAwaiter().GetResult();
                    if (!channelStillOpen) 
                        break; // channel completed

                    ProcessAllPendingWork(reader, shutdown);
                }
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{Name}] WorkItemQueue loop error: {ex}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Stop accepting new items and signal the reader
            _channel.Writer.TryComplete();

            // Cancel any in-flight work and also break the wait
            _preemptCts.Cancel();
            _shutdownCts.Cancel();

            // Bound the join so we never hang Dispose
            if (_thread is not null && !_thread.Join(millisecondsTimeout: 2000))
            {
                Debug.WriteLine($"[{Name}] Worker did not stop within timeout.");
                // last resort: let process shutdown reclaim background thread
            }

            _preemptCts.Dispose();
            _shutdownCts.Dispose();
        }
    }
}
