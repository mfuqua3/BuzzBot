using System.Collections.Generic;

namespace BuzzBotData.Data
{
    public class Faction
    {
        public string Id { get; set; }
        public string ServerId { get; set; }
        public Server Server { get; set; }
        public List<LiveItemData> ItemData { get; set; }

    }
}