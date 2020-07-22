using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class QueryService
    {
        public const string Confirm = @"✔️";
        public const string Cancel = @"❌";
        private readonly ConcurrentDictionary<ulong, Query> _activeQueries = new ConcurrentDictionary<ulong, Query>();

        public QueryService(DiscordSocketClient discordClient)
        {
            discordClient.ReactionAdded += ReactionAdded;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        {
            if (!reaction.ValidateReaction(Confirm, Cancel) || !_activeQueries.ContainsKey(reaction.MessageId)) return;
            if (!_activeQueries.TryRemove(reaction.MessageId, out var query)) return;
            query.QueryTcs.TrySetResult(reaction.Emote.Name.Equals(Confirm));
        }

        public async Task SendQuery(string queryString, IMessageChannel channel, Func<Task> onConfirm, Func<Task> onCancel)
        {
            var query = new Query(TimeSpan.FromMinutes(1), channel.Id);
            var embedBuilder = new EmbedBuilder();
            embedBuilder.AddField("Query", queryString);
            var message = await channel.SendMessageAsync("", false, embedBuilder.Build());
            if (!_activeQueries.TryAdd(message.Id, query)) return;
            await message.AddReactionAsync(new Emoji(Confirm));
            await message.AddReactionAsync(new Emoji(Cancel));
            Task.Factory.StartNew(async () => AwaitQuery(query, onConfirm, onCancel), TaskCreationOptions.LongRunning);
        }

        private async Task AwaitQuery(Query query, Func<Task> onConfirm, Func<Task> onCancel)
        {
            bool result;
            try
            {
                result = await query.QueryTcs.Task;
            }
            catch (TaskCanceledException)
            {
                result = false;
            }

            _activeQueries.TryRemove(query.Key, out _);
            if (result) await onConfirm();
            else await onCancel();
        }

        private class Query
        {
            public Query(TimeSpan timeout, ulong key)
            {
                Key = key;
                var cts = new CancellationTokenSource(timeout);
                QueryTcs = new TaskCompletionSource<bool>(cts.Token);
            }
            public ulong Key { get; }
            public TaskCompletionSource<bool> QueryTcs { get; }
        }
    }
}