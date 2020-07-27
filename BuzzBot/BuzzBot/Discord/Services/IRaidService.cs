using System;
using System.Threading.Tasks;
using BuzzBot.Epgp;

namespace BuzzBot.Discord.Services
{
    public interface IRaidService
    {
        Task<ulong> PostRaid(ReplyDelegate replyDelegate, EpgpRaid raidObject);
        void Start(ulong raidId = 0);
        void Extend(TimeSpan extend, ulong raidId = 0);
        void End(ulong raidId = 0);
        Task KickUser(ulong userId, ulong raidId = 0);
        Task KickUser(string alias, ulong raidId = 0);
    }
}