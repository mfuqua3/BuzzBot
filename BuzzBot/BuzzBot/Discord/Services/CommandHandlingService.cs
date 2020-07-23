using System;
using System.Reflection;
using System.Threading.Tasks;
using BuzzBot.Discord.Utility;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly ItemRequestService _itemRequestService;
        private IServiceProvider _provider;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, ItemRequestService itemRequestService)
        {
            _discord = discord;
            _commands = commands;
            _itemRequestService = itemRequestService;
            _provider = provider;

            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            _commands.AddTypeReader<IMentionable>(new MentionableTypeReader(), true);
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var isDm = rawMessage.Channel is IDMChannel;
            if (message.Channel.Id != 727626202977927188 && !isDm) return;//DevSandbox
            if (isDm)
                _itemRequestService.TryResolveResponse(rawMessage);
            int argPos = 0;
            //if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;
            if (!message.HasStringPrefix("buzz.", ref argPos, StringComparison.CurrentCultureIgnoreCase) ||
                message.Author.IsBot) return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue
                /*&& result.Error.Value != CommandError.UnknownCommand*/)
                await context.Channel.SendMessageAsync(result.ToString());
        }
    }
}
