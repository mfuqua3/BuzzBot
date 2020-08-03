using BuzzBotData.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBotData.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddData(this IServiceCollection services)
        {
            services.AddTransient<BuzzBotDbContext>()
                .AddTransient<IDbContextFactory, DbContextFactory>();
            return services;
        }
    }
}