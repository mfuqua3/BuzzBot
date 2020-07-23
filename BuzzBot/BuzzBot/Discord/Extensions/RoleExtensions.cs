using BuzzBot.Epgp;
using Discord;

namespace BuzzBot.Discord.Extensions
{
    public static class RoleExtensions
    {
        public static bool TryParseClass(this IRole role, out WowClass wowClass)
            => role.Name.TryParseClass(out wowClass);
    }
}