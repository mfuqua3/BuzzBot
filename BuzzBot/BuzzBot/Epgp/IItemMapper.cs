using System.Collections.Generic;
using BuzzBotData.Data;
using Discord.Commands;

namespace BuzzBot.Epgp
{
    public interface IItemMapper
    {
        bool ContainsMap(Item item);
        IEnumerable<Item> GetItems(Item item, ICommandContext commandContext, EpgpAlias targetAlias);
    }
}