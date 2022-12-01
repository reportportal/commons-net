using System;

namespace ReportPortal.Shared.Internal.Delegating.Exceptions
{
    /// <summary>
    /// Represents exception which throws by retriable executor.
    /// </summary>
    public class RetryHttpRequestException : Exception
    {
        /// <summary>
        /// Initializes new instance of <see cref="RetryHttpRequestException"/>.
        /// </summary>
        /// <param name="attemptNumber">Request attempt number.</param>
        /// <param name="duration">Request duration.</param>
        /// <param name="innerException">Inner exeception.</param>
        public RetryHttpRequestException(int attemptNumber, TimeSpan? duration, Exception innerException)
            : base($"{innerException.Message}. Attempt number = {attemptNumber}. Duration = {duration}", innerException)
        {
            AttemptNumber = attemptNumber;
            Duration = duration;

            _message = $"{innerException.Message}\n Attempt number: {attemptNumber}";

            if (duration.HasValue)
            {
                _message += $"\nDuration: {duration}";
            }
        }

        private readonly string _message;

        /// <summary>
        /// Request attempt number.
        /// </summary>
        public int AttemptNumber { get; }

        /// <summary>
        /// Request duration.
        /// </summary>
        public TimeSpan? Duration { get; }

        /// <inheritdoc/>
        public override string Message => _message;
    }
}
