using Kraken.Net.Objects.Models.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenTrader
{
    public class Ticker(string symbol)
    {
        public string Symbol { get; set; } = symbol;
        public int? SubscribeId { get; set; }
        public decimal? Price { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? PriceChange { get; set; }
        public decimal? PriceChangePercentage { get; set; }

        public void Update(KrakenTickerUpdate data)
        {
            Price = data.LastPrice;
            LowPrice = data.LowPrice;
            HighPrice = data.HighPrice;
            PriceChange = data.PriceChange;
            PriceChangePercentage = data.PriceChangePercentage;
        }
    }
}
