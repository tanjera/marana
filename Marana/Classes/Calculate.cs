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

            EmaResult[] ema7 = amount > 110 ? Indicator.GetEma(dd.Prices, 7).ToArray() : null;
            EmaResult[] ema20 = amount > 120 ? Indicator.GetEma(dd.Prices, 20).ToArray() : null;
            EmaResult[] ema50 = amount > 150 ? Indicator.GetEma(dd.Prices, 50).ToArray() : null;

            EmaResult[] dema7 = amount > 120 ? Indicator.GetDoubleEma(dd.Prices, 7).ToArray() : null;
            EmaResult[] dema20 = amount > 140 ? Indicator.GetDoubleEma(dd.Prices, 20).ToArray() : null;
            EmaResult[] dema50 = amount > 200 ? Indicator.GetDoubleEma(dd.Prices, 50).ToArray() : null;

            EmaResult[] tema7 = amount > 130 ? Indicator.GetTripleEma(dd.Prices, 7).ToArray() : null;
            EmaResult[] tema20 = amount > 160 ? Indicator.GetTripleEma(dd.Prices, 20).ToArray() : null;
            EmaResult[] tema50 = amount > 250 ? Indicator.GetTripleEma(dd.Prices, 50).ToArray() : null;

            RsiResult[] rsi = amount > 140 ? Indicator.GetRsi(dd.Prices).ToArray() : null;

            BollingerBandsResult[] bb = amount > 20 ? Indicator.GetBollingerBands(dd.Prices).ToArray() : null;

            MacdResult[] macd = amount > 140 ? Indicator.GetMacd(dd.Prices).ToArray() : null;

            // Put indicator data back into data set for usability

            for (int i = 0; i < dd.Metrics.Count; i++) {
                try {
                    dd.Metrics[i].SMA7 = sma7 != null ? sma7[i].Sma : null;
                    dd.Metrics[i].SMA20 = sma20 != null ? sma20[i].Sma : null;
                    dd.Metrics[i].SMA50 = sma50 != null ? sma50[i].Sma : null;
                    dd.Metrics[i].SMA100 = sma100 != null ? sma100[i].Sma : null;
                    dd.Metrics[i].SMA200 = sma200 != null ? sma200[i].Sma : null;

                    dd.Metrics[i].EMA7 = ema7 != null ? ema7[i].Ema : null;
                    dd.Metrics[i].EMA20 = ema20 != null ? ema20[i].Ema : null;
                    dd.Metrics[i].EMA50 = ema50 != null ? ema50[i].Ema : null;

                    dd.Metrics[i].DEMA7 = dema7 != null ? dema7[i].Ema : null;
                    dd.Metrics[i].DEMA20 = dema20 != null ? dema20[i].Ema : null;
                    dd.Metrics[i].DEMA50 = dema50 != null ? dema50[i].Ema : null;

                    dd.Metrics[i].TEMA7 = tema7 != null ? tema7[i].Ema : null;
                    dd.Metrics[i].TEMA20 = tema20 != null ? tema20[i].Ema : null;
                    dd.Metrics[i].TEMA50 = tema50 != null ? tema50[i].Ema : null;

                    dd.Metrics[i].RSI = rsi != null ? rsi[i].Rsi : null;

                    dd.Metrics[i].BB = bb != null ? bb[i] : null;

                    dd.Metrics[i].MACD = macd != null ? macd[i] : null;
                } catch (Exception ex) {
                    Prompt.WriteLine($"Error casting indicators to dataset for {dd.Asset.Symbol}!", ConsoleColor.Red);
                }
            }
        }
    }
}