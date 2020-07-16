using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.ClassicGuildBank.Buzz;
using BuzzBot.Discord.Services;
using BuzzBotData.Repositories;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    [Group(GroupName)]
    public class BankModule : ModuleBase<SocketCommandContext>
    {
        private readonly ClassicGuildBankClient _client;
        private readonly CommandService _commandService;
        private readonly ItemRequestService _itemRequestService;
        private readonly AdministrationService _administrationService;
        private readonly GuildBankRepository _bankRepository;
        public const string GroupName = "bank";
        public BankModule(ClassicGuildBankClient client, CommandService commandService, ItemRequestService itemRequestService, AdministrationService administrationService, GuildBankRepository bankRepository)
        {
            _client = client;
            _commandService = commandService;
            _itemRequestService = itemRequestService;
            _administrationService = administrationService;
            _bankRepository = bankRepository;
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

        [Command("search")]
        [Summary("Searches the guild bank for the specified item")]
        [Alias("query", "find")]
        public async Task Search([Remainder] [Summary("Item to search")]
            string item)
        {
            if (item.Length < 3)
            {
                await ReplyAsync("Query string must be at least three characters");
                return;
            }
            var result = await _client.QueryItem(item);
            if (!result.Any())
            {
                await ReplyAsync($"No items called \"{item}\" were found in the Buzz guild bank");
            }
            var resultSb = new StringBuilder();
            var total = result.Select(r => r.Quantity).Sum();
            resultSb.AppendLine($"{total} item(s) found across {result.Count} character(s)");
            foreach (var queryResult in result)
            {
                resultSb.AppendLine($"{queryResult.CharacterName} : {queryResult.Quantity} {queryResult.ItemName} total stored");
            }

            await ReplyAsync(resultSb.ToString());
        }
        [Command("request")]
        [Summary("Requests the specified item from the guild bank.")]
        public async Task Request([Remainder] [Summary("Item to search")]
            string item)
        {
            var result = await _client.QueryItem(item);
            if (!result.Any())
            {
                await ReplyAsync($"No items called \"{item}\" were found in the Buzz guild bank");
            }
            var total = result.Select(r => r.Quantity).Sum();
            Task.Run(() => _itemRequestService.ProcessRequest(Context, item, total));

        }

        [Command("gold")]
        [Summary("Returns the total gold value available in the guild bank.")]
        public async Task Gold()
        {
            var totalCopper = _bankRepository.GetTotalGold();
            var gold = (int)Math.Floor((double) totalCopper / 10000);
            totalCopper -= gold*10000;
            var silver = (int)Math.Floor((double) totalCopper / 100);
            totalCopper -= silver * 100;
            await ReplyAsync($"Total gold across all characters: {gold}g{silver}s{totalCopper}c");
        }

        [RequiresBotAdmin]
        [Command("sync")]
        [Summary("Syncs the bot's guild bank database to the ClassicGuildBank server.")]
        public async Task Sync()
        {
            await ReplyAsync("Querying server for guilds registered to bot owner");
            var guilds = await _client.GetGuilds();
            var replySb = new StringBuilder();
            if (guilds.Count == 0)
            {
                await ReplyAsync("No guilds could be found");
            }

            for (var i = 0; i < guilds.Count; i++)
            {
                var guild = guilds[i];
                replySb.Append(guild.Name);
                if (i == guilds.Count - 1)
                {
                    replySb.Append(".");
                    break;
                }

                replySb.Append(", ");
            }

            await ReplyAsync($"{guilds.Count} guild(s) successfully pulled from server: {replySb.ToString()}");
            foreach (var guild in guilds)
            {
                var characters = await _client.GetCharacters(guild._Id);
                guild.Characters = characters;
                _bankRepository.AddOrUpdateGuild(guild);
            }

            await ReplyAsync("Database successfully synced with ClassicGuildBank server");
        }
    }
}