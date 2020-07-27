using System;
using BuzzBotData.Repositories;

namespace BuzzBot.Epgp
{
    public class RaidMonitorFactory : IRaidMonitorFactory
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IEpgpService _epgpService;

        public RaidMonitorFactory(IEpgpService epgpService, EpgpRepository epgpRepository)
        {
            _epgpService = epgpService;
            _epgpRepository = epgpRepository;
        }

        public EpgpRaidMonitor GetNew(Action onRaidEndedAction)
        {
            return new EpgpRaidMonitor(_epgpService, _epgpRepository, onRaidEndedAction);
        }
    }
}