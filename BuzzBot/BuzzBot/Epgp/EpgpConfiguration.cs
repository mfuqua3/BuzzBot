using System;
using System.Collections.Generic;

namespace BuzzBot.Epgp
{
    public class EpgpConfiguration
    {
        [ConfigurationKey(1)]
        public int EpMinimum { get; set; }
        [ConfigurationKey(2)]
        public int GpMinimum { get; set; }
        [ConfigurationKey(3)]
        public int EpDecayPercentage { get; set; }
        [ConfigurationKey(4)]
        public int GpDecayPercentage { get; set; }
        [ConfigurationKey(5)]
        public DayOfWeek DecayDayOfWeek { get; set; }
        [ConfigurationKey(6)]
        public int UseEarnedGpDecay { get; set; }
        [ConfigurationKey(7)]
        public int EarnedGpDecayValue { get; set; }
        public List<EpgpRaidTemplate> Templates { get; set; }
    }
}