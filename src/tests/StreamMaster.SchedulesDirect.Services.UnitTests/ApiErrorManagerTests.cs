using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Extensions;
using StreamMaster.Domain.Helpers;
using StreamMaster.SchedulesDirect.Domain;
using System.Reflection;

namespace StreamMaster.SchedulesDirect.Services.UnitTests;

public class ApiErrorManagerTests
{
    [Fact]
    public void IsInCooldown_WhenCooldownActive_ReturnsTrue()
    {
        // Arrange
        var (manager, _, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        var futureTime = SMDT.UtcNow.AddMinutes(10);
        manager.SetCooldown(code, futureTime, "Test reason");

        // Act
        bool result = manager.IsInCooldown(code);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsInCooldown_WhenCooldownExpired_ReturnsFalse()
    {
        // Arrange
        var (manager, _, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        var pastTime = SMDT.UtcNow.AddMinutes(-10);
        manager.SetCooldown(code, pastTime, "Test reason");

        // Act
        bool result = manager.IsInCooldown(code);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsInCooldown_WhenCooldownNotSet_ReturnsFalse()
    {
        // Arrange
        var (manager, _, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;

        // Act
        bool result = manager.IsInCooldown(code);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void SetCooldown_WithDateTime_SetsCooldownCorrectly()
    {
        // Arrange
        var (manager, settings, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        var cooldownTime = SMDT.UtcNow.AddMinutes(30);
        var reason = "Test reason";

        // Act
        manager.SetCooldown(code, cooldownTime, reason);

        // Assert
        var info = manager.GetCooldownInfo(code);
        info.ShouldNotBeNull();
        info.CooldownUntil.ShouldBe(cooldownTime);
        info.Reason.ShouldBe(reason);
        info.IsActive.ShouldBeTrue();

        // Verify settings were updated
        settings.ErrorCooldowns.ShouldContain(c =>
            c.ErrorCode == (int)code &&
            c.CooldownUntil == cooldownTime &&
            c.Reason == reason);
    }

    [Fact]
    public void SetCooldown_WithTimeSpan_SetsCooldownCorrectly()
    {
        // Arrange
        var (manager, settings, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        var duration = TimeSpan.FromMinutes(30);
        var reason = "Test reason";
        var beforeSet = SMDT.UtcNow;

        // Act
        manager.SetCooldown(code, duration, reason);

        // Assert
        var info = manager.GetCooldownInfo(code);
        info.ShouldNotBeNull();
        info.CooldownUntil.ShouldBeGreaterThan(beforeSet.Add(duration).AddSeconds(-1));
        info.CooldownUntil.ShouldBeLessThan(beforeSet.Add(duration).AddSeconds(1));
        info.Reason.ShouldBe(reason);
        info.IsActive.ShouldBeTrue();

        // Verify settings were updated
        settings.ErrorCooldowns.ShouldContain(c =>
            c.ErrorCode == (int)code &&
            c.Reason == reason);
    }

    [Fact]
    public void SetCooldown_WhenCooldownAlreadyExists_UpdatesExistingCooldown()
    {
        // Arrange
        var (manager, settings, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;

        // Set initial cooldown
        var initialTime = SMDT.UtcNow.AddMinutes(10);
        manager.SetCooldown(code, initialTime, "Initial reason");

        // Update with new values
        var newTime = SMDT.UtcNow.AddMinutes(20);
        var newReason = "Updated reason";

        // Act
        manager.SetCooldown(code, newTime, newReason);

        // Assert
        var info = manager.GetCooldownInfo(code);
        info.ShouldNotBeNull();
        info.CooldownUntil.ShouldBe(newTime);
        info.Reason.ShouldBe(newReason);

        // Verify settings were updated
        settings.ErrorCooldowns.Count(c => c.ErrorCode == (int)code).ShouldBe(1);
        settings.ErrorCooldowns.ShouldContain(c =>
            c.ErrorCode == (int)code &&
            c.CooldownUntil == newTime &&
            c.Reason == newReason);
    }

    [Fact]
    public void GetCooldownInfo_WhenCooldownExists_ReturnsInfo()
    {
        // Arrange
        var (manager, _, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        var cooldownTime = SMDT.UtcNow.AddMinutes(30);
        var reason = "Test reason";
        manager.SetCooldown(code, cooldownTime, reason);

        // Act
        var info = manager.GetCooldownInfo(code);

        // Assert
        info.ShouldNotBeNull();
        info.CooldownUntil.ShouldBe(cooldownTime);
        info.Reason.ShouldBe(reason);
    }

    [Fact]
    public void GetCooldownInfo_WhenCooldownDoesNotExist_ReturnsNull()
    {
        // Arrange
        var (manager, _, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;

        // Act
        var info = manager.GetCooldownInfo(code);

        // Assert
        info.ShouldBeNull();
    }

    [Fact]
    public void GetActiveCooldowns_ReturnsOnlyActiveCooldowns()
    {
        // Arrange
        var (manager, _, _) = CreateManagerAndMocks();

        // Set active cooldown
        var activeCode = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        manager.SetCooldown(activeCode, SMDT.UtcNow.AddMinutes(10), "Active");

        // Set expired cooldown
        var expiredCode = SDHttpResponseCode.IMAGE_NOT_FOUND;
        manager.SetCooldown(expiredCode, SMDT.UtcNow.AddMinutes(-10), "Expired");

        // Act
        var activeCooldowns = manager.GetActiveCooldowns().ToList();

        // Assert
        activeCooldowns.Count.ShouldBe(1);
        activeCooldowns.ShouldContain(activeCode);
        activeCooldowns.ShouldNotContain(expiredCode);
    }

    [Fact]
    public void ClearCooldown_RemovesCooldownFromMemoryAndSettings()
    {
        // Arrange
        var (manager, settings, _) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;
        manager.SetCooldown(code, SMDT.UtcNow.AddMinutes(30), "Test reason");

        // Verify cooldown exists before clearing
        manager.GetCooldownInfo(code).ShouldNotBeNull();
        settings.ErrorCooldowns.ShouldContain(c => c.ErrorCode == (int)code);

        // Act
        manager.ClearCooldown(code);

        // Assert
        manager.GetCooldownInfo(code).ShouldBeNull();
        settings.ErrorCooldowns.ShouldNotContain(c => c.ErrorCode == (int)code);
    }

    [Fact]
    public void ClearCooldown_WhenCooldownDoesNotExist_DoesNothing()
    {
        // Arrange
        var (manager, settings, mockSettingsHelper) = CreateManagerAndMocks();
        var code = SDHttpResponseCode.ACCOUNT_LOCKOUT;

        // Act
        manager.ClearCooldown(code);

        // Assert
        manager.GetCooldownInfo(code).ShouldBeNull();
        mockSettingsHelper.Verify(x => x.UpdateSetting(It.IsAny<SDSettings>()), Times.Never);
    }

    [Fact]
    public void LoadFromSettings_LoadsCooldownsFromSettings()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ApiErrorManager>>();
        var mockSDSettings = new Mock<IOptionsMonitor<SDSettings>>();
        var mockSettingsHelper = MockStaticSettingsHelper();

        var settings = new SDSettings
        {
            ErrorCooldowns = new List<ErrorCooldownSetting>
            {
                new ErrorCooldownSetting
                {
                    ErrorCode = (int)SDHttpResponseCode.ACCOUNT_LOCKOUT,
                    CooldownUntil = SMDT.UtcNow.AddMinutes(30),
                    Reason = "Test reason 1"
                },
                new ErrorCooldownSetting
                {
                    ErrorCode = (int)SDHttpResponseCode.MAX_LINEUP_CHANGES_REACHED,
                    CooldownUntil = SMDT.UtcNow.AddMinutes(60),
                    Reason = "Test reason 2"
                }
            }
        };

        mockSDSettings.Setup(x => x.CurrentValue).Returns(settings);

        var manager = new ApiErrorManager(mockLogger.Object, mockSDSettings.Object);

        // Act - LoadFromSettings is called in constructor

        // Assert
        var info1 = manager.GetCooldownInfo(SDHttpResponseCode.ACCOUNT_LOCKOUT);
        var info2 = manager.GetCooldownInfo(SDHttpResponseCode.MAX_LINEUP_CHANGES_REACHED);

        info1.ShouldNotBeNull();
        info1.CooldownUntil.ShouldBe(settings.ErrorCooldowns[0].CooldownUntil);
        info1.Reason.ShouldBe("Test reason 1");

        info2.ShouldNotBeNull();
        info2.CooldownUntil.ShouldBe(settings.ErrorCooldowns[1].CooldownUntil);
        info2.Reason.ShouldBe("Test reason 2");
    }

    [Fact]
    public void ErrorCooldownInfo_IsActive_ReturnsTrueWhenActive()
    {
        // Arrange
        var futureTime = SMDT.UtcNow.AddMinutes(10);
        var info = new ErrorCooldownInfo(futureTime, "Test reason");

        // Act & Assert
        info.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ErrorCooldownInfo_IsActive_ReturnsFalseWhenExpired()
    {
        // Arrange
        var pastTime = SMDT.UtcNow.AddMinutes(-10);
        var info = new ErrorCooldownInfo(pastTime, "Test reason");

        // Act & Assert
        info.IsActive.ShouldBeFalse();
    }

    private (ApiErrorManager manager, SDSettings settings, Mock<StaticSettingsHelper> mockSettingsHelper) CreateManagerAndMocks()
    {
        var mockLogger = new Mock<ILogger<ApiErrorManager>>();
        var mockSDSettings = new Mock<IOptionsMonitor<SDSettings>>();
        var mockSettingsHelper = MockStaticSettingsHelper();

        var settings = new SDSettings
        {
            ErrorCooldowns = new List<ErrorCooldownSetting>()
        };

        mockSDSettings.Setup(x => x.CurrentValue).Returns(settings);

        var manager = new ApiErrorManager(mockLogger.Object, mockSDSettings.Object);

        return (manager, settings, mockSettingsHelper);
    }

    private Mock<StaticSettingsHelper> MockStaticSettingsHelper()
    {
        var mockSettingsHelper = new Mock<StaticSettingsHelper>();

        // Use reflection to replace the static class with our mock
        var helperType = typeof(SettingsHelper);
        var field = helperType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, mockSettingsHelper.Object);

        return mockSettingsHelper;
    }

    // Mock for static SettingsHelper class
    public class StaticSettingsHelper
    {
        public virtual void UpdateSetting<T>(T settings) where T : class
        {
            // Mock implementation
        }
    }
}