using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ReportPortal.Shared.Internal.Delegating.Exceptions
{
    /// <summary>
    /// Occurs when retry request execution is unsuccessful.
    /// </summary>
    public class RetryExecutionException : AggregateException
    {
        private readonly string _message;

        /// <summary>
        /// Initializes a new instance of <see cref="RetryExecutionException"/>
        /// </summary>
        /// <param name="innerExceptions">Inner exceptions.</param>
        public RetryExecutionException(IEnumerable<Exception> innerExceptions)
            : this("Request has not finished successfully", innerExceptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RetryExecutionException"/>
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerExceptions">Inner exceptions.</param>
        public RetryExecutionException(string message, IEnumerable<Exception> innerExceptions)
            : base(message: null, innerExceptions)
        {
            _message = message;
        }

        /// <inheritdoc/>
        public override string Message => _message;

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine($"{GetType().Name}: {Message}");
            text.Append(StackTrace);

            for (int index = 0; index < InnerExceptions.Count; index++)
            {
                text.Append(Environment.NewLine + " ---> ");
                text.AppendFormat(CultureInfo.InvariantCulture, "(Inner Exception #{0}) ", index);
                text.Append(InnerExceptions[index].ToString());

                text.Append(" <--- ");
                text.AppendLine();
            }

            
            return text.ToString();
        }
    }
}
