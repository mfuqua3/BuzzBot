using System;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Epgp
{
    public class RaidMonitorFactory : IRaidMonitorFactory
    {
        private readonly IEmoteService _emoteService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RaidMonitorFactory(IEmoteService emoteService, IServiceScopeFactory serviceScopeFactory)
        {
            _emoteService = emoteService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public EpgpRaidMonitor GetNew(RaidData raidData)
        {
            return new EpgpRaidMonitor(_emoteService, raidData, _serviceScopeFactory);
        }
    }
}