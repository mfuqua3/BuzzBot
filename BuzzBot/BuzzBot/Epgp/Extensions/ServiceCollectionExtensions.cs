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
                .AddTransient<IRaidFactory, RaidFactory>()
                .AddSingleton<IEpgpConfigurationService, EpgpConfigurationService>()
                .AddSingleton<IEpgpService, EpgpService>()
                .AddTransient<IRaidMonitorFactory, RaidMonitorFactory>()
                .AddSingleton<IAliasService, AliasService>()
                .AddSingleton<NexusHubClient>()
                .AddTransient<IUserService, UserService>()
                .AddSingleton<IRaidRepository, RaidRepository>();
            return services;
        }
    }
}