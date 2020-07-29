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
        public WowClass WowClass { get; set; }
        public bool IsPrimaryAlias { get; set; } = true;
        public string Alias { get; set; }
        public Role Role { get; set; }
    }

    public enum Role
    {
        Tank,
        Healer,
        Caster,
        Melee,
        Ranged,
        Bench
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