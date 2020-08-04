using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class AuthenticateResponseViewModel
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }
        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }
    }
}