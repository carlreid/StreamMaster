using MockQueryable.Moq;
using Moq;
using Shouldly;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Models;

namespace StreamMaster.Application.UnitTests.StreamGroups;

public partial class StreamGroupServiceTests
{
    [Fact]
    public async Task GetProfileFromSGIdsCommandProfileNameAsync_NonDefaultCommandProfile_ReturnsCommandProfile()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string commandProfileName = "CustomProfile";

        // Create a StreamGroupProfile that will be returned by GetStreamGroupProfileAsync
        var streamGroupProfile = new StreamGroupProfile
        {
            Id = streamGroupProfileId,
            StreamGroupId = streamGroupId,
            CommandProfileName = "Default" // Using Default so it will use the requested profile
        };

        // Set up the mock queryable for StreamGroupProfile
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile };
        var mockDbSet = streamGroupProfiles.AsQueryable().BuildMockDbSet();
        _repositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockDbSet.Object);

        var commandProfileDict = new CommandProfileDict();
        var commandProfile = new CommandProfile
        {
            Command = "ffmpeg",
            Parameters = "-test",
            IsReadOnly = false
        };
        commandProfileDict.AddProfile(commandProfileName, commandProfile);

        _commandProfileSettings.Setup(x => x.CurrentValue)
            .Returns(commandProfileDict);

        // Act
        var result = await _streamGroupService.GetProfileFromSGIdsCommandProfileNameAsync(streamGroupId, streamGroupProfileId, commandProfileName);

        // Assert
        result.ShouldNotBeNull();
        result.ProfileName.ShouldBe(commandProfileName);
    }

    [Fact]
    public async Task GetProfileFromSGIdsCommandProfileNameAsync_DefaultCommandProfile_UsesStreamGroupProfileCommandProfile()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string commandProfileName = "Default";
        string streamGroupProfileCommandName = "CustomProfile";

        var streamGroupProfile = new StreamGroupProfile
        {
            Id = streamGroupProfileId,
            StreamGroupId = streamGroupId,
            CommandProfileName = streamGroupProfileCommandName
        };

        // Set up the mock queryable for StreamGroupProfile
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile };
        var mockDbSet = streamGroupProfiles.AsQueryable().BuildMockDbSet();
        _repositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockDbSet.Object);

        var commandProfileDict = new CommandProfileDict();
        var commandProfile = new CommandProfile
        {
            Command = "ffmpeg",
            Parameters = "-test",
            IsReadOnly = false
        };
        commandProfileDict.AddProfile(streamGroupProfileCommandName, commandProfile);

        // Set up the settings to return our dictionary
        _commandProfileSettings.Setup(x => x.CurrentValue)
            .Returns(commandProfileDict);

        // Act
        var result = await _streamGroupService.GetProfileFromSGIdsCommandProfileNameAsync(streamGroupId, streamGroupProfileId, commandProfileName);

        // Assert
        result.ShouldNotBeNull();
        result.ProfileName.ShouldBe(streamGroupProfileCommandName);
    }

    [Fact]
    public async Task GetProfileFromSGIdsCommandProfileNameAsync_DefaultCommandProfileAndDefaultStreamGroupProfile_UsesSettingsDefaultCommandProfile()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string commandProfileName = "Default";
        string streamGroupProfileCommandName = "Default";
        string settingsDefaultCommandName = "DefaultCommandProfile";

        var streamGroupProfile = new StreamGroupProfile
        {
            Id = streamGroupProfileId,
            StreamGroupId = streamGroupId,
            CommandProfileName = streamGroupProfileCommandName
        };

        // Set up the mock queryable for StreamGroupProfile
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile };
        var mockDbSet = streamGroupProfiles.AsQueryable().BuildMockDbSet();
        _repositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockDbSet.Object);

        var commandProfileDict = new CommandProfileDict();
        var commandProfile = new CommandProfile
        {
            Command = "ffmpeg",
            Parameters = "-test",
            IsReadOnly = false
        };
        commandProfileDict.AddProfile(settingsDefaultCommandName, commandProfile);

        _commandProfileSettings.Setup(x => x.CurrentValue)
            .Returns(commandProfileDict);

        _settings.Setup(x => x.CurrentValue)
            .Returns(new Setting { DefaultCommandProfileName = settingsDefaultCommandName });

        // Act
        var result = await _streamGroupService.GetProfileFromSGIdsCommandProfileNameAsync(streamGroupId, streamGroupProfileId, commandProfileName);

        // Assert
        result.ShouldNotBeNull();
        result.ProfileName.ShouldBe(settingsDefaultCommandName);
    }

    [Fact]
    public async Task GetProfileFromSGIdsCommandProfileNameAsync_StreamGroupProfileHasCommandProfile_PrefersStreamGroupProfile()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string requestedCommandProfileName = "RequestedProfile";
        string streamGroupProfileCommandName = "StreamGroupProfile";

        var streamGroupProfile = new StreamGroupProfile
        {
            Id = streamGroupProfileId,
            StreamGroupId = streamGroupId,
            CommandProfileName = streamGroupProfileCommandName
        };

        // Set up the mock queryable for StreamGroupProfile
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile };
        var mockDbSet = streamGroupProfiles.AsQueryable().BuildMockDbSet();
        _repositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockDbSet.Object);

        var commandProfileDict = new CommandProfileDict();

        var requestedProfile = new CommandProfile
        {
            Command = "requested",
            Parameters = "-test",
            IsReadOnly = false
        };

        var streamGroupCommandProfile = new CommandProfile
        {
            Command = "streamgroup",
            Parameters = "-test",
            IsReadOnly = false
        };

        commandProfileDict.AddProfile(requestedCommandProfileName, requestedProfile);
        commandProfileDict.AddProfile(streamGroupProfileCommandName, streamGroupCommandProfile);

        _commandProfileSettings.Setup(x => x.CurrentValue)
            .Returns(commandProfileDict);

        // Act
        var result = await _streamGroupService.GetProfileFromSGIdsCommandProfileNameAsync(streamGroupId, streamGroupProfileId, requestedCommandProfileName);

        // Assert
        result.ShouldNotBeNull();
        result.ProfileName.ShouldBe(streamGroupProfileCommandName);
        result.Command.ShouldBe("streamgroup");
    }

    [Fact]
    public async Task GetProfileFromSGIdsCommandProfileNameAsync_NonExistentRequestedProfile_FallsBackToDefault()
    {
        // Arrange
        int streamGroupId = 1;
        int streamGroupProfileId = 2;
        string requestedCommandProfileName = "NonExistentProfile";
        string defaultCommandProfileName = "DefaultCommandProfile";

        var streamGroupProfile = new StreamGroupProfile
        {
            Id = streamGroupProfileId,
            StreamGroupId = streamGroupId,
            CommandProfileName = "Default"
        };

        // Set up the mock queryable for StreamGroupProfile
        var streamGroupProfiles = new List<StreamGroupProfile> { streamGroupProfile };
        var mockDbSet = streamGroupProfiles.AsQueryable().BuildMockDbSet();
        _repositoryWrapper.Setup(x => x.StreamGroupProfile.GetQuery(It.IsAny<bool>()))
            .Returns(mockDbSet.Object);

        var commandProfileDict = new CommandProfileDict();

        var defaultProfile = new CommandProfile
        {
            Command = "default",
            Parameters = "-test",
            IsReadOnly = false
        };

        commandProfileDict.AddProfile(defaultCommandProfileName, defaultProfile);

        // Setup the mock to return our dictionary
        _commandProfileSettings.Setup(x => x.CurrentValue)
            .Returns(commandProfileDict);

        // Setup the settings to return our default profile name
        _settings.Setup(x => x.CurrentValue)
            .Returns(new Setting { DefaultCommandProfileName = defaultCommandProfileName });

        // Act
        var result = await _streamGroupService.GetProfileFromSGIdsCommandProfileNameAsync(streamGroupId, streamGroupProfileId, requestedCommandProfileName);

        // Assert
        result.ShouldNotBeNull();
        result.ProfileName.ShouldBe(defaultCommandProfileName);
        result.Command.ShouldBe("default");
    }
}