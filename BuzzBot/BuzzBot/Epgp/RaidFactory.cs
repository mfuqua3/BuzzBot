using System;
using System.Linq;

namespace BuzzBot.Epgp
{
    class RaidFactory : IRaidFactory
    {
        private readonly IEpgpConfigurationService _configurationService;

        public RaidFactory(IEpgpConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }
        public EpgpRaid CreateNew(string templateId)
        {
            var template = _configurationService.GetConfiguration().Templates
                .FirstOrDefault(t => t.TemplateId.Equals(templateId));
            if (template == null) return null;
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var startTime = DateTime.UtcNow + TimeSpan.FromMinutes(template.SignUpDurationMinutes);
            var startTimeEst = TimeZoneInfo.ConvertTimeFromUtc(startTime, tzi);
            return new EpgpRaid
            {
                StartBonus = template.StartBonus,
                EndBonus = template.EndBonus,
                TimeBonusDuration = TimeSpan.FromMinutes(template.TimeBonusDurationMinutes),
                Capacity = template.RaidCapacity,
                StartTime = startTimeEst
            };
        }
    }
}