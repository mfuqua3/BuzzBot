using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BuzzBot.Discord.Services;

namespace BuzzBot.Epgp
{
    public class RaidRepository : IRaidRepository
    {
        private readonly ConcurrentDictionary<ulong, RaidData> _raids = new ConcurrentDictionary<ulong, RaidData>();
        public IReadOnlyCollection<RaidData> GetRaids()
        {
            return _raids.Values.ToList().AsReadOnly();
        }

        public void AddOrUpdateRaid(RaidData raidData)
        {
            _raids.AddOrUpdate(raidData.Id, id => raidData, (id, data) => raidData);
        }

        public void RemoveRaid(ulong id)
        {
            _raids.TryRemove(id, out _);
        }

        public RaidData GetRaid(ulong id = 0)
        {
            if (id == 0) return _raids.Values.LastOrDefault();
            return Contains(id) ? _raids[id] : null;
        }

        public bool Contains(ulong raidId)
        {
            return _raids.ContainsKey(raidId);
        }

        public int Count => _raids.Count;
    }
}