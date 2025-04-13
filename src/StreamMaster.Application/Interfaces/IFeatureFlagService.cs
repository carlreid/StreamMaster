namespace StreamMaster.Application.Interfaces;

public interface IFeatureFlagService
{
    bool IsFeatureEnabled(FeatureFlags featureFlag);
}