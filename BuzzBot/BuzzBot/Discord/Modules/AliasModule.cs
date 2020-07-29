using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    public class AliasModule : BuzzBotModuleBase<SocketCommandContext>
    {
        private EpgpRepository _epgpRepository;
        private IPageService _pageService;
        private readonly IAliasService _aliasService;
        private readonly IEmoteService _emoteService;
        private readonly IQueryService _queryService;

        public AliasModule(
            EpgpRepository epgpRepository,
            IPageService pageService,
            IAliasService aliasService,
            IEmoteService emoteService,
            IQueryService queryService)
        {
            _epgpRepository = epgpRepository;
            _pageService = pageService;
            _aliasService = aliasService;
            _emoteService = emoteService;
            _queryService = queryService;
        }

        [Command("switch", RunMode = RunMode.Async)]
        [RequiresBotAdmin]
        public async Task Switch(IGuildUser user)
        {
            if (user == null) return;
            var aliases = _aliasService.GetAliases(user.Id);
            if (aliases.Count <= 1)
            {
                await ReplyAsync("Invalid command, only one alias is registered to user.");
                return;
            }

            var userChannel = await GetUserChannel();
            var selection = await _queryService.SendOptionSelectQuery(
                "Please select new active alias:",
                aliases,
                GetAliasString,
                userChannel,
                CancellationToken.None);
            if (selection == -1)
            {
                await userChannel.SendMessageAsync("Switch operation cancelled");
                return;
            };
            _aliasService.ActiveAliasChanged += SendSwitchConfirmation;
            _aliasService.SetActiveAlias(user.Id, aliases[selection].Name);
            _aliasService.ActiveAliasChanged -= SendSwitchConfirmation;
            await userChannel.SendMessageAsync($"Switch to {GetAliasString(aliases[selection])} confirmed.");
        }

        private async void SendSwitchConfirmation(object sender, AliasChangeEventArgs e)
        {
            await ReplyAsync(
                $"<@{e.User}> has swapped from {GetAliasString(e.OldValue)} to {GetAliasString(e.NewValue)}");
        }


        [Command("switch", RunMode = RunMode.Async)]
        public async Task Switch() => await Switch(Context.User as IGuildUser);

        private string GetAliasString(EpgpAlias alias)
        {
            var emoteName = alias.Class.GetEmoteName();
            var fullyQualifiedName = _emoteService.GetFullyQualifiedName(Context.Guild.Id, emoteName);
            return $"{fullyQualifiedName} {alias.Name}";
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