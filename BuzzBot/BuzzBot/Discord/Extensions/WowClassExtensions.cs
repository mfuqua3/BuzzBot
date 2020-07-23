using System;
using BuzzBot.Epgp;
using BuzzBotData.Data;

namespace BuzzBot.Discord.Extensions
{
    public static class WowClassExtensions
    {
        public static Class ToDomainClass(this WowClass wowClass)
        {
            switch (wowClass)
            {
                case WowClass.Unknown:
                    return Class.Undefined;
                case WowClass.Warrior:
                    return Class.Warrior;
                case WowClass.Paladin:
                    return Class.Paladin;
                case WowClass.Hunter:
                    return Class.Hunter;
                case WowClass.Shaman:
                    return Class.Shaman;
                case WowClass.Rogue:
                    return Class.Rogue;
                case WowClass.Druid:
                    return Class.Druid;
                case WowClass.Warlock:
                    return Class.Warlock;
                case WowClass.Mage:
                    return Class.Mage;
                case WowClass.Priest:
                    return Class.Priest;
                default:
                    throw new ArgumentOutOfRangeException(nameof(wowClass), wowClass, null);
            }
        }
    }
}