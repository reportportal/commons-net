namespace ReportPortal.Shared.Reporter.Statistics
{
    /// <summary>
    /// Measuring of requests duration.
    /// </summary>
    public interface IStatisticsCounter
    {
        /// <summary>
        /// Minimum duration of measured requests.
        /// </summary>
        long Min { get; }

        /// <summary>
        /// Maximum duration of measured requests.
        /// </summary>
        long Max { get; }

        /// <summary>
        /// Average duration of measured requests.
        /// </summary>
        long Avg { get; }

        /// <summary>
        /// Measure of request's duration in sequence.
        /// </summary>
        /// <param name="duration">Duration in milliseconds.</param>
        void Measure(long duration);
    }
}
