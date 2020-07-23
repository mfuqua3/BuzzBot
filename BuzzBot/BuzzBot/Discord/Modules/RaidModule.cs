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
    public class RaidModule : ModuleBase<SocketCommandContext>
    {
        private const string GroupName = "raid";
        private readonly IRaidService _raidService;
        private readonly IRaidFactory _raidFactory;
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly CommandService _commandService;
        private readonly AdministrationService _administrationService;
        private readonly PageService _pageService;

        public RaidModule(
            IRaidService raidService,
            IRaidFactory raidFactory,
            IEpgpConfigurationService epgpConfigurationService,
            CommandService commandService,
            AdministrationService administrationService,
            PageService pageService)
        {
            _raidService = raidService;
            _raidFactory = raidFactory;
            _epgpConfigurationService = epgpConfigurationService;
            _commandService = commandService;
            _administrationService = administrationService;
            _pageService = pageService;
        }

        [Command("help")]
        [Alias("?")]
        public async Task Help()
        {
            var commands = _commandService.Commands.Where(cmd => cmd.Module.Name.Equals(GroupName)).Where(cmd => !string.IsNullOrEmpty(cmd.Summary));
            var embedBuilder = new EmbedBuilder();
            foreach (var command in commands)
            {
                if (command.Preconditions.Any(pc => pc.GetType() == typeof(RequiresBotAdminAttribute)))
                {
                    if (!_administrationService.IsUserAdmin(Context.User)) continue;
                }
                var embedFieldText = command.Summary;
                embedBuilder.AddField(command.Name, embedFieldText);
            }
            await ReplyAsync("Here's a list of commands and their descriptions: ", false, embedBuilder.Build());
        }

        [Command("begin")]
        [Summary("Begins a new raid event")]
        [Alias("start")]
        public async Task Begin(string templateId)
        {
            EpgpRaid raid;
            try
            {
                raid = _raidFactory.CreateNew(templateId);

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
            raid.RaidLeader = Context.User.Id;
            await _raidService.PostRaid(ReplyAsync, raid);
        }
        [Command("template")]
        [Summary("Prints a summary of all configured raid templates.")]
        public async Task Template()
        {
            var config = _epgpConfigurationService.GetConfiguration();
            if (!config.Templates.Any())
            {
                await ReplyAsync($"No raid templates are currently configured. Try buzz.{GroupName} help");
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
        [Command("template")]
        [Summary("Prints a summary of the configured fields for the provided raid template")]
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
        [Summary("Updates the specified template using the key-value pair provided.\n" +
                 "e.g. update moltencore 2 60")]
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
        [Summary("Adds a new raid template. \n" +
                 "Format must be add {id} {capacity} {startBonus} {endBonus} {timeBonus} {timeBonusIntervalMinutes} {raidDurationMinutes} {signupDurationMinutes} \n" +
                 "e.g. add moltencore 40 10 15 2 5 120 30")]
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