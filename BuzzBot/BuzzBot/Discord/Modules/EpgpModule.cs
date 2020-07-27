using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using CsvHelper;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.VisualBasic.FileIO;

namespace BuzzBot.Discord.Modules
{

    [Group(GroupName)]
    public class EpgpModule : ModuleBase<SocketCommandContext>
    {
        private readonly EpgpRepository _repository;
        private readonly PriorityReportingService _priorityReportingService;
        private readonly QueryService _queryService;
        private readonly IEpgpService _epgpService;
        private readonly AuditService _auditService;
        private readonly ItemRepository _itemRepository;
        private readonly EpgpCalculator _epgpCalculator;
        private IEpgpConfigurationService _epgpConfigurationService;
        private PageService _pageService;
        private DocumentationService _documentationService;
        public const string GroupName = "epgp";

        public EpgpModule(
            EpgpRepository repository,
            PriorityReportingService priorityReportingService,
            QueryService queryService,
            IEpgpService epgpService,
            AuditService auditService,
            ItemRepository itemRepository,
            EpgpCalculator epgpCalculator,
            IEpgpConfigurationService epgpConfigurationService,
            PageService pageService, DocumentationService documentationService)
        {
            _repository = repository;
            _priorityReportingService = priorityReportingService;
            _queryService = queryService;
            _epgpService = epgpService;
            _auditService = auditService;
            _itemRepository = itemRepository;
            _epgpCalculator = epgpCalculator;
            _epgpConfigurationService = epgpConfigurationService;
            _pageService = pageService;
            _documentationService = documentationService;
        }

        [Command("decay")]
        [RequiresBotAdmin]
        public Task Decay(int percentage, string epgpFlag = null)
        {
            Task.Run(async () => await _queryService.SendQuery($"Decay all user EP/GP values by {percentage}%?", Context.Channel,
                async () =>
                {
                    _epgpService.Decay(percentage, epgpFlag);
                    await ReplyAsync("Decay executed successfully.");
                },
                async () =>
                {
                    await ReplyAsync("Decay operation cancelled.");
                }));
            return Task.CompletedTask;
        }

        [Command("gp")]
        public async Task GearPoints(IGuildUser user, [Remainder] string queryString)
        {
            var items = _itemRepository.GetItems(queryString).OrderByDescending(itm => itm.ItemLevel).ToList();
            if (items.Count == 0)
            {
                await ReplyAsync($"\"{queryString}\" returned no results.");
                return;
            }
            if (items.Count == 1)
            {
                await GiveItemGearPoints(user, items.First());
                return;
            }

#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                var result = await _queryService.SendOptionSelectQuery(
                    $"\"{queryString}\" yielded multiple results. Please select below",
                    items,
                    (itm) => itm.Name,
                    Context.Channel, CancellationToken.None);
                if (result == 0) return;
                await GiveItemGearPoints(user, items[result - 1]);
            });
        }

        [Command("cost")]
        public async Task Cost([Remainder] string queryString)
        {
            var isHunter = queryString.EndsWith(" -h");
            if (isHunter)
                queryString = queryString.Substring(0, queryString.Length - 3);

            var items = _itemRepository.GetItems(queryString).OrderByDescending(itm => itm.ItemLevel).ToList();
            if (items.Count == 0)
            {
                await ReplyAsync($"\"{queryString}\" returned no results.");
                return;
            }
            if (items.Count == 1)
            {
                await ReplyAsync("", false, CreateItemEmbed(items.First(), isHunter, out _));
                return;
            }

#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                var result = await _queryService.SendOptionSelectQuery(
                    $"\"{queryString}\" yielded multiple results. Please select below",
                    items,
                    (itm) => itm.Name,
                    Context.Channel, CancellationToken.None);
                if (result == 0) return;
                await ReplyAsync("", false, CreateItemEmbed(items[result - 1], isHunter, out _));
            });
        }

        private Embed CreateItemEmbed(Item item, bool isHunter, out double gp)
        {
            gp = _epgpCalculator.Calculate(item, isHunter);
            var embed = new EmbedBuilder();
            embed.WithTitle($"{item.Name} : {gp:F0} GP");
            embed.WithImageUrl($"http://www.korkd.com/wow_img/{item.Id}.png");
            return embed.Build();
        }

        private async Task GiveItemGearPoints(IGuildUser user, Item item)
        {
            var embed = CreateItemEmbed(item, user.GetClass() == WowClass.Hunter, out var value);
            await ReplyAsync($"Assigning to <@{user.Id}>", false, embed);
            _epgpService.Gp(user.GetAliasName(), (int)Math.Round(value), item.Name, TransactionType.GpFromGear);
        }

        [Command("correct")]
        [RequiresBotAdmin]
        public async Task Correct(ulong csvMessageId)
        {
            var message = await Context.Channel.GetMessageAsync(csvMessageId);
            if (message == null)
            {
                await ReplyAsync("No message was found with that ID in this channel");
                return;
            }

            if (!message.Attachments.Any())
            {
                await ReplyAsync("No attachments detected in file");
                return;
            }

            using var client = new HttpClient();
            var file = await client.GetStringAsync(message.Attachments.First().Url);
            using var streamReader = new StringReader(file);
            try
            {
                var csv = new CsvReader(streamReader, CultureInfo.CurrentCulture);
                var records = csv.GetRecords<EpgpCsvResult>();
                foreach (var record in records)
                {
                    if (!_epgpService.Set(record.Name, record.EP, record.GP))
                    {
                        await ReplyAsync($"{record.Name} not set. (No record found)");
                    }
                }
            }
            catch (Exception)
            {
                await ReplyAsync(
                    "An error occurred while attempting to parse the file from the specified message");
            }
        }

        

        [Command("audit")]
        public async Task Audit(IGuildUser user) =>
            await Audit(string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname);

        [Command("audit")]
        public async Task Audit(string aliasName)
        {
            await _auditService.Audit(aliasName, Context.Channel);
        }


        [Command("help")]
        [Alias("?")]
        public async Task Help() => await _documentationService.SendDocumentation(await Context.User.GetOrCreateDMChannelAsync(), GroupName, Context.User.Id);
        [Command("pr")]
        public async Task PrintPriority()
        {
            await _priorityReportingService.ReportAll(Context.Channel);
        }

        [Command("pr")]
        public async Task PrintPriority(params string[] userNames)
        {
            await _priorityReportingService.ReportAliases(Context.Channel, userNames);
        }

        [Command("pr")]
        public async Task PrintPriority(params IMentionable[] mentionables)
        {
            var roles = mentionables.Where(m => m is IRole).Cast<IRole>();
            var users = mentionables.Where(m => m is IGuildUser).Cast<IGuildUser>().ToList();
            foreach (var role in roles)
            {
                users.AddRange(Context.Guild.Users.Where(usr => usr.Roles.Contains(role)));
            }

            await PrintPriority(users.ToArray());
        }
        [Command("pr")]
        public async Task PrintPriority(params IGuildUser[] users)
        {
            var prunedUsers = users.Select(usr => usr.GetAliasName()).Distinct();
            await PrintPriority(prunedUsers.ToArray());
        }

        [Command("config")]
        public async Task Configuration()
        {
            var config = _epgpConfigurationService.GetConfiguration();
            var pageBuilder = new PageFormatBuilder()
                .AddColumn("Configuration Property Key")
                .AddColumn("Property Name")
                .AddColumn("Value")
                .AlternateRowColors();

            var properties = config.GetType().GetProperties()
                .Where(pi => Attribute.IsDefined(pi, typeof(ConfigurationKeyAttribute)));

            foreach (var propertyInfo in properties)
            {
                var key = propertyInfo.GetCustomAttribute<ConfigurationKeyAttribute>().Key;
                var name = propertyInfo.Name;
                var value = propertyInfo.GetValue(config);
                string valueString = string.Empty;
                if (((int)value).ToString() != value.ToString())
                {
                    valueString = $" ({value.ToString()})";
                }
                pageBuilder.AddRow(new[] { key.ToString(), name, $"{(int)value}{valueString}"});
            }

            await _pageService.SendPages(Context.Channel, pageBuilder.Build());
        }
        [Command("config")]
        [RequiresBotAdmin]
        public async Task Configure(int key, int value)
        {
            _epgpConfigurationService.UpdateConfig(key, value);
            await ReplyAsync("Configuration change accepted");
        }

        [Command("add")]
        [RequiresBotAdmin]
        public async Task AddUser(string username, int ep = 0, int gp = 0)
        {
            var user = Context.Guild.Users.FirstOrDefault(gu => gu.Username.StartsWith(username) || !string.IsNullOrWhiteSpace(gu.Nickname) && gu.Nickname.StartsWith(username));
            if (user == null)
            {
                await ReplyAsync("No user could be found with that name");
                return;
            }

            var id = user.Id;
            _repository.AddGuildUser(id);
            var userClass = user.GetClass();
            if (userClass == WowClass.Unknown)
            {
                await ReplyAsync("Unable to infer class for specified user. No primary alias will be created");
                return;
            }
            var alias = new EpgpAlias
            {
                UserId = id,
                Class = (Class)userClass,
                EffortPoints = ep,
                GearPoints = gp,
                IsPrimary = true,
                Name = username,
                Id = Guid.NewGuid()
            };
            _repository.AddAlias(alias);
            await ReplyAsync($"New user added with primary alias of \"{username} : {userClass}\"");
        }
        [Command("alias")]
        [RequiresBotAdmin]
        public async Task AddAlias(string userName, string aliasName, string className, int ep = 0, int gp = 0)
        {
            var user = Context.Guild.Users.FirstOrDefault(gu => gu.Username.StartsWith(userName) || !string.IsNullOrWhiteSpace(gu.Nickname) && gu.Nickname.StartsWith(userName));
            if (user == null)
            {
                await ReplyAsync($"No user could be found with that name: {userName}");
                return;
            }

            var id = user.Id;
            if (!className.TryParseClass(out var userClass))
            {
                await ReplyAsync("Unable to parse class from provided class argument");
                return;
            }
            var alias = new EpgpAlias
            {
                UserId = id,
                Class = (Class)userClass,
                EffortPoints = ep,
                GearPoints = gp,
                IsPrimary = false,
                Name = aliasName,
                Id = Guid.NewGuid()
            };
            _repository.AddAlias(alias);
            await ReplyAsync($"New alias add to {userName}: \"{aliasName} : {userClass}\"");
        }


    }
}