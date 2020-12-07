namespace ReportPortal.Shared.Reporter.Statistics
{
    public interface ILaunchStatisticsCounter
    {
        IStatisticsCounter StartTestItemStatisticsCounter { get; }

        IStatisticsCounter FinishTestItemStatisticsCounter { get; }
    }
}
