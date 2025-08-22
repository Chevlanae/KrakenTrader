using Kraken.Net.Objects.Models.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenTrader.Strategies
{
    public class StrategyFiftyFifty : StrategyBase
    {
        public override StrategyAction DetermineAction(Dictionary<string, Ticker> tickers, KrakenBalanceSnapshot balanceSnapshot)
        {
            // init random and collection
            Random rand = new();
            List<Ticker> selectedTickers = [];

            // select two random items from tickers
            for (int i = 0; i < 2; i++)
            {
                int randomIndex = rand.Next(0, tickers.Count() - 1);
                Ticker ticker = tickers.Values.ToArray()[randomIndex];
                selectedTickers.Add(ticker);
            }
            
            // determine action type from price change percentage
            // if less than -5 percent, buy
            // if greater than -5 percent, sell
            // else hold
            Ticker selectedTicker = selectedTickers[rand.Next(0, 1)];
            StrategyAction.ActionType actionType;
            if(selectedTicker.PriceChangePercentage < -5) actionType = StrategyAction.ActionType.Buy;
            else if(selectedTicker.PriceChangePercentage > 5) actionType = StrategyAction.ActionType.Sell;
            else actionType = StrategyAction.ActionType.Hold;

            decimal percent = (decimal)(rand.NextDouble() * 0.2);

            decimal amount = balanceSnapshot.Balance * percent;

            return new() { Symbol = selectedTicker.Symbol, Asset = balanceSnapshot.Asset, Type = actionType, Amount = amount };

        }
    }
}
