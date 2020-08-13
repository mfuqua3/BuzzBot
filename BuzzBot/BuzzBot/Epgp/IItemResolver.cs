using System.Collections.Generic;
using System.Threading.Tasks;
using BuzzBotData.Data;
using Discord;
using Discord.Commands;

namespace BuzzBot.Epgp
{
    public interface IItemResolver
    {
        Task<Item> ResolveItem(Item toResolve, ICommandContext messageChannel);
    }

    public class ItemResolver : IItemResolver
    {
        public Task<Item> ResolveItem(Item toResolve, ICommandContext messageChannel)
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IItemMapper
    {
        bool ContainsMap(Item item);
        List<Item> GetItems(Item item, ICommandContext commandContext);
    }
}