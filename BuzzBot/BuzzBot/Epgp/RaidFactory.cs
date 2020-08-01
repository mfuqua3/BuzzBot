using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.NexusHub;

namespace BuzzBot.Epgp
{
    class RaidFactory : IRaidFactory
    {
        private readonly IEpgpConfigurationService _configurationService;
        private readonly NexusHubClient _nexusHubClient;
        private const int NexusCrystalId = 20725;

        public RaidFactory(IEpgpConfigurationService configurationService, NexusHubClient nexusHubClient)
        {
            _configurationService = configurationService;
            _nexusHubClient = nexusHubClient;
        }
        public async Task<EpgpRaid> CreateNew(string templateId)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            int nexusCrystalPrice;
            try
            {

                var nexusCrystalData = await _nexusHubClient.GetItem(NexusCrystalId, cts.Token);
                nexusCrystalPrice = nexusCrystalData?.Stats?.Current?.MarketValue ?? 100000;
            }
            catch (TaskCanceledException)
            {
                nexusCrystalPrice = 100000;
            }
            var template = _configurationService.GetConfiguration().Templates
                .FirstOrDefault(t => t.TemplateId.Equals(templateId));
            if (template == null) return null;
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var startTime = DateTime.UtcNow + TimeSpan.FromMinutes(template.SignUpDurationMinutes);
            var startTimeEst = TimeZoneInfo.ConvertTimeFromUtc(startTime, tzi);
            return new EpgpRaid
            {
                NexusCrystalValue = nexusCrystalPrice,
                StartBonus = template.StartBonus,
                EndBonus = template.EndBonus,
                Duration = TimeSpan.FromMinutes(template.RaidDurationMinutes),
                TimeBonusDuration = TimeSpan.FromMinutes(template.TimeBonusDurationMinutes),
                Capacity = template.RaidCapacity,
                StartTime = startTimeEst,
                TimeBonus = template.TimeBonus
            };
        }
    }
}