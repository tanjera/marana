using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Analyze {

        // Task to be run when looking to enter new trading in general good market conditions
        public static void Insert_Long(List<string> args, Database db, Settings settings) {
            Run(args, db, settings,
                Strategy_Insert_Long,
                Path.Combine(settings.Directory_Working, $"Insert Long Analysis Signals, {DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.csv"));
        }

        public static void Insert_Short(List<string> args, Database db, Settings settings) {
            Run(args, db, settings,
                Strategy_Insert_Short,
                Path.Combine(settings.Directory_Working, $"Insert Short Analysis Signals, {DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.csv"));
        }

        // Task to be run daily for buy/sell signals to inform trading
        public static void Daily(List<string> args, Database db, Settings settings) {
            Run(args, db, settings,
                Strategy_Daily,
                Path.Combine(settings.Directory_Working, $"Daily Analysis Signals, {DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.csv"));
        }

        public static void Run(List<string> args, Database db, Settings settings,
                Func<Data.Daily, List<Data.Signal>> strategy, string filepath) {
            Prompt.WriteLine("Querying database for list of ticker symbols.");

            List<Data.Asset> assets = db.GetData_Assets();
            Data.Select_Assets(ref assets, args);

            List<Data.Signal> signals = new List<Data.Signal>();

            for (int i = 0; i < assets.Count; i++) {
                Prompt.Write($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm")} [{i + 1:0000} / {assets.Count:0000}]  {assets[i].Symbol,-8}  Retrieving data. ");

                Data.Daily dd = db.GetData_TSD(assets[i]);

                if (dd == null) {
                    Prompt.WriteLine("Data unavailable- update library?", ConsoleColor.Yellow);
                    continue;
                }

                Prompt.Write("Calculating metrics. ");
                Calculate.Metrics(ref dd);

                Prompt.Write("Analyzing signals. ");

                signals.AddRange(strategy(dd));

                Prompt.WriteLine("Complete.", ConsoleColor.Green);

                string fp = Path.Combine(settings.Directory_Working, $"{dd.Asset.Symbol}.csv");
                //Export.Data_To_CSV(dd, fp);
            }

            signals.Sort((a, b) => (a.Strength ?? 0).CompareTo(b.Strength ?? 0));

            Prompt.WriteLine($"Signals exported to {filepath}", ConsoleColor.Green);
        }

        public static List<Data.Signal> Strategy_Daily(Data.Daily dd) {
            List<Data.Signal> signals = new List<Data.Signal>();

            for (int i = dd.Metrics.Count - 1; i >= 0 && i < dd.Metrics.Count; i++) {
                if (dd.Metrics[i].RSI14 == null || dd.Metrics[i].SMA200 == null)
                    continue;

                if (dd.Prices[i].Close > dd.Metrics[i].SMA200 && dd.Metrics[i].RSI14 <= 30)
                    signals.Add(new Data.Signal() {
                        Asset = dd.Asset,
                        Timestamp = dd.Prices[i].Date,
                        Description = "RSI < 30; Close > SMA200",
                        Direction = Data.Signal.Directions.Buy,
                        Strength = dd.Metrics[i].RSI14
                    });

                if (dd.Prices[i].Close < dd.Metrics[i].SMA200 && dd.Metrics[i].RSI14 >= 70)
                    signals.Add(new Data.Signal() {
                        Asset = dd.Asset,
                        Timestamp = dd.Prices[i].Date,
                        Description = "RSI > 70; Close < SMA200",
                        Direction = Data.Signal.Directions.Sell,
                        Strength = dd.Metrics[i].RSI14
                    });
            }

            return signals;
        }

        public static List<Data.Signal> Strategy_Insert_Long(Data.Daily dd) {
            List<Data.Signal> signals = new List<Data.Signal>();
            /*
            for (int j = dd.Prices.Count - 1; j >= 0 && j < dd.Prices.Count; j++) {
                if ((dd.Prices[j].Metric.RSI != null && dd.Prices[j].Metric.RSI <= 50)
                    && (dd.Prices[j].Metric.HasSMA200 && dd.Prices[j].Close > dd.Prices[j].Metric.SMA200)
                    && dd.Prices[j].Metric.MGR200 > 0)
                    signals.Add(new Data.Signal() {
                        Asset = dd.Asset,
                        Timestamp = dd.Prices[j].Timestamp,
                        Description = "RSI < 50; Close > SMA100; MGR200 (Strength)",
                        Direction = Data.Signal.Directions.Buy,
                        Strength = dd.Prices[j].Metric.MGR200
                    });
            }
            */
            return signals;
        }

        public static List<Data.Signal> Strategy_Insert_Short(Data.Daily dd) {
            List<Data.Signal> signals = new List<Data.Signal>();
            /*
            for (int j = dd.Prices.Count - 1; j >= 0 && j < dd.Prices.Count; j++) {
                if ((dd.Prices[j].Metric.HasRSI && dd.Prices[j].Metric.RSI <= 50)
                    && (dd.Prices[j].Metric.HasSMA7 && dd.Prices[j].Close > dd.Prices[j].Metric.SMA7)
                    && dd.Prices[j].Metric.MGR50 > 0)
                    signals.Add(new Data.Signal() {
                        Asset = dd.Asset,
                        Timestamp = dd.Prices[j].Timestamp,
                        Description = "RSI < 50; Close > SMA7; MGR50 (Strength)",
                        Direction = Data.Signal.Directions.Buy,
                        Strength = dd.Prices[j].Metric.MGR50
                    });
            }
            */
            return signals;
        }
    }
}