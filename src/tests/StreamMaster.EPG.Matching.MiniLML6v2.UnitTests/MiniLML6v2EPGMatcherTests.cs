using Moq;
using Shouldly;
using StreamMaster.Domain.Models;
using StreamMaster.Domain.XmltvXml;
using StreamMaster.EPG.Matching.MiniLML6v2;
using StreamMaster.Streams.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace StreamMaster.EPG.UnitTests.Matching
{
    public class MiniLML6v2EPGMatcherTests
    {
        private readonly Mock<ICacheManager> _mockCacheManager;
        private readonly MiniLML6v2EPGMatcher _matcher;

        public MiniLML6v2EPGMatcherTests()
        {
            _mockCacheManager = new Mock<ICacheManager>();
            SetupCacheManager();
            _matcher = new MiniLML6v2EPGMatcher(_mockCacheManager.Object);
        }

        private void SetupCacheManager()
        {
            // Use the same setup as provided before
            var stationChannelNames = new Dictionary<int, List<StationChannelName>>
            {
                {
                    1, new List<StationChannelName>
                    {
                        new StationChannelName(channel: "bbc-one", displayName: "BBC One", channelName: "British Broadcasting Corporation", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "bbc-two", displayName: "BBC 2", channelName: "British Broadcasting Corporation", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "cnn", displayName: "CNN", channelName: "Cable News Network", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "fox", displayName: "FOX", channelName: "Fox Broadcasting Company", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "itv", displayName: "ITV", channelName: "ITV Hub", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "channel4", displayName: "Channel 4", channelName: "Channel Four", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "channel4-plus", displayName: "Channel Four +1", channelName: "Channel Four +1", logo: "", epgNumber: 1),
                        new StationChannelName(channel: "channel5", displayName: "Channel 5", channelName: "Channel 5", logo: "", epgNumber: 1),
                    }
                },
                {
                    2, new List<StationChannelName>
                    {
                        new StationChannelName(channel: "ard", displayName: "Das Erste", channelName: "ARD", logo: "", epgNumber: 2),
                        new StationChannelName(channel: "zdf", displayName: "ZDF", channelName: "Zweites Deutsches Fernsehen", logo: "", epgNumber: 2),
                    }
                },
                {
                    3, new List<StationChannelName>
                    {
                        new StationChannelName(channel: "tf1", displayName: "TF1", channelName: "Télévision Française 1", logo: "", epgNumber: 3),
                        new StationChannelName(channel: "france2", displayName: "France 2", channelName: "France Télévisions", logo: "", epgNumber: 3),
                    }
                }
            };
            _mockCacheManager.Setup(x => x.StationChannelNames)
                             .Returns(new ConcurrentDictionary<int, List<StationChannelName>>(stationChannelNames));
        }

        [Fact]
        public async Task MatchAsync_ExactMatch_ReturnsCorrectChannel()
        {
            // Arrange
            var channel = new SMChannel
            {
                EPGId = "1#cnn",
                TVGName = "CNN",
                Name = "CNN"
            };
            // Act
            var result = await _matcher.MatchAsync(channel, CancellationToken.None);
            // Assert
            result.ShouldNotBeNull();
            result.Channel.ShouldBe("cnn");
        }

        [Fact]
        public async Task MatchAsync_NoExactMatch_UsesSimilarityMatching()
        {
            // Arrange
            var channel = new SMChannel
            {
                EPGId = "1#unknown",
                TVGName = "Cable News",
                Name = "Cable News"
            };
            // Act
            var result = await _matcher.MatchAsync(channel, CancellationToken.None);
            // Assert
            result.ShouldNotBeNull();
            result.Channel.ShouldBe("cnn");
        }

        [Fact]
        public async Task MatchAsync_InvalidEPGId_SearchesAllChannels()
        {
            // Arrange
            var channel = new SMChannel
            {
                EPGId = "invalid",
                TVGName = "Zweites Deutsches Fernsehen",
                Name = "ZDF Germany"
            };
            // Act
            var result = await _matcher.MatchAsync(channel, CancellationToken.None);
            // Assert
            result.ShouldNotBeNull();
            result.Channel.ShouldBe("zdf");
        }

        [Fact]
        public async Task MatchAsync_EmptyTVGName_UsesNameInstead()
        {
            // Arrange
            var channel = new SMChannel
            {
                EPGId = "1#unknown",
                TVGName = "",
                Name = "British Broadcasting"
            };
            // Act
            var result = await _matcher.MatchAsync(channel, CancellationToken.None);
            // Assert
            result.ShouldNotBeNull();
            result.Channel.ShouldBe("bbc-one");
        }

        [Fact]
        public async Task MatchAsync_LowSimilarity_ReturnsNull()
        {
            // Arrange
            var channel = new SMChannel
            {
                EPGId = "1#unknown",
                TVGName = "Something Completely Different",
                Name = "No Match At All"
            };
            // Act
            var result = await _matcher.MatchAsync(channel, CancellationToken.None);
            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task MatchAsync_CancellationRequested_ThrowsCancellationException()
        {
            // Arrange
            var channel = new SMChannel
            {
                EPGId = "1#cnn",
                TVGName = "CNN",
                Name = "CNN"
            };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            // Act & Assert
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await _matcher.MatchAsync(channel, cts.Token));
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            // Arrange
            // Need a separate instance to avoid interfering with other tests via shared matcher
            var tempMatcher = new MiniLML6v2EPGMatcher(_mockCacheManager.Object);

            // Act
            tempMatcher.Dispose();

            // Assert - no exception means success
            Should.NotThrow(() => tempMatcher.Dispose());
        }

        [Theory]
        [MemberData(nameof(GetMatchingTestCases))]
        public async Task MatchAsync_TheoryTests_MatchesCorrectly(SMChannel channel, string expectedChannel)
        {
            // Act
            var result = await _matcher.MatchAsync(channel, CancellationToken.None);

            // Assert
            if (expectedChannel == null)
            {
                // Add tolerance for slight variations or potential false positives if score > 0.6
                // result?.Channel.ShouldBeNullOrEmpty($"Expected null but got {result?.Channel} for input {channel.EPGId}/{channel.TVGName}/{channel.Name}");
                result.ShouldBeNull($"Expected null but got {result?.Channel} for input {channel.EPGId}/{channel.TVGName}/{channel.Name}");
            }
            else
            {
                result.ShouldNotBeNull($"Expected {expectedChannel} but got null for input {channel.EPGId}/{channel.TVGName}/{channel.Name}");
                result.Channel.ShouldBe(expectedChannel, $"Input: {channel.EPGId}/{channel.TVGName}/{channel.Name}");
            }
        }

        public static IEnumerable<object[]> GetMatchingTestCases()
        {
            string testCasesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "MatchingTestCases.json");

            if (!File.Exists(testCasesPath))
            {
                string? assemblyLocation = Path.GetDirectoryName(typeof(MiniLML6v2EPGMatcherTests).Assembly.Location);
                if (assemblyLocation != null)
                {
                    testCasesPath = Path.Combine(assemblyLocation, "TestData", "MatchingTestCases.json");
                }
            }

            List<MatchingTestCase>? testCases = null;

            if (File.Exists(testCasesPath))
            {
                Console.WriteLine($"Loading test cases from: {testCasesPath}");
                try
                {
                    var testCasesJson = File.ReadAllText(testCasesPath);
                    testCases = JsonSerializer.Deserialize<List<MatchingTestCase>>(
                        testCasesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading/parsing test cases from {testCasesPath}: {ex}");
                    throw;
                }

                if (testCases != null)
                {
                    foreach (var testCase in testCases)
                    {
                        yield return new object[]
                        {
                            new SMChannel
                            {
                                EPGId = testCase.EPGId,
                                TVGName = testCase.TVGName,
                                Name = testCase.Name
                            },
                            testCase.ExpectedChannel
                        };
                    }
                }
                else
                {
                    Console.WriteLine("Warning: Deserialized test cases is null (JSON might be empty or invalid format).");
                }
            }
        }

        public class MatchingTestCase
        {
            public string EPGId { get; set; } = ""; // Initialize to avoid nulls
            public string TVGName { get; set; } = ""; // Initialize to avoid nulls
            public string Name { get; set; } = ""; // Initialize to avoid nulls
            public string? ExpectedChannel { get; set; } // Allow null for expected value
        }
    }
}