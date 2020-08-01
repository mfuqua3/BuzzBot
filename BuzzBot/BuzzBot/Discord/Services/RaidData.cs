using BuzzBot.Epgp;
using Discord;

namespace BuzzBot.Discord.Services
{
    public class RaidData
    {
        public ulong ServerId { get; }
        public IUserMessage Message { get; }
        public EpgpRaid RaidObject { get; }
        public ulong Id => Message.Id;
        public bool Started { get; set; }

        public RaidData(IUserMessage message, EpgpRaid raidObject, ulong serverId)
        {
            Message = message;
            RaidObject = raidObject;
            ServerId = serverId;
        }
    }
}