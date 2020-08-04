using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Wowhead;
using Discord.Commands;
using Discord.WebSocket;

namespace BuzzBot.Discord.Modules
{
    public class AdminModule:BuzzBotModuleBase
    {
        private readonly IAdministrationService _administrationService;

        public AdminModule(IAdministrationService administrationService)
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