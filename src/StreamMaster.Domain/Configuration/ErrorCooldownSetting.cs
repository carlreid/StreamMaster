using System.Text.Json.Serialization;

namespace StreamMaster.Domain.Configuration
{
    public class ErrorCooldownSetting
    {
        [JsonPropertyName("code")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("until")]
        public DateTime CooldownUntil { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}