using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="days"></param>
        /// <returns></returns>
        public async Task RunBacktest(Settings settings, Database db, Data.Format format, int days) {
            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Running backtesting against rules for {format} instructions\n");

            List<Data.Instruction> instructions = (await db.GetInstructions())?.Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await db.GetStrategies();

            List<Data.Asset> assets = await new Library().GetAssets(db);

            if (assets == null || assets.Count == 0) {
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

            for (int i = 0; i < instructions.Count; i++) {
                Data.Strategy strategy = strategies.Find(s => s.Name == instructions[i].Strategy);
                Data.Asset asset = assets.Find(a => a.Symbol == instructions[i].Symbol);

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
                    await RunBacktest_Daily(settings, db, instructions[i], strategy, days, asset);
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
        /// <param name="strategy"></param>
        /// <param name="days"></param>
        /// <param name="shares"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        public async Task RunBacktest(Settings settings, Database db, int days, string argStrategies, int quantity, string argAssets) {
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
            List<string> lstrStrategies = argStrategies.Split(',', ' ', ')', '(', '\'').Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            List<string> lstrAssets = argAssets.Split(',', ' ', ')', '(', '\'').Where(s => !String.IsNullOrWhiteSpace(s)).ToList();

            List<Data.Strategy> strategies = allStrategies.Where(s => lstrStrategies.Contains(s.Name)).ToList();
            List<Data.Asset> assets = allAssets.Where(a => lstrAssets.Contains(a.Symbol)).ToList();

            WriteLine($"Running library update to ensure data present for all requested symbols.");
            await new Library().Update_TSD(assets, settings, db);

            WriteLine($"\nRetrieving data from database.");
            int counter = 1;
            List<Data.Daily> allData = new List<Data.Daily>();
            foreach (Data.Asset asset in assets) {
                allData.Add(await db.GetData_Daily(asset));
                Write("." + (counter % 50 == 0 ? "\n" : ""));
                counter++;
            }

            WriteLine("\n\n\nAssembling test instructions.\n");

            foreach (Data.Strategy s in strategies) {
                foreach (Data.Asset a in assets) {
                    instructions.Add(new Data.Instruction() {
                        Name = $"cli__{s.Name}__{a.Symbol}",
                        Description = $"CLI {s.Name} {a.Symbol}",
                        Symbol = a.Symbol,
                        Strategy = s.Name,
                        Quantity = quantity,
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
                    tests.Add(await RunBacktest_Daily(settings, db, instructions[i], strategy, days, asset));
                }
            }

            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Completed running backtesting against rules for command-line arguments\n\n");

            WriteLine($">>>>>>>>>> Summary <<<<<<<<<<");                                // Summary output beginning

            WriteLine($"\n>>> Tests sorted by overall return percent:");               // Show tests sorted by return percent
            foreach (Data.Test test in tests.OrderBy(o => -o.GainPercent)) {
                WriteLine($"  {test.GainPercent:00.00}%\t{test.Strategy.Name} \t{test.Instruction.Symbol} \tt${test.GainAmount:n2} / {Math.Abs(test.Trades?.First()?.Gain ?? 0m):n2}");
            }

            WriteLine($"\n>>> Assets sorted by overall short-term rate of change (ROC-14):");     // Show assets sorted by rate of change
            foreach (Data.Daily dd in allData.OrderBy(o => o.Metrics.Last()?.ROC14)) {
                decimal roc14 = dd.Metrics.Count > 0 ? dd.Metrics.Last()?.ROC14 ?? 0m : 0m;
                decimal roc50 = dd.Metrics.Count > 0 ? dd.Metrics.Last()?.ROC50 ?? 0m : 0m;
                WriteLine($"  {dd.Asset.Symbol} \t\tROC14: {roc14:00.00}% \t\tROC50: {roc50:00.00}%");
            }

            WriteLine($"\n>>> Assets sorted by overall long-term rate of change (ROC-200):");     // Show assets sorted by rate of change
            foreach (Data.Daily dd in allData.OrderBy(o => o.Metrics.Last()?.ROC200)) {
                decimal roc200 = dd.Metrics.Count > 0 ? dd.Metrics.Last()?.ROC200 ?? 0m : 0m;
                decimal roc100 = dd.Metrics.Count > 0 ? dd.Metrics.Last()?.ROC100 ?? 0m : 0m;
                WriteLine($"  {dd.Asset.Symbol} \t\tROC200: {roc200:00.00}% \t\tROC100: {roc100:00.00}%");
            }
        }

        public async Task<Data.Test> RunBacktest_Daily(Settings settings, Database db,
            Data.Instruction instruction, Data.Strategy strategy,
            int days, Data.Asset asset) {
            // Get last market close to ensure most up-to-date data exists
            // And that today's potential data exists (since SQL query interprets to query for today's prices!)
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);
            if (result is DateTime r) {
                lastMarketClose = r;
            }

            // Check data validity (last update time) to ensure it is most recent
            Library library = new Library();
            DateTime validity = await db.GetValidity_Daily(asset);

            if (validity.CompareTo(lastMarketClose) < 0) {              // If data is invalid
                WriteLine("Latest market data for this symbol needs updating. Updating now.");
                await library.Update_TSD(new List<Data.Asset>() { asset }, settings, db);
                await Task.Delay(10000);                         // Allow library update's database threads to get ahead
                validity = await db.GetValidity_Daily(asset);
            }

            if (validity.CompareTo(lastMarketClose) > 0) {       // If validity is current, data is valid
                DateTime day;
                bool position = false;                           // Placeholder for whether a position is held
                Data.Daily allData = await db.GetData_Daily(asset);     // Full dataset for pulling daily prices
                Data.Test testData = new Data.Test() {                  // Collection of information for calculating test results
                    Asset = asset, Instruction = instruction, Strategy = strategy
                };

                if (allData.Prices.Count < days) {
                    WriteLine($"Insufficient historical data. {allData.Prices.Count} days of trading data available for this asset.");
                    return null;
                }

                int counter = 1;
                for (int i = allData.Prices.Count - days; i < allData.Prices.Count; i++) {
                    day = allData.Prices[i].Date;

                    bool toBuy = await db.ScalarQuery(await Strategy.Interpret(strategy.Entry, instruction.Symbol, day));
                    bool toSellGain = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol, day));
                    bool toSellLoss = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitStopLoss, instruction.Symbol, day));

                    if (toBuy && (toSellGain || toSellLoss)) {   // Cannot simultaneously buy and sell ... erroneous queries?
                        continue;
                    }

                    if (!position && toBuy) {
                        Write("buy......." + (counter % 7 == 0 ? $"  {day:yyyy-MM-dd}\n" : ""));        // Progress indicator

                        position = true;                                        // Buying a position
                        Data.Daily.Price price = allData.Prices.Find(p => p.Date.Date.CompareTo(day.Date) == 0);
                        testData.Trades.Add(new Data.Test.Trade() {
                            Timestamp = day,
                            Transaction = Data.Test.Trade.Direction.Buy,
                            Price = price.Close,
                            Gain = -price.Close * instruction.Quantity
                        });
                    } else if (position && (toSellGain || toSellLoss)) {
                        if (toSellGain) {
                            Write("sale-gain." + (counter % 7 == 0 ? $"  {day:yyyy-MM-dd}\n" : ""));        // Progress indicator
                        } else if (toSellLoss) {
                            Write("sale-loss." + (counter % 7 == 0 ? $"  {day:yyyy-MM-dd}\n" : ""));        // Progress indicator
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
                        Write(".........." + (counter % 7 == 0 ? $"  {day:yyyy-MM-dd}\n" : ""));        // Progress indicator
                    }

                    counter++;
                }

                if (position) {     // If position is held at the end of the test- liquidate shares into gains for calculations
                    Write("-liquidating");                                      // Progress indicator

                    Data.Daily.Price price = allData.Prices.Last();
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
                WriteLine($"{allData.Prices[^days].Date:yyyy-MM-dd} to {allData.Prices.Last().Date:yyyy-MM-dd}");
                WriteLine($"{positions} positions held; Entry at ${entry:n2}; Exit at ${exit:n2}");
                WriteLine($"Total gain ${testData.GainAmount:n2}; Total percent gain {testData.GainPercent:0.00}%");
                WriteLine("\n");

                return testData;
            }

            return null;
        }
    }
}