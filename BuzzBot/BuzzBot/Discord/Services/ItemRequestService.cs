using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Discord.Services
{
    public class ItemRequestService
    {
        private readonly IConfiguration _configuration;
        private ConcurrentDictionary<ulong, TaskCompletionSource<string>> _userResponses = new ConcurrentDictionary<ulong, TaskCompletionSource<string>>();

        public ItemRequestService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void TryResolveResponse(SocketMessage message)
        {
            var user = message.Author;
            if (!_userResponses.TryGetValue(user.Id, out var tcs)) return;
            tcs.TrySetResult(message.Content);
        }
        public async Task ProcessRequest(SocketCommandContext context, string itemName, int maxQuantity)
        {
            var requestingUser = context.User as SocketGuildUser;
            if (requestingUser == null) return;
            var quantityResponseTcs = new TaskCompletionSource<string>();
            _userResponses.AddOrUpdate(requestingUser.Id, user => quantityResponseTcs, (user, source) =>
            {
                source.TrySetCanceled();
                return quantityResponseTcs;
            });
            await requestingUser.SendMessageAsync($"How many of {itemName} would you like to request? (please enter a number between 1 and {maxQuantity})");
            var response = await quantityResponseTcs.Task;
            var isInt = int.TryParse(response, out var requestQuantity);
            if (!isInt)
            {
                await requestingUser.SendMessageAsync($"Unable to parse quantity from \"{response}\"\nCancelling request.");
                return;
            }

            if (requestQuantity > maxQuantity)
            {

                await requestingUser.SendMessageAsync($"Requested quantity exceeds the maximum available. Changing request to {maxQuantity}");
                requestQuantity = maxQuantity;
            }

            var bankerId = _configuration["guildBanker"];
            var officerChannelId = _configuration["officerChannel"];
            var bankUser = context.Client.GetUser(ulong.Parse(bankerId));
            var officerChannel = context.Guild.GetTextChannel(ulong.Parse(officerChannelId));

            var embedBuilder = new EmbedBuilder();
            embedBuilder.AddField("New guild bank request",
                $"{requestingUser.Nickname} has requested ({requestQuantity}) {itemName} from the guild bank.");
            var embed = embedBuilder.Build();
            await bankUser.SendMessageAsync("", false, embed);
            await officerChannel.SendMessageAsync("", false, embed);

        }
    }
}