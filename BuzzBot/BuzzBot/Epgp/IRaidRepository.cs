using System.Collections.Generic;
using BuzzBot.Discord.Services;

namespace BuzzBot.Epgp
{
    public interface IRaidRepository
    {
        public IReadOnlyCollection<RaidData> GetRaids();
        public void AddOrUpdateRaid(RaidData raidData);
        public void RemoveRaid(ulong id);
        public RaidData GetRaid(ulong id = 0);
        bool Contains(ulong raidId);
        public int Count { get; }
    }
}