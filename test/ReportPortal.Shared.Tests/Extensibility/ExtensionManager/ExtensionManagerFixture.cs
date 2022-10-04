using FluentAssertions;
using System;
using Xunit;

namespace ReportPortal.Shared.Tests.Extensibility.ExtensionManager
{
    public class ExtensionManagerFixture
    {
        [Fact]
        public void ShouldExploreExtensions()
        {
            var manager = new Shared.Extensibility.ExtensionManager();
            manager.Explore(Environment.CurrentDirectory);

            manager.ReportEventObservers.Count.Should().Be(1, "google analytic event observer");
        }
    }
}
