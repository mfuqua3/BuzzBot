using System.Collections.Generic;
using System.Linq;
using BuzzBotData.Data;
using Discord.Commands;

namespace BuzzBot.Epgp
{
    public abstract class TierTokenItemMapper : IItemMapper
    {
        protected abstract Dictionary<int, HashSet<int>> MappedItemDictionary { get; }
        protected abstract Dictionary<int, Class> ClassDictionary { get; }
        protected readonly BuzzBotDbContext DbContext;

        protected TierTokenItemMapper(BuzzBotDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public virtual bool ContainsMap(Item item)
        {
            return MappedItemDictionary.ContainsKey(item.Id);
        }

        public virtual IEnumerable<Item> GetItems(Item item, ICommandContext commandContext, EpgpAlias targetAlias)
        {
            var candidateItems = MappedItemDictionary[item.Id];
            foreach (var candidateItem in candidateItems)
            {
                if (!ClassDictionary.ContainsKey(candidateItem))
                {
                    yield return GetItem(candidateItem);
                    continue;
                }

                if (ClassDictionary[candidateItem] != targetAlias.Class) continue;
                yield return GetItem(candidateItem);
            }

        }

        protected Item GetItem(int id)
        {

            return DbContext.Items.FirstOrDefault(itm => itm.Id == id);
        }
    }
}