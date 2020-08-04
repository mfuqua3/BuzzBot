using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class AuthenticateRequestViewModel
    {
        [JsonProperty(PropertyName = "user_key")]
        public string UserKey { get; set; }
        [JsonProperty(PropertyName = "user_secret")]
        public string UserSecret { get; set; }
    }
}