using System;

namespace ReportPortal.Shared.Reporter
{
    public interface IReporterInfo
    {
        string Uuid { get; }

        string Name { get; }

        DateTime StartTime { get; internal set; }

        DateTime? FinishTime { get; internal set; }
    }
}
