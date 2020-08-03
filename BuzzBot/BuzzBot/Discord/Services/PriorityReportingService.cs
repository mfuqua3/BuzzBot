using BuzzBotData.Data;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuzzBot.Discord.Utility;

namespace BuzzBot.Discord.Services
{
    public interface IPriorityReportingService
    {
        Task ReportAll(IMessageChannel messageChannel);
        Task ReportAliases(IMessageChannel messageChannel, params string[] names);
    }

    public class PriorityReportingService : IPriorityReportingService
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IPageService _pageService;

        public PriorityReportingService(EpgpRepository epgpRepository, IPageService pageService)
        {
            _epgpRepository = epgpRepository;
            _pageService = pageService;
        }

        public async Task ReportAll(IMessageChannel messageChannel)
        {
            var aliases = _epgpRepository.GetAliases()
                .Where(a => a != null).
                OrderByDescending(a => (double)a.EffortPoints / a.GearPoints);
            await Report(messageChannel, aliases);
        }

        public async Task ReportAliases(IMessageChannel messageChannel, params string[] names)
        {
            var aliases =  names
                    .Select(name => _epgpRepository.GetAlias(name))
                    .Where(a => a != null)
                    .OrderByDescending(a => (double)a.EffortPoints / a.GearPoints);
            await Report(messageChannel, aliases);
        }

        private async Task Report(IMessageChannel messageChannel, IEnumerable<EpgpAlias> aliases)
        {
            var formatBuilder = new PageFormatBuilder()
                .AlternateRowColors()
                .AddColumn("Member")
                .AddColumn("EP")
                .AddColumn("GP")
                .AddColumn("PR");
            //var header = $"{Header}\n{HorizontalRule}";
            //var formattedAliases = new List<string>();
            //var alternate = false;
            foreach (var alias in aliases)
            {
                formatBuilder.AddRow(new[]
                {
                    alias.Name, 
                    alias.EffortPoints.ToString(), 
                    alias.GearPoints.ToString(),
                    ((double) alias.EffortPoints / alias.GearPoints).ToString("F2")
                });
            }

            await _pageService.SendPages(messageChannel, formatBuilder.Build());
        }
    }
}