using System.Collections.Generic;
using Newtonsoft.Json;

namespace BuzzBot.NexusHub.Models
{
    public class NexusHubItemsViewModel
    {
        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }
        [JsonProperty(PropertyName = "itemId")]
        public int ItemId { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "uniqueName")]
        public string UniqueName { get; set; }
        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }
        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; }
        [JsonProperty(PropertyName = "requiredLevel")]
        public int RequiredLevel { get; set; }
        [JsonProperty(PropertyName = "itemLevel")]
        public int ItemLevel { get; set; }
        [JsonProperty(PropertyName = "sellPrice")]
        public int? SellPrice { get; set; }
        [JsonProperty(PropertyName = "vendorPrice")]
        public int? VendorPrice { get; set; }
        [JsonProperty(PropertyName = "itemLink")]
        public string ItemLink { get; set; }
        [JsonProperty(PropertyName = "tooltip")]
        public List<TooltipViewModel> Tooltip { get; set; }
        [JsonProperty(PropertyName = "stats")]
        public StatsViewModel Stats { get; set; }
    }
}