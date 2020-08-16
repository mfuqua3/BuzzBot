using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    [Group(GroupName)]
    public class RaidModule : BuzzBotModuleBase
    {
        private const string GroupName = @"raid";
        private readonly IRaidService _raidService;
        private readonly IRaidFactory _raidFactory;
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly IDocumentationService _documentationService;
        private readonly IPageService _pageService;

        public RaidModule(
            IRaidService raidService,
            IRaidFactory raidFactory,
            IEpgpConfigurationService epgpConfigurationService,
            IDocumentationService documentationService,
            IPageService pageService)
        {
            _raidService = raidService;
            _raidFactory = raidFactory;
            _epgpConfigurationService = epgpConfigurationService;
            _documentationService = documentationService;
            _pageService = pageService;
        }
        [Command("help")]
        [Alias("?")]
        public async Task Help() => await _documentationService.SendDocumentation(await Context.User.GetOrCreateDMChannelAsync(), GroupName, Context.User.Id);

        [Command("begin", RunMode = RunMode.Async)]
        [Summary("Begins a new raid event")]
        [Remarks("begin moltencore")]
        [RequiresBotAdmin]
        public async Task Begin(string templateId)
        {
            EpgpRaid raid;
            try
            {
                raid = await _raidFactory.CreateNew(templateId);

            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
                return;
            }
            catch (InvalidOperationException ex)
            {
                await ReplyAsync(ex.Message);
                return;
            }

            if (raid == null)
            {
                await ReplyAsync("No template by that name exists");
                return;
            }
            raid.Name = templateId;
            raid.RaidLeader = Context.User.Id;
            await _raidService.PostRaid(Context.Channel, raid);
        }
        [Command("template")]
        [Summary("Prints a summary of all configured raid templates.")]
        [RequiresBotAdmin]
        public async Task Template()
        {
            var config = _epgpConfigurationService.GetConfiguration();
            if (!config.Templates.Any())
            {
                await ReplyAsync($"No raid templates are currently configured. Try !{GroupName} help");
                return;
            }
            var pageBuilder = new PageFormatBuilder()
                .AddColumn("Template ID")
                .AddColumn("EP Start Bonus")
                .AddColumn("EP End Bonus")
                .AddColumn("Duration")
                .AlternateRowColors();

            foreach (var template in config.Templates)
            {
                pageBuilder.AddRow(new[]
                {
                    template.TemplateId,
                    $"{template.StartBonus} EP",
                    $"{template.EndBonus} EP",
                    TimeSpan.FromMinutes(template.RaidDurationMinutes).ToString("g")
                });
            }

            await _pageService.SendPages(Context.Channel, pageBuilder.Build());
        }
        [Command("startnow")]
        [Summary("Immediately starts the specified (or latest active) raid")]
        [Remarks("startnow 737338848065093773")]
        [RequiresBotAdmin]
        public async Task StartNow(ulong raidId = 0)
        {
            try
            {
                _raidService.Start(raidId);
            }
            catch (InvalidOperationException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("extend")]
        [Summary("Extends the specified (or latest active) raid by provided minutes.")]
        [Remarks("extend 30 737338848065093773")]
        [RequiresBotAdmin]
        public async Task Extend(int extensionMinutes, ulong raidId = 0)
        {
            try
            {
                _raidService.Extend(TimeSpan.FromMinutes(extensionMinutes), raidId);
            }
            catch (InvalidOperationException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("kick")]
        [Alias(("remove"))]
        [Summary("Kicks the user from the specified (or latest active) raid ID")]
        [Remarks("kick Pusslayering 737338848065093773")]
        [RequiresBotAdmin]
        public async Task Kick(IGuildUser user, ulong raidId = 0)
        {
            await _raidService.KickUser(user.Id, raidId);
        }

        [Command("kick")]
        [Alias(("remove"))]
        [Summary("Kicks the user from the specified (or latest active) raid ID")]
        [RequiresBotAdmin]
        public async Task Kick(string user, ulong raidId = 0)
        {
            await _raidService.KickUser(user, raidId);
        }

        [Command("end")]
        [Summary("Ends the specified (or latest active) raid ID")]
        [Remarks("end 737338848065093773")]
        [RequiresBotAdmin]
        public async Task End(ulong raidId = 0)
        {
            try
            {
                _raidService.End(raidId);
            }
            catch (InvalidOperationException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("template")]
        [Summary("Prints configuration summary for template")]
        [RequiresBotAdmin]
        [Remarks("template moltencore")]
        public async Task Template(string templateId)
        {
            EpgpRaidTemplate template;
            try
            {
                template = _epgpConfigurationService.GetTemplate(templateId);
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
                return;
            }
            var pageBuilder = new PageFormatBuilder()
                .AddColumn("Configuration Property Key")
                .AddColumn("Property Name")
                .AddColumn("Value")
                .AlternateRowColors()
                .AddRow(new[] { "N/A", nameof(EpgpRaidTemplate.TemplateId), template.TemplateId });

            var properties = template.GetType().GetProperties()
                .Where(pi => Attribute.IsDefined(pi, typeof(ConfigurationKeyAttribute)));

            foreach (var propertyInfo in properties)
            {
                var key = propertyInfo.GetCustomAttribute<ConfigurationKeyAttribute>().Key;
                var name = propertyInfo.Name;
                var value = propertyInfo.GetValue(template);
                pageBuilder.AddRow(new[] { key.ToString(), name, value.ToString() });
            }

            await _pageService.SendPages(Context.Channel, pageBuilder.Build());
        }
        [Command("update")]
        [Summary("Updates the specified template using the key-value pair provided")]
        [Remarks("update moltencore 2 60")]
        [RequiresBotAdmin]
        public async Task Update(string templateId, int key, int value)
        {
            try
            {
                _epgpConfigurationService.UpdateTemplate(templateId, key, value);
            }
            catch (InvalidOperationException ex)
            {
                await ReplyAsync(ex.Message);
                return;
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
                return;
            }

            await ReplyAsync($"{templateId} has been updated successfully.");
        }

        [Command("add")]
        [Summary("Adds a new raid template.")]
        [Remarks("add {id} {capacity} {startBonus} {endBonus} {timeBonus} {timeBonusIntervalMinutes} {raidDurationMinutes} {signupDurationMinutes}")]
        [RequiresBotAdmin]
        public async Task Add(string id, int capacity, int startBonus, int endBonus, int timeBonus,
            int timeBonusDurationMinutes, int raidDurationMinutes, int signUpDurationMinutes)
        {
            var errors = new StringBuilder();
            if (string.IsNullOrWhiteSpace(id))
                errors.AppendLine("Invalid value for id, can not be null or empty");
            if (capacity < 1 || capacity > 99)
                errors.AppendLine("Invalid value for capacity, must be between 1 and 99");
            if (timeBonusDurationMinutes < 0 || timeBonusDurationMinutes > 8 * 60)
                errors.AppendLine("Invalid value for timebonus duration minutes, must be between 0 and 480");
            if (timeBonusDurationMinutes > raidDurationMinutes)
                errors.AppendLine("Invalid values f");
            if (raidDurationMinutes < 0 || raidDurationMinutes > 8 * 60)
                errors.AppendLine("Invalid value for raid duration minutes, must be between 0 and 480");
            if (signUpDurationMinutes < 0 || signUpDurationMinutes > 8 * 60)
                errors.AppendLine("Invalid value for signup duration minutes, must be between 0 and 480");
            if (errors.Length > 0)
            {
                await ReplyAsync(errors.ToString());
                return;
            }
            var template = new EpgpRaidTemplate
            {
                TemplateId = id,
                RaidCapacity = capacity,
                StartBonus = startBonus,
                EndBonus = endBonus,
                TimeBonus = timeBonus,
                TimeBonusDurationMinutes = timeBonusDurationMinutes,
                RaidDurationMinutes = raidDurationMinutes,
                SignUpDurationMinutes = signUpDurationMinutes
            };
            _epgpConfigurationService.AddTemplate(template);
        }
        [Command("delete")]
        [Summary("Deletes the specified raid template.")]
        [Remarks("delete moltencore")]
        [RequiresBotAdmin]
        public async Task Delete(string templateId)
        {
            try
            {
                _epgpConfigurationService.DeleteTemplate(templateId);
            }
            catch (ArgumentException ex)
            {
                await ReplyAsync(ex.Message);
                return;
            }

            await ReplyAsync($"{templateId} deleted successfully.");
        }
    }
}