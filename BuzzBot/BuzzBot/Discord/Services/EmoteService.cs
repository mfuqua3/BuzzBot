using System;
using System.Collections.Concurrent;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public interface IEmoteService
    {
        ulong GetEmoteId(ulong serverId, string emoteName);
        string GetFullyQualifiedName(ulong serverId, string emoteName);
    }

    public class EmoteService : IEmoteService
    {
        private readonly DiscordSocketClient _discordClient;

        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<string, ulong>> _serverEmoteDictionary = new ConcurrentDictionary<ulong, ConcurrentDictionary<string, ulong>>();

        public EmoteService(DiscordSocketClient discordClient)
        {
            _discordClient = discordClient;
        }

        public ulong GetEmoteId(ulong serverId, string emoteName)
        {
            var emoteDictionary =
                _serverEmoteDictionary.GetOrAdd(serverId, id => new ConcurrentDictionary<string, ulong>());
            return emoteDictionary.GetOrAdd(emoteName, str => CreateEmoteId(serverId, str));
        }

        private ulong CreateEmoteId(ulong serverId, string emoteName)
        {
            var guild = _discordClient.GetGuild(serverId);
            if(guild == null)
                throw new InvalidOperationException("Unable to locate server with the specified server ID");
            var emotes = guild.Emotes;
            foreach (var emote in emotes)
            {
                if (emote.Name.Equals(emoteName)) return emote.Id;
            }
            throw new ArgumentException($"No emote named \"{emoteName}\" could be located in the discord server");
        }

        public string GetFullyQualifiedName(ulong serverId, string emoteName)
        {
            var id = GetEmoteId(serverId, emoteName);
            return $"<:{emoteName}:{GetEmoteId(serverId, emoteName)}>";
        }
    }
}