using StreamMasterDomain.Mappings;

namespace StreamMasterDomain.Dto;

public class SettingDto : Setting, IMapFrom<Setting>
{
    public IconFileDto DefaultIconDto { get; set; }
    public string Release { get; set; } = BuildInfo.Release.ToString();
    public string Version { get; set; } = BuildInfo.Version.ToString();
}
