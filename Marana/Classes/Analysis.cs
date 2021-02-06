using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Analysis {

        public static void Running(List<string> args, Database db, Settings settings) {
            List<Asset> assets = db.GetData_Assets();

            Data.Select_Assets(ref assets, args);

            foreach (Asset asset in assets) {
                DatasetTSD ds = db.GetData_TSD(asset);

                /* Run analysis for crossover signals
                 * Start at 1, compare to j - 1
                 */
                for (int j = 1; j < ds.TSDValues.Count; j++) {
                    Signal.Directions testCross;

                    // Test SMA7 - SMA20
                    testCross = HasCrossover(
                        ds.TSDValues[j].SMA7, ds.TSDValues[j].SMA20,
                        ds.TSDValues[j - 1].SMA7, ds.TSDValues[j - 1].SMA20);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.TSDValues[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA7_20));

                    // Test SMA20 - SMA50
                    testCross = HasCrossover(
                    ds.TSDValues[j].SMA20, ds.TSDValues[j].SMA50,
                    ds.TSDValues[j - 1].SMA20, ds.TSDValues[j - 1].SMA50);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.TSDValues[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA20_50));

                    // Test SMA50 - SMA100
                    testCross = HasCrossover(
                    ds.TSDValues[j].SMA50, ds.TSDValues[j].SMA100,
                    ds.TSDValues[j - 1].SMA50, ds.TSDValues[j - 1].SMA100);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.TSDValues[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA50_100));
                }

                /* FOR DEBUGGING
                 */
                Prompt.WriteLine(String.Format("Detected {0} signals", ds.Signals.Count), ConsoleColor.Yellow);
                foreach (Signal s in ds.Signals) {
                    Prompt.WriteLine(String.Format("{0}: {1}\t{2}\t{3}\t{4}", s.Type.ToString(), ds.Asset.Symbol, s.Timestamp.ToShortDateString(), s.Direction, s.Metric));
                }
            }
        }

        public static Signal.Directions HasCrossover(decimal new1, decimal new2, decimal old1, decimal old2) {
            /* All the metrics (prices, etc) should be positive numbers
             * Zero indicates data unavailable or inapplicable
             */

            if (new1 == 0 || new2 == 0 || old1 == 0 || old2 == 0)
                return Signal.Directions.Same;

            decimal diff1 = new1 - new2;
            decimal diff2 = old1 - old2;

            if (diff1 == 0 && diff2 == 0)
                return Signal.Directions.Same;
            else if (diff1 > 0 && diff2 > 0)
                return Signal.Directions.Same;
            else if (diff1 < 0 && diff2 < 0)
                return Signal.Directions.Same;
            else if (diff1 > 0 && diff2 <= 0)
                return Signal.Directions.Up;
            else if (diff1 <= 0 && diff2 > 0)
                return Signal.Directions.Down;
            else
                return Signal.Directions.Same;
        }

        public static Signal.Directions HasReversal(decimal num1, decimal num2, decimal num3) {
            /* All the metrics (prices, etc) should be positive numbers
             * Zero indicates data unavailable or inapplicable
             */

            if (num1 == 0 || num2 == 0 || num3 == 0)
                return Signal.Directions.Same;

            decimal diff1 = num1 - num2;
            decimal diff2 = num2 - num3;

            /* Known issue: Will not detect reversal if ((diff1 || diff2) == 0)
             * ... it's a minor edge case, marginal effect on outcomes
             */

            if (diff1 == 0 && diff2 == 0)
                return Signal.Directions.Same;
            else if (diff1 > 0 && diff2 > 0)
                return Signal.Directions.Same;
            else if (diff1 < 0 && diff2 < 0)
                return Signal.Directions.Same;
            else if (diff1 != 0 && diff2 == 0)
                return Signal.Directions.Plateau;
            else if (diff1 > 0 && diff2 < 0)
                return Signal.Directions.Trough;
            else if (diff1 < 0 && diff2 > 0)
                return Signal.Directions.Peak;
            else
                return Signal.Directions.Same;
        }
    }
}