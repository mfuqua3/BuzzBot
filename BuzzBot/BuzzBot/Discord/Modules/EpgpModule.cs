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
using CsvHelper;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.FileIO;

namespace BuzzBot.Discord.Modules
{

    [Group(GroupName)]
    public class EpgpModule : BuzzBotModuleBase<SocketCommandContext>
    {
        private readonly IPriorityReportingService _priorityReportingService;
        private readonly IQueryService _queryService;
        private readonly IEpgpService _epgpService;
        private readonly IAuditService _auditService;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly IPageService _pageService;
        private readonly IDocumentationService _documentationService;
        private IEmoteService _emoteService;
        private IAliasService _aliasService;
        private readonly IConfiguration _configuration;
        private readonly IItemService _itemService;
        private readonly IRaidService _raidService;
        public const string GroupName = "epgp";

        public EpgpModule(
            IPriorityReportingService priorityReportingService,
            IQueryService queryService,
            IEpgpService epgpService,
            IAuditService auditService,
            IEpgpCalculator epgpCalculator,
            IEpgpConfigurationService epgpConfigurationService,
            IPageService pageService,
            IDocumentationService documentationService,
            IEmoteService emoteService,
            IAliasService aliasService,
            IItemService itemService,
            IRaidService raidService
            IConfiguration configuration)
        {
            _priorityReportingService = priorityReportingService;
            _queryService = queryService;
            _epgpService = epgpService;
            _auditService = auditService;
            _epgpCalculator = epgpCalculator;
            _epgpConfigurationService = epgpConfigurationService;
            _pageService = pageService;
            _documentationService = documentationService;
            _emoteService = emoteService;
            _aliasService = aliasService;
            _configuration = configuration;
            _itemService = itemService;
            _raidService = raidService;
        }

        [Priority(0)]
        [Command("rolls", RunMode = RunMode.Async)]
        [Summary("Awards an item that has been rolled off in the game")]
        [Remarks("rolls Azar Earthfury Bracers")]
        public async Task AwardRoll(IGuildUser user, [Remainder] string itemQueryString)
        {
            EpgpRaid raid;
            try
            {
                raid = _raidService.GetRaid();
            }
            catch (InvalidOperationException)
            {
                await ReplyAsync("No active raid could be found.");
                return;
            }
            var item = await _itemService.TryGetItem(itemQueryString, Context.Channel);
            if (item == null) return;
            var gp = _epgpCalculator.ConvertGpFromGold(raid.NexusCrystalValue) * 2;
            var activeAlias = _aliasService.GetActiveAlias(user.Id);
            _epgpService.Gp(activeAlias, item, $"[Roll] {itemQueryString}", gp);
            var embed = CreateItemEmbed(item, gp);
            var userString = activeAlias.IsPrimary ? $"<@{user.Id}>" : _emoteService.GetAliasString(activeAlias, Context.Guild.Id);
            await ReplyAsync($"Assigning to {userString}", false, embed);
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


        [Command("gp", RunMode = RunMode.Async)]
        [RequiresBotAdmin]
        public async Task GearPoints(IGuildUser user, [Remainder] string queryString)
        {
            var isOffhand = queryString.EndsWith(" -oh");
            if (isOffhand)
                queryString = queryString.Substring(0, queryString.Length - 4);
            var item = await _itemService.TryGetItem(queryString, Context.Channel);
            if (item == null) return;
            await GiveItemGearPoints(user, item, isOffhand);
        }

        [Command("cost", RunMode = RunMode.Async)]
        public async Task Cost([Remainder] string queryString)
        {
            var isHunter = queryString.EndsWith(" -h");
            var isOffhand = queryString.EndsWith(" -oh");
            if (isHunter || isOffhand)
                queryString = queryString.Substring(0, queryString.Length - 4);
            var item = await _itemService.TryGetItem(queryString, Context.Channel);
            if (item == null) return;
            await ReplyAsync("", false, CreateItemEmbed(item, isHunter, isOffhand, out _));
        }

        private Embed CreateItemEmbed(Item item, bool isHunter, bool isOffhand, out double gp)
        {
            gp = _epgpCalculator.CalculateItem(item, isHunter, isOffhand);
            return CreateItemEmbed(item, gp);
        }

        private Embed CreateItemEmbed(Item item, double gp)
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"{item.Name} : {gp:F0} GP");
            embed.WithImageUrl($"http://www.korkd.com/wow_img/{item.Id}.png");
            return embed.Build();
        }

        private async Task GiveItemGearPoints(IGuildUser user, Item item, bool isOffhand)
        {
            var activeAlias = _aliasService.GetActiveAlias(user.Id);
            var gp = _epgpCalculator.CalculateItem(item, activeAlias.Class == Class.Hunter, isOffhand);
            var embed = CreateItemEmbed(item, gp);
            var userString = activeAlias.IsPrimary ? $"<@{user.Id}>" : _emoteService.GetAliasString(activeAlias, Context.Guild.Id);
            await ReplyAsync($"Assigning to {userString}", false, embed);
            _epgpService.Gp(activeAlias, item, $"[Claim] {item.Name}");
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
                var records = csv.GetRecords<EpgpCsvRecord>();
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
            await _auditService.Audit(aliasName, await GetUserChannel());
        }

        [Command("csv")]
        [Summary("Exports a csv summary of the entire EPGP PR list")]
        public async Task ExportCsv()
        {
            var channel = await GetUserChannel();
            var aliases = _repository.GetAliases().OrderByDescending(a => (double)a.EffortPoints / a.GearPoints).ToList();
            using var stream = new MemoryStream() { Capacity = 10240 };
            using var textWriter = new StreamWriter(stream) { AutoFlush = true };
            using var writer = new CsvWriter(textWriter, CultureInfo.CurrentCulture);
            {
                //writer.WriteHeader<EpgpCsvResult>();
                var records = aliases.Select(a => new EpgpCsvRecord()
                { Name = a.Name, EP = a.EffortPoints, GP = a.GearPoints, PR = ((double)a.EffortPoints / a.GearPoints).ToString("F2") });
                await writer.WriteRecordsAsync(records);
            }
            var data = stream.ToArray();
            await channel.SendFileAsync(new MemoryStream(data), "epgp.csv");
        }

        [Command("help")]
        [Alias("?")]
        public async Task Help() => await _documentationService.SendDocumentation(await GetUserChannel(), GroupName, Context.User.Id);
        [Command("pr")]
        [Summary("DMs the guilds priority list to requesting user")]
        public async Task PrintPriority()
        {
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
        [Priority(0)]
        [RequiresBotAdmin]
        public async Task AssignEffortPoints(string alias, int value)
        {
            _epgpService.Ep(alias, value, $"Granted by {(Context.User as IGuildUser).GetAliasName()}");
            var dmChannel = await GetUserChannel();
            await dmChannel.SendMessageAsync($"{value} EP successfully granted to {alias}");
        }

        [Command("ep")]
        [Summary("Grants EP to the user")]
        [Remarks("ep Azar 10")]
        [RequiresBotAdmin]
        public async Task AssignEffortPoints(IGuildUser user, int value)
        {
            var alias = _aliasService.GetActiveAlias(user.Id);
            _epgpService.Ep(alias, value, $"Granted by {(Context.User as IGuildUser).GetAliasName()}");
            var dmChannel = await GetUserChannel();
            await dmChannel.SendMessageAsync($"{value} EP successfully granted to {_emoteService.GetAliasString(alias, Context.Guild.Id)}");
        }

        [Command("gp")]
        [Summary("Grants GP to the user")]
        [Remarks("gp Azar 10")]
        [Priority(0)]
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
                pageBuilder.AddRow(new[] { key.ToString(), name, $"{(int)value}{valueString}" });
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
        [Command("remove_records", RunMode = RunMode.Async)]
        [RequiresBotAdmin]
        public async Task RemoveRecords(int day, int month, int year, int hour, int minute)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, 0);
            await _queryService.SendQuery($"Delete all records after {dateTime}?", Context.Channel, async () =>
                {
                    await using var context = new BuzzBotDbContext(_configuration);
                    var transactions = (context.EpgpTransactions as IQueryable<EpgpTransaction>).Where(t => t.TransactionDateTime >= dateTime)
                        .Include(t=>t.Alias)
                        .ToList();
                    foreach (var transaction in transactions)
                    {
                        switch (transaction.TransactionType)
                        {
                            case TransactionType.EpAutomated:
                            case TransactionType.EpManual:
                            case TransactionType.EpDecay:
                                transaction.Alias.EffortPoints -= transaction.Value;
                                break;
                            case TransactionType.GpFromGear:
                            case TransactionType.GpManual:
                            case TransactionType.GpDecay:
                                transaction.Alias.GearPoints -= transaction.Value;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        transaction.Alias = null;
                        context.EpgpTransactions.Remove(transaction);
                    }

                    context.SaveChanges();
                    return;
                },
                async () => await ReplyAsync("Operation cancelled"));
        }
    }
}