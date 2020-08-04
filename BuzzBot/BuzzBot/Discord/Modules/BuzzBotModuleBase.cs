using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Builders;

namespace BuzzBot.Discord.Modules
{
    public class BuzzBotModuleBase:ModuleBase<ScopedCommandContext>
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

        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        { 
            base.OnModuleBuilding(commandService, builder);
        }
    }

}