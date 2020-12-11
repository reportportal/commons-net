﻿using FluentAssertions;
using Moq;
using ReportPortal.Shared.Internal.Delegating;
using ReportPortal.Shared.Reporter.Statistics;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ReportPortal.Shared.Tests.Internal.Delegating
{
    public class ExponentialRetryRequestExecuterTest
    {
        [Fact]
        public void BaseIndexShouldBeGreaterOrEqualZero()
        {
            Action ctor = () => new ExponentialRetryRequestExecuter(1, baseIndex: -1, throttler: null);
            ctor.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MaxAttemptsShouldBeGreaterOrEqualOne()
        {
            Action ctor = () => new ExponentialRetryRequestExecuter(maxRetryAttempts: 0, baseIndex: 0, throttler: null);
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
        public void ShouldRetryTaskCanceledExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<TaskCanceledException>();

            var executer = new ExponentialRetryRequestExecuter(3, 0);
            executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().Throw<TaskCanceledException>();

            action.Verify(a => a(), Times.Exactly(3));
        }

        [Fact]
        public void ShouldRetryHttpRequestExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<HttpRequestException>();

            var executer = new ExponentialRetryRequestExecuter(3, 0);
            executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().Throw<HttpRequestException>();

            action.Verify(a => a(), Times.Exactly(3));
        }

        [Fact]
        public void ShouldNotRetryAnyOtherExceptionAction()
        {
            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<Exception>();

            var executer = new ExponentialRetryRequestExecuter(3, 0);
            executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().Throw<Exception>();

            action.Verify(a => a(), Times.Exactly(1));
        }

        [Fact]
        public void ShouldUseThrottler()
        {
            var throttler = new Mock<IRequestExecutionThrottler>();

            var executer = new ExponentialRetryRequestExecuter(5, 0, throttler.Object);

            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<TaskCanceledException>();

            executer.Awaiting(e => e.ExecuteAsync(action.Object)).Should().Throw<Exception>();

            throttler.Verify(t => t.ReserveAsync(), Times.Exactly(5));
            throttler.Verify(t => t.Release(), Times.Exactly(5));
        }

        [Fact]
        public void ShouldInvokeCallbackAction()
        {
            var executer = new ExponentialRetryRequestExecuter(5, 0);

            var action = new Mock<Func<Task<string>>>();
            action.Setup(a => a()).Throws<TaskCanceledException>();

            var invokedTimes = 0;

            executer.Awaiting(e => e.ExecuteAsync(action.Object, (exp) => invokedTimes++)).Should().Throw<TaskCanceledException>();

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

            counter.Verify(c => c.Measure(It.IsAny<double>()), Times.Once);
        }
    }
}
