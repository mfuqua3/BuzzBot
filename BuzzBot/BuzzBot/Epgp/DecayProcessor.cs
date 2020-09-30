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
        private readonly IEpgpTransactionFactory _epgpTransactionFactory;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private DateTime _lastDecayDateTime;


        public DecayProcessor(IEpgpConfigurationService epgpConfigurationService, IConfiguration configuration, IEpgpTransactionFactory epgpTransactionFactory)
        {
            _epgpConfigurationService = epgpConfigurationService;
            _configuration = configuration;
            _epgpTransactionFactory = epgpTransactionFactory;
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
                    _epgpTransactionFactory.GetTransaction(alias, epDecay, $"{decayPercent}% Decay", TransactionType.EpDecay);
                var gpTransaction =
                    _epgpTransactionFactory.GetTransaction(alias, gpDecay, $"{decayPercent}% Decay", TransactionType.GpDecay);
                PostTransaction(alias, epTransaction, dbContext);
                PostTransaction(alias, gpTransaction, dbContext);
            }
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