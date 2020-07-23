using System;
using BuzzBot.Epgp;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;

namespace BuzzBot.Discord.Extensions
{
    public static class StringExtensions
    {
        public static bool TryParseClass(this string classString, out WowClass wowClass)
        {
            wowClass = WowClass.Unknown;
            switch (classString)
            {
                case { } s when s.Equals("warrior", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Warrior;
                    break;
                case { } s when s.Equals("paladin", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Paladin;
                    break;
                case { } s when s.Equals("hunter", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Hunter;
                    break;
                case { } s when s.Equals("shaman", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Shaman;
                    break;
                case { } s when s.Equals("rogue", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Rogue;
                    break;
                case { } s when s.Equals("druid", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Druid;
                    break;
                case { } s when s.Equals("warlock", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Warlock;
                    break;
                case { } s when s.Equals("priest", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Priest;
                    break;
                case { } s when s.Equals("mage", StringComparison.CurrentCultureIgnoreCase):
                    wowClass = WowClass.Mage;
                    break;
            }

            return wowClass != WowClass.Unknown;
        }
    }
}