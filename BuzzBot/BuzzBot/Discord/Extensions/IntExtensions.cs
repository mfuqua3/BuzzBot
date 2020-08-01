using System;

namespace BuzzBot.Discord.Extensions
{
    public static class IntExtensions
    {
        public static string ToGoldString(this int goldValue)
        {
            var remaining = goldValue;
            var gold = (int)Math.Floor((double)goldValue / 10000);
            remaining -= gold * 10000;
            var silver = (int) Math.Floor((double) remaining / 100);
            remaining -= silver * 100;
            return $"{gold}g {silver}s {remaining}c";
        }
    }
}