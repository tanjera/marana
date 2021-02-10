using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Export {

        public static void Signals_To_CSV(List<Data.Signal> signals, string filepath) {
            using (StreamWriter sw = new StreamWriter(filepath)) {
                sw.WriteLine("symbol, exchange, alpaca_id, timestamp, description, direction, strength, yahoo_link, yahoo_chart");

                foreach (Data.Signal s in signals) {
                    sw.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}",
                        s.Asset.Symbol,
                        s.Asset.Exchange,
                        s.Asset.ID,
                        s.Timestamp,
                        s.Description,
                        s.Direction,
                        s.Strength,
                        String.Format("https://finance.yahoo.com/quote/{0}", s.Asset.Symbol),
                        String.Format("https://finance.yahoo.com/chart/{0}", s.Asset.Symbol)
                        );
                }
            }
        }

        public static void Data_To_CSV(Data.Daily dd, string filepath) {
            using (StreamWriter sw = new StreamWriter(filepath)) {
                sw.WriteLine("timestamp, open, high, low, close, volume, sma7, sma20, sma50, sma100, sma200, rsi");

                foreach (Data.Daily.Price p in dd.Prices) {
                    sw.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}",
                        p.Date,
                        p.Open,
                        p.High,
                        p.Low,
                        p.Close,
                        p.Volume,
                        p.Metric.SMA7,
                        p.Metric.SMA20,
                        p.Metric.SMA50,
                        p.Metric.SMA100,
                        p.Metric.SMA200,
                        p.Metric.RSI14
                        );
                }
            }
        }
    }
}