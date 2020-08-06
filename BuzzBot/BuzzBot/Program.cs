using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace BuzzBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main function");

                var isService = !(Debugger.IsAttached || args.Contains("--console"));
                var pathToContentRoot = Directory.GetCurrentDirectory();
                var webHostArgs = args.Where(arg => arg != "--console").ToArray();

                if (isService)
                {
                    var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                    pathToContentRoot = Path.GetDirectoryName(pathToExe);
                }

                var host = WebHost.CreateDefaultBuilder(webHostArgs)
                    .ConfigureAppConfiguration(builder =>
                    {
                        builder.SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("botConfig.json");
                    })
                    .UseContentRoot(pathToContentRoot)
                    .UseStartup<Startup>()
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Information);
                    })
                    .UseNLog()
                    .Build();

                if (isService)
                {
                    host.RunAsService();
                }
                else
                {
                    host.Run();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in init");
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
