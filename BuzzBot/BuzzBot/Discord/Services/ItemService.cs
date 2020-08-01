using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Discord;

namespace BuzzBot.Discord.Services
{
    public class ItemService : IItemService
    {
        private readonly ItemRepository _itemRepository;
        private readonly IQueryService _queryService;

        public ItemService(ItemRepository itemRepository, IQueryService queryService)
        {
            _itemRepository = itemRepository;
            _queryService = queryService;
        }
        public async Task<Item> TryGetItem(string queryString, IMessageChannel queryChannel)
        {
            var items = _itemRepository.GetItems(queryString).OrderByDescending(itm => itm.ItemLevel).ToList();
            if (items.Count == 0)
            {
                await queryChannel.SendMessageAsync($"\"{queryString}\" returned no results.");
                return null;
            }
            if (items.Count == 1)
            {
                return items.First();
            }
            var result = await _queryService.SendOptionSelectQuery(
                $"\"{queryString}\" yielded multiple results. Please select below",
                items,
                (itm) => itm.Name,
                queryChannel, CancellationToken.None);
            if (result == -1) return null;
            return items[result];
        }
    }
}