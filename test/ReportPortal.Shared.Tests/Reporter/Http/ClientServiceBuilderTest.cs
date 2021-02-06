using FluentAssertions;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Reporter.Http;
using System;
using Xunit;

namespace ReportPortal.Shared.Tests.Reporter.Http
{
    public class ClientServiceBuilderTest
    {
        [Fact]
        public void ShouldBuildClientService()
        {
            var configuration = new ConfigurationBuilder().Build();
            configuration.Properties["Server:Url"] = "http://abc.com";
            configuration.Properties["Server:Project"] = "proj1";
            configuration.Properties["Server:Authentication:Uuid"] = "123";

            var builder = new ClientServiceBuilder(configuration);

            var client = builder.Build();

            client.Should().NotBeNull();
        }

        [Fact]
        public void ConfigurationShouldBeMandatory()
        {
            Action ctor = () => new ClientServiceBuilder(null);

            ctor.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}
