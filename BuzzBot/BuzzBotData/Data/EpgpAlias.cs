using System;
using System.Collections.Generic;

namespace BuzzBotData.Data
{
    public class EpgpAlias
    {
        public Guid Id { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public Class Class { get; set; }
        public int EffortPoints { get; set; }
        public int GearPoints { get; set; }
        public GuildUser User { get; set; }
        public ulong UserId { get; set; }
        public List<EpgpTransaction> Transactions { get; set; }
        public List<RaidAlias> Raids { get; set; }
        public List<RaidItem> AwardedItems { get; set; }
    }

    public enum Class
    {
        Warrior,
        Paladin,
        Hunter,
        Shaman,
        Rogue,
        Druid,
        Mage,
        Warlock,
        Priest,
        Undefined
    }
}