using BuzzBot.Discord.Modules;
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
                .AddScoped<ScopedCommandContext>()
                .AddSingleton<IAdministrationService, AdministrationService>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<LogService>()
                .AddSingleton<ItemRequestService>()
                .AddScoped<IRaidService, RaidService>()
                .AddScoped<IPriorityReportingService, PriorityReportingService>()
                .AddSingleton<IPageService, PageService>()
                .AddSingleton<IQueryService, QueryService>()
                .AddScoped<IDocumentationService, DocumentationService>()
                .AddScoped<IAuditService, AuditService>()
                .AddSingleton<IEmoteService, EmoteService>()
                .AddScoped<IItemService, ItemService>()
                .AddScoped<IBankService, BankService>();
            return services;
        }
    }
}