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
        EpAutomated,
        EpManual,
        GpFromGear,
        GpManual,
        EpDecay,
        GpDecay,
    }
}