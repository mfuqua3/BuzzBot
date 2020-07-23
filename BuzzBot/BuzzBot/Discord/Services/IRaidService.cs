using System;
using System.Threading.Tasks;
using BuzzBot.Epgp;

namespace BuzzBot.Discord.Services
{
    public interface IRaidService
    {
        Task<ulong> PostRaid(ReplyDelegate replyDelegate, EpgpRaid raidObject);
        event EventHandler<RaidData> RaidAdded;
        event EventHandler<RaidData> RaidRemoved;
        event EventHandler<RaidData> RaidUpdated;
    }
}