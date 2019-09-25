using System;
using System.Collections.Generic;
using System.Text;

namespace ReportPortal.Shared.Internal
{
    public interface ITraceLogger
    {
        void Info(string message);

        void Warn(string message);

        void Error(string message);
    }
}
