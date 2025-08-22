using Kraken.Net.Objects.Models.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenTrader.Strategies
{
    public class StrategyBase
    {
        public class StrategyAction
        {
            public enum ActionType
            {
                Buy,
                Sell,
                Hold
            }

            public required string Symbol { get; set; }
            public required string Asset { get; set; }
            public ActionType Type { get; set; }
            public decimal Amount { get; set; }
        }

        protected StrategyBase() { }

        public virtual StrategyAction? DetermineAction(Dictionary<string, Ticker> tickers, KrakenBalanceSnapshot? balanceSnapshots) { throw new NotImplementedException(); }
        public virtual StrategyAction? DetermineAction(Ticker ticker, KrakenBalanceSnapshot? balanceSnapshots) { throw new NotImplementedException(); }
    }
}
