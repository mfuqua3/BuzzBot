using System;
using BuzzBot.Discord.Services;

namespace BuzzBot.Epgp
{
    public class RaidMonitorFactory : IRaidMonitorFactory
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IEpgpService _epgpService;
        private IEmoteService _emoteService;

        public RaidMonitorFactory(IEpgpService epgpService, EpgpRepository epgpRepository, IEmoteService emoteService)
        {
            _epgpService = epgpService;
            _epgpRepository = epgpRepository;
            _emoteService = emoteService;
        }

        public EpgpRaidMonitor GetNew(RaidData raidData)
        {
            return new EpgpRaidMonitor(_epgpService, _epgpRepository, _emoteService, raidData);
        }
    }
}