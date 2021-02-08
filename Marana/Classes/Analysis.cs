using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Analysis {

        public static void Week(List<string> args, Database db, Settings settings) {
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
                Calculations.CalculateMetrics(ref dd);

                Prompt.Write("Analyzing signals. ");

                /* Run analysis for signals
                 * Start at 1, compare to j - 1
                 */
                for (int j = dd.Prices.Count - 5; j >= 0 && j < dd.Prices.Count; j++) {
                    /* Search criteria for RSI oversold in an uptrend
                     * See https://school.stockcharts.com/doku.php?id=technical_indicators:relative_strength_index_rsi for more information
                     */
                    if (dd.Prices[j].Metric.HasSMA200 && dd.Prices[j].Close > dd.Prices[j].Metric.SMA200
                        && dd.Prices[j].Metric.HasRSI && dd.Prices[j].Metric.RSI <= 30)
                        signals.Add(new Data.Signal() {
                            Asset = dd.Asset,
                            Timestamp = dd.Prices[j].Timestamp,
                            Description = "RSI Oversold in Uptrend",
                            Direction = Data.Signal.Directions.Buy
                        });

                    if (dd.Prices[j].Metric.HasSMA200 && dd.Prices[j].Close < dd.Prices[j].Metric.SMA200
                        && dd.Prices[j].Metric.HasRSI && dd.Prices[j].Metric.RSI >= 70)
                        signals.Add(new Data.Signal() {
                            Asset = dd.Asset,
                            Timestamp = dd.Prices[j].Timestamp,
                            Description = "RSI Overbought in Downtrend",
                            Direction = Data.Signal.Directions.Sell
                        });
                }

                Prompt.WriteLine("Complete.", ConsoleColor.Green);
            }

            string filepath = Path.Combine(settings.Directory_Working, String.Format("Week Analysis Signals, {0}.csv", DateTime.Now.ToString("yyyy-MM-dd-HHmm")));
            Export.Signals_To_CSV(signals, filepath);
            Prompt.WriteLine(String.Format("Signals exported to {0}", filepath), ConsoleColor.Green);
        }

        public static Data.Signal.Directions HasCrossover(decimal new1, decimal new2, decimal old1, decimal old2) {
            /* All the metrics (prices, etc) should be positive numbers
             * Zero indicates data unavailable or inapplicable
             */

            if (new1 == 0 || new2 == 0 || old1 == 0 || old2 == 0)
                return Data.Signal.Directions.Same;

            decimal diff1 = new1 - new2;
            decimal diff2 = old1 - old2;

            if (diff1 == 0 && diff2 == 0)
                return Data.Signal.Directions.Same;
            else if (diff1 > 0 && diff2 > 0)
                return Data.Signal.Directions.Same;
            else if (diff1 < 0 && diff2 < 0)
                return Data.Signal.Directions.Same;
            else if (diff1 > 0 && diff2 <= 0)
                return Data.Signal.Directions.Up;
            else if (diff1 <= 0 && diff2 > 0)
                return Data.Signal.Directions.Down;
            else
                return Data.Signal.Directions.Same;
        }

        public static Data.Signal.Directions HasReversal(decimal num1, decimal num2, decimal num3) {
            /* All the metrics (prices, etc) should be positive numbers
             * Zero indicates data unavailable or inapplicable
             */

            if (num1 == 0 || num2 == 0 || num3 == 0)
                return Data.Signal.Directions.Same;

            decimal diff1 = num1 - num2;
            decimal diff2 = num2 - num3;

            /* Known issue: Will not detect reversal if ((diff1 || diff2) == 0)
             * ... it's a minor edge case, marginal effect on outcomes
             */

            if (diff1 == 0 && diff2 == 0)
                return Data.Signal.Directions.Same;
            else if (diff1 > 0 && diff2 > 0)
                return Data.Signal.Directions.Same;
            else if (diff1 < 0 && diff2 < 0)
                return Data.Signal.Directions.Same;
            else if (diff1 != 0 && diff2 == 0)
                return Data.Signal.Directions.Plateau;
            else if (diff1 > 0 && diff2 < 0)
                return Data.Signal.Directions.Trough;
            else if (diff1 < 0 && diff2 > 0)
                return Data.Signal.Directions.Peak;
            else
                return Data.Signal.Directions.Same;
        }
    }
}