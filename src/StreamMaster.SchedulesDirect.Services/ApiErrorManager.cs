using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Extensions;
using StreamMaster.Domain.Helpers;
using System.Collections.Concurrent;

namespace StreamMaster.SchedulesDirect.Services;

public class ErrorCooldownInfo
{
    public DateTime CooldownUntil { get; set; }
    public string Reason { get; set; }
    public bool IsActive => CooldownUntil > SMDT.UtcNow;

    public ErrorCooldownInfo(DateTime cooldownUntil, string reason)
    {
        CooldownUntil = cooldownUntil;
        Reason = reason;
    }
}

public interface IApiErrorManager
{
    bool IsInCooldown(SDHttpResponseCode code);

    void SetCooldown(SDHttpResponseCode code, DateTime until, string reason);

    void SetCooldown(SDHttpResponseCode code, TimeSpan duration, string reason);

    ErrorCooldownInfo? GetCooldownInfo(SDHttpResponseCode code);

    IEnumerable<SDHttpResponseCode> GetActiveCooldowns();

    void ClearCooldown(SDHttpResponseCode code);

    void LoadFromSettings();
}

public class ApiErrorManager : IApiErrorManager
{
    private readonly ILogger<ApiErrorManager> _logger;
    private readonly IOptionsMonitor<SDSettings> _sdSettings;
    private readonly ConcurrentDictionary<SDHttpResponseCode, ErrorCooldownInfo> _errorCooldowns = new();

    public ApiErrorManager(
        ILogger<ApiErrorManager> logger,
        IOptionsMonitor<SDSettings> sdSettings)
    {
        _logger = logger;
        _sdSettings = sdSettings;
        LoadFromSettings();

        _sdSettings.OnChange(_ => LoadFromSettings());
    }

    public bool IsInCooldown(SDHttpResponseCode code)
    {
        return _errorCooldowns.TryGetValue(code, out var info) && info.CooldownUntil > SMDT.UtcNow;
    }

    public void SetCooldown(SDHttpResponseCode code, DateTime until, string reason)
    {
        // Update in-memory cache
        var cooldownInfo = new ErrorCooldownInfo(until, reason);
        _errorCooldowns.AddOrUpdate(code, cooldownInfo, (_, _) => cooldownInfo);

        // Persist to settings
        PersistToSettings(code, cooldownInfo);

        _logger.LogDebug("Set cooldown for error code {Code}: {Reason} until {Until}",
            code, reason, until.ToString("g"));
    }

    public void SetCooldown(SDHttpResponseCode code, TimeSpan duration, string reason)
    {
        SetCooldown(code, SMDT.UtcNow.Add(duration), reason);
    }

    public ErrorCooldownInfo? GetCooldownInfo(SDHttpResponseCode code)
    {
        return _errorCooldowns.TryGetValue(code, out var info) ? info : null;
    }

    public IEnumerable<SDHttpResponseCode> GetActiveCooldowns()
    {
        return _errorCooldowns
            .Where(kv => kv.Value.CooldownUntil > SMDT.UtcNow)
            .Select(kv => kv.Key);
    }

    public void ClearCooldown(SDHttpResponseCode code)
    {
        if (_errorCooldowns.TryRemove(code, out _))
        {
            var settings = _sdSettings.CurrentValue;
            var cooldown = settings.ErrorCooldowns.FirstOrDefault(c => c.ErrorCode == (int)code);
            if (cooldown != null)
            {
                settings.ErrorCooldowns.Remove(cooldown);
                SettingsHelper.UpdateSetting(settings);
                _logger.LogDebug("Cleared cooldown for error code {Code}", code);
            }
        }
    }

    public void LoadFromSettings()
    {
        _errorCooldowns.Clear();
        var settings = _sdSettings.CurrentValue;

        foreach (var cooldown in settings.ErrorCooldowns)
        {
            if (Enum.IsDefined(typeof(SDHttpResponseCode), cooldown.ErrorCode))
            {
                var code = (SDHttpResponseCode)cooldown.ErrorCode;
                var info = new ErrorCooldownInfo(cooldown.CooldownUntil, cooldown.Reason);
                _errorCooldowns[code] = info;
            }
            else
            {
                _logger.LogWarning("Unknown error code {Code} found in settings", cooldown.ErrorCode);
            }
        }

        _logger.LogDebug("Loaded {Count} error cooldowns from settings", _errorCooldowns.Count);
    }

    private void PersistToSettings(SDHttpResponseCode code, ErrorCooldownInfo info)
    {
        var settings = _sdSettings.CurrentValue;
        var existingCooldown = settings.ErrorCooldowns.FirstOrDefault(c => c.ErrorCode == (int)code);

        if (existingCooldown != null)
        {
            existingCooldown.CooldownUntil = info.CooldownUntil;
            existingCooldown.Reason = info.Reason;
        }
        else
        {
            settings.ErrorCooldowns.Add(new ErrorCooldownSetting
            {
                ErrorCode = (int)code,
                CooldownUntil = info.CooldownUntil,
                Reason = info.Reason
            });
        }

        SettingsHelper.UpdateSetting(settings);
    }
}