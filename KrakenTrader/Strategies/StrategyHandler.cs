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
        private readonly ILogger _Logger;

        private Dictionary<string, Ticker> _Tickers { get; set; } = [];
        private StrategyBase? _SelectedStrategy { get; set; }
        private string? _WalletAsset { get; set; }
        private KrakenBalanceSnapshot[] _BalanceSnapshot { get; set; } = [];
        private readonly Assembly _ExecutingAssembly = Assembly.GetExecutingAssembly();

        private List<Type> _AvailableStrategies = [];

        public StrategyHandler(ILogger<StrategyHandler> logger)
        {
            _Logger = logger;

            foreach (var type in _ExecutingAssembly.GetTypes())
            {
                if (type.IsClass && type.IsPublic && type.Namespace == "KrakenTrader.Strategies")
                {
                    _AvailableStrategies.Add(type);
                }
            }
        }

        public void Init(Dictionary<string, Ticker> tickers, KrakenBalanceSnapshot[] balanceSnapshots, string walletAsset)
        {
            _Tickers = tickers;
            _BalanceSnapshot = balanceSnapshots;
            _WalletAsset = walletAsset;
        }

        public void SelectStrategy(string selectedStrategy)
        {
            try
            {
                foreach (Type strategy in _AvailableStrategies)
                {
                    if (strategy.Name == selectedStrategy)
                    {
                        object? instance = Activator.CreateInstance(strategy);

                        if (instance is not null)
                        {
                            _SelectedStrategy = (StrategyBase)instance;
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

        public StrategyBase.StrategyAction? RunSelectedStrategy(Ticker selectedTicker)
        {
            return _SelectedStrategy?.DetermineAction(selectedTicker, balanceSnapshots: _BalanceSnapshot.FirstOrDefault(s => s.Asset == _WalletAsset));
        }

        public StrategyBase.StrategyAction? RunSelectedStrategy(Dictionary<string, Ticker> tickers)
        {
            return _SelectedStrategy?.DetermineAction(tickers, balanceSnapshots: _BalanceSnapshot.FirstOrDefault(s => s.Asset == _WalletAsset));
        }
    }
}
