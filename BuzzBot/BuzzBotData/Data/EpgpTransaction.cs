using System;

namespace BuzzBotData.Data
{
    public class EpgpTransaction
    {
        public Guid Id { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public EpgpAlias Alias { get; set; }
        public Guid AliasId { get; set; }
        public TransactionType TransactionType { get; set; }
        public int Value { get; set; }
        public string Memo { get; set; }

    }

    public enum TransactionType
    {
        [Currency(Currency.Ep)]
        EpAutomated,
        [Currency(Currency.Ep)]
        EpManual,
        [Currency(Currency.Gp)]
        GpFromGear,
        [Currency(Currency.Gp)]
        GpManual,
        [Currency(Currency.Ep)]
        EpDecay,
        [Currency(Currency.Gp)]
        GpDecay,
    }

    public enum Currency
    {
        Ep,
        Gp
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class CurrencyAttribute : Attribute
    {
        public CurrencyAttribute(Currency currency)
        {
            Currency = currency;
        }

        public Currency Currency { get; }
    }

}