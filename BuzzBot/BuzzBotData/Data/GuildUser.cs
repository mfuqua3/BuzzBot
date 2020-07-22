using System.Collections.Generic;

namespace BuzzBotData.Data
{
    public class GuildUser
    {
        public ulong Id { get; set; }
        public List<EpgpAlias> Aliases { get; set; }
    }
}