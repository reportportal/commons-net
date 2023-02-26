using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Converters;
using System;
using System.Collections.Generic;

namespace ReportPortal.Shared.Reporter
{
    internal class RequestPreprocessor
    {
        internal const int MAX_LAUNCH_NAME_LENGTH = 256;
        internal const int MAX_TEST_ITEM_NAME_LENGTH = 1024;

        internal const int MAX_ATTRIBUTE_KEY_LENGTH = 128;
        internal const int MAX_ATTRIBUTE_VALUE_LENGTH = 128;

        public static void Preprocess(StartLaunchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.Name = StringTrimmer.Trim(request.Name, MAX_LAUNCH_NAME_LENGTH);

            Preprocess(request.Attributes);
        }

        public static void Preprocess(StartTestItemRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.Name = StringTrimmer.Trim(request.Name, MAX_TEST_ITEM_NAME_LENGTH);

            Preprocess(request.Attributes);
        }

        public static void Preprocess(FinishTestItemRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Preprocess(request.Attributes);
        }

        private static void Preprocess(IEnumerable<ItemAttribute> attributes)
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
