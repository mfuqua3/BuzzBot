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
using AutoMapper;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using CsvHelper;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.FileIO;

namespace BuzzBot.Discord.Modules
{

    [Group(GroupName)]
    public class EpgpModule : BuzzBotModuleBase
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
        private readonly IItemService _itemService;
        private readonly IRaidService _raidService;
        private readonly IAdministrationService _administrationService;
        private readonly BuzzBotDbContext _dbContext;
        private IUserService _userService;
        private IMapper _mapper;
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
            IRaidService raidService,
            IAdministrationService administrationService,
            BuzzBotDbContext dbContext, IUserService userService, IMapper mapper)
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
            _itemService = itemService;
            _raidService = raidService;
            _administrationService = administrationService;
            _dbContext = dbContext;
            _userService = userService;
            _mapper = mapper;
        }

        [Command("forcecorrect")]
        [Summary("Corrects the user record to the sum of their transaction history")]
        [RequiresBotAdmin]
        public async Task ForceCorrect(IGuildUser user)
        {
            var aliases = _aliasService.GetActiveAliases(user.Id).ToList();
            foreach (var alias in aliases)
            {
                _auditService.ForceCorrect(alias.Id);
            }

            await ReplyAsync("Done.");

        }
        

        [Command("reconcile")]
        [Alias("validate")]
        [Summary("Reconciles the users printed EP and GP values against their transaction history.")]
        public async Task ReconcileAccount(IGuildUser user)
        {
            var aliases = _aliasService.GetActiveAliases(user.Id).ToList();
            try
            {
                foreach (var epgpAlias in aliases)
                {
                    await ReplyAsync($"Reconciling alias identity: {epgpAlias.Name}");
                    _auditService.ValidateTransactionHistory(epgpAlias.Id);
                    await ReplyAsync($"Alias reconciliation complete.");
                }
                await ReplyAsync("User's account reconciled successfully. No discrepancies detected.");
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                await ReplyAsync(ex.Message);
            }
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
            var activeAliases = _aliasService.GetActiveAliases(user.Id).ToList();
            var activeAlias = await QueryTargetAlias(activeAliases);
            var item = await _itemService.TryGetItem(itemQueryString, Context, activeAlias);
            if (item == null) return;
            var gp = _epgpCalculator.ConvertGpFromGold(raid.NexusCrystalValue) * 2;
            _epgpService.Gp(activeAlias, item, $"[Roll] {item.Name}", gp);
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
            var aliases = _aliasService.GetActiveAliases(user.Id);
            var alias = await QueryTargetAlias(aliases.ToList());
            var item = await _itemService.TryGetItem(queryString, Context, alias);
            if (item == null) return;
            await GiveItemGearPoints(alias, item, isOffhand);
        }

        [Command("cost", RunMode = RunMode.Async)]
        public async Task Cost([Remainder] string queryString)
        {
            var isHunter = queryString.EndsWith(" -h");
            var isOffhand = queryString.EndsWith(" -oh");
            if (isHunter || isOffhand)
                queryString = queryString.Substring(0, queryString.Length - 4);
            var item = await _itemService.TryGetItem(queryString, Context);
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

        private async Task GiveItemGearPoints(EpgpAlias alias, Item item, bool isOffhand)
        {
            var gp = _epgpCalculator.CalculateItem(item, alias.Class == Class.Hunter, isOffhand);
            var embed = CreateItemEmbed(item, gp);
            var userString = alias.IsPrimary ? $"<@{alias.UserId}>" : _emoteService.GetAliasString(alias, Context.Guild.Id);
            await ReplyAsync($"Assigning to {userString}", false, embed);
            _epgpService.Gp(alias, item, $"[Claim] {item.Name}");
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
            await _auditService.Audit(aliasName, await GetUserChannel(), _administrationService.IsUserAdmin(Context.User));
        }

        [Command("csv")]
        [Summary("Exports a csv summary of the entire EPGP PR list")]
        public async Task ExportCsv()
        {
            var channel = await GetUserChannel();
            var aliases = _dbContext.Aliases.AsQueryable().OrderByDescending(a => (double)a.EffortPoints / a.GearPoints).ToList();
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

        [Command("transactioncsv")]
        [Summary("DMs the calling user a csv file with all of the transactions in the bots database")]
        public async Task PrintTransactionCsv()
        {
            var transactions = await _dbContext.EpgpTransactions
                .Include(t => t.Alias)
                .OrderByDescending(t => t.TransactionDateTime)
                .ToListAsync();
            var csvRecords = _mapper.Map<List<EpgpTransaction>, List<TransactionCsvRecord>>(transactions);
            var channel = await GetUserChannel();
            await using var stream = new MemoryStream() { Capacity = 1024000 };
            await using var textWriter = new StreamWriter(stream) { AutoFlush = true };
            await using var writer = new CsvWriter(textWriter, CultureInfo.CurrentCulture);
            {
                await writer.WriteRecordsAsync(csvRecords);
            }
            var data = stream.ToArray();
            await channel.SendFileAsync(new MemoryStream(data), "transactions.csv");
        }

        [Command("lootcsv")]
        [Summary("DMs the calling user a csv file with all of the loot in the bots database")]
        public async Task PrintLootCsv()
        {
            var loot = await _dbContext.RaidItems
                .Include(ri => ri.Transaction)
                .Include(ri => ri.Item)
                .Include(ri => ri.AwardedAlias)
                .OrderByDescending(ri => ri.Transaction.TransactionDateTime)
                .ToListAsync();
            var csvRecords = _mapper.Map<List<RaidItem>, List<LootCsvRecord>>(loot);
            var channel = await GetUserChannel();
            await using var stream = new MemoryStream() { Capacity = 10240 };
            await using var textWriter = new StreamWriter(stream) { AutoFlush = true };
            await using var writer = new CsvWriter(textWriter, CultureInfo.CurrentCulture);
            {
                await writer.WriteRecordsAsync(csvRecords);
            }
            var data = stream.ToArray();
            await channel.SendFileAsync(new MemoryStream(data), "loot.csv");
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
            var userNames = users.SelectMany(usr => _aliasService.GetActiveAliases(usr.Id)).Select(a => a.Name).ToArray();
            await PrintPriority(userNames);
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

        [Command("ep", RunMode = RunMode.Async)]
        [Summary("Grants EP to the user")]
        [Remarks("ep Azar 10")]
        [RequiresBotAdmin]
        public async Task AssignEffortPoints(IGuildUser user, int value)
        {
            var aliases = _aliasService.GetActiveAliases(user.Id);
            var alias = await QueryTargetAlias(aliases.ToList());
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

        [Command("gp", RunMode = RunMode.Async)]
        [RequiresBotAdmin]
        public async Task AssignGearPoints(IGuildUser user, int value)
        {
            var aliases = _aliasService.GetActiveAliases(user.Id);
            var alias = await QueryTargetAlias(aliases.ToList());
            _epgpService.Gp(alias, value, $"Granted by {(Context.User as IGuildUser).GetAliasName()}");
            var dmChannel = await GetUserChannel();
            await dmChannel.SendMessageAsync($"{value} GP successfully granted to {_emoteService.GetAliasString(alias, Context.Guild.Id)}");
        }



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

        [Command("loot", RunMode = RunMode.Async)]
        [Alias("items")]
        [Summary("Prints the users entire awarded item history")]
        [Remarks("loot @Azar")]
        public async Task PrintLootHistory(IGuildUser user)
        {
            var aliases = _aliasService.GetActiveAliases(user.Id).ToList();
            var alias = aliases.Count > 2 ? await QueryTargetAlias(aliases) : aliases.FirstOrDefault();
            var channel = await GetUserChannel();
            await _itemService.PrintLootHistory(channel, alias, _administrationService.IsUserAdmin(Context.User));
        }
        [Command("history")]
        [Summary("Prints the an entire awarded item history")]
        [Remarks("loot Claw of Chromaggus")]
        public async Task PrintItemHistory([Remainder] string queryString)
        {
            var item = await _itemService.TryGetItem(queryString, Context);
            if (item == null) return;
            await _itemService.PrintItemHistory(await GetUserChannel(), item,
                _administrationService.IsUserAdmin(Context.User));
        }

        private async Task<EpgpAlias> QueryTargetAlias(List<EpgpAlias> ambiguousAliases)
        {
            if (ambiguousAliases.Count < 2) return ambiguousAliases.FirstOrDefault();
            var idx = await _queryService.SendOptionSelectQuery("Please select the intended target alias", ambiguousAliases,
                alias => alias.Name, Context.Channel, CancellationToken.None);
            return idx == -1 ? null : ambiguousAliases[idx];
        }

        [Command("undo")]
        [Summary("Removes an EPGP transaction from the database.")]
        [Remarks("undo 0f23c9b0e9fd43429d429706116fad9e")]
        [RequiresBotAdmin]
        public async Task RemoveTransaction(Guid guid)
        {
            _epgpService.DeleteTransaction(guid);
            await ReplyAsync("Record removed successfully.");
        }

        [Command("undo", RunMode = RunMode.Async)]
        [Summary("Removes the last manual EP or GP transaction in the record")]
        [RequiresBotAdmin]
        public async Task Undo()
        {
            var transaction = _dbContext.EpgpTransactions.Include(t => t.Alias).AsQueryable().OrderByDescending(t => t.TransactionDateTime)
                .FirstOrDefault(t =>
                    t.TransactionType == TransactionType.EpManual |
                    t.TransactionType == TransactionType.GpManual |
                    t.TransactionType == TransactionType.GpFromGear);
            if (transaction == null)
            {
                await ReplyAsync("No transaction could be found to undo.");
                return;
            }

            await _queryService.SendQuery($"Undo {transaction.Value}{transaction.TransactionType.GetAttributeOfType<CurrencyAttribute>().Currency.ToString().ToUpper()} " +
                                          $"given to {transaction.Alias.Name} ({transaction.Memo})?",
                Context.Channel, async () =>
            {
                _epgpService.DeleteTransaction(transaction.Id);
                await Task.CompletedTask;
            }, async () => { await ReplyAsync("Operation cancelled."); });
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
            await _userService.TryAddUser(id, Context.Guild);
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
            _aliasService.AddAlias(alias);
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

            _aliasService.AddAlias(alias);
            _epgpService.Set(aliasName, ep, gp, "Alias initialized");
            await ReplyAsync($"New alias add to {user.GetAliasName()}: \"{aliasName} : {userClass}\"");
        }

        [Command("deletealias", RunMode = RunMode.Async)]
        [Summary("Deletes the alias from the EPGP database (but retains the user)")]
        [Remarks("delete Baxterdruid")]
        [RequiresBotAdmin]
        public async Task DeleteAlias(string aliasName)
        {
            await _queryService.SendQuery(
                $"Are you sure you want to delete all record of {aliasName}? The user record will be retained, but all record of this alias will be purged. This can not be undone.",
                Context.Channel,
                async () =>
                {
                    try
                    {
                        _aliasService.DeleteAlias(aliasName);
                    }
                    catch (Exception ex)
                    {
                        await ReplyAsync($"Unable to successfully remove alias: {ex.Message}");
                        return;
                    }

                    await ReplyAsync("Alias removed successfully.");
                },
                async () => { await ReplyAsync("Operation cancelled"); });
        }

        [Command("deleteuser", RunMode = RunMode.Async)]
        [Summary("Deletes the user (and all aliases) from the EPGP database")]
        [Remarks("delete @Marathonz")]
        [RequiresBotAdmin]
        public async Task DeleteUser(IGuildUser user)
        {
            await _queryService.SendQuery(
                $"Are you sure you want to delete all record of {user.GetAliasName()} and their aliases? This can not be undone.",
                Context.Channel,
                async () =>
                {
                    if (await _userService.TryDeleteUser(user.Id))
                    {
                        await ReplyAsync("User removed successfully.");
                        return;

                    }
                    await ReplyAsync($"Unable to successfully remove user");

                },
                async () => { await ReplyAsync("Operation cancelled"); });
        }
        [Command("remove_records", RunMode = RunMode.Async)]
        [RequiresBotAdmin]
        public async Task RemoveRecords(int day, int month, int year, int hour, int minute)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, 0);
            await _queryService.SendQuery($"Delete all records after {dateTime}?", Context.Channel, async () =>
                {
                    var transactions = (_dbContext.EpgpTransactions as IQueryable<EpgpTransaction>).Where(t => t.TransactionDateTime >= dateTime)
                        .Include(t => t.Alias)
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
                        _dbContext.EpgpTransactions.Remove(transaction);
                    }

                    _dbContext.SaveChanges();
                    return;
                },
                async () => await ReplyAsync("Operation cancelled"));
        }
    }
}