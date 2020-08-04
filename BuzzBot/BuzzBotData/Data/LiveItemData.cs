namespace BuzzBotData.Data
{
    public class LiveItemData
    {
        public string FactionId { get; set; }
        public Faction Faction { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; }
        public int MarketValue { get; set; }
        public int HistoricalValue { get; set; }
        public int MinimumBuyout { get; set; }
        public int NumberOfAuctions { get; set; }
        public int Quantity { get; set; }
    }
}