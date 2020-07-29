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

        [Command("characters")]
        public async Task Characters(IGuildUser user)
        {
            if (!_epgpRepository.ContainsUser(user.Id))
            {
                await ReplyAsync("No record of that user exists.");
                return;
            }
            var aliases = _epgpRepository.GetAliasesForUser(user.Id);
            if (!aliases.Any())
            {
                await ReplyAsync("No aliases found for that user.");
                return;
            }

            if (!aliases.Any(a => a.IsActive))
            {
                var primary = aliases.FirstOrDefault(a => a.IsPrimary);
                if (primary != null) primary.IsActive = true;
                _epgpRepository.Save();
            }
            var pageBuilder = new PageFormatBuilder()
                .AddColumn("Alias")
                .AddColumn("Is Primary?")
                .AddColumn("Is Active?")
                .AddColumn("EP")
                .AddColumn("GP")
                .AddColumn("PR");
            foreach (var alias in aliases)
            {
                pageBuilder.AddRow(new[]
                {
                    alias.Name,
                    alias.IsPrimary ? "Yes" : "No",
                    alias.IsActive ? "Yes" : "No",
                    alias.EffortPoints.ToString(),
                    alias.GearPoints.ToString(),
                    ((double) alias.EffortPoints / alias.GearPoints).ToString("F2")
                });
            }

            await _pageService.SendPages(Context.Channel, pageBuilder.Build());
        }
    }
}
