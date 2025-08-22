using Kraken.Net.Objects.Models.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenTrader.Strategies
{
    public class StrategyPlusTwoMinusTwo : StrategyBase
    {
        public override StrategyAction? DetermineAction(Ticker ticker, KrakenBalanceSnapshot? balanceSnapshot)
        {
            if(balanceSnapshot is null)
            {
                throw new ArgumentException("Balance snapshot cannot be null", nameof(balanceSnapshot));
            }

            // init random and collection
            Random rand = new();
            
            // determine action type from price change percentage
            // if less than -5 percent, buy
            // if greater than -5 percent, sell
            // else hold
            StrategyAction.ActionType actionType;
            if(ticker.PriceChangePercentage < -2) actionType = StrategyAction.ActionType.Buy;
            else if(ticker.PriceChangePercentage > 2) actionType = StrategyAction.ActionType.Sell;
            else actionType = StrategyAction.ActionType.Hold;

            decimal percent = (decimal)(rand.NextDouble() * 0.2);

            decimal amount = balanceSnapshot.Balance * percent;

            return new() { Symbol = ticker.Symbol, Asset = balanceSnapshot.Asset, Type = actionType, Amount = amount };
        }
    }
}
