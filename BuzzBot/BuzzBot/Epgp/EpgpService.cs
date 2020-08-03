using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public class EpgpService : IEpgpService
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IEpgpConfigurationService _configurationService;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IRaidRepository _raidRepository;
        public const string EpFlag = "-ep";
        public const string GpFlag = "-gp";
        private DateTime _lastDecayApplied;

        public EpgpService(EpgpRepository epgpRepository, IEpgpConfigurationService configurationService, IEpgpCalculator epgpCalculator, IRaidRepository raidRepository, IAliasService aliasService)
        {
            _epgpRepository = epgpRepository;
            _configurationService = configurationService;
            _epgpCalculator = epgpCalculator;
            _raidRepository = raidRepository;
            configurationService.ConfigurationChanged += ConfigurationChanged;
            aliasService.AliasAdded += AliasAdded;
            Task.Factory.StartNew(DecayProcess);
        }

        private void AliasAdded(object? sender, EpgpAlias e)
        {
            var config = _configurationService.GetConfiguration();
            var ep = config.EpMinimum;
            var gp = config.GpMinimum;
            Set(e.Name, ep, gp, "User initialization");
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
            var alias = _epgpRepository.GetAlias(aliasName);
            Ep(alias, value, memo, type);
        }

        public void Ep(EpgpAlias alias, int value, string memo, TransactionType type = TransactionType.GpManual)
        {
            var transaction = GetTransaction(alias, value, memo, type);
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
            var transaction = GetTransaction(alias, value, memo, type);
            _epgpRepository.PostTransaction(transaction);
            _epgpRepository.Save();
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
            var raidItem = new RaidItem
            {
                AwardedAlias = alias,
                AwardedAliasId = alias.Id,
                Item = item,
                ItemId = item.Id,
                RaidId = raid.RaidObject.RaidId,
                Transaction = transaction,
                TransactionId = transaction.Id
            };
            _epgpRepository.PostRaidItem(raidItem);
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
            var alias = _epgpRepository.GetAlias(aliasName);
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
            var aliases = _epgpRepository.GetAliases();
            foreach (var alias in aliases)
            {
                var epDecay = (int)Math.Round(alias.EffortPoints * asPercent, MidpointRounding.AwayFromZero);
                var gpDecay = (int)Math.Round(alias.GearPoints * asPercent, MidpointRounding.AwayFromZero);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(EpFlag))
                    Ep(alias, epDecay, $"{decayPercent}% Decay", TransactionType.EpDecay);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(GpFlag))
                    Gp(alias, gpDecay, $"{decayPercent}% Decay", TransactionType.GpDecay);
            }
        }
    }
}