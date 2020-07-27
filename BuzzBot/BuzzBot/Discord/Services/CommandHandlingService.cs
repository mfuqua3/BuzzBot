using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BuzzBot.Discord.Utility;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Discord.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        private readonly HashSet<ulong> _channels;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IConfiguration configuration)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _channels = configuration.GetSection("authorizedChannels").AsEnumerable()
                .Where(c => ulong.TryParse(c.Value, out _)).Select(c => ulong.Parse(c.Value)).ToHashSet();

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
            if (!_channels.Contains(rawMessage.Channel.Id)) return;
            var argPos = 0;
            //if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;
            if (!message.HasCharPrefix('!', ref argPos) ||
                message.Author.IsBot) return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider, MultiMatchHandling.Best);

            if (result.Error.HasValue
                /*&& result.Error.Value != CommandError.UnknownCommand*/)
                await context.Channel.SendMessageAsync(result.ToString());
        }
    }
}
