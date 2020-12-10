namespace ReportPortal.Shared.Reporter.Statistics
{
    /// <inheritdoc/>
    public class LaunchStatisticsCounter : ILaunchStatisticsCounter
    {
        /// <inheritdoc/>
        public IStatisticsCounter StartTestItemStatisticsCounter { get; } = new StatisticsCounter();

        /// <inheritdoc/>
        public IStatisticsCounter FinishTestItemStatisticsCounter { get; } = new StatisticsCounter();
    }
}
