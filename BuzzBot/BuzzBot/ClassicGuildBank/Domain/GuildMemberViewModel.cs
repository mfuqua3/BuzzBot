using System;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class GuildMemberViewModel
    {
        public string DisplayName { get; set; }
        public bool CanUpload { get; set; }
        public string UserId { get; set; }
        public Guid GuildId { get; set; }
    }
}
