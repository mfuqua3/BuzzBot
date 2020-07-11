using BuzzBot.Wowhead;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Epgp.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEpgpComponents(this IServiceCollection services)
        {
            services.AddSingleton<EpgpCalculator>();
            return services;
        }
    }
}