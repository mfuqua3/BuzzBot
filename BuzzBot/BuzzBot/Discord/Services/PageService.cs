using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Utility;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class PageService : IPageService
    {
        private readonly ConcurrentDictionary<ulong, PagedContent> _pagedContentDictionary = new ConcurrentDictionary<ulong, PagedContent>();
        private const string ArrowBackward = @"⬅️";
        private const string ArrowForward = @"➡️";
        private static int _id = 0;
        private ConcurrentDictionary<IEmote, HashSet<int>> _revealEmoteDictionary = new ConcurrentDictionary<IEmote, HashSet<int>>();

        public PageService(DiscordSocketClient discordClient)
        {
            discordClient.ReactionAdded += ReactionManipulated;
            discordClient.ReactionRemoved += ReactionManipulated;
        }

        private async Task ReactionManipulated(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot) return;
            if (!_pagedContentDictionary.TryGetValue(reaction.MessageId, out var pagedContent)) return;
            var isReveal = _revealEmoteDictionary.TryGetValue(reaction.Emote, out var contentPageIds) &&
                           contentPageIds.Contains(pagedContent.Id);
            if (!reaction.Emote.Name.Equals(ArrowBackward) && !reaction.Emote.Name.Equals(ArrowForward) && !isReveal) return;
            if (!(await reaction.Channel.GetMessageAsync(reaction.MessageId) is IUserMessage message)) return;
            Page pageToSend;
            if (isReveal)
            {
                pagedContent.IsRevealed = !pagedContent.IsRevealed;
                pageToSend = pagedContent.CurrentPage();
            }
            else
            {
                var isForward = reaction.Emote.Name.Equals(ArrowForward);
                pageToSend = isForward ? pagedContent.GetNextPage() : pagedContent.GetPreviousPage();
            }
            await message.ModifyAsync(opt => opt.Content = pageToSend.Content);
        }

        private List<Page> BuildPages(BasePageFormat pageFormat)
        {
            var numberOfPages = (int)Math.Ceiling((double)pageFormat.ContentLines.Count / pageFormat.LinesPerPage);
            var contentLineQueue = new Queue<string>(pageFormat.ContentLines);
            var pageNumber = 0;
            var returnList = new List<Page>();
            while (contentLineQueue.Any())
            {
                pageNumber++;
                var longestLineLength = 0;
                var pageLineIterator = 0;
                var contentSb = new StringBuilder();
                contentSb.AppendLine("```diff");
                contentSb.AppendLine($"{pageFormat.HeaderLine}{(!string.IsNullOrEmpty(pageFormat.HorizontalRule) ? Environment.NewLine + pageFormat.HorizontalRule : string.Empty)}");
                while (pageLineIterator < pageFormat.LinesPerPage)
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
                returnList.Add(page);
            }

            return returnList;
        }

        public async Task SendPages(IMessageChannel channel, PageFormat pageFormat)
        {
            var pagedContent = new PagedContent(_id++)
            {
                Pages = BuildPages(pageFormat),
                HasHiddenContent = pageFormat.HasHiddenColumns
            };
            if (pagedContent.TotalPages == 0) return;
            if (pagedContent.HasHiddenContent)
            {
                pagedContent.RevealedPages = BuildPages(pageFormat.RevealedPageFormat);
                _revealEmoteDictionary.AddOrUpdate(pageFormat.RevealEmote,
                    emote => new HashSet<int>(new[] { pagedContent.Id }), (emote, set) =>
                      {
                          set.Add(pagedContent.Id);
                          return set;
                      });
            }
            var message = await channel.SendMessageAsync(pagedContent.Pages.First().Content);
            if (pagedContent.TotalPages == 1 && !pageFormat.HasHiddenColumns) return;
            if (!_pagedContentDictionary.TryAdd(message.Id, pagedContent)) return;
            //TODO This design will need to revisited if this bot is scaled up.
            //Right now, memory is being managed by preventing more than 100 concurrent page processes from being stored in RAM
            if (_pagedContentDictionary.Count > 100)
                _pagedContentDictionary.Remove(_pagedContentDictionary.Keys.First(), out _);
            if(pagedContent.TotalPages > 1)
            {
                await message.AddReactionAsync(new Emoji(ArrowBackward));
                await message.AddReactionAsync(new Emoji(ArrowForward));
            }
            if (pageFormat.HasHiddenColumns)
                await message.AddReactionAsync(pageFormat.RevealEmote);
        }

        public async Task SendPages(IMessageChannel channel, string header, params string[] contentLines)
            => await SendPages(channel,
                new PageFormat { HeaderLine = header, ContentLines = contentLines.ToList(), HorizontalRule = null, LinesPerPage = 15 });


        private class PagedContent
        {
            public int Id { get; }
            public int TotalPages => Pages.Count;
            private int _currentPage = 1;

            public PagedContent(int id)
            {
                Id = id;
            }

            public Page CurrentPage()
            {
                return ActivePages[_currentPage-1];
            }

            public Page GetNextPage()
            {
                if (TotalPages == 0) return null;
                if (_currentPage >= TotalPages || _currentPage <= 0)
                {
                    _currentPage = Pages.First().PageNumber;
                    return ActivePages.First();
                }

                _currentPage++;
                return ActivePages[_currentPage - 1];
            }

            public Page GetPreviousPage()
            {
                if (TotalPages == 0) return null;
                if (_currentPage > TotalPages || _currentPage <= 1)
                {
                    _currentPage = Pages.Last().PageNumber;
                    return ActivePages.Last();
                }

                _currentPage--;
                return ActivePages[_currentPage - 1];
            }
            public List<Page> Pages { get; set; } = new List<Page>();
            public List<Page> RevealedPages { get; set; } = new List<Page>();
            public bool IsRevealed { get; set; }
            private List<Page> ActivePages => IsRevealed ? RevealedPages : Pages;
            public bool HasHiddenContent { get; set; }
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