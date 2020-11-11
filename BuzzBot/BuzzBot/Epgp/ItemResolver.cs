using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBotData.Data;
using Discord.Commands;

namespace BuzzBot.Epgp
{
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
        public async Task<Item> ResolveItem(Item toResolve, ICommandContext context, EpgpAlias targetAlias)
        {
            foreach (var mapper in _itemMappers)
            {
                if (!mapper.ContainsMap(toResolve)) continue;
                var items = mapper.GetItems(toResolve, context, targetAlias).ToList();
                if (items.Count <= 1)
                {
                    if (!items.Any())
                    {
                        await context.Channel.SendMessageAsync(
                            "Unable to find any items that were eligible to be assigned to that user");
                    }
                    return items.FirstOrDefault();
                }
                return await QueryItem(items, context, targetAlias);
            }

            return toResolve;
        }

        private async Task<Item> QueryItem(List<Item> candidateItems, ICommandContext context, EpgpAlias targetAlias)
        {
            var index = await _queryService.SendOptionSelectQuery("Please select the item\n⚠️ - item has been claimed by this user previously\n", candidateItems,
                item => GetQueryString(item, targetAlias), context.Channel, CancellationToken.None);
            if (index == -1) return null;
            return candidateItems[index];
        }

        private string GetQueryString(Item item, EpgpAlias alias)
        {
            var receivedPreviously = alias.AwardedItems.Any(itm => itm.ItemId == item.Id);
            return $"{item.Name} {(receivedPreviously ? "⚠️" : string.Empty)}";
        }
    }
}