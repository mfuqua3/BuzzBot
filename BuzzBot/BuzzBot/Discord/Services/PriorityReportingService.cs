using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Discord;

namespace BuzzBot.Discord.Services
{
    public class PriorityReportingService
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly PageService _pageService;
        private const string Header = @"  Member                    EP        GP        PR";
        private const string HorizontalRule = @"--------------------------------------------------";

        public PriorityReportingService(EpgpRepository epgpRepository, PageService pageService)
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
            var header = $"{Header}\n{HorizontalRule}";
            var formattedAliases = new List<string>();
            var alternate = false;
            foreach (var alias in aliases)
            {
                formattedAliases.Add(FormatAlias(alias, alternate));
                alternate = !alternate;
            }

            await _pageService.SendPages(messageChannel, header, formattedAliases.ToArray());
        }

        private string FormatAlias(EpgpAlias alias, bool alternate)
        {
            var codeSb = new StringBuilder();
            var codeIdentifier = alternate ? "+" : "-";
            var epIndex = Header.IndexOf("EP", StringComparison.Ordinal) - 2;
            var gpIndex = Header.IndexOf("GP", StringComparison.Ordinal) - 2;
            var prIndex = Header.IndexOf("PR", StringComparison.Ordinal) - 2;
            codeSb.Append($"{codeIdentifier} ");
            var aliasInfoSb = new StringBuilder();
            aliasInfoSb.Append(alias.Name);
            while (aliasInfoSb.Length < epIndex)
            {
                aliasInfoSb.Append(' ');
            }

            if (alias.EffortPoints < 100) aliasInfoSb.Append(' ');
            if (alias.EffortPoints < 10) aliasInfoSb.Append(' ');
            aliasInfoSb.Append(alias.EffortPoints);


            while (aliasInfoSb.Length < gpIndex)
            {
                aliasInfoSb.Append(' ');
            }

            if (alias.GearPoints < 100) aliasInfoSb.Append(' ');
            if (alias.GearPoints < 10) aliasInfoSb.Append(' ');
            aliasInfoSb.Append(alias.GearPoints);


            while (aliasInfoSb.Length < prIndex)
            {
                aliasInfoSb.Append(' ');
            }

            var priority = (double)alias.EffortPoints / alias.GearPoints;
            aliasInfoSb.Append(priority.ToString("F2"));
            codeSb.Append(aliasInfoSb.ToString());
            return codeSb.ToString();
        }


    }
}