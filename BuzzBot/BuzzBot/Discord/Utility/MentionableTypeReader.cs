using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Utility
{
    public class MentionableTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (!context.Message.MentionedUserIds.Any() && !context.Message.MentionedRoleIds.Any())
                return TypeReaderResult.FromError(CommandError.Unsuccessful, "Could not parse, no mentions in message");
            if (MentionUtils.TryParseRole(input, out var roleId) && context.Guild.GetRole(roleId) is { } parsedRole)
            {
                return TypeReaderResult.FromSuccess(parsedRole);
            }
            if (MentionUtils.TryParseUser(input, out var userId) && await context.Guild.GetUserAsync(userId) is { } parsedUser)
            {
                return TypeReaderResult.FromSuccess(parsedUser);
            }
            return TypeReaderResult.FromError(CommandError.ParseFailed, "Could not parse mention");
        }
    }
}