using System.Collections.Generic;

namespace BuzzBot.Discord.Utility
{
    public class PageFormat
    {
        public string HeaderLine { get; set; }
        public string HorizontalRule { get; set; }
        public List<string> ContentLines { get; set; }
        public int LinesPerPage { get; set; } 
    }
}