using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Wowhead.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWowheadComponents(this IServiceCollection services)
        {
            services.AddSingleton<IWowheadClient, WowheadClient>();
            return services;
        }
    }
}