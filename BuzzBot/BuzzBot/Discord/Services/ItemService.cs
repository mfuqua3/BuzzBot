using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Discord.Services
{
    public class ItemService : IItemService
    {
        private readonly IQueryService _queryService;
        private readonly BuzzBotDbContext _dbContext;
        private readonly IItemResolver _itemResolver;

        public ItemService(IQueryService queryService, BuzzBotDbContext dbContext, IItemResolver itemResolver)
        {
            _queryService = queryService;
            _dbContext = dbContext;
            _itemResolver = itemResolver;
        }
        public async Task<Item> TryGetItem(string queryString, ICommandContext commandContext, ulong targetUserId)
        {
            if (targetUserId == 0)
                targetUserId = commandContext.User.Id;
            var item = await GetQueriedItem(queryString, commandContext);
            if (item == null) return null;
            return await _itemResolver.ResolveItem(item, commandContext, targetUserId);
        }

        private async Task<Item> GetQueriedItem(string queryString, ICommandContext commandContext)
        {
            var queryChannel = commandContext.Channel;
            var items = _dbContext.Items.AsQueryable().Where(itm => EF.Functions.Like(itm.Name, $"%{queryString}%")).OrderByDescending(i => i.ItemLevel).ToList();
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
            var item = items[result];
            return item;
        }
    }
}