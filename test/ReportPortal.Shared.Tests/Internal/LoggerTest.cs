using ReportPortal.Shared.Internal.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ReportPortal.Shared.Tests.Internal
{
    public class LoggerTest : IDisposable
    {
        public LoggerTest()
        {
            Environment.SetEnvironmentVariable("ReportPortal_TraceLevel", "Verbose");
        }

        [Fact]
        public void ConcurrentWriters()
        {
            var logger = TraceLogManager.GetLogger(typeof(LoggerTest));

            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                var eventId = i;
                tasks.Add(Task.Factory.StartNew(() => logger.Info($"my message #{eventId}")));
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ReportPortal_TraceLevel", "");
        }
    }
}
