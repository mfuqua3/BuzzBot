using BuzzBot.NexusHub;
using BuzzBot.Wowhead;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Epgp.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEpgpComponents(this IServiceCollection services)
        {
            services.AddSingleton<IEpgpCalculator, EpgpCalculator>()
                .AddScoped<IRaidFactory, RaidFactory>()
                .AddSingleton<IEpgpConfigurationService, EpgpConfigurationService>()
                .AddScoped<IEpgpService, EpgpService>()
                .AddScoped<IRaidMonitorFactory, RaidMonitorFactory>()
                .AddScoped<IAliasService, AliasService>()
                .AddSingleton<NexusHubClient>()
                .AddScoped<IUserService, UserService>()
                .AddSingleton<IRaidRepository, RaidRepository>();
            return services;
        }
    }
}