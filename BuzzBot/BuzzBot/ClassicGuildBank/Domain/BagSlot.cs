using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class BagSlot
    {
        public int SlotId { get; set; }
        public int Quantity { get; set; }
        [JsonIgnore]
        public Guid BagId { get; set; }

        [NotMapped]
        [JsonProperty("bagId")]
        public string _BagId { get => BagId.ToString(); set => BagId = new Guid(value); }

        [JsonIgnore]
        public int? ItemId { get; set; }

        [JsonIgnore]
        public Bag Bag { get; set; }

        public Item Item { get; set; }
    };
}
