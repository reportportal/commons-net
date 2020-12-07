using System;

namespace ReportPortal.Shared.Reporter.Statistics
{
    public class StatisticsCounter : IStatisticsCounter
    {
        public long Min => throw new NotImplementedException();

        public long Max => throw new NotImplementedException();

        public long Avg => throw new NotImplementedException();

        public void Count(long duration)
        {
            throw new NotImplementedException();
        }
    }
}
