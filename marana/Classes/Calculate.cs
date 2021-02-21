using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Skender.Stock.Indicators;

namespace Marana {

    public class Calculate {

        // Calculates all the indicator metrics for a dataset
        public static async Task<Data.Daily> Metrics(Data.Daily dd) {
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
            RocResult[] roc14 = amount > 15 ? Indicator.GetRoc(dd.Prices, 14).ToArray() : null;

            BollingerBandsResult[] bb = amount > 20 ? Indicator.GetBollingerBands(dd.Prices).ToArray() : null;
            MacdResult[] macd = amount > 140 ? Indicator.GetMacd(dd.Prices).ToArray() : null;
            StochResult[] stoch = amount > 20 ? Indicator.GetStoch(dd.Prices).ToArray() : null;
            ChopResult[] chop = amount > 15 ? Indicator.GetChop(dd.Prices).ToArray() : null;

            // Put indicator data back into data set for usability

            for (int i = 0; i < dd.Metrics.Count; i++) {
                try {
                    dd.Metrics[i].SMA7 = sma7?[i].Sma;
                    dd.Metrics[i].SMA20 = sma20?[i].Sma;
                    dd.Metrics[i].SMA50 = sma50?[i].Sma;
                    dd.Metrics[i].SMA100 = sma100?[i].Sma;
                    dd.Metrics[i].SMA200 = sma200?[i].Sma;

                    dd.Metrics[i].EMA7 = ema7?[i].Ema;
                    dd.Metrics[i].EMA20 = ema20?[i].Ema;
                    dd.Metrics[i].EMA50 = ema50?[i].Ema;

                    dd.Metrics[i].DEMA7 = dema7?[i].Ema;
                    dd.Metrics[i].DEMA20 = dema20?[i].Ema;
                    dd.Metrics[i].DEMA50 = dema50?[i].Ema;

                    dd.Metrics[i].TEMA7 = tema7?[i].Ema;
                    dd.Metrics[i].TEMA20 = tema20?[i].Ema;
                    dd.Metrics[i].TEMA50 = tema50?[i].Ema;

                    dd.Metrics[i].Choppiness = chop?[i].Chop;
                    dd.Metrics[i].RSI = rsi?[i].Rsi;
                    dd.Metrics[i].ROC14 = roc14?[i].Roc;

                    dd.Metrics[i].BB = bb?[i];
                    dd.Metrics[i].MACD = macd?[i];
                    dd.Metrics[i].Stochastic = stoch?[i];
                } catch (Exception ex) {
                    Prompt.WriteLine($"Error casting indicators to dataset for {dd.Asset.Symbol}!", ConsoleColor.Red);
                    await Error.Log($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                }
            }

            return dd;
        }
    }
}