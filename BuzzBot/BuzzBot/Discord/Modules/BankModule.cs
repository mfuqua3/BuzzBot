using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.ClassicGuildBank.Buzz;
using BuzzBot.Discord.Services;
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
        public const string GroupName = "bank";
        public BankModule(ClassicGuildBankClient client, CommandService commandService, ItemRequestService itemRequestService, AdministrationService administrationService)
        {
            _client = client;
            _commandService = commandService;
            _itemRequestService = itemRequestService;
            _administrationService = administrationService;
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
                resultSb.AppendLine($"{queryResult.CharacterName} : {queryResult.Quantity} total stored");
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

        [RequiresBotAdmin]
        [Command("adminTest")]
        [Summary("This is a test for admin only commands.")]
        public async Task AdminTest()
        {

        }
    }
}