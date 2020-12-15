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
        double Min { get; }

        /// <summary>
        /// Maximum duration of measured requests.
        /// </summary>
        double Max { get; }

        /// <summary>
        /// Average duration of measured requests.
        /// </summary>
        double Avg { get; }

        /// <summary>
        /// Total count of measured requests.
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Measure of request's duration in sequence.
        /// </summary>
        /// <param name="duration">Duration in seconds.</param>
        void Measure(double duration);
    }
}
