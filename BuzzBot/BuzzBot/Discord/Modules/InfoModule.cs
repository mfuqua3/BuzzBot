using System.Linq;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using BuzzBotData.Repositories;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IPageService _pageService;

        public InfoModule(EpgpRepository epgpRepository, IPageService pageService)
        {
            _epgpRepository = epgpRepository;
            _pageService = pageService;
        }
        [Command("help")]
        [Alias("?")]
        public async Task Help()
        {
            await ReplyAsync("Commands must specify a module name before the command is provided\n " +
                             "Type !moduleName help for more info\n" +
                             "Available modules:\n" +
                             "!bank\n" +
                             "!raid\n" +
                             "!epgp");
        }

    }
}
