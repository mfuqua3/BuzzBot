using System;
using System.Collections.Generic;

namespace BuzzBotData.Data
{
    public class Raid
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<RaidAlias> Participants { get; set; }
        public List<RaidItem> Loot { get; set; }
    }
}