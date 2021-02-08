using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Analysis {

        // Task to be run when looking to enter new trading in general good market conditions
        public static void Insert_Long(List<string> args, Database db, Settings settings) {
            Analyze(args, db, settings,
                Strategy_Insert_Long,
                Path.Combine(settings.Directory_Working, String.Format("Insert Long Analysis Signals, {0}.csv", DateTime.Now.ToString("yyyy-MM-dd-HHmm"))));
        }

        public static void Insert_Short(List<string> args, Database db, Settings settings) {
            Analyze(args, db, settings,
                Strategy_Insert_Short,
                Path.Combine(settings.Directory_Working, String.Format("Insert Short Analysis Signals, {0}.csv", DateTime.Now.ToString("yyyy-MM-dd-HHmm"))));
        }

        // Task to be run daily for buy/sell signals to inform trading
        public static void Daily(List<string> args, Database db, Settings settings) {
            Analyze(args, db, settings,
                Strategy_Daily,
                Path.Combine(settings.Directory_Working, String.Format("Daily Analysis Signals, {0}.csv", DateTime.Now.ToString("yyyy-MM-dd-HHmm"))));
        }

        public static void Analyze(List<string> args, Database db, Settings settings,
                Func<Data.Daily, List<Data.Signal>> strategy, string filepath) {
            Prompt.WriteLine("Querying database for list of ticker symbols.");

            List<Data.Asset> assets = db.GetData_Assets();
            Data.Select_Assets(ref assets, args);

            List<Data.Signal> signals = new List<Data.Signal>();

            for (int i = 0; i < assets.Count; i++) {
                Prompt.Write(String.Format("{0} [{1:0000} / {2:0000}]  {3,-8}  Retrieving data. ",
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i + 1, assets.Count, assets[i].Symbol));

                Data.Daily dd = db.GetData_TSD(assets[i]);

                if (dd == null) {
                    Prompt.WriteLine("Data unavailable- update library?", ConsoleColor.Yellow);
                    continue;
                }

                Prompt.Write("Calculating metrics. ");
                Calculations.Metrics(ref dd);

                Prompt.Write("Analyzing signals. ");

                signals.AddRange(strategy(dd));

                Prompt.WriteLine("Complete.", ConsoleColor.Green);
            }

            signals.Sort((a, b) => a.Strength.CompareTo(b.Strength));

            Export.Signals_To_CSV(signals, filepath);
            Prompt.WriteLine(String.Format("Signals exported to {0}", filepath), ConsoleColor.Green);
        }

        public static List<Data.Signal> Strategy_Daily(Data.Daily dd) {
            List<Data.Signal> signals = new List<Data.Signal>();

            for (int j = dd.Prices.Count - 1; j >= 0 && j < dd.Prices.Count; j++) {
                if ((dd.Prices[j].Metric.HasSMA200 && dd.Prices[j].Close > dd.Prices[j].Metric.SMA200)
                    && (dd.Prices[j].Metric.HasRSI && dd.Prices[j].Metric.RSI <= 30))
                    signals.Add(new Data.Signal() {
                        Asset = dd.Asset,
                        Timestamp = dd.Prices[j].Timestamp,
                        Description = "RSI < 30; Close > SMA200",
                        Direction = Data.Signal.Directions.Buy,
                        Strength = dd.Prices[j].Metric.RSI
                    });

                if ((dd.Prices[j].Metric.HasSMA200 && dd.Prices[j].Close < dd.Prices[j].Metric.SMA200)
                    && (dd.Prices[j].Metric.HasRSI && dd.Prices[j].Metric.RSI >= 70))
                    signals.Add(new Data.Signal() {
                        Asset = dd.Asset,
                        Timestamp = dd.Prices[j].Timestamp,
                        Description = "RSI > 70; Close < SMA200",
                        Direction = Data.Signal.Directions.Sell,
                        Strength = dd.Prices[j].Metric.RSI
                    });
            }

            return signals;
        }

        public static List<Data.Signal> Strategy_Insert_Long(Data.Daily dd) {
            List<Data.Signal> signals = new List<Data.Signal>();

            for (int j = dd.Prices.Count - 1; j >= 0 && j < dd.Prices.Count; j++) {
                if ((dd.Prices[j].Metric.HasRSI && dd.Prices[j].Metric.RSI <= 50)
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

            return signals;
        }

        public static List<Data.Signal> Strategy_Insert_Short(Data.Daily dd) {
            List<Data.Signal> signals = new List<Data.Signal>();

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

            return signals;
        }
    }
}