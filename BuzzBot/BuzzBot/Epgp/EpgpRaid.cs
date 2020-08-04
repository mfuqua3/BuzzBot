using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using BuzzBot.Utility;

namespace BuzzBot.Epgp
{
    public class EpgpRaid:ViewModel
    {
        public Guid RaidId
        {
            get => Get<Guid>();
            set => Set(value);
        }
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
        public int NexusCrystalValue
        {
            get => Get<int>();
            set => Set(value);
        }
        public bool Started
        {
            get => Get<bool>();
            set => Set(value);
        }
        public DateTime StartTime
        {
            get => Get<DateTime>();
            set => Set(value);
        }
        public int StartBonus
        {
            get => Get<int>();
            set => Set(value);
        }
        public int EndBonus
        {
            get => Get<int>();
            set => Set(value);
        }
        public int TimeBonus
        {
            get => Get<int>();
            set => Set(value);
        }
        public TimeSpan TimeBonusDuration
        {
            get => Get<TimeSpan>();
            set => Set(value);
        }
        public TimeSpan Duration
        {
            get => Get<TimeSpan>();
            set => Set(value);
        }
        public ulong RaidLeader
        {
            get => Get<ulong>();
            set => Set(value);
        }
        public int Capacity
        {
            get => Get<int>();
            set => Set(value);
        }
        public int Joined => Participants.Count(p => p.Value.Role != Role.Bench);
        public ObservableConcurrentDictionary<ulong, RaidParticipant> Participants { get; set; } = new ObservableConcurrentDictionary<ulong, RaidParticipant>();
    }
}