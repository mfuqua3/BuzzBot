using System;
using System.Collections.Generic;
using System.Linq;
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