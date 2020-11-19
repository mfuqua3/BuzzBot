using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Utility;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Epgp
{
    public class DecayProcessor : IDecayProcessor
    {
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly IConfiguration _configuration;
        private readonly IEpgpTransactionFactory _epgpTransactionFactory;
        private readonly IBuzzBotDbContextFactory _buzzBotDbContextFactory;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private DateTime _lastDecayDateTime;


        public DecayProcessor(IEpgpConfigurationService epgpConfigurationService, IConfiguration configuration, IEpgpTransactionFactory epgpTransactionFactory, IBuzzBotDbContextFactory buzzBotDbContextFactory)
        {
            _epgpConfigurationService = epgpConfigurationService;
            _configuration = configuration;
            _epgpTransactionFactory = epgpTransactionFactory;
            _buzzBotDbContextFactory = buzzBotDbContextFactory;
        }

        public void Initialize()
        {
            Task.Factory.StartNew(
                RunDecay,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task RunDecay()
        {
            await using (var context = new BuzzBotDbContext(_configuration))
            {
                var lastDecay = context.EpgpTransactions.AsQueryable().OrderByDescending(t => t.TransactionDateTime)
                    .FirstOrDefault(t => t.TransactionType == TransactionType.EpDecay);
                _lastDecayDateTime = lastDecay?.TransactionDateTime ?? DateTime.MinValue;
            }
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(6));
                var config = _epgpConfigurationService.GetConfiguration();
                if (DateTime.Now.DayOfWeek != config.DecayDayOfWeek) continue;
                if (DateTime.UtcNow - _lastDecayDateTime < TimeSpan.FromHours(24)) continue;
                Decay(config);
                _lastDecayDateTime = DateTime.UtcNow;
            }
        }

        public void Decay(EpgpConfiguration config)
        {
            var epDecayPercentage = AsPercentage(config.EpDecayPercentage);
            using var dbContext = _buzzBotDbContextFactory.GetNew();
            foreach (var data in GetAliasData(dbContext, config))
            {
                var alias = data.Alias;
                var epDecay = -(int)Math.Round(alias.EffortPoints * epDecayPercentage, MidpointRounding.AwayFromZero);
                var gpDecay = -(int)Math.Round(alias.GearPoints * AsPercentage(data.GpDecayPercent), MidpointRounding.AwayFromZero);
                var epTransaction =
                    _epgpTransactionFactory.GetTransaction(alias, epDecay, $"{config.EpDecayPercentage}% EP Decay", TransactionType.EpDecay);
                var gpTransaction =
                    _epgpTransactionFactory.GetTransaction(alias, gpDecay, $"{data.GpDecayPercent}% GP Decay", TransactionType.GpDecay);
                PostTransaction(alias, epTransaction, dbContext);
                PostTransaction(alias, gpTransaction, dbContext);
            }

            dbContext.SaveChanges();
        }

        private List<AliasDecayData> GetAliasData(BuzzBotDbContext dbContext, EpgpConfiguration config)
        {
            //Get all EPGP characters in system
            var aliases = dbContext.Aliases.ToList();
            //If bot is not configured to "use earned decay", short circuit the method
            if (config.UseEarnedGpDecay != 1)
            {
                return aliases.Select(a =>
                    new AliasDecayData()
                    {
                        Alias = a,
                        GpDecayPercent = config.GpDecayPercentage
                    }).ToList();
            }
            //Get all raid data from the previous 7 days
            var raids = dbContext.Raids
                .Include(r => r.Participants)
                .ThenInclude(p => p.Alias)
                .ToList();
            var previousWeeksRaids = raids.Where(r => (DateTime.UtcNow - r.EndTime) < TimeSpan.FromDays(7))
            .ToList();
            //If no raid data is found, short circuit the method
            if (!previousWeeksRaids.Any())
            {
                return aliases.Select(a =>
                    new AliasDecayData()
                    {
                        Alias = a,
                        GpDecayPercent = config.GpDecayPercentage
                    }).ToList();
            }
            //Aggregate all unique participants from the week's raids
            var participants =
                previousWeeksRaids
                    .SelectMany(r => r.Participants)
                    .Select(p => p.Alias.UserId)
                    .Distinct()
                    .ToList();
            //If no participants are found, short circuit the method
            if (!participants.Any())
            {
                return aliases.Select(a =>
                    new AliasDecayData()
                    {
                        Alias = a,
                        GpDecayPercent = config.GpDecayPercentage
                    }).ToList();
            }
            var returnList = new List<AliasDecayData>();
            //Iterate all aliases
            foreach (var alias in aliases)
            {
                var returnVal = new AliasDecayData
                {
                    Alias = alias,
                    GpDecayPercent = config.GpDecayPercentage - config.EarnedGpDecayValue
                };
                //If an alias is non-primary (alt) or did not participate in a raid, award minimum GP decay
                if (!alias.IsPrimary || !participants.Contains(alias.UserId))
                {
                    returnList.Add(returnVal);
                    continue;
                }
                //Otherwise, award maximum GP decay
                returnVal.GpDecayPercent += config.EarnedGpDecayValue;
                returnList.Add(returnVal);
            }

            return returnList;
        }

        private double AsPercentage(int value) => (double)value / 100;

        private void PostTransaction(EpgpAlias alias, EpgpTransaction transaction, BuzzBotDbContext dbContext)
        {
            dbContext.EpgpTransactions.Add(transaction);
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
        }

        private class AliasDecayData
        {
            public EpgpAlias Alias { get; set; }
            public int GpDecayPercent { get; set; }
        }
    }
}