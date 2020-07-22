using System.Linq;
using Discord.WebSocket;

namespace BuzzBot.Discord.Extensions
{
    public static class SocketReactionExtensions
    {
        public static bool ValidateReaction(this SocketReaction reaction, params string[] validReactionStrings)
        {
            return !reaction.User.Value.IsBot && validReactionStrings.Any(vrs => vrs.Equals(reaction.Emote.Name));
        }
    }
}