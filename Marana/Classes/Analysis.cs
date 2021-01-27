using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Analysis {

        public static void Year(List<string> args, Settings settings) {
            // Process each .csv file to a data structure
            DirectoryInfo ddir = new DirectoryInfo(settings.Directory_LibraryData_TSDA);
            List<FileInfo> dfiles = new List<FileInfo>(ddir.GetFiles("*.csv"));

            List<SymbolPair> pairs = API_NasdaqTrader.GetSymbolPairs();

            Data.Select_Symbols(ref dfiles, args);

            for (int i = 0; i < dfiles.Count; i++) {
                DatasetTSDA ds = API.AlphaVantage.ParseData_TSDA(dfiles[i].FullName, 300);

                ds.Symbol = dfiles[i].Name.Substring(0, dfiles[i].Name.IndexOf(".csv")).Trim();
                ds.CompanyName = (from pair in pairs where pair.Symbol == ds.Symbol select pair.Name).First();

                // Process calculations that analysis is based on
                // TO-DO: this task can be completed at another time, as long as it is marked as completed

                Prompt.Write(String.Format("Calculating statistics for {0}: ", ds.Symbol));

                Prompt.Write("SMA7");
                Statistics.CalculateSMA(ref ds.Values, 7);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA20");
                Statistics.CalculateSMA(ref ds.Values, 20);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA50");
                Statistics.CalculateSMA(ref ds.Values, 50);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA100");
                Statistics.CalculateSMA(ref ds.Values, 100);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA200");
                Statistics.CalculateSMA(ref ds.Values, 200);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("MDS20");
                Statistics.CalculateMSD20(ref ds.Values);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.NewLine();

                // Export to .csv for debugging purposes (spot checking math, etc.)
                string exportpath = Path.Combine(settings.Directory_Library, String.Format("{0}.csv", ds.Symbol));
                Export.TSDA_To_CSV(ds, exportpath);

                // Run analysis for crossover signals; start at 1, compare to j - 1
                for (int j = 1; j < ds.Values.Count; j++) {
                    Signal.Directions testCross;

                    // Test SMA7 - SMA20
                    testCross = HasCrossover(
                        ds.Values[j].SMA7, ds.Values[j].SMA20,
                        ds.Values[j - 1].SMA7, ds.Values[j - 1].SMA20);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA7_20));

                    // Test SMA20 - SMA50
                    testCross = HasCrossover(
                    ds.Values[j].SMA20, ds.Values[j].SMA50,
                    ds.Values[j - 1].SMA20, ds.Values[j - 1].SMA50);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA20_50));

                    // Test SMA50 - SMA100
                    testCross = HasCrossover(
                    ds.Values[j].SMA50, ds.Values[j].SMA100,
                    ds.Values[j - 1].SMA50, ds.Values[j - 1].SMA100);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA50_100));

                    // Test SMA100 - SMA200
                    testCross = HasCrossover(
                    ds.Values[j].SMA100, ds.Values[j].SMA200,
                    ds.Values[j - 1].SMA100, ds.Values[j - 1].SMA200);
                    if (testCross != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Crossover, testCross, Signal.Metrics.SMA100_200));
                }

                /* Run analysis for reversal signals
                 * Start at 1, compare j - 1, j, and j + 1
                 */
                for (int j = 1; j < ds.Values.Count - 1; j++) {
                    Signal.Directions testReversal;

                    // Test SMA7
                    testReversal = HasReversal(ds.Values[j - 1].SMA7, ds.Values[j].SMA7, ds.Values[j + 1].SMA7);
                    if (testReversal != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Reversal, testReversal, Signal.Metrics.SMA7));

                    // Test SMA20
                    testReversal = HasReversal(ds.Values[j - 1].SMA20, ds.Values[j].SMA20, ds.Values[j + 1].SMA20);
                    if (testReversal != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Reversal, testReversal, Signal.Metrics.SMA20));

                    // Test SMA50
                    testReversal = HasReversal(ds.Values[j - 1].SMA50, ds.Values[j].SMA50, ds.Values[j + 1].SMA50);
                    if (testReversal != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Reversal, testReversal, Signal.Metrics.SMA50));

                    // Test SMA100
                    testReversal = HasReversal(ds.Values[j - 1].SMA100, ds.Values[j].SMA100, ds.Values[j + 1].SMA100);
                    if (testReversal != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Reversal, testReversal, Signal.Metrics.SMA100));

                    // Test SMA200
                    testReversal = HasReversal(ds.Values[j - 1].SMA200, ds.Values[j].SMA200, ds.Values[j + 1].SMA200);
                    if (testReversal != Signal.Directions.Same)
                        ds.Signals.Add(new Signal(ds.Values[j].Timestamp, Signal.Types.Reversal, testReversal, Signal.Metrics.SMA200));
                }

                /* FOR DEBUGGING
                 */
                Prompt.WriteLine(String.Format("Detected {0} signals", ds.Signals.Count), ConsoleColor.Yellow);
                foreach (Signal s in ds.Signals) {
                    Prompt.WriteLine(String.Format("{0}: {1}\t{2}\t{3}\t{4}", s.Type.ToString(), ds.Symbol, s.Timestamp.ToShortDateString(), s.Direction, s.Metric));
                }
                ds.Signals.Clear();
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