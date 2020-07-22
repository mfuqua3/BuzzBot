using Discord;

namespace BuzzBot.Discord.Extensions
{
    public static class GuildUserExtensions
    {
        public static string GetAliasName(this IGuildUser user)
        {
            return !string.IsNullOrEmpty(user.Nickname) ? user.Nickname : user.Username;
        }
    }
}