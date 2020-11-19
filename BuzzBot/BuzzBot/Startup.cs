using System;
using System.Threading.Tasks;
using AutoMapper;
using BuzzBot.ClassicGuildBank.Extensions;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBot.Epgp;
using BuzzBot.Epgp.Extensions;
using BuzzBot.NexusHub;
using BuzzBot.Utility;
using BuzzBot.Wowhead.Extensions;
using BuzzBotData.Extensions;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuzzBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mapperCfg = new MapperConfiguration(cfg => cfg.AddProfile<BuzzBotMapperProfile>());
            services
                .AddSingleton(mapperCfg.CreateMapper())
                .AddSingleton<IBuzzBotDbContextFactory, BuzzBotDbContextFactory>()
                .AddData()
                .AddClassicGuildBankComponents()
                .AddEpgpComponents()
                .AddWowheadComponents()
                .AddDiscordComponents()
                .AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            StartClient(app.ApplicationServices);
        }

        private async void StartClient(IServiceProvider services)
        {
            var client = services.GetService<DiscordSocketClient>();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);
            services.GetRequiredService<NexusHubItemPoller>().Initialize();
            services.GetRequiredService<IDecayProcessor>().Initialize();

            await client.LoginAsync(TokenType.Bot, Configuration["token"]);
            await client.StartAsync();
            await Task.Delay(-1);

        }
    }
}
