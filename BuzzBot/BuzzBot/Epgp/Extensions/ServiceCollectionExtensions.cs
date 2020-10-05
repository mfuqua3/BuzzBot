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
                .AddTransient<NexusHubItemPoller>()
                .AddSingleton<DecayProcessor>()
                .AddSingleton<IAliasEventAlerter, AliasEventAlerter>()
                .AddScoped<IUserService, UserService>()
                .AddTransient<IEpgpTransactionFactory, EpgpTransactionFactory>()
                .AddSingleton<IRaidRepository, RaidRepository>();

            services.AddTransient<AhnQirajTempleItemMapper>();
            services.AddTransient<IItemResolver, ItemResolver>();

            services.AddTransient<ItemMapperResolver>(provider => () => 
                new IItemMapper[]
            {
                provider.GetService<AhnQirajTempleItemMapper>()
            });
            return services;
        }
    }
}