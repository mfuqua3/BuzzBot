using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;
using Discord;
using Discord.Commands;

namespace BuzzBot.Epgp
{
    public interface IItemResolver
    {
        Task<Item> ResolveItem(Item toResolve, ICommandContext context, ulong targetUserId);
    }

    public class ItemResolver : IItemResolver
    {
        private readonly BuzzBotDbContext _dbContext;
        private readonly IQueryService _queryService;
        private readonly IAliasService _aliasService;
        private readonly IItemMapper[] _itemMappers;

        public ItemResolver(ItemMapperResolver mapperResolver, BuzzBotDbContext dbContext, IQueryService queryService, IAliasService aliasService)
        {
            _dbContext = dbContext;
            _queryService = queryService;
            _aliasService = aliasService;
            _itemMappers = mapperResolver();
        }
        public async Task<Item> ResolveItem(Item toResolve, ICommandContext context, ulong targetUserId)
        {
            foreach (var mapper in _itemMappers)
            {
                if (!mapper.ContainsMap(toResolve)) continue;
                var items = mapper.GetItems(toResolve, context, targetUserId).ToList();
                if (items.Count <= 1)
                {
                    if (!items.Any())
                    {
                        await context.Channel.SendMessageAsync(
                            "Unable to find any items that were eligible to be assigned to that user");
                    }
                    return items.FirstOrDefault();
                }
                return await QueryItem(items, context, targetUserId);
            }

            return toResolve;
        }

        private async Task<Item> QueryItem(List<Item> candidateItems, ICommandContext context, ulong targetUserId)
        {
            var index = await _queryService.SendOptionSelectQuery("Please select the item\n⚠️ - item has been claimed by this user previously\n", candidateItems,
                item => GetQueryString(item, targetUserId), context.Channel, CancellationToken.None);
            if (index == -1) return null;
            return candidateItems[index];
        }

        private string GetQueryString(Item item, ulong userId)
        {
            var alias = _aliasService.GetActiveAlias(userId);
            var receivedPreviously = alias.AwardedItems.Any(itm => itm.ItemId == item.Id);
            return $"{item.Name} {(receivedPreviously ? "⚠️" : string.Empty)}";
        }
    }

    public class AhnQirajTempleItemMapper : IItemMapper
    {
        private readonly IAliasService _aliasService;
        private readonly BuzzBotDbContext _dbContext;

        private readonly Dictionary<int, HashSet<int>> _mappedItemDictionary = new Dictionary<int, HashSet<int>>
        {
            {21232, new HashSet<int>{21242,21272,21244,21269}}, //Imperial Qiraji Armaments
            {21237,  new HashSet<int>{21273,21275,21268}}, //Imperial Qiraji Regalia
            {20932, new HashSet<int>{21388,21391,21338,21335,21344,21345,21355,21354,21373,21376} }, //Qiraji Bindings of Dominance
            {20928, new HashSet<int>{21333,21330,21359,21361,21349,21350,21365,21367}}, //Qiraji Bindings of Command
            {20930, new HashSet<int>{21387,21360,21353,21372,21366}}, //Veklors Diadem
            {20926, new HashSet<int>{21329,21337,21347,21348}}, //Veknilash's Circlet
            {20931, new HashSet<int>{21390,21336,21356,21375,21368} }, //Skin of the Great Sandworm
            {20927, new HashSet<int>{21332,21362,21346,21352} }, //Ouro's Intact Hide
            {20933, new HashSet<int>{21334,21343,21357,21351}  }, //Husk of the Old God
            {20929,new HashSet<int>{21389,21331,21364,21374,21370} }
        };

        private readonly Dictionary<int, Class> _classDictionary = new Dictionary<int, Class>
        {
            {21388, Class.Paladin },
            {21391, Class.Paladin },
            {21387, Class.Paladin },
            {21390, Class.Paladin },
            {21389, Class.Paladin },

            {21333, Class.Warrior },
            {21330, Class.Warrior },
            {21332, Class.Warrior },
            {21331, Class.Warrior },
            {21329, Class.Warrior },

            {21365, Class.Hunter },
            {21367, Class.Hunter },
            {21366, Class.Hunter },
            {21368, Class.Hunter },
            {21370, Class.Hunter },

            {21359, Class.Rogue },
            {21361, Class.Rogue },
            {21360, Class.Rogue },
            {21362, Class.Rogue },
            {21364, Class.Rogue },

            {21338, Class.Warlock },
            {21335, Class.Warlock },
            {21336, Class.Warlock },
            {21334, Class.Warlock },
            {21337, Class.Warlock },

            {21344, Class.Mage },
            {21345, Class.Mage },
            {21343, Class.Mage },
            {21346, Class.Mage },
            {21347, Class.Mage },

            {21349, Class.Priest },
            {21350, Class.Priest },
            {21351, Class.Priest },
            {21352, Class.Priest },
            {21348, Class.Priest },

            {21354, Class.Druid },
            {21355, Class.Druid },
            {21353, Class.Druid },
            {21356, Class.Druid },
            {21357, Class.Druid },

            {21373, Class.Shaman },
            {21376, Class.Shaman },
            {21372, Class.Shaman },
            {21375, Class.Shaman },
            {21374, Class.Shaman },
        };

        public AhnQirajTempleItemMapper(IAliasService aliasService, BuzzBotDbContext dbContext)
        {
            _aliasService = aliasService;
            _dbContext = dbContext;
        }
        public bool ContainsMap(Item item)
        {
            return _mappedItemDictionary.ContainsKey(item.Id);
        }

        public IEnumerable<Item> GetItems(Item item, ICommandContext commandContext, ulong targetUserId)
        {
            var candidateItems = _mappedItemDictionary[item.Id];
            var alias = _aliasService.GetActiveAlias(targetUserId);
            foreach (var candidateItem in candidateItems)
            {
                if (!_classDictionary.ContainsKey(candidateItem))
                {
                    yield return GetItem(candidateItem);
                    continue;
                }

                if (_classDictionary[candidateItem] != alias.Class) continue;
                yield return GetItem(candidateItem);
            }

        }

        private Item GetItem(int id)
        {

            return _dbContext.Items.FirstOrDefault(itm => itm.Id == id);
        }

    }

    public interface IItemMapper
    {
        bool ContainsMap(Item item);
        IEnumerable<Item> GetItems(Item item, ICommandContext commandContext, ulong targetUserId);
    }

    public delegate IItemMapper[] ItemMapperResolver();
}