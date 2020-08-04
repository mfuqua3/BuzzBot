using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuzzBot.Discord.Utility;
using BuzzBotData.Data;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Discord.Services
{
    public interface IAuditService
    {
        Task Audit(string aliasId, IMessageChannel messageChannel);
    }

    public class AuditService : IAuditService
    {
        private readonly IPageService _pageService;
        private readonly BuzzBotDbContext _dbContext;

        public AuditService(IPageService pageService, BuzzBotDbContext dbContext)
        {
            _pageService = pageService;
            _dbContext = dbContext;
        }
        public async Task Audit(string aliasId, IMessageChannel messageChannel)
        {
            var transactions = _dbContext.Aliases.Include(a => a.Transactions).FirstOrDefault(a => a.Name == aliasId)?
                .Transactions.OrderByDescending(t => t.TransactionDateTime).ToList();
            //var transactions = _epgpRepository.GetTransactions(aliasId).OrderByDescending(t => t.TransactionDateTime).ToList();
            var builder = new PageFormatBuilder()
                .AddColumn("Time (UTC)")
                .AddColumn("Type")
                .AddColumn("Currency")
                .AddColumn("Amount")
                .AddColumn("Memo")
                .AlternateRowColors();

            if (transactions==null || !transactions.Any())
            {
                builder.AddRow(new[] { "", "", "", "", "" });
            }

            foreach (var transaction in transactions)
            {
                var memoToPrint = transaction.Memo;
                var currency =
                    transaction.TransactionType.ToString().Contains("EP", StringComparison.CurrentCultureIgnoreCase)
                        ? "EP"
                        : "GP";
                if (transaction.Memo.Length > 28)
                {
                    memoToPrint = $"{transaction.Memo.Substring(0, 25)}...";
                }

                builder.AddRow(new[]
                {
                    $"{transaction.TransactionDateTime.ToShortDateString()}:{transaction.TransactionDateTime.ToShortTimeString()}",
                    $"{transaction.TransactionType}",
                    $"{currency}",
                    transaction.Value.ToString(),
                    memoToPrint
                });
            }

            var format = builder.Build();
            var header = $"{format.HeaderLine}\n{format.HorizontalRule}";
            await _pageService.SendPages(messageChannel, header, format.ContentLines.ToArray());
        }
    }
}