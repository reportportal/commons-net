﻿using FluentAssertions;
using Moq;
using ReportPortal.Client;
using ReportPortal.Shared.Internal.Delegating;
using ReportPortal.Shared.Internal.Delegating.Exceptions;
using ReportPortal.Shared.Reporter.Statistics;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ReportPortal.Shared.Tests.Internal.Delegating
{
    public class ExponentialRetryRequestExecuterTest
    {
        [Fact]
        public void MaxAttemptsShouldBeGreaterOrEqualOne()
        {
            Action ctor = () => new ExponentialRetryRequestExecuter(maxRetryAttempts: 0, baseIndex: 0, throttler: null, httpStatusCodes: null);
            ctor.Should().Throw<ArgumentException>();
        }

        [Fact]
        public async Task ExecuteValidActionOneTime()
        {
            var action = new Mock<Func<Task<string>>>();

            var executer = new ExponentialRetryRequestExecuter(3, 2);
            var res = await executer.ExecuteAsync(action.Object);
            res.Should().Be(null);
            action.Verify(a => a(), Times.Once);
        }

        [Fact]
        public async Task ShouldRetryTaskCanceledExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<TaskCanceledException>();

            var executer = new ExponentialRetryRequestExecuter(3, 0);
            await executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().ThrowAsync<RetryExecutionException>();

            action.Verify(a => a(), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldRetryHttpRequestExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<HttpRequestException>();

            var executer = new ExponentialRetryRequestExecuter(3, 0);
            await executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().ThrowAsync<HttpRequestException>();

            action.Verify(a => a(), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldRetryServiceExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws(() => new ServiceException("", HttpStatusCode.BadGateway, new Uri("https://example.com"), HttpMethod.Post, ""));

            var executer = new ExponentialRetryRequestExecuter(3, 0, null, new HttpStatusCode[] { HttpStatusCode.BadGateway });
            await executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().ThrowAsync<RetryExecutionException>();

            action.Verify(a => a(), Times.Exactly(3));
        }

        [Fact]
        public async Task ShouldKeepAllHandledExceptionsAsInnerExceptions()
        {
            var action = new Mock<Func<Task<string>>>();

            action.Setup(a => a()).Throws(() => new ServiceException("", HttpStatusCode.Conflict, new Uri("https://example.com"), HttpMethod.Post, ""));

            var executer = new ExponentialRetryRequestExecuter(5, 0, null, new HttpStatusCode[] { HttpStatusCode.Conflict });
            await executer.Awaiting(e => e.ExecuteAsync(action.Object))
                .Should()
                .ThrowAsync<RetryExecutionException>()
                .Where(ex => ex.InnerExceptions.Count == 5);

            action.Verify(a => a(), Times.Exactly(5));
        }

        [Fact]
        public async Task ShouldNotRetryAnyOtherExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<Exception>();

            var executer = new ExponentialRetryRequestExecuter(3, 0);
            await executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().ThrowAsync<Exception>();

            action.Verify(a => a(), Times.Exactly(1));
        }

        [Fact]
        public async Task ShouldUseThrottler()
        {
            var throttler = new Mock<IRequestExecutionThrottler>();

            var executer = new ExponentialRetryRequestExecuter(5, 0, throttler.Object, null);

            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<TaskCanceledException>();

            await executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().ThrowAsync<Exception>();

            throttler.Verify(t => t.ReserveAsync(), Times.Exactly(5));
            throttler.Verify(t => t.Release(), Times.Exactly(5));
        }

        [Fact]
        public async Task ShouldInvokeCallbackAction()
        {
            var executer = new ExponentialRetryRequestExecuter(5, 0);

            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<TaskCanceledException>();

            var invokedTimes = 0;

            await executer.Awaiting(e => e.ExecuteAsync(action.Object, (exp) => invokedTimes++)).Should().ThrowAsync<RetryExecutionException>();

            invokedTimes.Should().Be(4);
        }

        [Fact]
        public async Task ShouldMeasureRequestsStatistics()
        {
            var counter = new Mock<IStatisticsCounter>();

            var executer = new ExponentialRetryRequestExecuter(1, 0);

            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).ReturnsAsync("a");

            await executer.ExecuteAsync(action.Object, null, counter.Object);

            counter.Verify(c => c.Measure(It.IsAny<TimeSpan>()), Times.Once);
        }
    }
}
