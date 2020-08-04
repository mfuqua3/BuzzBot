using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class OverviewStatsDataViewModel : StatsDataViewModel
    {
        [JsonProperty(PropertyName = "itemId")]
        public int ItemId { get; set; }
        [JsonProperty(PropertyName = "previous")]
        public StatsDataViewModel Previous { get; set; }
    }
}