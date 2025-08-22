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
        }

        private readonly IKrakenSocketClient _Client;
        private readonly ILogger _Logger;
        private readonly Settings _Settings;

        private CancellationTokenSource _CancelTokenSource = new();
        private Dictionary<string, Ticker> _Tickers { get; set; }
        private KrakenBalanceSnapshot[] _BalanceSnapshot { get; set; } = [];
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

        public async Task Start()
        {
            if (_StopFlag) return;

            await _Client.SpotApi.SubscribeToBalanceUpdatesAsync(OnBalanceSnapshot, OnBalanceUpdate, ct: _CancelTokenSource.Token);
            _Logger.LogInformation("Subscribed to balance updates");

            await SubscribeToTickers();

            _StrategyHandler.Init(_Tickers, _BalanceSnapshot);

            while (!_StopFlag)
            {
                await RunStrategies();
            }
        }

        public void Stop()
        {
            _CancelTokenSource.Cancel();
            _StopFlag = true;
        }

        private async Task RunStrategies()
        {
            var action = _StrategyHandler.RunSelectedStrategy();

            if (action is not null)
            {
                await ProcessStrategyAction(action);
            }

            await Task.Delay(_Settings.StrategyInterval);
        }

        private async Task ProcessStrategyAction(StrategyBase.StrategyAction action)
        {
            switch (action.Type)
            {
                case StrategyBase.StrategyAction.ActionType.Buy:
                    await _Client.SpotApi.PlaceOrderAsync(action.Symbol, Kraken.Net.Enums.OrderSide.Buy, Kraken.Net.Enums.OrderType.Market, action.Amount);
                    _Logger.LogInformation("Bought {Amount} {Symbol} {Type}", action.Amount, action.Symbol, Kraken.Net.Enums.OrderType.Market);
                    break;
                case StrategyBase.StrategyAction.ActionType.Sell:
                    await _Client.SpotApi.PlaceOrderAsync(action.Symbol, Kraken.Net.Enums.OrderSide.Sell, Kraken.Net.Enums.OrderType.Market, action.Amount);
                    _Logger.LogInformation("Sold {Amount} {Symbol} {Type}", action.Amount, action.Symbol, Kraken.Net.Enums.OrderType.Market);
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
            _BalanceSnapshot = data.Data;
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
