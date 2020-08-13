using System.Threading.Tasks;
using BuzzBotData.Data;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Services
{
    public interface IItemService
    {
        public Task<Item> TryGetItem(string queryString, ICommandContext queryChannel, ulong targetUserId = 0);
    }
}