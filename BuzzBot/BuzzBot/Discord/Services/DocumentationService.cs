using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Utility;
using Discord;
using Discord.Commands;

namespace BuzzBot.Discord.Services
{
    public class DocumentationService : IDocumentationService
    {
        private readonly CommandService _commandService;
        private readonly IAdministrationService _administrationService;
        private readonly IPageService _pageService;

        public DocumentationService(CommandService commandService, IAdministrationService administrationService, IPageService pageService)
        {
            _commandService = commandService;
            _administrationService = administrationService;
            _pageService = pageService;
        }

        public async Task SendDocumentation(IMessageChannel channel, string moduleName, ulong requestingUser)
        {
            var commands = _commandService.Commands
                .Where(cmd => cmd.Module.Name.Equals(moduleName))
                .Where(cmd => !string.IsNullOrEmpty(cmd.Summary))
                .OrderBy(cmd => cmd.Name);
            var pageBuilder = new RowHeaderPageFormatBuilder();
            pageBuilder.AddRow("Command:")
                .AddRow("Summary:")
                .AddRow("Usage:")
                .LinesPerPage(5);
            foreach (var command in commands)
            {
                if (command.Preconditions.Any(pc => pc.GetType() == typeof(RequiresBotAdminAttribute)))
                {
                    if (!_administrationService.IsUserAdmin(requestingUser)) continue;
                }

                pageBuilder.AddSectionDefinition(new[]
                {
                    BuildCommandString(command),
                    command.Summary,
                    !string.IsNullOrWhiteSpace(command.Remarks) ? command.Remarks : @"N/A"
                });
            }

            var format = pageBuilder.Build();
            format.HorizontalRule = null;
            format.HeaderLine = $"Here's a list of commands and their descriptions ([*opt] = optional parameter):\n";
            await _pageService.SendPages(channel, format);
        }

        private string BuildCommandString(CommandInfo command)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(command.Name);
            if (!command.Parameters.Any())
                return stringBuilder.ToString();

            stringBuilder.Append("(");
            for (var index = 0; index < command.Parameters.Count; index++)
            {
                if (index != 0) stringBuilder.Append(", ");
                var parameter = command.Parameters[index];
                if (parameter.IsOptional)
                    stringBuilder.Append(@"[*opt] ");
                stringBuilder.Append(parameter.Type.Name);
            }

            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }
    }
}