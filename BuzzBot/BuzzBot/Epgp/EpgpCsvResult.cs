using System;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public class EpgpCsvResult
    {
        public string Name { get; set; }
        // ReSharper disable once InconsistentNaming
        public int EP { get; set; }
        // ReSharper disable once InconsistentNaming
        public int GP { get; set; }
    }

    public class EpgpCsvRecord
    {
        public string Name { get; set; }
        // ReSharper disable once InconsistentNaming
        public int EP { get; set; }
        // ReSharper disable once InconsistentNaming
        public int GP { get; set; }
        // ReSharper disable once InconsistentNaming
        public string PR { get; set; }
    }

    public class TransactionCsvRecord
    {

        public Guid Id { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public ulong DiscordUserId { get; set; }
        public string CharacterName { get; set; }
        public TransactionType TransactionType { get; set; }
        public int Value { get; set; }
        public string Memo { get; set; }
    }
    public class LootCsvRecord
    {
        public string TransactionId { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public string UserAlias { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string RaidEventId { get; set; }
    }
}