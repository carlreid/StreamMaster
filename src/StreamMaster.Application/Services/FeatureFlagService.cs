namespace StreamMaster.Application.Services
{
    internal class FeatureFlagService : IFeatureFlagService
    {
        private readonly IOptionsMonitor<Setting> _settings;
        private readonly Dictionary<FeatureFlags, bool> _cachedFlags;

        public FeatureFlagService(IOptionsMonitor<Setting> settings)
        {
            _settings = settings;
            _cachedFlags = new Dictionary<FeatureFlags, bool>();

            // Initialize cache with current settings
            UpdateCachedFlags(_settings.CurrentValue);

            // Register for changes
            _settings.OnChange(HandleSettingsUpdate);
        }

        public bool IsFeatureEnabled(FeatureFlags featureFlag)
        {
            return _cachedFlags.TryGetValue(featureFlag, out bool isEnabled) && isEnabled;
        }

        private void HandleSettingsUpdate(Setting setting)
        {
            UpdateCachedFlags(setting);
        }

        private void UpdateCachedFlags(Setting setting)
        {
            _cachedFlags[FeatureFlags.ShortLinks] = setting.EnableShortLinks || false;
        }
    }
}