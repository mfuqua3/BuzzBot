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
        public int DecayPercentage { get; set; }
        [ConfigurationKey(4)]
        public DayOfWeek DecayDayOfWeek { get; set; }
        public List<EpgpRaidTemplate> Templates { get; set; }
    }
}