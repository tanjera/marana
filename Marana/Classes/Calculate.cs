using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Skender.Stock.Indicators;

namespace Marana {

    public class Calculate {

        // Calculates all the indicator metrics for a dataset
        public static void Metrics(ref Data.Daily dd) {
            // Prepare the data structures and tie Prices <-> Metrics

            dd.Prices.Sort((a, b) => a.Date.CompareTo(b.Date));
            foreach (Data.Daily.Price p in dd.Prices) {
                Data.Daily.Metric m = new Data.Daily.Metric() { Price = p };
                p.Metric = m;
                dd.Metrics.Add(m);
            }

            // Run all calculations! Get all indicators!

            int amount = dd.Prices.Count;

            SmaResult[] sma7 = amount > 7 ? Indicator.GetSma(dd.Prices, 7).ToArray() : null;
            SmaResult[] sma20 = amount > 20 ? Indicator.GetSma(dd.Prices, 20).ToArray() : null;
            SmaResult[] sma50 = amount > 50 ? Indicator.GetSma(dd.Prices, 50).ToArray() : null;
            SmaResult[] sma100 = amount > 100 ? Indicator.GetSma(dd.Prices, 100).ToArray() : null;
            SmaResult[] sma200 = amount > 200 ? Indicator.GetSma(dd.Prices, 200).ToArray() : null;

            RsiResult[] rsi14 = amount > 140 ? Indicator.GetRsi(dd.Prices).ToArray() : null;

            BollingerBandsResult[] bb20 = amount > 20 ? Indicator.GetBollingerBands(dd.Prices).ToArray() : null;

            MacdResult[] macd12269 = amount > 140 ? Indicator.GetMacd(dd.Prices).ToArray() : null;

            // Put indicator data back into data set for usability

            for (int i = 0; i < dd.Metrics.Count; i++) {
                try {
                    if ((sma7 != null && sma7[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (sma20 != null && sma20[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (sma50 != null && sma50[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (sma100 != null && sma100[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (sma200 != null && sma200[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (rsi14 != null && rsi14[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (bb20 != null && bb20[i].Date != dd.Metrics[i].Timestamp.Date)
                        || (macd12269 != null && macd12269[i].Date != dd.Metrics[i].Timestamp.Date))
                        throw new ApplicationException("Indicator date mismatch with Price data.");

                    dd.Metrics[i].SMA7 = sma7 != null ? sma7[i].Sma : null;
                    dd.Metrics[i].SMA20 = sma20 != null ? sma20[i].Sma : null;
                    dd.Metrics[i].SMA50 = sma50 != null ? sma50[i].Sma : null;
                    dd.Metrics[i].SMA100 = sma100 != null ? sma100[i].Sma : null;
                    dd.Metrics[i].SMA200 = sma200 != null ? sma200[i].Sma : null;

                    dd.Metrics[i].RSI14 = rsi14 != null ? rsi14[i].Rsi : null;
                    dd.Metrics[i].BB20 = bb20 != null ? bb20[i] : null;
                    dd.Metrics[i].MACD12269 = macd12269 != null ? macd12269[i] : null;
                } catch (Exception ex) {
                    Prompt.WriteLine($"Error casting indicators to dataset for {dd.Asset.Symbol}!", ConsoleColor.Red);
                }
            }
        }
    }
}