using System.Threading.Tasks;
using BuzzBot.Discord.Utility;
using Discord;

namespace BuzzBot.Discord.Services
{
    public interface IPageService
    {
        Task SendPages(IMessageChannel channel, PageFormat pageFormat);
        Task SendPages(IMessageChannel channel, string header, params string[] contentLines);
    }
}