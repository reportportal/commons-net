﻿using Moq;
using ReportPortal.Shared.Reporter;
using ReportPortal.Shared.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ReportPortal.Shared.Tests.Faked
{
    public class BigTreeInvocation
    {
        [Theory]
        [InlineData(1, 1, 0)]
        [InlineData(1, 1, 1)]
        [InlineData(5, 10000, 0)]
        [InlineData(5, 10000, 10)]
        public void SuccessReporting(int suitesPerLaunch, int testsPerSuite, int logsPerTest)
        {
            var fakeService = new FakeService(new Uri("https://rp.epam.com/api/v1/"), "ci-agents-checks", "b79e81a5-8448-49b5-857d-945ff5fd5ed2");

            var launchScheduler = new LaunchScheduler(fakeService);
            var launchReporter = launchScheduler.Build(suitesPerLaunch, testsPerSuite, logsPerTest);

            launchReporter.FinishTask.Wait();

            Assert.Equal(suitesPerLaunch * testsPerSuite + suitesPerLaunch, fakeService.StartTestItemCounter);

            Assert.Equal(suitesPerLaunch * testsPerSuite * logsPerTest, fakeService.AddLogItemCounter);
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(10, 10, 10)]
        public void FailedLogsShouldNotAffectFinishingLaunch(int suitesPerLaunch, int testsPerSuite, int logsPerTest)
        {
            var fakeService = new FakeServiceWithFailedAddLogItemMethod(new Uri("https://rp.epam.com/api/v1/"), "ci-agents-checks", "b79e81a5-8448-49b5-857d-945ff5fd5ed2");

            var launchScheduler = new LaunchScheduler(fakeService);
            var launchReporter = launchScheduler.Build(suitesPerLaunch, testsPerSuite, logsPerTest);

            launchReporter.FinishTask.Wait();

            Assert.Equal(suitesPerLaunch * testsPerSuite + suitesPerLaunch, fakeService.StartTestItemCounter);

            Assert.Equal(suitesPerLaunch * testsPerSuite + suitesPerLaunch, fakeService.FinishTestItemCounter);
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 1, 2)]
        public void FailedFirstFinishTestItemShouldRaiseExceptionAtFinishLaunch(int suitesPerLaunch, int testsPerSuite, int logsPerTest)
        {
            var fakeService = new FakeServiceWithFailedFirstFinishTestItemMethod(new Uri("https://rp.epam.com/api/v1/"), "ci-agents-checks", "b79e81a5-8448-49b5-857d-945ff5fd5ed2");

            var launchScheduler = new LaunchScheduler(fakeService);
            var launchReporter = launchScheduler.Build(suitesPerLaunch, testsPerSuite, logsPerTest);

            var exp = Assert.ThrowsAny<Exception>(() => launchReporter.FinishTask.Wait());

            Assert.Equal(suitesPerLaunch * testsPerSuite + suitesPerLaunch, fakeService.StartTestItemCounter);

            Assert.Equal(suitesPerLaunch * testsPerSuite - 1, fakeService.FinishTestItemCounter);
        }

        [Theory]
        [InlineData(100, 1, 1)]
        [InlineData(1, 100, 10000)]
        [InlineData(100, 100, 1)]
        public void FailedFirstStartSuiteItemShouldRaiseExceptionAtFinishLaunch(int suitesPerLaunch, int testsPerSuite, int logsPerTest)
        {
            var fakeService = new FakeServiceWithFailedFirstStartTestItemMethod(new Uri("https://rp.epam.com/api/v1/"), "ci-agents-checks", "b79e81a5-8448-49b5-857d-945ff5fd5ed2");

            var launchScheduler = new LaunchScheduler(fakeService);
            var launchReporter = launchScheduler.Build(suitesPerLaunch, testsPerSuite, logsPerTest);

            var exp = Assert.ThrowsAny<Exception>(() => launchReporter.FinishTask.Wait());

            Assert.Equal((suitesPerLaunch - 1) * testsPerSuite + (suitesPerLaunch - 1), fakeService.StartTestItemCounter);
        }

        [Fact]
        public void StartLaunchScheduling()
        {
            var service = new Mock<Client.Service>(new Uri("http://abc.com"), It.IsAny<string>(), It.IsAny<string>());
            service.Setup(s => s.StartLaunchAsync(It.IsAny<Client.Requests.StartLaunchRequest>())).Returns(Task.FromResult(new Client.Models.Launch()));

            var launchReporters = new List<Mock<LaunchReporter>>();

            for (int i = 0; i < 1000; i++)
            {
                var launchReporter = new Mock<LaunchReporter>(service.Object);

                launchReporter.Object.Start(new Client.Requests.StartLaunchRequest
                {
                    Name = $"ReportPortal Shared {i}",
                    StartTime = DateTime.UtcNow
                });

                launchReporters.Add(launchReporter);
            }

            for (int i = 0; i < 1000; i++)
            {
                var launchReporter = launchReporters[i];

                Assert.NotNull(launchReporter.Object.StartTask);

                launchReporter.Object.Sync();

                Assert.Equal($"ReportPortal Shared {i}", launchReporter.Object.LaunchInfo.Name);
            }

            service.Verify(s => s.StartLaunchAsync(It.IsAny<Client.Requests.StartLaunchRequest>()), Times.Exactly(1000));
        }
    }
}
