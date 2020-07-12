using FileHelpers;

namespace WowheadDataSeeder
{
   [DelimitedRecord(",")]
    public class Item
    {
        public int Entry { get; set; }
        public string Name { get; set; }
    }
}