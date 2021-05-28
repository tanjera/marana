using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Skender.Stock.Indicators;

namespace Marana {

    public class Data {

        public class Daily {
            public Asset Asset = new Asset();
            public List<Price> Prices = new List<Price>();
            public List<Metric> Metrics = new List<Metric>();

            public class Price : IQuote {
                // Data received from outside API

                public DateTime Date { get; set; }

                public Metric Metric { get; set; }

                public decimal Open { get; set; }
                public decimal High { get; set; }
                public decimal Low { get; set; }
                public decimal Close { get; set; }
                public decimal Volume { get; set; }

                public Skender.Stock.Indicators.Quote Quote {
                    get {
                        return new Skender.Stock.Indicators.Quote() {
                            Date = Date,
                            Open = Open,
                            High = High,
                            Low = Low,
                            Close = Close,
                            Volume = Volume
                        };
                    }
                }
            }

            public class Metric {
                public Price Price { get; set; }

                public DateTime Timestamp { get { return Price.Date; } }

                // Simple moving averages, various periods, for price

                public decimal? SMA7 { get; set; }
                public decimal? SMA20 { get; set; }
                public decimal? SMA50 { get; set; }
                public decimal? SMA100 { get; set; }
                public decimal? SMA200 { get; set; }

                // Exponential moving averages

                public decimal? EMA7 { get; set; }
                public decimal? EMA20 { get; set; }
                public decimal? EMA50 { get; set; }
                public decimal? DEMA7 { get; set; }
                public decimal? DEMA20 { get; set; }
                public decimal? DEMA50 { get; set; }
                public decimal? TEMA7 { get; set; }
                public decimal? TEMA20 { get; set; }
                public decimal? TEMA50 { get; set; }

                // Rate of Change (ROC) %

                public decimal? ROC7 { get; set; }
                public decimal? ROC14 { get; set; }
                public decimal? ROC50 { get; set; }
                public decimal? ROC100 { get; set; }
                public decimal? ROC200 { get; set; }

                // Specific indicators

                public decimal? Choppiness { get; set; }            // Choppiness Index
                public decimal? RSI { get; set; }                   // Relative Strength Indicator (RSI)

                public BollingerBandsResult BB { get; set; }        // Bollinger Bands
                public MacdResult MACD { get; set; }                // Moving Average Convergence/Divergence (MACD)
                public StochResult Stochastic { get; set; }         // Stochastic Indicator

                public Metric() {
                    BB = new BollingerBandsResult();
                    MACD = new MacdResult();
                }
            }
        }

        public class Asset {
            public string ID { get; set; }
            public string Symbol { get; set; }
            public string Class { get; set; }
            public string Exchange { get; set; }
            public string Status { get; set; }
            public bool Tradeable { get; set; }
            public bool Marginable { get; set; }
            public bool Shortable { get; set; }
            public bool EasyToBorrow { get; set; }
        }

        public class Strategy {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Entry { get; set; }
            public string ExitGain { get; set; }
            public string ExitStopLoss { get; set; }
            public string SortBy { get; set; }
        }

        public class Instruction {
            public string Name { get; set; }
            public string Description { get; set; }
            public Format Format { get; set; }
            public Frequency Frequency { get; set; }
            public string Symbol { get; set; }
            public string Strategy { get; set; }
            public int Quantity { get; set; }

            public bool Enabled { get; set; }           // Flag: whether the instruction should run at all.
            public bool Buy { get; set; }               // Flag: whether to place buy orders? False to allow for liquidation.
            public bool Sell { get; set; }              // Flag: whether to place sell orders? False to hold trade position.

            public Instruction() {
                Enabled = false;
                Buy = false;
                Sell = false;

                Format = Format.Paper;
            }

            public bool IsEnabled { get { return Enabled; } }
            public bool ToBuy { get { return Buy; } }
            public bool ToSell { get { return Sell; } }
        }

        public class Order {
            public string Symbol { get; set; }
            public int Quantity { get; set; }
            public Direction Transaction { get; set; }

            public enum Direction {
                Buy,
                Sell
            }
        }

        public class Position {
            public string ID { get; set; }
            public string Symbol { get; set; }
            public int Quantity { get; set; }
        }

        public class Quote {
            public string Symbol { get; set; }
            public decimal? Price { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class Test {
            public Asset Asset { get; set; }
            public Strategy Strategy { get; set; }
            public Instruction Instruction { get; set; }

            public List<Trade> Trades { get; set; }

            public int Shares { get; set; }

            public decimal AssetRateOfChange { get; set; }

            public int DaysHeld { get; set; }

            public decimal GainAmount { get; set; }
            public decimal GainPercent { get; set; }
            public decimal GainPercentPerDay { get; set; }

            public Test() {
                Trades = new List<Trade>();
            }

            public class Trade {
                public DateTime Timestamp { get; set; }
                public Direction Transaction { get; set; }
                public decimal Price { get; set; }
                public decimal Gain { get; set; }
                public int Quantity { get; set; }

                public enum Sales {
                    Gain,
                    StopLoss
                }

                public enum Direction {
                    Buy,
                    Sell
                }
            }
        }

        public enum Frequency {
            Daily,
            Intraday
        }

        public enum Format {
            Paper,
            Live,
            Test
        }

        public static void Select_Assets(ref List<Asset> assets, List<string> args) {
            // Select symbols to update (trim list) based on user input args
            if (args.Count == 0)
                return;

            if (args.Count > 0) {       // Need to trim the symbol list per input args
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                Asset s = null, e = null;

                s = (from pair
                     in assets
                     where pair.Symbol == args[0].Trim().ToUpper()
                     select pair)
                     .DefaultIfEmpty(new Asset()).First();

                if (args.Count > 1)
                    e = (from pair
                         in assets
                         where pair.Symbol == (args.Count > 1 ? args[1] : "").Trim().ToUpper()
                         select pair)
                         .DefaultIfEmpty(new Asset()).First();

                si = assets.IndexOf(s);
                ei = assets.IndexOf(e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    assets.RemoveRange(0, si);
                if (ei > 0)
                    assets.RemoveRange(ei, assets.Count - ei);
            }
        }
    }
}