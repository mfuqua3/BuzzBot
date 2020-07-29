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
    public class EpgpModule : BuzzBotModuleBase<SocketCommandContext>
    {
        private readonly EpgpRepository _repository;
        private readonly IPriorityReportingService _priorityReportingService;
        private readonly IQueryService _queryService;
        private readonly IEpgpService _epgpService;
        private readonly IAuditService _auditService;
        private readonly ItemRepository _itemRepository;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly IPageService _pageService;
        private readonly IDocumentationService _documentationService;
        public const string GroupName = "epgp";

        public EpgpModule(
            EpgpRepository repository,
            IPriorityReportingService priorityReportingService,
            IQueryService queryService,
            IEpgpService epgpService,
            IAuditService auditService,
            ItemRepository itemRepository,
            IEpgpCalculator epgpCalculator,
            IEpgpConfigurationService epgpConfigurationService,
            IPageService pageService, 
            IDocumentationService documentationService)
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
        [RequiresBotAdmin]
        public async Task GearPoints(IGuildUser user, [Remainder] string queryString)
        {
            var isOffhand = queryString.EndsWith(" -oh");
            if (isOffhand)
                queryString = queryString.Substring(0, queryString.Length - 4);
            var items = _itemRepository.GetItems(queryString).OrderByDescending(itm => itm.ItemLevel).ToList();
            if (items.Count == 0)
            {
                await ReplyAsync($"\"{queryString}\" returned no results.");
                return;
            }
            if (items.Count == 1)
            {
                await GiveItemGearPoints(user, items.First(), isOffhand);
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
                await GiveItemGearPoints(user, items[result - 1], isOffhand);
            });
        }

        [Command("cost")]
        public async Task Cost([Remainder] string queryString)
        {
            var isHunter = queryString.EndsWith(" -h");
            var isOffhand = queryString.EndsWith(" -oh");
            if (isHunter || isOffhand)
                queryString = queryString.Substring(0, queryString.Length - 4);

            var items = _itemRepository.GetItems(queryString).OrderByDescending(itm => itm.ItemLevel).ToList();
            if (items.Count == 0)
            {
                await ReplyAsync($"\"{queryString}\" returned no results.");
                return;
            }
            if (items.Count == 1)
            {
                await ReplyAsync("", false, CreateItemEmbed(items.First(), isHunter, isOffhand, out _));
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
                await ReplyAsync("", false, CreateItemEmbed(items[result - 1], isHunter, isOffhand, out _));
            });
        }

        private Embed CreateItemEmbed(Item item, bool isHunter, bool isOffhand, out double gp)
        {
            gp = _epgpCalculator.Calculate(item, isHunter, isOffhand);
            var embed = new EmbedBuilder();
            embed.WithTitle($"{item.Name} : {gp:F0} GP");
            embed.WithImageUrl($"http://www.korkd.com/wow_img/{item.Id}.png");
            return embed.Build();
        }

        private async Task GiveItemGearPoints(IGuildUser user, Item item, bool isOffhand)
        {
            var embed = CreateItemEmbed(item, user.GetClass() == WowClass.Hunter, isOffhand, out var value);
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
                    _epgpService.Set(record.Name, record.EP, record.GP);
                }
            }
            catch (Exception)
            {
                await ReplyAsync(
                    "An error occurred while attempting to parse the file from the specified message");
                return;
            }

            await ReplyAsync("Done.");
        }

        

        [Command("audit")]
        public async Task Audit(IGuildUser user) =>
            await Audit(string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname);

        [Command("audit")]
        public async Task Audit(string aliasName)
        {
            // ReSharper disable once RedundantAssignment
            IMessageChannel channel = await GetUserChannel();
#if DEBUG
            channel = Context.Channel;
#endif
            await _auditService.Audit(aliasName, channel);
        }


        [Command("help")]
        [Alias("?")]
        public async Task Help() => await _documentationService.SendDocumentation(await GetUserChannel(), GroupName, Context.User.Id);
        [Command("pr")]
        [Summary("DMs the guilds priority list to requesting user")]
        public async Task PrintPriority()
        {
            // ReSharper disable once RedundantAssignment
            IMessageChannel channel = await Context.User.GetOrCreateDMChannelAsync();
#if DEBUG
            channel = Context.Channel;
#endif
            await _priorityReportingService.ReportAll(await GetUserChannel());
        }

        [Command("pr")]
        [Summary("DMs a trimmed priority list with the provided usernames")]
        [Remarks(@"pr Azar Triqueta Fragrock")]
        public async Task PrintPriority(params string[] userNames)
        {
            await _priorityReportingService.ReportAliases(await GetUserChannel(), userNames);
        }

        [Command("pr")]
        [Summary("DMs a trimmed priority list with the provided mentions (roles and users)")]
        [Remarks(@"pr @Druid @Shaman @Priest")]
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
        [Command("ep")]
        [Summary("Grants EP to the user")]
        [Remarks("ep Azar 10")]
        [RequiresBotAdmin]
        public async Task AssignEffortPoints(string alias, int value)
        {
            _epgpService.Ep(alias, value, $"Granted by {(Context.User as IGuildUser).GetAliasName()}");
            var dmChannel = await GetUserChannel();
            await dmChannel.SendMessageAsync($"{value} EP successfully granted to {alias}");
        }

        [Command("ep")]
        [RequiresBotAdmin]
        public async Task AssignEffortPoints(IGuildUser user, int value) =>
            await AssignEffortPoints(user.GetAliasName(), value);

        [Command("gp")]
        [Summary("Grants GP to the user")]
        [Remarks("gp Azar 10")]
        [RequiresBotAdmin]
        public async Task AssignGearPoints(string alias, int value)
        {
            _epgpService.Gp(alias, value, $"Granted by {(Context.User as IGuildUser).GetAliasName()}");
            var dmChannel = await GetUserChannel();
            await dmChannel.SendMessageAsync($"{value} GP successfully granted to {alias}");
        }

        [Command("gp")]
        [RequiresBotAdmin]
        public async Task AssignGearPoints(IGuildUser user, int value) =>
            await AssignGearPoints(user.GetAliasName(), value);



        [Command("config")]
        [RequiresBotAdmin]
        [Summary("Shows the EPGP configuration for the bot")]
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
        [Summary("Changes a configuration property for the EPGP bot")]
        [Remarks(@"config 2 15")]
        public async Task Configure(int key, int value)
        {
            _epgpConfigurationService.UpdateConfig(key, value);
            await ReplyAsync("Configuration change accepted");
        }

        [Command("add")]
        [RequiresBotAdmin]
        [Summary("Adds a user to the database")]
        [Remarks("add @Azar 1 25")]
        public async Task AddUser(IGuildUser user, int ep = 0, int gp = 0)
        {
            var config = _epgpConfigurationService.GetConfiguration();
            if (ep < config.EpMinimum) ep = config.EpMinimum;
            if (gp < config.GpMinimum) gp = config.GpMinimum;
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
                Class = userClass.ToDomainClass(),
                EffortPoints = ep,
                GearPoints = gp,
                IsPrimary = true,
                Name = user.GetAliasName(),
                Id = Guid.NewGuid()
            };
            _repository.AddAlias(alias);
            await ReplyAsync($"New user added with primary alias of \"{user.GetAliasName()} : {userClass}\"");
        }
        [Command("alias")]
        [RequiresBotAdmin]
        [Summary("Adds an alias/alt to the specified user")]
        [Remarks("alias @Cantuna Zynum Warlock 1 25")]
        public async Task AddAlias(IGuildUser user, string aliasName, string className, int ep = 0, int gp = 0)
        {
            var config = _epgpConfigurationService.GetConfiguration();
            if (ep < config.EpMinimum) ep = config.EpMinimum;
            if (gp < config.GpMinimum) gp = config.GpMinimum;

            if (!className.TryParseClass(out var userClass))
            {
                await ReplyAsync("Unable to parse class from provided class argument");
                return;
            }
            var alias = new EpgpAlias
            {
                UserId = user.Id,
                Class = userClass.ToDomainClass(),
                EffortPoints = 0,
                GearPoints = 0,
                IsPrimary = false,
                Name = aliasName,
                Id = Guid.NewGuid()
            };
            _repository.AddAlias(alias);
            _epgpService.Set(aliasName, ep, gp, "Alias initialized");
            await ReplyAsync($"New alias add to {user.GetAliasName()}: \"{aliasName} : {userClass}\"");
        }

        [Command("deletealias")]
        [Summary("Deletes the alias from the EPGP database (but retains the user)")]
        [Remarks("delete Baxterdruid")]
        [RequiresBotAdmin]
        public Task DeleteAlias(string aliasName)
        {
            Task.Run(async () =>
            {
                await _queryService.SendQuery(
                    $"Are you sure you want to delete all record of {aliasName}? The user record will be retained, but all record of this alias will be purged. This can not be undone.",
                    Context.Channel,
                    async () =>
                    {
                        try
                        {
                            _repository.DeleteAlias(aliasName);
                        }
                        catch (Exception ex)
                        {
                            await ReplyAsync($"Unable to successfully remove alias: {ex.Message}");
                            return;
                        }

                        await ReplyAsync("Alias removed successfully.");
                    },
                    async () => { await ReplyAsync("Operation cancelled"); });
            });
            return Task.CompletedTask;
        }

        [Command("deleteuser")]
        [Summary("Deletes the user (and all aliases) from the EPGP database")]
        [Remarks("delete @Marathonz")]
        [RequiresBotAdmin]
        public Task DeleteUser(IGuildUser user)
        {
            Task.Run(async () =>
            {
                await _queryService.SendQuery(
                    $"Are you sure you want to delete all record of {user.GetAliasName()} and their aliases? This can not be undone.",
                    Context.Channel,
                    async () =>
                    {
                        try
                        {
                            _repository.DeleteGuildUser(user.Id);
                        }
                        catch (Exception ex)
                        {
                            await ReplyAsync($"Unable to successfully remove user: {ex.Message}");
                            return;
                        }

                        await ReplyAsync("User removed successfully.");
                    },
                    async () => { await ReplyAsync("Operation cancelled"); });
            });
            return Task.CompletedTask;
        }
    }
}