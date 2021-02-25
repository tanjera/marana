using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Skender.Stock.Indicators;

namespace Marana {

    public class Backtest {

        public enum OrderResult {
            Success,
            Fail,
            FailInsufficientFunds
        }

        /* Status and Output variables, handler, and event triggering
         * For updating GUI with status and output text in an asynchronous manner
         */

        public Statuses Status;
        public List<string> Output;
        public bool CancelUpdate = false;

        public enum Statuses {
            Inactive,
            Executing
        };

        public enum ExitCode {
            Completed,
            Cancelled
        }

        public event StatusUpdateHandler StatusUpdate;

        public delegate void StatusUpdateHandler(object sender, StatusEventArgs e);

        public class StatusEventArgs : EventArgs {
            public Statuses Status { get; set; }
            public List<string> Output { get; set; }
        }

        public Backtest() {
            Status = Statuses.Inactive;
            Output = new List<string>();
        }

        private void OnStatusUpdate()
            => StatusUpdate?.Invoke(this, new StatusEventArgs() {
                Status = Status,
                Output = Output
            });

        /* Utility methods
         * For messaging
         */

        public void Write(string message, ConsoleColor color = ConsoleColor.Gray) {
            if (Output.Count == 0)
                Output.Add("");

            Output[^1] = $"{Output[^1]}{message}";
            OnStatusUpdate();

            Prompt.Write(message, color);
        }

        public void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray) {
            if (Output.Count == 0)
                Output.Add("");

            Output[^1] = $"{Output[^1]}{message}";
            Output.Add("");
            OnStatusUpdate();

            Prompt.WriteLine(message, color);
        }

        /* Backtesting functionality:
         */

        /// <summary>
        /// Runs a backtest against existing instructions in the database
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="db"></param>
        /// <param name="format"></param>
        /// <param name="argDays"></param>
        /// <returns></returns>
        public async Task RunBacktest(Settings settings, Database db, Data.Format format, int argDays, DateTime argEnding) {
            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Running backtesting against rules for {format} instructions\n");

            List<Data.Instruction> instructions = (await db.GetInstructions())?.Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await db.GetStrategies();

            List<Data.Asset> allAssets = await new Library().GetAssets(db);

            if (allAssets == null || allAssets.Count == 0) {
                WriteLine("Unable to retrieve asset list from database.\n");
                return;
            }

            if (instructions == null || instructions.Count == 0) {
                WriteLine("No automated instruction rules found in database. Aborting.\n");
                return;
            }

            if (strategies == null || strategies.Count == 0) {
                WriteLine("No automation strategies found in database. Aborting.\n");
                return;
            }

            // Run library update prior to test; ensures all data is up to date
            List<string> insSymbols = instructions.Select(i => i.Symbol).ToList();
            List<Data.Asset> insAssets = allAssets.Where(a => insSymbols.Contains(a.Symbol)).ToList();

            WriteLine($"Running library update to ensure data present for all requested symbols.");
            await new Library().Update_TSD(insAssets, settings, db);

            for (int i = 0; i < instructions.Count; i++) {
                Data.Strategy strategy = strategies.Find(s => s.Name == instructions[i].Strategy);
                Data.Asset asset = allAssets.Find(a => a.Symbol == instructions[i].Symbol);

                WriteLine($"\n[{i + 1:0000} / {instructions.Count:0000}] {instructions[i].Name} ({instructions[i].Format}): "
                    + $"{instructions[i].Symbol} x {instructions[i].Quantity} @ {instructions[i].Strategy} ({instructions[i].Frequency})");

                if (strategy == null) {
                    WriteLine($"Strategy '{instructions[i].Strategy}' not found in database. Aborting.\n");
                    continue;
                }

                if (asset == null) {
                    WriteLine($"Asset '{instructions[i].Symbol}' not found in database. Aborting.\n");
                    continue;
                }

                if (instructions[i].Frequency == Data.Frequency.Daily) {
                    await RunBacktest_Daily(settings, db, instructions[i], strategy, asset, argDays, argEnding);
                }
            }

            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Completed running backtesting against rules for {format} instructions\n");
        }

        /// <summary>
        /// Runs a backtest using command-line user-inputted instructions
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="db"></param>
        /// <param name="argStrategies"></param>
        /// <param name="argAssets"></param>
        /// <param name="argDays"></param>
        /// <param name="argEnding"></param>
        /// <returns></returns>
        public async Task RunBacktest(Settings settings, Database db, string argStrategies, string argAssets, int argDays, DateTime argEnding) {
            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Running backtesting against rules for command-line arguments\n");

            List<Data.Strategy> allStrategies = await db.GetStrategies();
            List<Data.Asset> allAssets = await new Library().GetAssets(db);

            if (allAssets == null || allAssets.Count == 0) {
                WriteLine("Unable to retrieve asset list from database.\n");
                return;
            }

            if (allStrategies == null || allStrategies.Count == 0) {
                WriteLine("No automation strategies found in database. Aborting.\n");
                return;
            }

            List<Data.Instruction> instructions = new List<Data.Instruction>();

            List<Data.Strategy> strategies;
            if (argStrategies.ToLower() == "all") {                   // Parse command-line input for strategies
                strategies = allStrategies;
            } else {
                List<string> lstrStrategies = argStrategies.Split(',', ' ', ')', '(', '\'').Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
                strategies = allStrategies.Where(s => lstrStrategies.Contains(s.Name)).ToList();
            }

            List<Data.Asset> assets;
            if (argAssets.ToLower() == "watchlist") {                 // Parse command-line input for assets
                List<string> watchlist = await db.GetWatchlist();
                assets = allAssets.Where(a => watchlist.Contains(a.Symbol)).ToList();
            } else if (argAssets.ToLower() == "all") {
                assets = allAssets;
            } else {
                List<string> lstrAssets = argAssets.Split(',', ' ', ')', '(', '\'').Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
                assets = allAssets.Where(a => lstrAssets.Contains(a.Symbol)).ToList();
            }

            // Run library update prior to test; ensures all data is up to date
            WriteLine($"Running library update to ensure data present for all requested symbols.");
            await new Library().Update_TSD(assets, settings, db);

            WriteLine("\n\n\nAssembling test instructions.\n");

            foreach (Data.Strategy s in strategies) {
                foreach (Data.Asset a in assets) {
                    instructions.Add(new Data.Instruction() {
                        Name = $"cli__{s.Name}__{a.Symbol}",
                        Description = $"CLI {s.Name} {a.Symbol}",
                        Symbol = a.Symbol,
                        Strategy = s.Name,
                        Quantity = 10,
                        Active = true,
                        Format = Data.Format.Test,
                        Frequency = Data.Frequency.Daily
                    });
                }
            }

            List<Data.Test> tests = new List<Data.Test>();

            for (int i = 0; i < instructions.Count; i++) {
                Data.Strategy strategy = allStrategies.Find(s => s.Name == instructions[i].Strategy);
                Data.Asset asset = allAssets.Find(a => a.Symbol == instructions[i].Symbol);

                WriteLine($"\n[{i + 1:0000} / {instructions.Count:0000}] {instructions[i].Name} ({instructions[i].Format}): "
                    + $"{instructions[i].Symbol} x {instructions[i].Quantity} @ {instructions[i].Strategy} ({instructions[i].Frequency})");

                if (strategy == null) {
                    WriteLine($"Strategy '{instructions[i].Strategy}' not found in database. Aborting.\n");
                    continue;
                }

                if (asset == null) {
                    WriteLine($"Asset '{instructions[i].Symbol}' not found in database. Aborting.\n");
                    continue;
                }

                if (instructions[i].Frequency == Data.Frequency.Daily) {
                    tests.Add(await RunBacktest_Daily(settings, db, instructions[i], strategy, asset, argDays, argEnding));
                }
            }

            WriteLine($"Calculating metrics.");

            int counter = 1;
            Data.Daily data;
            foreach (Data.Asset asset in assets) {                  // Get each daily dataset, calculate ROC, place metric in tests
                Write("." + (counter % 50 == 0 ? $"  ${counter:00000}\n" : ""));
                counter++;

                data = await db.GetData_Daily(asset);
                if (data != null) {
                    RocResult[] roc = data.Prices.Count > argDays + 1 ? Indicator.GetRoc(data.Prices, argDays).ToArray() : null;
                    foreach (Data.Test test in tests.Where(t => t.Asset.ID == asset.ID)) {
                        if (roc != null && roc.Length > 0) {
                            test.RateOfChange = roc.Find(argEnding).Roc ?? 0m;
                        }
                    }
                }
            }

            WriteLine($"\n\n\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Completed running backtesting against rules for command-line arguments\n\n");

            WriteLine($">>>>>>>>>> Summary <<<<<<<<<<");                                // Summary output beginning

            int removed = tests.RemoveAll(t => t == null);
            WriteLine($"\nRemoved {removed} invalid test results (see test output for errors)");

            WriteLine("\n  --------------------------------------------------------------------------------------------------------");
            WriteLine($"  {"Gain %",10} \t {"Strategy",-20} {"Symbol",-10} {"ROC %",10}\t {"$ Gain",-12} / {"$ Entry",-10}");
            WriteLine("  --------------------------------------------------------------------------------------------------------");

            foreach (Data.Test t in tests.OrderBy(o => -o.GainPercent)) {
                decimal gain = t.Trades.Count > 0 ? Math.Abs(t.Trades.First()?.Gain ?? 0m) : 0m;
                WriteLine($"  {t.GainPercent,8:00.00} %\t {t.Strategy.Name,-20} {t.Instruction.Symbol,-10} {t.RateOfChange,8:00.00} %\t $ {t.GainAmount,-10:n2} / $ {gain,-10:n2}");
            }

            WriteLine("\n");

            WriteLine("\n  --------------------------------------------------------------------------------------------------------");
            WriteLine($"     {"Strategy",-20} \t\t {"Mean Gain %",14}");
            WriteLine("  --------------------------------------------------------------------------------------------------------");

            foreach (Data.Strategy s in strategies) {
                decimal gainTotal = 0m;
                List<Data.Test> lt = tests.Where(w => w.Strategy == s).ToList();
                foreach (Data.Test t in lt) {
                    gainTotal += t.GainPercent;
                }
                gainTotal = (lt.Count > 0 ? (gainTotal / lt.Count) : 0m);

                WriteLine($"     {s.Name,-20}\t\t {gainTotal,12:00.00} %");
            }

            WriteLine("\n");
        }

        public async Task<Data.Test> RunBacktest_Daily(Settings settings, Database db,
            Data.Instruction instruction, Data.Strategy strategy,
            Data.Asset asset, int days, DateTime ending) {
            Library library = new Library();

            DateTime day;
            bool position = false;                           // Placeholder for whether a position is held
            Data.Daily allData = await db.GetData_Daily(asset);     // Full dataset for pulling daily prices
            Data.Test testData = new Data.Test() {                  // Collection of information for calculating test results
                Asset = asset, Instruction = instruction, Strategy = strategy
            };

            // Calculate range of index to be simulating; do error checking
            int indexEnd = allData.Prices.FindIndex(p => p.Date == ending);
            if (indexEnd < 0) {
                WriteLine($"Unable to find ending date in historical data- ensure ending date is a valid trading day.");
                return null;
            } else if (indexEnd - days < 0) {
                WriteLine($"Insufficient historical data. {indexEnd} days of trading data available given ending date of {ending:yyyy-MM-dd}");
                return null;
            }

            int counter = 1;
            int weekdays = 1;
            for (int i = indexEnd - days; i < indexEnd; i++) {
                day = allData.Prices[i].Date;

                bool? toBuy = await db.ScalarQuery(await Strategy.Interpret(strategy.Entry, instruction.Symbol, day));
                bool? toSellGain = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol, day));
                bool? toSellLoss = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitStopLoss, instruction.Symbol, day));

                if (!toBuy.HasValue || !toSellGain.HasValue || !toSellLoss.HasValue) {
                    WriteLine($"Error detected in SQL query. Please validate queries. Skipping.");
                    return null;
                }

                // Split output by weeks; account for holidays (missing days in dataset); skip weekends
                if (day.DayOfWeek == DayOfWeek.Monday || counter == 1 || weekdays > 4) {
                    Write($"\n  {day:yyyy-MM-dd}  ");
                    weekdays = 1;
                } else {
                    weekdays++;
                }

                if (toBuy.Value && (toSellGain.Value || toSellLoss.Value)) {   // Cannot simultaneously buy and sell ... erroneous queries?
                    Write("?buysell??");        // Progress indicator
                    counter++;
                    continue;
                } else if (!position && toBuy.Value) {
                    Write("buy.......");        // Progress indicator

                    position = true;                                        // Buying a position
                    Data.Daily.Price price = allData.Prices.Find(p => p.Date.Date.CompareTo(day.Date) == 0);
                    testData.Trades.Add(new Data.Test.Trade() {
                        Timestamp = day,
                        Transaction = Data.Test.Trade.Direction.Buy,
                        Price = price.Close,
                        Gain = -price.Close * instruction.Quantity
                    });
                } else if (position && (toSellGain.Value || toSellLoss.Value)) {
                    if (toSellGain.Value) {
                        Write("sale-gain.");        // Progress indicator
                    } else if (toSellLoss.Value) {
                        Write("sale-loss.");        // Progress indicator
                    }

                    position = false;                                       // Selling the position
                    Data.Daily.Price price = allData.Prices.Find(p => p.Date.Date.CompareTo(day.Date) == 0);
                    testData.Trades.Add(new Data.Test.Trade() {
                        Timestamp = day,
                        Transaction = Data.Test.Trade.Direction.Sell,
                        Price = price.Close,
                        Gain = price.Close * instruction.Quantity
                    });
                } else {
                    Write("..........");        // Progress indicator
                }

                counter++;
            }

            if (position) {     // If position is held at the end of the test- liquidate shares into gains for calculations
                Write("-liquidating");                                      // Progress indicator

                Data.Daily.Price price = allData.Prices[indexEnd];
                testData.Trades.Add(new Data.Test.Trade() {
                    Timestamp = DateTime.Today,
                    Transaction = Data.Test.Trade.Direction.Sell,
                    Price = price.Close,
                    Gain = price.Close * instruction.Quantity
                });
            }

            WriteLine("\n");

            foreach (Data.Test.Trade trade in testData.Trades) {
                Console.WriteLine($"{trade.Timestamp:yyyy-MM-dd}:  {trade.Transaction} {instruction.Quantity} @ {trade.Price:n2} (${trade.Gain:n2})");
                testData.GainAmount += trade.Gain;
            }

            int positions = testData.Trades.Count / 2;

            if (testData.Trades.Count > 0) {
                decimal begin = Math.Abs(testData.Trades.First().Gain);
                testData.GainPercent = testData.GainAmount / begin * 100;
            }

            decimal entry = testData.Trades.Count > 0 ? testData.Trades.First()?.Price ?? 0m : 0m;
            decimal exit = testData.Trades.Count > 0 ? testData.Trades.Last()?.Price ?? 0m : 0m;
            WriteLine("");
            WriteLine($"{allData.Prices[indexEnd - days].Date:yyyy-MM-dd} to {allData.Prices[indexEnd].Date:yyyy-MM-dd}");
            WriteLine($"{positions} positions held; Entry at ${entry:n2}; Exit at ${exit:n2}");
            WriteLine($"Total gain ${testData.GainAmount:n2}; Total percent gain {testData.GainPercent:0.00}%");
            WriteLine("\n");

            return testData;
        }
    }
}