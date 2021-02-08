using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Data {

        public class Daily {
            public Asset Asset = new Asset();
            public List<Price> Prices = new List<Price>();
            public List<Metric> Metrics = new List<Metric>();

            public class Price {
                // Data received from outside API

                public DateTime Timestamp { get; set; }
                public Metric Metric { get; set; }

                public decimal Open { get; set; }
                public decimal High { get; set; }
                public decimal Low { get; set; }
                public decimal Close { get; set; }
                public decimal Volume { get; set; }
            }

            public class Metric {
                public Price Price { get; set; }

                public DateTime Timestamp { get { return Price.Timestamp; } }

                // Simple moving averages, various periods, for price

                public decimal SMA7 { get; set; }
                public decimal SMA20 { get; set; }
                public decimal SMA50 { get; set; }
                public decimal SMA100 { get; set; }
                public decimal SMA200 { get; set; }

                // Moving standard deviation and according ratio (percentage) for price

                public decimal MSD20 { get; set; }
                public decimal MSDr20 { get; set; }

                // Metrics for analyzing trading volume

                public decimal vSMA20 { get; set; }
                public decimal vMSD20 { get; set; }

                // Relative Strength Indicator (RSI)

                public decimal RSI { get; set; }

                // Quick references of whether the calculations were possible

                public bool HasSMA7 { get { return SMA7 > 0; } }
                public bool HasSMA20 { get { return SMA20 > 0; } }
                public bool HasSMA50 { get { return SMA50 > 0; } }
                public bool HasSMA100 { get { return SMA100 > 0; } }
                public bool HasSMA200 { get { return SMA200 > 0; } }

                public bool HasRSI { get { return RSI > 0; } }
            }
        }

        public class Signal {
            public Asset Asset;
            public DateTime Timestamp;
            public string Description;
            public Directions Direction;

            public enum Directions {
                None,
                Same,

                Buy,
                Sell,

                Up,
                Down,

                Plateau,
                Peak,
                Trough
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