using System.Security.Policy;
using System.Threading.Tasks;
using BuzzBotData.Data;
using Discord;
using Discord.Commands;

namespace BuzzBot.Epgp
{
    public interface IItemResolver
    {
        Task<Item> ResolveItem(Item toResolve, ICommandContext context, EpgpAlias targetAlias);
    }
}