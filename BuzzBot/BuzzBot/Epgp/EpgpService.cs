﻿using System;
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
        public const string EpFlag = "-ep";
        public const string GpFlag = "-gp";

        public EpgpService(IEpgpConfigurationService configurationService, 
            IEpgpCalculator epgpCalculator, 
            IRaidRepository raidRepository, 
            IAliasEventAlerter aliasEventAlerter,
            IAliasService aliasService,
            BuzzBotDbContext dbContext)
        {
            _configurationService = configurationService;
            _epgpCalculator = epgpCalculator;
            _raidRepository = raidRepository;
            _aliasEventAlerter = aliasEventAlerter;
            _aliasService = aliasService;
            _dbContext = dbContext;
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
            var transaction = GetTransaction(alias, value, memo, type);
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
            var transaction = GetTransaction(alias, value, memo, type);
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

            var transaction = GetTransaction(alias, (int)Math.Round(gpValue, MidpointRounding.AwayFromZero), memo, TransactionType.GpFromGear);
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

        private EpgpTransaction GetTransaction(EpgpAlias alias, int value, string memo, TransactionType transactionType)
        {

            var config = _configurationService.GetConfiguration();
            var currencyType = transactionType.GetAttributeOfType<CurrencyAttribute>().Currency;
            int change;
            switch (currencyType)
            {
                case Currency.Ep:
                    change = alias.EffortPoints + value < config.EpMinimum ? alias.EffortPoints - config.EpMinimum : value;
                    break;
                case Currency.Gp:
                    change = alias.GearPoints + value < config.GpMinimum ? alias.GearPoints - config.GpMinimum : value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new EpgpTransaction
            {
                Id = Guid.NewGuid(),
                AliasId = alias.Id,
                Memo = memo,
                TransactionDateTime = DateTime.UtcNow,
                TransactionType = transactionType,
                Value = change
            };
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

        public void Decay(int decayPercent)
            => Decay(decayPercent, null);

        public void Decay(int decayPercent, string epgpFlag)
        {
            var asPercent = (double)decayPercent / 100;
            var aliases = _dbContext.Aliases.ToList();
            foreach (var alias in aliases)
            {
                var epDecay = -(int)Math.Round(alias.EffortPoints * asPercent, MidpointRounding.AwayFromZero);
                var gpDecay = -(int)Math.Round(alias.GearPoints * asPercent, MidpointRounding.AwayFromZero);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(EpFlag))
                    Ep(alias, epDecay, $"{decayPercent}% Decay", TransactionType.EpDecay);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(GpFlag))
                    Gp(alias, gpDecay, $"{decayPercent}% Decay", TransactionType.GpDecay);
            }
        }

        public void Dispose()
        {
            _aliasEventAlerter.AliasAdded -= AliasAdded;
        }
    }
}