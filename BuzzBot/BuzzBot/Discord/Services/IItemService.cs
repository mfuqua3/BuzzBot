using System.Threading.Tasks;
using BuzzBotData.Data;
using Discord;

namespace BuzzBot.Discord.Services
{
    public interface IItemService
    {
        public Task<Item> TryGetItem(string queryString, IMessageChannel queryChannel);
    }
}