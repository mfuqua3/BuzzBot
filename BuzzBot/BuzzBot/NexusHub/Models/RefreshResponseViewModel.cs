using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class RefreshResponseViewModel
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }
    }
}