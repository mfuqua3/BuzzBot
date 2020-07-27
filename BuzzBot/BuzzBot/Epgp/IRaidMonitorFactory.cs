using System;

namespace BuzzBot.Epgp
{
    public interface IRaidMonitorFactory
    {
        EpgpRaidMonitor GetNew(Action onRaidEndedAction);
    }
}