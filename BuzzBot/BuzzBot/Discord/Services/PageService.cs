using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class PageService
    {
        private readonly ConcurrentDictionary<ulong, PagedContent> _pagedContentDictionary = new ConcurrentDictionary<ulong, PagedContent>();
        private const string ArrowBackward = @"⬅️";
        private const string ArrowForward = @"➡️";

        public PageService(DiscordSocketClient discordClient)
        {
            discordClient.ReactionAdded += ReactionManipulated;
            discordClient.ReactionRemoved += ReactionManipulated;
        }

        private async Task ReactionManipulated(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot) return;
            if (!reaction.Emote.Name.Equals(ArrowBackward) && !reaction.Emote.Name.Equals(ArrowForward)) return;
            if (!_pagedContentDictionary.TryGetValue(reaction.MessageId, out var pagedContent)) return;
            var isForward = reaction.Emote.Name.Equals(ArrowForward);
            if (!(await reaction.Channel.GetMessageAsync(reaction.MessageId) is IUserMessage message)) return;
            var pageToSend = isForward ? pagedContent.GetNextPage() : pagedContent.GetPreviousPage();
            await  message.ModifyAsync(opt => opt.Content = pageToSend.Content);
        }

        public async Task SendPages(IMessageChannel channel, string header, params string[] contentLines)
        {
            var numberOfPages = (int)Math.Ceiling((double)contentLines.Length / 15);
            var pageNumber = 0;
            var contentLineQueue = new Queue<string>(contentLines);
            var pagedContent = new PagedContent();
            while (contentLineQueue.Any())
            {
                pageNumber++;
                var longestLineLength = 0;
                var pageLineIterator = 0;
                var contentSb = new StringBuilder();
                contentSb.AppendLine("```diff");
                contentSb.AppendLine(header);
                while (pageLineIterator < 15)
                {
                    if (!contentLineQueue.Any()) break;
                    var line = contentLineQueue.Dequeue();
                    if (line.Length > longestLineLength)
                        longestLineLength = line.Length;
                    contentSb.AppendLine(line);
                    pageLineIterator++;
                }
                var pageNumSb = new StringBuilder();
                pageNumSb.Append($"Page {pageNumber}/{numberOfPages}");
                while (pageNumSb.Length < longestLineLength)
                {
                    pageNumSb.Insert(0, ' ');
                }

                contentSb.AppendLine();
                contentSb.AppendLine(pageNumSb.ToString());
                contentSb.Append("```");
                var page = new Page(pageNumber, contentSb.ToString());
                pagedContent.Pages.Add(page);
            }

            if (pagedContent.TotalPages == 0) return;
            var message = await channel.SendMessageAsync(pagedContent.Pages.First().Content);
            if (pagedContent.TotalPages == 1) return;
            if (!_pagedContentDictionary.TryAdd(message.Id, pagedContent)) return;
            //TODO This design will need to revisited if this bot is scaled up.
            //Right now, memory is being managed by preventing more than 100 concurrent page processes from being stored in RAM
            if (_pagedContentDictionary.Count > 100)
                _pagedContentDictionary.Remove(_pagedContentDictionary.Keys.First(), out _);
            await message.AddReactionAsync(new Emoji(ArrowBackward));
            await message.AddReactionAsync(new Emoji(ArrowForward));
        }

        private string JoinLines(string header, params string[] lines)
        {
            var returnSb = new StringBuilder();
            returnSb.AppendLine(header);
            foreach (var line in lines)
            {
                returnSb.AppendLine(line);
            }

            return returnSb.ToString();
        }

        private class PagedContent
        {
            public int TotalPages => Pages.Count;
            private int _currentPage = 1;

            public Page GetNextPage()
            {
                if (TotalPages == 0) return null;
                if (_currentPage >= TotalPages || _currentPage <= 0)
                {
                    _currentPage = Pages.First().PageNumber;
                    return Pages.First();
                }

                _currentPage++;
                return Pages[_currentPage - 1];
            }

            public Page GetPreviousPage()
            {
                if (TotalPages == 0) return null;
                if (_currentPage > TotalPages || _currentPage <= 1)
                {
                    _currentPage = Pages.Last().PageNumber;
                    return Pages.Last();
                }

                _currentPage--;
                return Pages[_currentPage - 1];
            }
            public List<Page> Pages { get; } = new List<Page>();
        }

        private class Page
        {
            public Page(int pageNumber, string content)
            {
                PageNumber = pageNumber;
                Content = content;
            }

            public int PageNumber { get; }
            public string Content { get; }
        }
    }
}