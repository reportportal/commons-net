using FluentAssertions;
using Moq;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Extensibility.Embedded.Normalization;
using ReportPortal.Shared.Reporter;
using ReportPortal.Shared.Tests.Helpers;
using System;
using Xunit;

namespace ReportPortal.Shared.Tests.Extensibility.Embedded.Normalization
{
    public class RequestNormalizerTest
    {
        [Fact]
        public void ShouldTrimLaunchNameDuringStarting()
        {
            var reporter = new LaunchReporter(
                new MockServiceBuilder().Build().Object, null, null, new Mock<IExtensionManager>().Object);

            var request = new StartLaunchRequest { Name = new string('a', 257) };

            reporter.Start(request);
            request.Name.Should().HaveLength(256);
        }

        [Fact]
        public void ShouldTrimLaunchAttributesDuringStarting()
        {
            var reporter = new LaunchReporter(
                new MockServiceBuilder().Build().Object, null, null, new Mock<IExtensionManager>().Object);

            var request = new StartLaunchRequest
            {
                Attributes = new[]
                {
                    new ItemAttribute { Key = new string('a', 129), Value = new string('b', 129) },
                    new ItemAttribute { Key = new string('a', 256), Value = new string('b', 256) },
                }
            };

            reporter.Start(request);

            request.Attributes.Should().AllSatisfy(attribute =>
            {
                attribute.Key.Should().HaveLength(RequestNormalizer.MAX_ATTRIBUTE_KEY_LENGTH);
                attribute.Value.Should().HaveLength(RequestNormalizer.MAX_ATTRIBUTE_VALUE_LENGTH);
            });
        }

        [Fact]
        public void ShouldTrimTestItemNameDuringStarting()
        {
            var service = new MockServiceBuilder().Build().Object;
            var extensionManager = new Mock<IExtensionManager>().Object;

            var launchReporter = new LaunchReporter(service, null, null, extensionManager);
            var testReporter = new TestReporter(service, null, launchReporter, null, null, extensionManager, new Mock<ReportEventsSource>().Object);

            var request = new StartTestItemRequest { Name = new string('a', 1025) };

            launchReporter.Start(new StartLaunchRequest());
            testReporter.Start(request);

            request.Name.Should().HaveLength(RequestNormalizer.MAX_TEST_ITEM_NAME_LENGTH);
        }

        [Fact]
        public void ShouldTrimTestItemAttributesDuringStarting()
        {
            var service = new MockServiceBuilder().Build().Object;
            var extensionManager = new Mock<IExtensionManager>().Object;

            var launchReporter = new LaunchReporter(service, null, null, extensionManager);
            var testReporter = new TestReporter(service, null, launchReporter, null, null, extensionManager, null);

            var request = new StartTestItemRequest
            {
                Attributes = new[]
                {
                    new ItemAttribute { Key = new string('a', 129), Value = new string('b', 129) },
                    new ItemAttribute { Key = new string('a', 256), Value = new string('b', 256) },
                }
            };

            launchReporter.Start(new StartLaunchRequest());
            testReporter.Start(request);

            request.Attributes.Should().AllSatisfy(attribute =>
            {
                attribute.Key.Should().HaveLength(RequestNormalizer.MAX_ATTRIBUTE_KEY_LENGTH);
                attribute.Value.Should().HaveLength(RequestNormalizer.MAX_ATTRIBUTE_VALUE_LENGTH);
            });
        }

        [Fact]
        public void ShouldTrimTestItemAttributesDuringFinishing()
        {
            var service = new MockServiceBuilder().Build().Object;
            var extensionManager = new Mock<IExtensionManager>().Object;

            var launchReporter = new LaunchReporter(service, null, null, extensionManager);
            var testReporter = new TestReporter(service, null, launchReporter, null, null, extensionManager, null);

            var request = new FinishTestItemRequest
            {
                Attributes = new[]
                {
                    new ItemAttribute { Key = new string('a', 129), Value = new string('b', 129) },
                    new ItemAttribute { Key = new string('a', 256), Value = new string('b', 256) },
                }
            };

            launchReporter.Start(new StartLaunchRequest());

            testReporter.Start(new StartTestItemRequest());
            testReporter.Finish(request);

            request.Attributes.Should().AllSatisfy(attribute =>
            {
                attribute.Key.Should().HaveLength(RequestNormalizer.MAX_ATTRIBUTE_KEY_LENGTH);
                attribute.Value.Should().HaveLength(RequestNormalizer.MAX_ATTRIBUTE_VALUE_LENGTH);
            });
        }

        [Fact]
        public void LaunchShouldCareOfFinishTime()
        {
            var launchStartTime = DateTime.UtcNow;

            var service = new MockServiceBuilder().Build();

            var extensionManager = new Shared.Extensibility.ExtensionManager();
            extensionManager.ReportEventObservers.Add(new RequestNormalizer());

            var launch = new LaunchReporter(service.Object, null, null, extensionManager);
            launch.Start(new StartLaunchRequest() { StartTime = launchStartTime });
            launch.Finish(new FinishLaunchRequest() { EndTime = launchStartTime.AddDays(-1) });
            launch.Sync();

            launch.Info.FinishTime.Should().Be(launch.Info.StartTime);
        }
    }
}
