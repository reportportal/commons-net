﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReportPortal.Shared.Internal.Delegating
{
    /// <inheritdoc/>
    public class RequestExecutionThrottler : IRequestExecutionThrottler, IDisposable
    {
        private Logging.ITraceLogger TraceLogger { get; } = Logging.TraceLogManager.Instance.GetLogger<RequestExecutionThrottler>();

        private readonly SemaphoreSlim _concurrentAwaiter;

        private int _maximumConcurrentCountRequested;

        /// <summary>
        /// Initializes new instance of <see cref="RequestExecutionThrottler"/>
        /// </summary>
        /// <param name="maxConcurrentRequests">Limit maximum number of concurrent requests.</param>
        public RequestExecutionThrottler(int maxConcurrentRequests)
        {
            if (maxConcurrentRequests < 1)
            {
                throw new ArgumentException("Maximum concurrent requests should be at least 1.", nameof(maxConcurrentRequests));
            }

            MaxCapacity = maxConcurrentRequests;

            _concurrentAwaiter = new SemaphoreSlim(maxConcurrentRequests);
        }

        /// <inheritdoc/>
        public int MaxCapacity { get; }

        /// <inheritdoc/>
        public async Task ReserveAsync()
        {
            TraceLogger.Verbose($"Awaiting free executor. Currently available: {_concurrentAwaiter.CurrentCount}");

            await _concurrentAwaiter.WaitAsync().ConfigureAwait(false);

            TraceLogger.Verbose($"Executer is reserved. Available executers after reservation: {_concurrentAwaiter.CurrentCount}");

            // track maximum level of actual concurrent requests
            lock (_concurrentAwaiter)
            {
                var maximumConcurrentCountRequested = MaxCapacity - _concurrentAwaiter.CurrentCount;
                if (maximumConcurrentCountRequested > _maximumConcurrentCountRequested)
                {
                    _maximumConcurrentCountRequested = maximumConcurrentCountRequested;
                    TraceLogger.Verbose($"Maximum level of actual concurrent requests has been changed to {_maximumConcurrentCountRequested}");
                }
            }
        }

        /// <inheritdoc/>
        public void Release()
        {
            _concurrentAwaiter.Release();
        }

        /// <summary>
        /// Releases all resources used by RequestExecutionThrottler.
        /// </summary>
        public void Dispose()
        {
            _concurrentAwaiter.Dispose();
        }
    }
}
