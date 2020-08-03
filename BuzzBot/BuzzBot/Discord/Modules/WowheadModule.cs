using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Epgp;
using BuzzBot.Wowhead;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace BuzzBot.Discord.Modules
{
    public class WowheadModule : ModuleBase<SocketCommandContext>
    {
        private readonly IWowheadClient _wowheadClient;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IQueryService _queryService;
        private readonly ItemRepository _itemRepository;

        public WowheadModule(IWowheadClient wowheadClient, IEpgpCalculator epgpCalculator, IQueryService queryService, ItemRepository itemRepository)
        {
            _wowheadClient = wowheadClient;
            _epgpCalculator = epgpCalculator;
            _queryService = queryService;
            _itemRepository = itemRepository;
        }
        [Command("update_items")]
        [RequiresBotAdmin]
        public Task UpdateItems()
        {
            Task.Run(async () => await _queryService.SendQuery(
                "Are you sure you want to update all item data from Wowhead? This could take a long time.",
                Context.Channel, async () => await ExecuteUpdate(), async () => await ReplyAsync("Cancelling update")));
            return Task.CompletedTask;
        }
        private async Task ExecuteUpdate()
        {
            var items = _itemRepository.GetItems();
            await ReplyAsync($"Beginning update for {items.Count} items.");
            var count = 0;
            foreach (var item in items)
            {
                var wowheadData = await _wowheadClient.Get(item.Id.ToString());
                if (wowheadData?.Item == null) continue;
                if (int.TryParse(wowheadData.Item.InventorySlot?.Id, out var slot))
                    item.InventorySlot = slot;
                count++;
            }
            _itemRepository.Save();
            await ReplyAsync($"{count} items updated to new database schema successfully");
        }
    }
}