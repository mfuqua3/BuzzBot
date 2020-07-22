using System;
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
                switch (role.Name)
                {
                    case { } s when s.Equals("warrior", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Warrior;
                    case { } s when s.Equals("paladin", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Paladin;
                    case { } s when s.Equals("hunter", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Hunter;
                    case { } s when s.Equals("shaman", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Shaman;
                    case { } s when s.Equals("rogue", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Rogue;
                    case { } s when s.Equals("druid", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Druid;
                    case { } s when s.Equals("warlock", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Warlock;
                    case { } s when s.Equals("priest", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Priest;
                    case { } s when s.Equals("mage", StringComparison.CurrentCultureIgnoreCase):
                        return WowClass.Mage;
                    default:
                        continue;
                }
            }

            return WowClass.Unknown;
        }
    }
}