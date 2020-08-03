using System;
using BuzzBot.Discord.Services;

namespace BuzzBot.Epgp
{
    public interface IRaidMonitorFactory
    {
        EpgpRaidMonitor GetNew(RaidData raidData);
    }
}