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
}