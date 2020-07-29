using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public interface IQueryService
    {
        Task<int> SendOptionSelectQuery<T>(string query, List<T> options, Func<T, string> optionQueryBuilder, IMessageChannel channel, CancellationToken token);
        Task SendQuery(string queryString, IMessageChannel channel, Func<Task> onConfirm, Func<Task> onCancel);
    }

    public class QueryService : IQueryService
    {
        public const string Confirm = @"✔️";
        public const string Cancel = @"❌";

        public const string One = @"1️⃣";
        public const string Two = @"2️⃣";
        public const string Three = @"3️⃣";
        public const string Four = @"4️⃣";
        public const string Five = @"5️⃣";

        private const int SupportedQueryOptions = 5;

        private readonly Dictionary<int, string> _optionsDictionary = new Dictionary<int, string>
        {
            {0,One},
            {1,Two},
            {2,Three},
            {3,Four},
            {4,Five}
        };

        private readonly Dictionary<string, int> _resultDictionary;

        private readonly ConcurrentDictionary<ulong, Query> _activeQueries = new ConcurrentDictionary<ulong, Query>();

        public QueryService(DiscordSocketClient discordClient)
        {
            _resultDictionary = new Dictionary<string, int>
            {
                {Cancel, 0 },
                {Confirm,1 }
            };
            _resultDictionary = _resultDictionary.Merge(_optionsDictionary.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
            discordClient.ReactionAdded += ReactionAdded;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        {
            if (!reaction.ValidateReaction(_resultDictionary.Keys.ToArray()) || !_activeQueries.ContainsKey(reaction.MessageId)) return;
            if (!_activeQueries.TryRemove(reaction.MessageId, out var query)) return;
            query.QueryTcs.TrySetResult(_resultDictionary[reaction.Emote.Name]);
        }

        public async Task<int> SendOptionSelectQuery<T>(string query, List<T> options, Func<T, string> optionQueryBuilder, IMessageChannel channel, CancellationToken token)
        {
            if (!options.Any()) return -1;
            if (options.Count == 1) return 1;
            var truncated = options.Count > SupportedQueryOptions;
            var querySb = new StringBuilder();
            querySb.AppendLine(query);
            if (truncated)
            {
                querySb.AppendLine(
                    $"List has been truncated to {SupportedQueryOptions} options, out of {options.Count} total. Consider revising your query.");
            }
            for (int i = 0; i < options.Count; i++)
            {
                if (i == SupportedQueryOptions) break;
                var line = $"{_optionsDictionary[i]} - {optionQueryBuilder(options[i])}";
                querySb.AppendLine(line);
            }

            querySb.Append($"Please select below, {Cancel} to cancel:");

            var queryObj = new Query(TimeSpan.FromMinutes(1), channel.Id);
            var embedBuilder = new EmbedBuilder();
            embedBuilder.AddField("Query", querySb.ToString());
            var message = await channel.SendMessageAsync("", false, embedBuilder.Build());
            if (!_activeQueries.TryAdd(message.Id, queryObj)) return -1;

            for (int i = 0; i < options.Count; i++)
            {
                if (i == SupportedQueryOptions) break;
                await message.AddReactionAsync(new Emoji(_optionsDictionary[i]));
            }

            await message.AddReactionAsync(new Emoji(Cancel));
            return await AwaitQuery(queryObj);
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
            var result = await AwaitQuery(query);
            if (result == 1) await onConfirm();
            if (result == 0) await onCancel();
        }

        private async Task<int> AwaitQuery(Query query)
        {
            int result;
            try
            {
                result = await query.QueryTcs.Task;
            }
            catch (TaskCanceledException)
            {
                result = -1;
            }

            _activeQueries.TryRemove(query.Key, out _);
            return result;
        }

        private class Query
        {
            public Query(TimeSpan timeout, ulong key)
            {
                Key = key;
                var cts = new CancellationTokenSource(timeout);
                QueryTcs = new TaskCompletionSource<int>(cts.Token);
            }
            public ulong Key { get; }
            public TaskCompletionSource<int> QueryTcs { get; }
        }
    }
}