using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReportPortal.Shared.Internal
{
    public class TraceLogManager
    {
        public static ITraceLogger GetLogger(Type type)
        {
            var traceSource = new TraceSource(type.Name);

            var envTraceLevel = Environment.GetEnvironmentVariable("ReportPortal_TraceLevel");

            traceSource.Switch = new SourceSwitch("ReportPortal_TraceSwitch", envTraceLevel);

            var logFileName = $"{type.Assembly.GetName().Name}.{Process.GetCurrentProcess().Id}.log";

            var traceListener = new DefaultTraceListener
            {
                Filter = new SourceFilter(traceSource.Name),
                LogFileName = logFileName
            };

            traceSource.Listeners.Add(traceListener);

            return new TraceLogger(traceSource);
        }
    }
}
