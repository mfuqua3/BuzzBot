using System;
using System.Collections.Generic;

namespace BuzzBot.Epgp
{
    public interface IRaidFactory
    {
        EpgpRaid CreateNew(int startBonus, int endBonus, int timeBonus, TimeSpan bonusDuration, TimeSpan signupDuration, int capacity);
    }

    class RaidFactory : IRaidFactory
    {
        public EpgpRaid CreateNew(int startBonus, int endBonus, int timeBonus, TimeSpan bonusDuration, TimeSpan signupDuration, int capacity)
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var startTime = DateTime.UtcNow + signupDuration;
            var startTimeEst = TimeZoneInfo.ConvertTimeFromUtc(startTime, tzi);
            return new EpgpRaid
            {
                StartBonus = startBonus,
                EndBonus = endBonus,
                TimeBonusDuration = bonusDuration,
                Capacity = capacity,
                StartTime = startTimeEst
            };
        }
    }


    public class EpgpRaid
    {
        public DateTime StartTime { get; set; }
        public int StartBonus { get; set; }
        public int EndBonus { get; set; }
        public int TimeBonus { get; set; }
        public TimeSpan TimeBonusDuration { get; set; }
        public ulong RaidLeader { get; set; }
        public int Capacity { get; set; }
        public int Joined => Casters.Count + Tanks.Count + Melee.Count + Healers.Count + Ranged.Count;
        public HashSet<RaidParticipant> Casters { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Tanks { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Melee { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Healers { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Ranged { get; set; } = new HashSet<RaidParticipant>();
    }

    public struct RaidParticipant
    {
        public RaidParticipant(ulong id, WowClass wowClass)
        {
            Id = id;
            WowClass = wowClass;
        }

        public ulong Id { get;  }
        public WowClass WowClass { get;  }
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