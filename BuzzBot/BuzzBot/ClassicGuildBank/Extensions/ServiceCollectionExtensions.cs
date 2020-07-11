using BuzzBot.ClassicGuildBank.Buzz;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.ClassicGuildBank.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddClassicGuildBankComponents(this IServiceCollection services)
        {
            services.AddSingleton<ClassicGuildBankClient>();
            return services;
        }
    }
}