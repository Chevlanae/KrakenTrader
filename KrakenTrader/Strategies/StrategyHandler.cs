using Kraken.Net.Objects.Models.Socket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KrakenTrader.Strategies
{
    internal class StrategyHandler
    {
        public class Settings
        {
            public required string SelectedStrategy { get; set; }
        }

        private readonly ILogger _Logger;
        private readonly Settings _Settings;

        private Dictionary<string, Ticker> _Tickers { get; set; } = [];
        private StrategyBase? _SelectedStrategy { get; set; }
        private KrakenBalanceSnapshot[] _BalanceSnapshot { get; set; } = [];
        private readonly Assembly _ExecutingAssembly = Assembly.GetExecutingAssembly();

        private List<Type> _AvailableStrategies = [];

        public StrategyHandler(ILogger<StrategyHandler> logger, IOptions<Settings> options)
        {
            _Logger = logger;
            _Settings = options.Value;

            foreach(var type in _ExecutingAssembly.GetTypes())
            {
                if (type.IsClass && type.IsPublic && type.Namespace == "KrakenTrader.Strategies")
                {
                    _AvailableStrategies.Add(type);
                }
            }

            SelectStrategy(_Settings.SelectedStrategy);
        }

        public void Init(Dictionary<string, Ticker> tickers, KrakenBalanceSnapshot[] balanceSnapshots)
        {
            _Tickers = tickers;
            _BalanceSnapshot = balanceSnapshots;
        }

        public void SelectStrategy(string selectedStrategy)
        {
            try
            {
                foreach(Type strategy in _AvailableStrategies)
                {
                    if(strategy.Name == selectedStrategy)
                    {
                        object? instance = Activator.CreateInstance(strategy);

                        if(instance is not null)
                        {
                            _SelectedStrategy = (StrategyBase) instance;
                            _Logger.LogInformation("Changed selected strategy to {Strategy}", selectedStrategy);
                        }

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _Logger.LogError(e, "Could not select given strategy {Strategy}", selectedStrategy);
            }
        }

        public StrategyBase.StrategyAction? RunSelectedStrategy()
        {
            return _SelectedStrategy?.DetermineAction(_Tickers, balanceSnapshots: _BalanceSnapshot.First(s => s.Asset == "USD"));
        }
    }
}
