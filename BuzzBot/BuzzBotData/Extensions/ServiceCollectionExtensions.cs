﻿using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBotData.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddData(this IServiceCollection services)
        {
            services.AddSingleton<GuildBankDbContext>()
                .AddSingleton<GuildBankRepository>();
            return services;
        }
    }
}