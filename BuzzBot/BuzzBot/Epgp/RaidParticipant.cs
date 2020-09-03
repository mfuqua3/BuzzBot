using System.Collections.Generic;
using BuzzBot.Models;

namespace BuzzBot.Epgp
{
    public class RaidParticipant
    {
        public RaidParticipant(ulong id)
        {
            Id = id;
        }

        public ulong Id { get; }
        public List<EpgpAliasViewModel> Aliases { get; set; }
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