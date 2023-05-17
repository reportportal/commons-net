using FluentAssertions;
using System;
using System.IO;
using Xunit;
using static ReportPortal.Shared.Extensibility.Embedded.Analytics.ClientIdProvider;

namespace ReportPortal.Shared.Tests.Extensibility.Embedded.Analytics
{
    public class ClientIdProviderTest
    {
        private const string TEST_PROPERTY = "test.property=555\n";

        [Fact]
        public async void GetClientIdShouldReturnSameIdForTwoCalls()
        {
            string clientId1 = await GetClientIdAsync();
            string clientId2 = await GetClientIdAsync();

            clientId2.Should().BeEquivalentTo(clientId1);
        }

        [Fact]
        public async void GetClientIdShouldReturnDifferentIdsIfStoreFileRemoved()
        {
            string clientId1 = await GetClientIdAsync();
            File.Delete(FILE_PATH);
            string clientId2 = await GetClientIdAsync();

            clientId2.Should().NotBeEquivalentTo(clientId1);
        }

        [Fact]
        public async void GetClientIdShouldReturnUuid()
        {
            string clientId = await GetClientIdAsync();
            Guid.Parse(clientId).Should().NotBeEmpty();
        }

        [Fact]
        public async void GetClientIdShouldSaveIdToPropertyFile()
        {
            File.Delete(FILE_PATH);
            string clientId = await GetClientIdAsync();
            string content;
            using (var reader = new StreamReader(FILE_PATH))
            {
                content = await reader.ReadToEndAsync();
            }
            content.Should().NotBeNull().And.MatchRegex($@"client.id\s*=\s*{clientId}");
        }

        [Fact]
        public async void GetClientIdShouldReadIdFromPropertyFile()
        {
            File.Delete(FILE_PATH);
            string clientId = Guid.NewGuid().ToString();
            using (var writer = new StreamWriter(FILE_PATH))
            {
                await writer.WriteAsync($"client.id={clientId}\n");
            }
            string actualClientId = await GetClientIdAsync();
            actualClientId.Should().BeEquivalentTo(clientId);
        }

        [Fact]
        public async void GetClientIdShouldReadIdFromPropertyFileIfNotEmptyAndIdIsTheFirstLine()
        {
            File.Delete(FILE_PATH);
            string clientId = Guid.NewGuid().ToString();
            using (var writer = new StreamWriter(FILE_PATH))
            {
                await writer.WriteAsync($"client.id={clientId}\n" + TEST_PROPERTY);
            }
            string actualClientId = await GetClientIdAsync();
            actualClientId.Should().BeEquivalentTo(clientId);
        }

        [Fact]
        public async void GetClientIdShouldReadIdFromPropertyFileIfNotEmptyAndIdIsNotTheFirstLine()
        {
            File.Delete(FILE_PATH);
            string clientId = Guid.NewGuid().ToString();
            using (var writer = new StreamWriter(FILE_PATH))
            {
                await writer.WriteAsync(TEST_PROPERTY + $"client.id={clientId}\n");
            }
            string actualClientId = await GetClientIdAsync();
            actualClientId.Should().BeEquivalentTo(clientId);
        }

        [Fact]
        public async void GetClientIdShouldWriteIdToPropertyFileIfNotEmpty()
        {
            File.Delete(FILE_PATH);
            using (var writer = new StreamWriter(FILE_PATH))
            {
                await writer.WriteAsync(TEST_PROPERTY);
            }
            string clientId = await GetClientIdAsync();
            string content;
            using (var reader = new StreamReader(FILE_PATH))
            {
                content = await reader.ReadToEndAsync();
            }
            content.Should().NotBeNull().And.MatchRegex(TEST_PROPERTY + $@"client.id\s*=\s*{clientId}");
        }
    }
}
