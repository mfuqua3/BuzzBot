using System;
using System.Collections.Generic;
using System.Linq;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBotData.Data;

namespace BuzzBot.Discord.Extensions
{
    public static class WowClassExtensions
    {
        private static readonly Dictionary<Class, WowClass> ConversionDictionary = new Dictionary<Class, WowClass>
        {
            {Class.Warlock, WowClass.Warlock },
            {Class.Mage, WowClass.Mage },
            {Class.Priest, WowClass.Priest },
            {Class.Druid, WowClass.Druid },
            {Class.Rogue, WowClass.Rogue },
            {Class.Hunter, WowClass.Hunter },
            {Class.Shaman, WowClass.Shaman },
            {Class.Paladin, WowClass.Paladin },
            {Class.Warrior, WowClass.Warrior },
            {Class.Undefined, WowClass.Unknown }
        };

        private static readonly Dictionary<WowClass, string> EmoteDictionary = new Dictionary<WowClass, string>
        {
            {WowClass.Warrior, EmbedConstants.WarriorEmoteName},
            {WowClass.Paladin, EmbedConstants.PaladinEmoteName},
            {WowClass.Hunter, EmbedConstants.HunterEmoteName},
            {WowClass.Shaman, EmbedConstants.ShamanEmoteName},
            {WowClass.Druid, EmbedConstants.DruidEmoteName},
            {WowClass.Rogue, EmbedConstants.RogueEmoteName},
            {WowClass.Priest, EmbedConstants.PriestEmoteName},
            {WowClass.Warlock, EmbedConstants.WarlockEmoteName},
            {WowClass.Mage, EmbedConstants.MageEmoteName},
            {WowClass.Unknown, string.Empty},
        };

        public static string GetEmoteName(this WowClass wowClass)
        {
            return EmoteDictionary[wowClass];
        }
        public static string GetEmoteName(this Class domainClass)
        {
            return domainClass.ToWowClass().GetEmoteName();
        }
        public static WowClass ToWowClass(this Class domainClass)
        {
            return ConversionDictionary[domainClass];
        }
        public static Class ToDomainClass(this WowClass wowClass)
        {
            return ConversionDictionary.ToDictionary(kvp => kvp.Value, kvp => kvp.Key)[wowClass];
        }
    }
}