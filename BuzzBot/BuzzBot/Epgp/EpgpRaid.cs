using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace BuzzBot.Epgp
{
    public class EpgpRaid
    {
        public bool Started { get; set; }
        public DateTime StartTime { get; set; }
        public int StartBonus { get; set; }
        public int EndBonus { get; set; }
        public int TimeBonus { get; set; }
        public TimeSpan TimeBonusDuration { get; set; }
        public TimeSpan Duration { get; set; }
        public ulong RaidLeader { get; set; }
        public int Capacity { get; set; }
        public int Joined => Participants.Count(p => p.Value.Role != Role.Bench);
        public ConcurrentDictionary<ulong, RaidParticipant> Participants { get; set; } = new ConcurrentDictionary<ulong, RaidParticipant>();
    }
}