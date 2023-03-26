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

            TraceLogger.Verbose($"Executor is reserved. Currently available: {_concurrentAwaiter.CurrentCount}");
        }

        /// <inheritdoc/>
        public void Release()
        {
            var previousCount = _concurrentAwaiter.Release();

            TraceLogger.Verbose($"Executor is released. Currently available: {previousCount + 1}");
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
