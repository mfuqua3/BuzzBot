using System;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class GuildMembershipViewModel
    {
        public string UserId { get; set; }
        public Guid GuildId { get; set; }
        public string DisplayName { get; set; }
        public string GuildName { get; set; }
        public bool IsOwner { get; set; }
    }
}
