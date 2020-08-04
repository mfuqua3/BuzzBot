using System.Collections.Generic;
using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class OverviewResponseViewModel
    {
        [JsonProperty(PropertyName = "slug")]
        public string Slug { get; set; }
        [JsonProperty(PropertyName = "data")]
        public List<OverviewStatsDataViewModel> Data { get; set; }
    }
}