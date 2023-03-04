using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Shared.Converters;
using ReportPortal.Shared.Extensibility.ReportEvents;
using ReportPortal.Shared.Extensibility.ReportEvents.EventArgs;
using ReportPortal.Shared.Reporter;
using System.Collections.Generic;

namespace ReportPortal.Shared.Extensibility.Embedded.Normalization
{
    /// <summary>
    /// Request normalizer.
    /// </summary>
    public class RequestNormalizer : IReportEventsObserver
    {
        internal const int MAX_LAUNCH_NAME_LENGTH = 256;
        internal const int MAX_TEST_ITEM_NAME_LENGTH = 1024;

        internal const int MAX_ATTRIBUTE_KEY_LENGTH = 128;
        internal const int MAX_ATTRIBUTE_VALUE_LENGTH = 128;

        /// <inheritdoc/>
        public void Initialize(IReportEventsSource reportEventsSource)
        {
            reportEventsSource.OnBeforeLaunchStarting += ReportEventsSource_OnBeforeLaunchStarting;
            reportEventsSource.OnBeforeTestStarting += ReportEventsSource_OnBeforeTestStarting;
            reportEventsSource.OnBeforeTestFinishing += ReportEventsSource_OnBeforeTestFinishing;
            reportEventsSource.OnBeforeLaunchFinishing += ReportEventsSource_OnBeforeLaunchFinishing;
        }

        private void ReportEventsSource_OnBeforeLaunchFinishing(ILaunchReporter launchReporter, BeforeLaunchFinishingEventArgs args)
        {
            if (args.FinishLaunchRequest.EndTime < launchReporter.Info.StartTime)
            {
                args.FinishLaunchRequest.EndTime = launchReporter.Info.StartTime;
                launchReporter.Info.FinishTime = args.FinishLaunchRequest.EndTime;
            }
        }

        private void ReportEventsSource_OnBeforeTestFinishing(ITestReporter testReporter, BeforeTestFinishingEventArgs args)
        {
            if (args.FinishTestItemRequest.EndTime < testReporter.Info.StartTime)
            {
                args.FinishTestItemRequest.EndTime = testReporter.Info.StartTime;
            }

            NormilizeAttributes(args.FinishTestItemRequest.Attributes);
        }

        private void ReportEventsSource_OnBeforeTestStarting(ITestReporter testReporter, BeforeTestStartingEventArgs args)
        {
            if (args.StartTestItemRequest.StartTime < testReporter.LaunchReporter.Info.StartTime)
            {
                args.StartTestItemRequest.StartTime = testReporter.LaunchReporter.Info.StartTime;
            }

            args.StartTestItemRequest.Name = StringTrimmer.Trim(args.StartTestItemRequest.Name, MAX_TEST_ITEM_NAME_LENGTH);
            NormilizeAttributes(args.StartTestItemRequest.Attributes);
        }

        private void ReportEventsSource_OnBeforeLaunchStarting(ILaunchReporter launchReporter, BeforeLaunchStartingEventArgs args)
        {
            args.StartLaunchRequest.Name = StringTrimmer.Trim(args.StartLaunchRequest.Name, MAX_LAUNCH_NAME_LENGTH);
            NormilizeAttributes(args.StartLaunchRequest.Attributes);
        }

        private static void NormilizeAttributes(IEnumerable<ItemAttribute> attributes)
        {
            if (attributes == null)
            {
                return;
            }

            foreach (var attribute in attributes)
            {
                attribute.Key = StringTrimmer.Trim(attribute.Key, MAX_ATTRIBUTE_KEY_LENGTH);
                attribute.Value = StringTrimmer.Trim(attribute.Value, MAX_ATTRIBUTE_VALUE_LENGTH);

            }
        }
    }
}
