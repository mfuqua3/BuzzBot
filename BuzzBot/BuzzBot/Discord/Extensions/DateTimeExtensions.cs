using System;

namespace BuzzBot.Discord.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToEasternTime(this DateTime dateTime)
        {
            var dt = dateTime.ToUniversalTime();
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var timeEst = TimeZoneInfo.ConvertTimeFromUtc(dt, tzi);
            return timeEst;
        }
    }
}