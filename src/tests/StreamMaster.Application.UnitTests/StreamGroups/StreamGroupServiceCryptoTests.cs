using Moq;
using Shouldly;
using StreamMaster.Domain.Crypto;
using StreamMaster.Domain.Models;
using System.Collections.Concurrent;

namespace StreamMaster.Application.UnitTests.StreamGroups;

public partial class StreamGroupServiceTests : IDisposable
{
    [Fact]
    public async Task EncodeStreamGroupIdProfileIdAsync_ValidInput_ReturnsEncodedString()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string groupKey = "TestGroupKey123";

        // Create a mock dictionary with our test data
        var mockDictionary = new ConcurrentDictionary<int, string?>();
        mockDictionary.TryAdd(streamGroupId, groupKey);

        // Set up the cache manager to return our mock dictionary
        _cacheManager.Setup(x => x.StreamGroupKeyCache)
            .Returns(mockDictionary);

        // Act
        string? result = await _streamGroupService.EncodeStreamGroupIdProfileIdAsync(streamGroupId, streamGroupProfileId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();

        (int? decodedStreamGroupId, string? valuesEncryptedString) = CryptoUtils.DecodeStreamGroupId(result, _settingsValue.ServerKey);
        decodedStreamGroupId.ShouldBe(streamGroupId);
        valuesEncryptedString.ShouldNotBeNull();
    }

    [Fact]
    public async Task EncodeStreamGroupIdProfileIdAsync_InvalidGroupKey_ReturnsNull()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;

        // Create a mock dictionary to return
        var mockDictionary = new ConcurrentDictionary<int, string?>();

        // Set up the cache manager to return our mock dictionary
        _cacheManager.Setup(x => x.StreamGroupKeyCache)
            .Returns(mockDictionary);

        _repositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StreamGroup?)null);

        // Act
        string? result = await _streamGroupService.EncodeStreamGroupIdProfileIdAsync(streamGroupId, streamGroupProfileId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DecodeStreamGroupIdProfileIdFromEncodedAsync_ValidInput_ReturnsDecodedValues()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string groupKey = "TestGroupKey123";

        string encodedString = CryptoUtils.EncodeTwoValues(streamGroupId, streamGroupProfileId, _settingsValue.ServerKey, groupKey);

        var streamGroup = new StreamGroup { Id = streamGroupId, GroupKey = groupKey };
        _repositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streamGroup);

        // Act
        var result = await _streamGroupService.DecodeStreamGroupIdProfileIdFromEncodedAsync(encodedString);

        // Assert
        result.StreamGroupId.ShouldBe(streamGroupId);
        result.StreamGroupProfileId.ShouldBe(streamGroupProfileId);
    }

    [Fact]
    public async Task DecodeProfileIdSMChannelIdFromEncodedAsync_ValidInput_ReturnsDecodedValues()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        int smChannelId = 3;
        string groupKey = "TestGroupKey123";

        string encodedString = CryptoUtils.EncodeThreeValues(streamGroupId, streamGroupProfileId, smChannelId, _settingsValue.ServerKey, groupKey);

        var streamGroup = new StreamGroup { Id = streamGroupId, GroupKey = groupKey };
        _repositoryWrapper.Setup(x => x.StreamGroup.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StreamGroup, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(streamGroup);

        // Act
        var result = await _streamGroupService.DecodeProfileIdSMChannelIdFromEncodedAsync(encodedString);

        // Assert
        result.StreamGroupId.ShouldBe(streamGroupId);
        result.StreamGroupProfileId.ShouldBe(streamGroupProfileId);
        result.SMChannelId.ShouldBe(smChannelId);
    }

    [Fact]
    public void EncodeProfileIds_ValidInput_ReturnsEncodedString()
    {
        // Arrange
        List<int> profileIds = [1, 2, 3];

        // Act
        string result = _streamGroupService.EncodeProfileIds(profileIds);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();

        string? decodedJson = CryptoUtils.DecodeValue(result, _settingsValue.ServerKey);
        decodedJson.ShouldNotBeNull();
    }

    [Fact]
    public void DecodeProfileIds_ValidInput_ReturnsDecodedList()
    {
        // Arrange
        List<int> profileIds = [1, 2, 3];
        string json = System.Text.Json.JsonSerializer.Serialize(profileIds);

        string encodedString = CryptoUtils.EncodeValue(json, _settingsValue.ServerKey);

        // Act
        List<int>? result = _streamGroupService.DecodeProfileIds(encodedString);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.ShouldContain(1);
        result.ShouldContain(2);
        result.ShouldContain(3);
    }

    [Fact]
    public void DecodeProfileIds_InvalidInput_ReturnsEmptyList()
    {
        // Arrange
        string encodedString = "InvalidEncodedString";
        CryptoUtils.EncodeValue("invalid", _settingsValue.ServerKey);

        // Act
        List<int>? result = _streamGroupService.DecodeProfileIds(encodedString);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }
}