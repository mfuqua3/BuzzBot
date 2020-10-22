using System;

namespace BuzzBotData.Data
{
    public class RaidItem
    {
        public Guid Id { get; set; }
        public Item Item { get; set; }
        public int ItemId { get; set; }
        public Raid Raid { get; set; }
        public Guid RaidId { get; set; }
        public EpgpTransaction Transaction { get; set; }
        public Guid TransactionId { get; set; }
        public EpgpAlias AwardedAlias { get; set; }
        public Guid AwardedAliasId { get; set; }
    }
}