using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReportPortal.Shared.Internal.Logging
{
    public static class TraceLogManager
    {
        public static ITraceLogger GetLogger<T>()
        {
            return GetLogger(typeof(T));
        }

        public static ITraceLogger GetLogger(Type type)
        {
            var traceSource = new TraceSource(type.Name);

            var envTraceLevelValue = Environment.GetEnvironmentVariable("ReportPortal_TraceLevel");

            SourceLevels traceLevel;
            if (!Enum.TryParse(envTraceLevelValue, out traceLevel))
            {
                traceLevel = SourceLevels.Error;
            }

            traceSource.Switch = new SourceSwitch("ReportPortal_TraceSwitch", traceLevel.ToString());

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
