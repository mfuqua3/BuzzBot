using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Epgp
{
    public class EpgpService : IEpgpService, IDisposable
    {
        private readonly IEpgpConfigurationService _configurationService;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IRaidRepository _raidRepository;
        private readonly IAliasEventAlerter _aliasEventAlerter;
        private readonly IAliasService _aliasService;
        private readonly BuzzBotDbContext _dbContext;
        private IEpgpTransactionFactory _epgpTransactionFactory;
        public const string EpFlag = "-ep";
        public const string GpFlag = "-gp";

        public EpgpService(IEpgpConfigurationService configurationService,
            IEpgpCalculator epgpCalculator,
            IRaidRepository raidRepository,
            IAliasEventAlerter aliasEventAlerter,
            IAliasService aliasService,
            BuzzBotDbContext dbContext, 
            IEpgpTransactionFactory epgpTransactionFactory)
        {
            _configurationService = configurationService;
            _epgpCalculator = epgpCalculator;
            _raidRepository = raidRepository;
            _aliasEventAlerter = aliasEventAlerter;
            _aliasService = aliasService;
            _dbContext = dbContext;
            _epgpTransactionFactory = epgpTransactionFactory;
            aliasEventAlerter.AliasAdded += AliasAdded;
        }

        private void AliasAdded(object? sender, EpgpAlias e)
        {
            var config = _configurationService.GetConfiguration();
            var ep = config.EpMinimum;
            var gp = config.GpMinimum;
            Set(e.Name, ep, gp, "User initialization");
        }

        public void Ep(string aliasName, int value, string memo, TransactionType type = TransactionType.EpManual)
        {
            var alias = _aliasService.GetAlias(aliasName);
            Ep(alias, value, memo, type);
        }

        public void Ep(EpgpAlias alias, int value, string memo, TransactionType type = TransactionType.EpManual)
        {
            var transaction = _epgpTransactionFactory.GetTransaction(alias, value, memo, type);
            PostTransaction(alias, transaction);
        }

        private void PostTransaction(EpgpAlias alias, EpgpTransaction transaction)
        {
            _dbContext.EpgpTransactions.Add(transaction);
            var currency = transaction.TransactionType.GetAttributeOfType<CurrencyAttribute>().Currency;
            switch (currency)
            {
                case Currency.Ep:
                    alias.EffortPoints += transaction.Value;
                    break;
                case Currency.Gp:
                    alias.GearPoints += transaction.Value;
                    break;
            }

            _dbContext.SaveChanges();
        }
        public void Gp(string aliasName, int value, string memo, TransactionType type = TransactionType.GpManual)
        {
            var alias = _aliasService.GetAlias(aliasName);
            Gp(alias, value, memo, type);
        }

        public void Gp(EpgpAlias alias, int value, string memo, TransactionType type = TransactionType.GpManual)
        {
            var transaction = _epgpTransactionFactory.GetTransaction(alias, value, memo, type);
            PostTransaction(alias, transaction);
        }

        public void Gp(EpgpAlias alias, Item item, string memo, int overrideGpValue = -1)
        {
            var gpValue = overrideGpValue == -1 ? _epgpCalculator.CalculateItem(item, alias.Class == Class.Hunter, false) : overrideGpValue;
            var rounded = (int)Math.Round(gpValue, MidpointRounding.AwayFromZero);
            var raid = _raidRepository.GetRaid();
            if (raid == null)
            {
                Gp(alias, rounded, memo, TransactionType.GpFromGear);
                return;
            }

            var transaction = _epgpTransactionFactory.GetTransaction(alias, (int)Math.Round(gpValue, MidpointRounding.AwayFromZero), memo, TransactionType.GpFromGear);
            _dbContext.EpgpTransactions.Add(transaction);
            alias.Transactions.Add(transaction);
            alias.GearPoints += transaction.Value;
            var raidData = _dbContext.Raids.Include(r => r.Loot).First(r => r.Id == raid.RaidObject.RaidId);
            var raidItem = new RaidItem
            {
                Id = Guid.NewGuid(),
                AwardedAliasId = alias.Id,
                ItemId = item.Id,
                RaidId = raidData.Id,
                TransactionId = transaction.Id
            };
            _dbContext.RaidItems.Add(raidItem);
            _dbContext.SaveChanges();
        }

        public void DeleteTransaction(Guid transactionId)
        {
            var transaction = _dbContext.EpgpTransactions.Include(t => t.Alias).ThenInclude(a => a.AwardedItems).FirstOrDefault(t => t.Id == transactionId);
            if (transaction == null) return;
            var raidItems = transaction.Alias.AwardedItems.Where(i => i.TransactionId == transactionId);
            switch (transaction.TransactionType)
            {
                case TransactionType.EpAutomated:
                case TransactionType.EpManual:
                case TransactionType.EpDecay:
                    transaction.Alias.EffortPoints -= transaction.Value;
                    break;
                case TransactionType.GpFromGear:
                case TransactionType.GpManual:
                case TransactionType.GpDecay:
                    transaction.Alias.GearPoints -= transaction.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var raidItem in raidItems)
            {
                _dbContext.RaidItems.Remove(raidItem);
            }

            _dbContext.EpgpTransactions.Remove(transaction);

            _dbContext.SaveChanges();
        }

        

        public bool Set([NotNull] string aliasName, int ep, int gp, string memo = "Manual Value Correction")
        {
            var alias = _aliasService.GetAlias(aliasName);
            if (alias == null) return false;
            var epChange = ep - alias.EffortPoints;
            var gpChange = gp - alias.GearPoints;
            if (epChange != 0)
            {
                Ep(alias, epChange, memo);
            }
            if (gpChange != 0)
            {
                Gp(alias, gpChange, memo);
            }

            return true;

        }

        public void Dispose()
        {
            _aliasEventAlerter.AliasAdded -= AliasAdded;
        }
    }
}