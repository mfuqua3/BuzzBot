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
                .AddSingleton<IAdministrationService, AdministrationService>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<LogService>()
                .AddSingleton<ItemRequestService>()
                .AddSingleton<IRaidService, RaidService>()
                .AddTransient<IPriorityReportingService, PriorityReportingService>()
                .AddSingleton<IPageService, PageService>()
                .AddSingleton<IQueryService, QueryService>()
                .AddTransient<IDocumentationService, DocumentationService>()
                .AddTransient<IAuditService, AuditService>()
                .AddSingleton<IEmoteService, EmoteService>()
                .AddTransient<IItemService, ItemService>()
                .AddTransient<IBankService, BankService>();
            return services;
        }
    }
}