using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
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
        private readonly IPageService _pageService;

        public ItemService(IQueryService queryService, BuzzBotDbContext dbContext, IItemResolver itemResolver, IPageService pageService)
        {
            _queryService = queryService;
            _dbContext = dbContext;
            _itemResolver = itemResolver;
            _pageService = pageService;
        }
        public async Task<Item> TryGetItem(string queryString, ICommandContext commandContext, ulong targetUserId)
        {
            if (targetUserId == 0)
                targetUserId = commandContext.User.Id;
            var item = await GetQueriedItem(queryString, commandContext);
            if (item == null) return null;
            return await _itemResolver.ResolveItem(item, commandContext, targetUserId);
        }

        public async Task PrintItemHistory(IMessageChannel channel, EpgpAlias alias, bool asAdmin)
        {
            alias = _dbContext.Aliases
                .Include(a => a.AwardedItems)
                .ThenInclude(ri => ri.Transaction)
                .Include(a => a.AwardedItems)
                .ThenInclude(ri => ri.Item)
                .Include(a => a.AwardedItems)
                .ThenInclude(ri => ri.Raid)
                .FirstOrDefault(a => a.Id == alias.Id);
            if (alias == null)
            {
                await channel.SendMessageAsync("Unable to locate record of that user in the database.");
                return;
            }

            if (!alias.AwardedItems.Any())
            {
                await channel.SendMessageAsync("There are no records of items being awarded to that user.");
                return;
            }
            var pageBuilder = new PageFormatBuilder()
                .AlternateRowColors()
                .AddColumn("Item Name:")
                .AddColumn("Raid")
                .AddColumn("Time (UTC)")
                .AddColumn("GP");
            if (asAdmin)
                pageBuilder.AddHiddenColumn("Transaction ID");

            foreach (var raidItem in alias.AwardedItems.OrderByDescending(ai=>ai.Transaction.TransactionDateTime))
            {
                var row = new List<string>()
                {
                    raidItem.Item.Name,
                    raidItem.Raid.Name,
                    raidItem.Transaction.TransactionDateTime.ToString("g"),
                    raidItem.Transaction.Value.ToString()
                };
                if(asAdmin)row.Add(raidItem.TransactionId.ToString("N"));
                pageBuilder.AddRow(row.ToArray());
            }

            await _pageService.SendPages(channel, pageBuilder.Build());
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