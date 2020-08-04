using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class StatsDataViewModel
    {
        [JsonProperty(PropertyName = "historicalValue")]
        public int? HistoricalValue { get; set; }
        [JsonProperty(PropertyName = "marketValue")]
        public int? MarketValue { get; set; }
        [JsonProperty(PropertyName = "minBuyout")]
        public int? MinimumBuyout { get; set; }
        [JsonProperty(PropertyName = "numAuctions")]
        public int? NumberAuctions { get; set; }
        [JsonProperty(PropertyName = "quantity")]
        public int? Quantity { get; set; }
    }
}