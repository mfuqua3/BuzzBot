using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Wowhead;
using Discord.Commands;
using Discord.WebSocket;

namespace BuzzBot.Discord.Modules
{
    public class AdminModule:ModuleBase<SocketCommandContext>
    {
        private readonly AdministrationService _administrationService;

        public AdminModule(AdministrationService administrationService)
        {
            _administrationService = administrationService;
        }

        [Command("authorize")]
        [RequiresBotAdmin]
        public async Task Authorize([Summary("User to authorize")] SocketUser user = null)
        {
            _administrationService.Authorize(
                user);
            await ReplyAsync("User authorized");
        }
    }
}