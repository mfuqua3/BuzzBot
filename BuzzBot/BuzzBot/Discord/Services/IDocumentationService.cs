using System.Threading.Tasks;
using Discord;

namespace BuzzBot.Discord.Services
{
    public interface IDocumentationService
    {
        Task SendDocumentation(IMessageChannel channel, string moduleName, ulong requestingUser);
    }
}