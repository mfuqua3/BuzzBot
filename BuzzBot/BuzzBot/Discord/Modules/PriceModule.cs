using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.NexusHub;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    public class PriceModule:BuzzBotModuleBase
    {
        private readonly IItemService _itemService;
        private readonly NexusHubClient _nexusHubClient;

        public PriceModule(IItemService itemService, NexusHubClient nexusHubClient)
        {
            _itemService = itemService;
            _nexusHubClient = nexusHubClient;
        }
        //[Command("price")]
        //public async Task Price([Remainder]string queryString)
        //{
        //    var item = await _itemService.TryGetItem(queryString, Context.Channel);
        //    if (item == null) return;
        //    var nexusHubData = await _nexusHubClient.GetItem(item.Id);
        //    if (nexusHubData == null) return;
        //    await ReplyAsync(
        //        $"Testing:\n {nexusHubData.Name} \n " +
        //        $"Market-> {nexusHubData.Stats.Current.MarketValue}\n " +
        //        $"Number of Auctions->{nexusHubData.Stats.Current?.NumberAuctions}\n" +
        //        $"Minimum Buyout->{nexusHubData.Stats.Current?.MinimumBuyout}");
        //}
    }
}