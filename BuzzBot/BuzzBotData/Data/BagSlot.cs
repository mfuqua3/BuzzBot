using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BuzzBotData.Data
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

    public class Money
    {
        public Money(int totalCopper)
        {
            var remaining = totalCopper;
            Gold = (int)Math.Floor((double) totalCopper / 10000);
            remaining -= Gold * 10000;
            Silver = (int)Math.Floor((double)remaining / 100);
            remaining -= Silver * 100;
            Copper = remaining;
        }
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Copper { get; set; }
    }
}
