﻿using FluentAssertions;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ReportPortal.Shared.Tests.Internal.Logging
{
    public class LoggerTest : IDisposable
    {
        private readonly ITestOutputHelper _out;

        private readonly string _defaultLogFilePath = $"ReportPortal.Shared.Tests.{Process.GetCurrentProcess().Id}.log";

        public LoggerTest(ITestOutputHelper output)
        {
            _out = output;
            Environment.SetEnvironmentVariable("ReportPortal_TraceLevel", "Information");
        }

        [Fact]
        public void ConcurrentWriters()
        {
            var logManager = new TraceLogManager();

            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                var eventId = i;
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    _out.WriteLine(logManager.GetLogger<LoggerTest>().GetHashCode().ToString());
                    logManager.GetLogger<LoggerTest>().Info($"my message #{eventId}");
                    logManager.GetLogger<LoggerTest>().Error($"my message #{eventId}");
                    logManager.GetLogger<LoggerTest>().Warn($"my message #{eventId}");
                }));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.True(File.Exists(_defaultLogFilePath));
        }

        [Fact]
        public void ShouldNotAffectDefaultTraceListeners()
        {
            Assert.Single(Trace.Listeners);
        }

        [Fact]
        public void ShouldNotCaptureDefaultTrace()
        {
            new TraceLogManager().GetLogger<LoggerTest>().Info("should_see_it");

            Assert.Contains("should_see_it", File.ReadAllText(_defaultLogFilePath));

            Trace.TraceInformation("should_not_see_it");

            Assert.DoesNotContain("should_not_see_it", File.ReadAllText(_defaultLogFilePath));
        }

        [Fact]
        public void ShouldSaveToBaseDir()
        {
            var obj = new { A = "a" };
            var tempDir = Directory.CreateDirectory(Path.GetRandomFileName());
            var logger = new TraceLogManager().WithBaseDir(tempDir.FullName).GetLogger(obj.GetType());
            logger.Info("some message");
            Assert.True(File.Exists($"{tempDir.FullName}/{_defaultLogFilePath}"));

            tempDir.Delete(true);
        }

        [Fact]
        public void ShouldSaveIfBaseDirDoesntExist()
        {
            var obj = new { A = "a" };
            var tempDir = new DirectoryInfo(Path.GetRandomFileName());
            var logger = new TraceLogManager().WithBaseDir(tempDir.FullName).GetLogger(obj.GetType());
            logger.Info("some message");
            Assert.False(File.Exists($"{tempDir.FullName}\\{_defaultLogFilePath}"));
            Assert.True(File.Exists(_defaultLogFilePath));
        }

        [Fact]
        public void ShouldThrowIfTypeIsNull()
        {
            var traceLogManager = new TraceLogManager();

            Action act = () => traceLogManager.GetLogger(null);

            act.Should().Throw<ArgumentNullException>();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("ReportPortal_TraceLevel", null);
        }
    }
}
