using System.Threading.Tasks;
using BuzzBot.Epgp;
using BuzzBot.Wowhead;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace BuzzBot.Discord.Modules
{
    public class WowheadModule:ModuleBase<SocketCommandContext>
    {
        private readonly IWowheadClient _wowheadClient;
        private readonly EpgpCalculator _epgpCalculator;

        public WowheadModule(IWowheadClient wowheadClient, EpgpCalculator epgpCalculator)
        {
            _wowheadClient = wowheadClient;
            _epgpCalculator = epgpCalculator;
        }
        [Command("query")]
        public async Task Query([Remainder] string query)
        {
            var isHunter = query.EndsWith("-h");
            if (isHunter)
                query = query.Substring(0, query.Length - 2).TrimEnd();
            var wowheadObj = await _wowheadClient.Get(query);
            if (wowheadObj.Item == null)
            {
                await ReplyAsync("No item found by that name");
                return;
            }

            var value = _epgpCalculator.Calculate(wowheadObj.Item, isHunter);
            //var test = JsonConvert.DeserializeObject<WowheadJson>(wowheadObj.Item.Json);
            var embed = new EmbedBuilder();
            embed.WithTitle($"{wowheadObj.Item.Name} : {value:F0} GP");
            embed.WithImageUrl($"http://www.korkd.com/wow_img/{wowheadObj.Item.Id}.png");
            await ReplyAsync("", false, embed.Build());
        }
    }
}