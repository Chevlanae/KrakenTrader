using CryptoExchange.Net.Objects.Sockets;
using Kraken.Net.Interfaces.Clients;
using Kraken.Net.Objects.Models.Socket;
using Kraken.Net.Objects.Models.Socket.Futures;
using KrakenTrader.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KrakenTrader
{
    internal class KrakenTrader
    {
        public class Settings
        {
            public required string[] TickerSymbols { get; set; }
            public TimeSpan StrategyInterval { get; set; }
            public string SelectedStrategy { get; set; }
            public string WalletAsset { get; set; }
        }

        private readonly IKrakenSocketClient _Client;
        private readonly ILogger _Logger;
        private readonly Settings _Settings;

        private CancellationTokenSource _CancelTokenSource = new();
        private Dictionary<string, Ticker> _Tickers { get; set; }
        private KrakenBalanceSnapshot[] _BalanceSnapshots { get; set; } = [];
        private StrategyHandler _StrategyHandler { get; set; }

        public KrakenTrader(ILogger<KrakenTrader> logger, IOptions<Settings> options)
        {
            _Client = Program.GetRequiredService<IKrakenSocketClient>();
            _Logger = logger;
            _Settings = options.Value;
            _Tickers = [];
            _StrategyHandler = Program.GetRequiredService<StrategyHandler>();

            foreach (string symbol in _Settings.TickerSymbols)
            {
                _Tickers[symbol] = new(symbol);
            }
        }

        private bool _StopFlag = false;

        public async Task Start(Func<StrategyHandler, Dictionary<string, Ticker>, KrakenBalanceSnapshot[], StrategyBase.StrategyAction?> function)
        {
            if (_StopFlag) return;

            await _Client.SpotApi.SubscribeToBalanceUpdatesAsync(OnBalanceSnapshot, OnBalanceUpdate, ct: _CancelTokenSource.Token);
            _Logger.LogInformation("Subscribed to balance updates");

            await SubscribeToTickers();

            _StrategyHandler.Init(_Tickers, _BalanceSnapshots, _Settings.WalletAsset);

            while (!_StopFlag)
            {
                await RunStrategy(function);
            }
        }

        public void Stop()
        {
            _CancelTokenSource.Cancel();
            _StopFlag = true;
        }

        private async Task RunStrategy(Func<StrategyHandler, Dictionary<string, Ticker>, KrakenBalanceSnapshot[], StrategyBase.StrategyAction?> function)
        {
            _StrategyHandler.SelectStrategy(_Settings.SelectedStrategy);

            var result = function(_StrategyHandler, _Tickers, _BalanceSnapshots);

            if (result is not null)
            {
                await ProcessStrategyAction(result);
            }

            await Task.Delay(_Settings.StrategyInterval);
        }

        private async Task ProcessStrategyAction(StrategyBase.StrategyAction action)
        {
            string asset;
            KrakenBalanceSnapshot? result;

            switch (action.Type)
            {
                case StrategyBase.StrategyAction.ActionType.Buy:

                    asset = action.Symbol.Split('/')[1];
                    result = _BalanceSnapshots.FirstOrDefault(b => b.Asset == asset);

                    if (result is not null && result.Balance < action.Amount)
                    {
                        await _Client.SpotApi.PlaceOrderAsync(action.Symbol, Kraken.Net.Enums.OrderSide.Buy, Kraken.Net.Enums.OrderType.Market, action.Amount);
                        _Logger.LogInformation("Bought {Amount} {Symbol} {Type}", action.Amount, action.Symbol, Kraken.Net.Enums.OrderType.Market);
                    }
                    else
                    {
                        _Logger.LogWarning("Not enough balance to buy {Amount} {Symbol}", action.Amount, action.Symbol);
                    }

                    break;

                case StrategyBase.StrategyAction.ActionType.Sell:

                    asset = action.Symbol.Split('/')[0];
                    result = _BalanceSnapshots.FirstOrDefault(b => b.Asset == asset);

                    if(result is not null && result.Balance > action.Amount)
                    {
                        await _Client.SpotApi.PlaceOrderAsync(action.Symbol, Kraken.Net.Enums.OrderSide.Sell, Kraken.Net.Enums.OrderType.Market, action.Amount);
                        _Logger.LogInformation("Sold {Amount} {Symbol} {Type}", action.Amount, action.Symbol, Kraken.Net.Enums.OrderType.Market);
                    }
                    else
                    {
                        _Logger.LogWarning("Not enough balance to sell {Amount} {Symbol}", action.Amount, action.Symbol);
                    }

                    break;

                case StrategyBase.StrategyAction.ActionType.Hold:
                    break;
            }
        }

        private async Task SubscribeToTickers()
        {
            foreach (string symbol in _Tickers.Keys)
            {
                var result = await _Client.SpotApi.SubscribeToTickerUpdatesAsync(symbol, OnTickerUpdate, ct: _CancelTokenSource.Token);
                _Logger.LogInformation("Subscribed to symbol {Symbol}", symbol);

                if (result is not null && result.Success)
                {
                    _Tickers[symbol] = new(symbol);
                }
            }
        }


        private void OnBalanceSnapshot(DataEvent<KrakenBalanceSnapshot[]> data)
        {
            _BalanceSnapshots = data.Data;
        }

        private void OnBalanceUpdate(DataEvent<KrakenBalanceUpdate[]> data)
        {

        }

        private void OnTickerUpdate(DataEvent<KrakenTickerUpdate> data)
        {
            _Tickers.TryGetValue(data.Data.Symbol, out Ticker? ticker);

            if (ticker is not null)
            {
                ticker.Update(data.Data);
            }
        }

    }
}
