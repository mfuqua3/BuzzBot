using System;
using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class StatsViewModel
    {
        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime? LastUpdated { get; set; }
        [JsonProperty(PropertyName = "current")]
        public StatsDataViewModel Current { get; set; }
        [JsonProperty(PropertyName = "previous")]
        public StatsDataViewModel Previous { get; set; }
    }
}