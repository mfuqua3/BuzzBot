using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class TooltipViewModel
    {

        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }
        [JsonProperty(PropertyName = "format")]
        public string Format { get; set; }
    }
}