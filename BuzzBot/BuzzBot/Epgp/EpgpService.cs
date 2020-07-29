using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBotData.Data;
using BuzzBotData.Repositories;

namespace BuzzBot.Epgp
{
    public class EpgpService : IEpgpService
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IEpgpConfigurationService _configurationService;
        public const string EpFlag = "-ep";
        public const string GpFlag = "-gp";
        private DateTime _lastDecayApplied;

        public EpgpService(EpgpRepository epgpRepository, IEpgpConfigurationService configurationService)
        {
            _epgpRepository = epgpRepository;
            _configurationService = configurationService;
            configurationService.ConfigurationChanged += ConfigurationChanged;
            Task.Factory.StartNew(DecayProcess);
        }

        private async Task DecayProcess()
        {
            var transactions = _epgpRepository.GetTransactions().OrderByDescending(t => t.TransactionDateTime);
            var lastDecay = transactions.FirstOrDefault(t => t.TransactionType == TransactionType.GpDecay);
            _lastDecayApplied = lastDecay?.TransactionDateTime.ToEasternTime() ?? DateTime.Now - TimeSpan.FromDays(7);
            while (true)
            {
                await Task.Delay(TimeSpan.FromHours(6));
                var time = DateTime.Now.ToEasternTime();
                var config = _configurationService.GetConfiguration();
                if (time.DayOfWeek != config.DecayDayOfWeek) continue;
                if (time - _lastDecayApplied < TimeSpan.FromHours(24)) continue;
                Decay(config.DecayPercentage);
            }
        }

        private void ConfigurationChanged(object? _, EventArgs ___)
        {
            var config = _configurationService.GetConfiguration();
            var needsEpAdjustment = _epgpRepository.GetAliases().Where(a => a.EffortPoints < config.EpMinimum);
            foreach (var alias in needsEpAdjustment)
            {
                Set(alias.Name, config.EpMinimum, alias.GearPoints, "Configuration update (EP minimum)");
            }
            var needsGpAdjustment = _epgpRepository.GetAliases().Where(a => a.GearPoints < config.GpMinimum);
            foreach (var alias in needsGpAdjustment)
            {
                Set(alias.Name, alias.EffortPoints, config.GpMinimum, "Configuration update (GP minimum)");
            }
        }

        public void Ep(string aliasName, int value, string memo, TransactionType type = TransactionType.EpManual)
        {
            var config = _configurationService.GetConfiguration();
            var alias = _epgpRepository.GetAlias(aliasName);
            var change = alias.EffortPoints + value < config.EpMinimum ? alias.EffortPoints - config.EpMinimum : value;
            var transaction = new EpgpTransaction
            {
                AliasId = alias.Id,
                Memo = memo,
                TransactionDateTime = DateTime.UtcNow,
                TransactionType = type,
                Value = change
            };
            _epgpRepository.PostTransaction(transaction);
            _epgpRepository.Save();
        }

        public void Gp(string aliasName, int value, string memo, TransactionType type = TransactionType.GpManual)
        {
            var alias = _epgpRepository.GetAlias(aliasName);
            Gp(alias, value, memo, type);
        }

        public void Gp(EpgpAlias alias, int value, string memo, TransactionType type = TransactionType.GpManual)
        {
            var config = _configurationService.GetConfiguration();
            var change = alias.GearPoints + value < config.GpMinimum ? alias.GearPoints - config.GpMinimum : value;
            var transaction = new EpgpTransaction
            {
                AliasId = alias.Id,
                Memo = memo,
                TransactionDateTime = DateTime.UtcNow,
                TransactionType = type,
                Value = change
            };
            _epgpRepository.PostTransaction(transaction);
            _epgpRepository.Save();
        }

        public bool Set([NotNull] string aliasName, int ep, int gp, string memo = "Manual Value Correction")
        {
            var alias = _epgpRepository.GetAlias(aliasName);
            if (alias == null) return false;
            var epChange = ep - alias.EffortPoints;
            var gpChange = gp - alias.GearPoints;
            if (epChange != 0)
            {
                Ep(aliasName, epChange, memo);
            }
            if (gpChange != 0)
            {
                Gp(aliasName, gpChange, memo);
            }

            return true;

        }

        public void Decay(int decayPercent)
            => Decay(decayPercent, null);

        public void Decay(int decayPercent, string epgpFlag)
        {
            var asPercent = (double)decayPercent / 100;
            var aliases = _epgpRepository.GetAliases();
            foreach (var alias in aliases)
            {
                var epDecay = (int)Math.Round(alias.EffortPoints * asPercent, MidpointRounding.AwayFromZero);
                var gpDecay = (int)Math.Round(alias.GearPoints * asPercent, MidpointRounding.AwayFromZero);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(EpFlag))
                    Ep(alias.Name, epDecay, $"{decayPercent}% Decay", TransactionType.EpDecay);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(GpFlag))
                    Gp(alias.Name, gpDecay, $"{decayPercent}% Decay", TransactionType.GpDecay);
            }
        }
    }
}