using System;

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