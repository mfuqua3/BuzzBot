using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBotData.Data;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Discord.Services
{
    public class ItemService : IItemService
    {
        private readonly IQueryService _queryService;
        private readonly BuzzBotDbContext _dbContext;

        public ItemService(IQueryService queryService, BuzzBotDbContext dbContext)
        {
            _queryService = queryService;
            _dbContext = dbContext;
        }
        public async Task<Item> TryGetItem(string queryString, IMessageChannel queryChannel)
        {
            var items = _dbContext.Items.AsQueryable().Where(itm => EF.Functions.Like(itm.Name, $"%{queryString}%")).OrderByDescending(i=>i.ItemLevel).ToList();
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