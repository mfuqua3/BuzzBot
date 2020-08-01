using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class RefreshRequestViewModel
    {
        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }
    }
}