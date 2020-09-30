using System;
using BuzzBot.Discord.Extensions;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    class EpgpTransactionFactory : IEpgpTransactionFactory
    {
        private readonly IEpgpConfigurationService _configurationService;

        public EpgpTransactionFactory(IEpgpConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public EpgpTransaction GetTransaction(EpgpAlias alias, int value, string memo, TransactionType transactionType)
        {

            var config = _configurationService.GetConfiguration();
            var currencyType = transactionType.GetAttributeOfType<CurrencyAttribute>().Currency;
            int change;
            switch (currencyType)
            {
                case Currency.Ep:
                    change = alias.EffortPoints + value < config.EpMinimum ? config.EpMinimum - alias.EffortPoints : value;
                    break;
                case Currency.Gp:
                    change = alias.GearPoints + value < config.GpMinimum ? config.GpMinimum - alias.GearPoints : value;
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
    }
}