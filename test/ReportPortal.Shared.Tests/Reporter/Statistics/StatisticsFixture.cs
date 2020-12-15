using FluentAssertions;
using ReportPortal.Shared.Reporter.Statistics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ReportPortal.Shared.Tests.Reporter.Statistics
{
    public class StatisticsFixture
    {
        [Fact]
        public void ShouldHaveDefaultValues()
        {
            var counter = new StatisticsCounter();

            counter.Min.Should().Be(0);
            counter.Max.Should().Be(0);
            counter.Avg.Should().Be(0);
            counter.Count.Should().Be(0);
        }

        [Fact]
        public void ShouldCountRequests()
        {
            var counter = new StatisticsCounter();

            counter.Measure(0);
            counter.Measure(0);

            counter.Count.Should().Be(2);
        }

        [Fact]
        public void ShouldCountMinValue()
        {
            var counter = new StatisticsCounter();

            counter.Measure(1);
            counter.Measure(2);

            counter.Min.Should().Be(1);
        }

        [Fact]
        public void ShouldCountMaxValue()
        {
            var counter = new StatisticsCounter();

            counter.Measure(1);
            counter.Measure(2);

            counter.Max.Should().Be(2);
        }

        [Fact]
        public void ShouldCountAverageValue()
        {
            var counter = new StatisticsCounter();

            counter.Measure(1);
            counter.Measure(2);
            counter.Measure(3);

            counter.Avg.Should().Be(2);
        }

        [Fact]
        public void ShouldCountAverageFloatingValue()
        {
            var counter = new StatisticsCounter();

            counter.Measure(1);
            counter.Measure(2);

            counter.Avg.Should().Be(1.5);
        }

        [Fact]
        public void ShouldCountParallelRequests()
        {
            var counter = new StatisticsCounter();

            var values = Enumerable.Range(1, 1000);

            Parallel.ForEach(values, (v) => counter.Measure(v));

            counter.Avg.Should().Be(500.5);
            counter.Min.Should().Be(1);
            counter.Max.Should().Be(1000);
        }

        [Fact]
        public void ShouldHaveStringRepresentation()
        {
            var counter = new StatisticsCounter();
            counter.Measure(1.111);
            counter.Measure(2.222);
            counter.Measure(3.333);

            counter.ToString().Should().Be("Cnt 3 Avg/Min/Max 2.22/1.11/3.33s");
        }
    }
}
