using System;
using System.Collections.Generic;

namespace BuzzBot.Epgp
{
    public class EpgpRaid
    {
        public DateTime StartTime { get; set; }
        public int StartBonus { get; set; }
        public int EndBonus { get; set; }
        public int TimeBonus { get; set; }
        public TimeSpan TimeBonusDuration { get; set; }
        public TimeSpan Duration { get; set; }
        public ulong RaidLeader { get; set; }
        public int Capacity { get; set; }
        public int Joined => Casters.Count + Tanks.Count + Melee.Count + Healers.Count + Ranged.Count;
        public HashSet<RaidParticipant> Casters { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Tanks { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Melee { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Healers { get; set; } = new HashSet<RaidParticipant>();
        public HashSet<RaidParticipant> Ranged { get; set; } = new HashSet<RaidParticipant>();
    }
}