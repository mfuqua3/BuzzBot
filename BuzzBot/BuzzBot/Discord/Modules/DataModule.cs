using System.Linq;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Discord.Modules
{
    [Group(GroupName)]
    public class DataModule : BuzzBotModuleBase
    {
        private readonly BuzzBotDbContext _dbContext;
        private readonly IPageService _pageService;
        private IDocumentationService _documentationService;
        public const string GroupName = "data";

        public DataModule(BuzzBotDbContext dbContext, IPageService pageService, IDocumentationService documentationService)
        {
            _dbContext = dbContext;
            _pageService = pageService;
            _documentationService = documentationService;
        }


        [Command("help")]
        [Alias("?")]
        public async Task Help() => await _documentationService.SendDocumentation(await GetUserChannel(), GroupName, Context.User.Id);

        [RequiresBotAdmin]
        [Command("transactions")]
        [Summary("Reports past transactions up to the report count")]
        public async Task GetTransactions(int reportCount)
        {
            var transactions = _dbContext.EpgpTransactions.Include(t => t.Alias)
                .OrderByDescending(t => t.TransactionDateTime).Take(reportCount).ToList();
            var pageBuilder1 = new PageFormatBuilder()
                .AlternateRowColors()
                .AddColumn("Time (UTC)")
                .AddColumn("Alias")
                .AddColumn("Currency")
                .AddColumn("Value")
                .AddColumn("Memo");
            var pageBuilder2 = new PageFormatBuilder()
                .AlternateRowColors()
                .AddColumn("Time (UTC)")
                .AddColumn("Alias")
                .AddColumn("Currency")
                .AddColumn("Value")
                .AddColumn("ID");
            foreach (var epgpTransaction in transactions)
            {
                pageBuilder1.AddRow(new[]
                {
                    epgpTransaction.TransactionDateTime.ToString("g"), 
                    epgpTransaction.Alias.Name,
                    epgpTransaction.TransactionType.GetAttributeOfType<CurrencyAttribute>().Currency.ToString(),
                    epgpTransaction.Value.ToString(), 
                    epgpTransaction.Memo
                });
                pageBuilder2.AddRow(new[]
                {
                    epgpTransaction.TransactionDateTime.ToString("g"),
                    epgpTransaction.Alias.Name,
                    epgpTransaction.TransactionType.GetAttributeOfType<CurrencyAttribute>().Currency.ToString(),
                    epgpTransaction.Value.ToString(),
                    epgpTransaction.Id.ToString("N")
                });
            }

            var format1 = pageBuilder1.Build();
            var format2 = pageBuilder2.Build();
            format1.HasHiddenColumns = true;
            format1.RevealedPageFormat = format2;
            await _pageService.SendPages(await GetUserChannel(), format1);
        }
    }
}