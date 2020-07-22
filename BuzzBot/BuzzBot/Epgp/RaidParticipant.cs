namespace BuzzBot.Epgp
{
    public struct RaidParticipant
    {
        public RaidParticipant(ulong id, WowClass wowClass)
        {
            Id = id;
            WowClass = wowClass;
        }

        public ulong Id { get; }
        public WowClass WowClass { get; }
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