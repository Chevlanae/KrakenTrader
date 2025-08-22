using Kraken.Net.Clients;
using Kraken.Net.Interfaces.Clients;
using KrakenTrader.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace KrakenTrader
{
    internal class Program
    {
        public static string LocalAppDataPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\KrakenTrader";
        public static string LogsDirectoryPath = $"{LocalAppDataPath}\\Logs";
        private static string? EnvironmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        private static IHost _Host = new HostBuilder()
            .UseEnvironment(EnvironmentName ?? "Development")
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSerilog((context, config) =>
                {
                    config.MinimumLevel.Verbose();
                    config.WriteTo.Console();
                    config.WriteTo.File($"{LocalAppDataPath}\\Logs\\log.txt", rollingInterval: RollingInterval.Day);
                });

                services.Configure<KrakenTrader.Settings>(context.Configuration.GetSection("KrakenTrader.Settings"));
                services.Configure<StrategyHandler.Settings>(context.Configuration.GetSection("StrategyHandler.Settings"));

                services.AddKraken(context.Configuration.GetSection("Kraken"));
                services.AddSingleton<KrakenTrader>();
                services.AddSingleton<StrategyHandler>();
            })
            .Build();

        public static KrakenTrader? _Trader;

        public static T GetRequiredService<T>() where T : class
        {
            return _Host.Services.GetRequiredService<T>();
        }

        static async Task Main(string[] args)
        {
            _Host.Start();
            _Trader = GetRequiredService<KrakenTrader>();

            Console.CancelKeyPress += Console_CancelKeyPress;

            await _Trader.Start();
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            _Trader?.Stop();
            _Host.StopAsync();
        }
    }
}
