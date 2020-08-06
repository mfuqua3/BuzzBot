﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Discord.Modules
{
    public class AliasModule : BuzzBotModuleBase
    {
        private IPageService _pageService;
        private readonly IAliasService _aliasService;
        private readonly IEmoteService _emoteService;
        private readonly IQueryService _queryService;
        private readonly IAliasEventAlerter _aliasEventAlerter;
        private readonly IConfiguration _configuration;

        public AliasModule(
            IPageService pageService,
            IAliasService aliasService,
            IEmoteService emoteService,
            IQueryService queryService,
            IAliasEventAlerter aliasEventAlerter,
            IConfiguration configuration)
        {
            _pageService = pageService;
            _aliasService = aliasService;
            _emoteService = emoteService;
            _queryService = queryService;
            _aliasEventAlerter = aliasEventAlerter;
            _configuration = configuration;
        }

        [Command("switch", RunMode = RunMode.Async)]
        [RequiresBotAdmin]
        public async Task Switch(IGuildUser user)
        {
            if (user == null);
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
            _aliasEventAlerter.ActiveAliasChanged += SendSwitchConfirmation;
            _aliasService.SetActiveAlias(user.Id, aliases[selection].Name);
            _aliasEventAlerter.ActiveAliasChanged -= SendSwitchConfirmation;
            await userChannel.SendMessageAsync($"Switch to {GetAliasString(aliases[selection])} confirmed.");
            return;
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
            List<EpgpAlias> aliases;
            await using (var dbContext = new BuzzBotDbContext(_configuration))
            {
                var guildUser = dbContext.GuildUsers.Include(gu => gu.Aliases).FirstOrDefault(usr => usr.Id == user.Id);
                if (guildUser == null)
                {
                    await ReplyAsync("No record of that user exists.");
                    return;
                }
                aliases = guildUser.Aliases;
                if (!aliases.Any())
                {
                    await ReplyAsync("No aliases found for that user.");
                    return;
                }

                if (!aliases.Any(a => a.IsActive))
                {
                    var primary = aliases.FirstOrDefault(a => a.IsPrimary);
                    if (primary != null) primary.IsActive = true;
                    await dbContext.SaveChangesAsync();
                }
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