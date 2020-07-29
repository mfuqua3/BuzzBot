using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    public class BuzzBotModuleBase<T>:ModuleBase<T> where T:class, ICommandContext
    {
        protected virtual async Task<IMessageChannel> GetUserChannel(bool contextChannelIfDebug = true)
        {
            // ReSharper disable once RedundantAssignment
            IMessageChannel channel = await Context.User.GetOrCreateDMChannelAsync();
#if DEBUG
            channel = Context.Channel;
#endif
            return channel;
        }
    }
}