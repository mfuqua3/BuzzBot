using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        Task Audit(string aliasId, IMessageChannel messageChannel, bool isAdmin = false);
        void ValidateTransactionHistory(Guid aliasId);
        void ForceCorrect(Guid aliasId);
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
        public async Task Audit(string aliasId, IMessageChannel messageChannel, bool isAdmin = false)
        {
            var transactions = _dbContext.Aliases.Include(a => a.Transactions).FirstOrDefault(a => a.Name == aliasId)?
                .Transactions.OrderByDescending(t => t.TransactionDateTime).ToList();
            //var transactions = _epgpRepository.GetTransactions(aliasId).OrderByDescending(t => t.TransactionDateTime).ToList();
            var builder1 = new PageFormatBuilder()
                .AddColumn("Time (UTC)")
                .AddColumn("Type")
                .AddColumn("Currency")
                .AddColumn("Amount")
                .AddColumn("Memo")
                .AlternateRowColors();
            var builder2 = new PageFormatBuilder()
                .AddColumn("Time (UTC)")
                .AddColumn("Type")
                .AddColumn("Currency")
                .AddColumn("Amount")
                .AddColumn("ID")
                .AlternateRowColors();
            if (transactions == null) transactions = new List<EpgpTransaction>();
            var rows = new List<string>();
            if (!transactions.Any())
            {
                rows.AddRange(new[] { "", "", "", "", "" });
                builder1.AddRow(rows.ToArray());
                builder2.AddRow(rows.ToArray());
            }

            foreach (var transaction in transactions)
            {
                var rows1 = new List<string>();
                var rows2 = new List<string>();
                var memoToPrint = transaction.Memo;
                var currency =
                    transaction.TransactionType.ToString().Contains("EP", StringComparison.CurrentCultureIgnoreCase)
                        ? "EP"
                        : "GP";
                if (transaction.Memo.Length > 28)
                {
                    memoToPrint = $"{transaction.Memo.Substring(0, 25)}...";
                }
                rows1.AddRange(new[]
                {
                    $"{transaction.TransactionDateTime.ToShortDateString()}:{transaction.TransactionDateTime.ToShortTimeString()}",
                    $"{transaction.TransactionType}",
                    $"{currency}",
                    transaction.Value.ToString()
                });
                rows2.AddRange(new[]
                {
                    $"{transaction.TransactionDateTime.ToShortDateString()}:{transaction.TransactionDateTime.ToShortTimeString()}",
                    $"{transaction.TransactionType}",
                    $"{currency}",
                    transaction.Value.ToString()
                });
                rows1.Add(memoToPrint);
                rows2.Add(transaction.Id.ToString("N"));
                builder1.AddRow(rows1.ToArray());
                builder2.AddRow(rows2.ToArray());
            }

            var format = builder1.Build();
            if (isAdmin)
            {
                format.HasHiddenColumns = true;
                format.RevealedPageFormat = builder2.Build();
            }
            await _pageService.SendPages(messageChannel, format);
        }

        public void ForceCorrect(Guid aliasId)
        {
            var alias = _dbContext.Aliases.Include(a => a.Transactions).FirstOrDefault(a => a.Id == aliasId);
            if (alias == null) return;
            var (ep, gp) = GetEpGpFromTransactions(alias.Transactions);
            alias.EffortPoints = ep;
            alias.GearPoints = gp;
            _dbContext.SaveChanges();
        }

        private (int, int) GetEpGpFromTransactions(ICollection<EpgpTransaction> transactions)
        {
            var gp = transactions
                .Where(t => GetCurrency(t.TransactionType) == Currency.Gp)
                .Sum(t => t.Value);
            var ep = transactions
                .Where(t => GetCurrency(t.TransactionType) == Currency.Ep)
                .Sum(t => t.Value);
            return (ep, gp);
        }

        public void ValidateTransactionHistory(Guid aliasId)
        {
            var alias = _dbContext.Aliases.Include(a => a.Transactions).FirstOrDefault(a => a.Id == aliasId);
            if (alias == null) throw new InvalidOperationException("No alias could be found by that name.");
            var claimedGp = alias.GearPoints;
            var claimedEp = alias.EffortPoints;
            var (epShouldBe, gpShouldBe) = GetEpGpFromTransactions(alias.Transactions);
            var gpReconciles = claimedGp == gpShouldBe;
            var epReconciles = epShouldBe == claimedEp;
            if (epReconciles && gpReconciles) return;
            throw new ValidationException("Reconciliation of transaction history returned discrepancies. \n" +
                                          $"Record claims EP = {claimedEp}. Transaction history sums to EP = {epShouldBe}.\n" +
                                          $"Record claims GP = {claimedGp}. Transaction history sums to EP = {gpShouldBe}.");
        }

        private Currency GetCurrency(TransactionType transactionType)
        {
            switch (transactionType)
            {
                case TransactionType.EpAutomated:
                case TransactionType.EpManual:
                case TransactionType.EpDecay:
                    return Currency.Ep;
                case TransactionType.GpFromGear:
                case TransactionType.GpManual:
                case TransactionType.GpDecay:
                    return Currency.Gp;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, null);
            }
        }
    }
}