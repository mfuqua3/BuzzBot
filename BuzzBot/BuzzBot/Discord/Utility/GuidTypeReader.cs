using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace BuzzBot.Discord.Utility
{
    public class GuidTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if(!Guid.TryParse(input, out var guidResult))
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Unable to parse a valid GUID from the input provided");
            return await Task.FromResult(TypeReaderResult.FromSuccess(guidResult));
        }
    }
}