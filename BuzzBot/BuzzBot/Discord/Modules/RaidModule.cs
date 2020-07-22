using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Epgp;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    [Group("raid")]
    public class RaidModule : ModuleBase<SocketCommandContext>
    {
        private readonly RaidService _raidService;
        private readonly IRaidFactory _raidFactory;

        public RaidModule(RaidService raidService, IRaidFactory raidFactory)
        {
            _raidService = raidService;
            _raidFactory = raidFactory;
        }

        [Command("begin")]
        [Summary("Begins a new raid event")]
        [Alias("start")]
        public async Task Begin()
        {
            var raid = _raidFactory.CreateNew(TODO);
            raid.RaidLeader = Context.User.Id;
            await _raidService.PostRaid(ReplyAsync, raid);
        }
    }
}