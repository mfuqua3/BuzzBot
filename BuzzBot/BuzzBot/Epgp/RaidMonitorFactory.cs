using System;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public class RaidMonitorFactory : IRaidMonitorFactory
    {
        private readonly IEpgpService _epgpService;
        private readonly IEmoteService _emoteService;
        private readonly IAliasService _aliasService;
        private readonly BuzzBotDbContext _dbContext;

        public RaidMonitorFactory(IEpgpService epgpService, IEmoteService emoteService, BuzzBotDbContext dbContext, IAliasService aliasService)
        {
            _epgpService = epgpService;
            _emoteService = emoteService;
            _dbContext = dbContext;
            _aliasService = aliasService;
        }

        public EpgpRaidMonitor GetNew(RaidData raidData)
        {
            return new EpgpRaidMonitor(_epgpService, _emoteService, raidData, _dbContext, _aliasService);
        }
    }
}