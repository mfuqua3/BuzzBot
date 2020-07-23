using BuzzBot.Epgp;
using Discord;

namespace BuzzBot.Discord.Extensions
{
    public static class GuildUserExtensions
    {
        public static string GetAliasName(this IGuildUser user)
        {
            return !string.IsNullOrEmpty(user.Nickname) ? user.Nickname : user.Username;
        }

        public static WowClass GetClass(this IGuildUser guildUser)
        {
            var guild = guildUser.Guild;
            foreach (var roleId in guildUser.RoleIds)
            {
                var role = guild.GetRole(roleId);
                if (role.TryParseClass(out var wowClass))
                    return wowClass;
            }

            return WowClass.Unknown;
        }
    }
}