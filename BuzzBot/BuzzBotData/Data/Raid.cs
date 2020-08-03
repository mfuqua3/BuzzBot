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

    public class RaidItem
    {
        public Item Item { get; set; }
        public int ItemId { get; set; }
        public Raid Raid { get; set; }
        public Guid RaidId { get; set; }
        public EpgpTransaction Transaction { get; set; }
        public Guid TransactionId { get; set; }
        public EpgpAlias AwardedAlias { get; set; }
        public Guid AwardedAliasId { get; set; }
    }

    public class RaidAlias
    {
        public Raid Raid { get; set; }
        public EpgpAlias Alias { get; set; }
        public Guid RaidId { get; set; }
        public Guid AliasId { get; set; }
    }
}