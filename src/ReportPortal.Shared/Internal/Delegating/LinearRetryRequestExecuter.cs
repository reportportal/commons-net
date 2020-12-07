﻿using ReportPortal.Shared.Reporter.Statistics;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReportPortal.Shared.Internal.Delegating
{
    /// <summary>
    /// Invokes given func with retry strategy and linear delay between attempts.
    /// </summary>
    public class LinearRetryRequestExecuter : BaseRequestExecuter
    {
        private Logging.ITraceLogger TraceLogger { get; } = Logging.TraceLogManager.Instance.GetLogger<LinearRetryRequestExecuter>();

        private readonly IRequestExecutionThrottler _concurrentThrottler;

        /// <summary>
        /// Initializes new instance of <see cref="LinearRetryRequestExecuter"/>.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum number of attempts.</param>
        /// <param name="delay">Delay between ateempts (in milliseconds).</param>
        public LinearRetryRequestExecuter(int maxRetryAttempts, int delay) : this(maxRetryAttempts, delay, null)
        {

        }

        /// <summary>
        /// Initializes new instance of <see cref="LinearRetryRequestExecuter"/>.
        /// </summary>
        /// <param name="maxRetryAttempts">Maximum number of attempts.</param>
        /// <param name="delay">Delay between ateempts (in milliseconds).</param>
        /// <param name="throttler">Limits concurrent execution of requests.</param>
        public LinearRetryRequestExecuter(int maxRetryAttempts, int delay, IRequestExecutionThrottler throttler)
        {
            if (maxRetryAttempts < 1)
            {
                throw new ArgumentException("Maximum attempts cannot be less than 1.", nameof(maxRetryAttempts));
            }

            if (delay < 0)
            {
                throw new ArgumentException("Delay cannot be less than 0.", nameof(delay));
            }

            _concurrentThrottler = throttler;
            MaxRetryAttemps = maxRetryAttempts;
            Delay = delay;
        }

        /// <summary>
        /// Maximum number of attempts
        /// </summary>
        public int MaxRetryAttemps { get; private set; }

        /// <summary>
        /// How many milliseconds to wait between attempts
        /// </summary>
        public int Delay { get; private set; }

        /// <inheritdoc/>
        public override async Task<T> ExecuteAsync<T>(Func<Task<T>> func, Action<Exception> beforeNextAttempt = null, IStatisticsCounter statisticsCounter = null)
        {
            T result = default;

            for (int i = 0; i < MaxRetryAttemps; i++)
            {
                try
                {
                    if (_concurrentThrottler != null)
                    {
                        await _concurrentThrottler.ReserveAsync().ConfigureAwait(false);
                    }

                    TraceLogger.Verbose($"Invoking {func.Method.Name} method... Current attempt: {i}");
                    result = await base.ExecuteAsync(func, beforeNextAttempt, statisticsCounter).ConfigureAwait(false);
                    break;
                }
                catch (Exception exp) when (exp is TaskCanceledException || exp is HttpRequestException)
                {
                    if (i < MaxRetryAttemps - 1)
                    {
                        TraceLogger.Error($"Error while invoking '{func.Method.Name}' method. Current attempt: {i}. Waiting {Delay} milliseconds and retrying it.\n{exp}");

                        await Task.Delay(Delay).ConfigureAwait(false);

                        beforeNextAttempt?.Invoke(exp);
                    }
                    else
                    {
                        TraceLogger.Error($"Error while invoking '{func.Method.Name}' method. Current attempt: {i}.\n{exp}");
                        throw;
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error($"Unexpected exception: {exp}");
                    throw;
                }
                finally
                {
                    if (_concurrentThrottler != null)
                    {
                        _concurrentThrottler.Release();
                    }
                };
            }

            return result;
        }
    }
}
