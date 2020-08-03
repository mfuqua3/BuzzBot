using System;
using System.Threading.Tasks;
using BuzzBot.Epgp;
using Discord;

namespace BuzzBot.Discord.Services
{
    public interface IRaidService : IDisposable
    {
        Task PostRaid(IMessageChannel messageChannel, EpgpRaid raidObject);
        EpgpRaid GetRaid(ulong raidId = 0);
        void Start(ulong raidId = 0);
        void Extend(TimeSpan extend, ulong raidId = 0);
        void End(ulong raidId = 0);
        Task KickUser(ulong userId, ulong raidId = 0);
        Task KickUser(string alias, ulong raidId = 0);
    }
}