using System.Threading.Tasks;
using Discord.Commands;

namespace BuzzBot.Discord.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        
        [Command("info")]
        public async Task Info()
            => await ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net 2.2.0\n");

        [Command("echo")]
        public async Task Echo([Remainder]string toEcho)
        {
            await ReplyAsync($"Echoing message: {toEcho}");
        }
    }
}
