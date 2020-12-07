namespace ReportPortal.Shared.Reporter.Statistics
{
    public interface IStatisticsCounter
    {
        long Min { get; }

        long Max { get; }

        long Avg { get; }

        void Count(long duration);
    }
}
