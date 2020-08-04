using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Discord.Modules
{
    public class ScopedCommandContext : SocketCommandContext
    {
        public IServiceScope ServiceScope { get; }

        public ScopedCommandContext(IServiceScope serviceScope, DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
        {
            ServiceScope = serviceScope;
        }
    }
}