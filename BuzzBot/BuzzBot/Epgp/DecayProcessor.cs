using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBotData.Data;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Epgp
{
    public class DecayProcessor
    {
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly IConfiguration _configuration;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private DateTime _lastDecayDateTime;


        public DecayProcessor(IEpgpConfigurationService epgpConfigurationService, IConfiguration configuration)
        {
            _epgpConfigurationService = epgpConfigurationService;
            _configuration = configuration;
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
            await using(var context = new BuzzBotDbContext(_configuration))
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
                Decay(20);
                _lastDecayDateTime = DateTime.UtcNow;
            }
        }

        public void Decay(int decayPercent)
        {
            var asPercent = (double)decayPercent / 100;
            using var dbContext = new BuzzBotDbContext(_configuration);
            var aliases = dbContext.Aliases.ToList();
            foreach (var alias in aliases)
            {
                var epDecay = -(int)Math.Round(alias.EffortPoints * asPercent, MidpointRounding.AwayFromZero);
                var gpDecay = -(int)Math.Round(alias.GearPoints * asPercent, MidpointRounding.AwayFromZero);
                var epTransaction =
                    GetTransaction(alias, epDecay, $"{decayPercent}% Decay", TransactionType.EpDecay);
                var gpTransaction =
                    GetTransaction(alias, gpDecay, $"{decayPercent}% Decay", TransactionType.GpDecay);
                PostTransaction(alias, epTransaction, dbContext);
                PostTransaction(alias, gpTransaction, dbContext);
            }
        }

        private EpgpTransaction GetTransaction(EpgpAlias alias, int value, string memo, TransactionType transactionType)
        {

            var config = _epgpConfigurationService.GetConfiguration();
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

            dbContext.SaveChanges();
        }
    }
}