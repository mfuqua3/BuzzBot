using System;

namespace BuzzBotData.Data
{
    public class RaidAlias
    {
        public Raid Raid { get; set; }
        public EpgpAlias Alias { get; set; }
        public Guid RaidId { get; set; }
        public Guid AliasId { get; set; }
    }
}