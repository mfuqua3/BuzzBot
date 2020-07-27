namespace BuzzBot.Epgp
{
    public class RaidParticipant
    {
        public RaidParticipant(ulong id, WowClass wowClass)
        {
            Id = id;
            WowClass = wowClass;
        }

        public ulong Id { get; }
        public WowClass WowClass { get; }
        public bool IsPrimaryAlias { get; set; } = true;
        public string Alias { get; set; }
    }
    public enum WowClass
    {
        Unknown,
        Warrior,
        Paladin,
        Hunter,
        Shaman,
        Rogue,
        Druid,
        Warlock,
        Mage,
        Priest
    }
}