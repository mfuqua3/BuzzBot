using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Discord.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordComponents(this IServiceCollection services)
        {
            var client = new DiscordSocketClient();
            services.AddSingleton(client)
                .AddSingleton<AdministrationService>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<LogService>()
                .AddSingleton<ItemRequestService>()
                .AddSingleton<IRaidService, RaidService>()
                .AddTransient<PriorityReportingService>()
                .AddSingleton<PageService>()
                .AddSingleton<QueryService>()
                .AddTransient<DocumentationService>()
                .AddTransient<AuditService>();
            return services;
        }
    }
}