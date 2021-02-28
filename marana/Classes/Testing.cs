using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Skender.Stock.Indicators;

namespace Marana {

    public class Testing {
        private Program Program;
        private Settings Settings => Program.Settings;
        private Database Database => Program.Database;
        private Library Library => Program.Library;

        public Testing(Program p) {
            Program = p;
        }

        public enum Test {
            Discrete,
            Parallel
        }

        public async Task RunDiscrete(string argStrategies, string argAssets, int argDays, DateTime argEndDate)
            => await RunTest_Daily(Test.Discrete, argStrategies, argAssets, argDays, argEndDate);

        public async Task RunParallel(string argStrategies, string argAssets, int argDays, DateTime argEndDate, decimal argDollars = 0m, decimal argDollarsPer = 0m)
            => await RunTest_Daily(Test.Parallel, argStrategies, argAssets, argDays, argEndDate, argDollars, argDollarsPer);

        public async Task RunTest_Daily(Test testType, string argStrategies, string argAssets, int argDays, DateTime argEndDate, decimal argDollars = 0m, decimal argDollarsPer = 0m) {
            Prompt.WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine($">>> Running backtesting against rules for command-line arguments\n");

            List<Data.Strategy> allStrategies = await Database.GetStrategies();
            List<Data.Asset> allAssets = await Library.GetAssets();

            if (allAssets == null || allAssets.Count == 0) {
                Prompt.WriteLine("Unable to retrieve asset list from database.\n");
                return;
            }

            if (allStrategies == null || allStrategies.Count == 0) {
                Prompt.WriteLine("No automation strategies found in database. Aborting.\n");
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
                List<string> watchlist = await Database.GetWatchlist();
                assets = allAssets.Where(a => watchlist.Contains(a.Symbol)).ToList();
            } else if (argAssets.ToLower() == "all") {
                assets = allAssets;
            } else {
                List<string> lstrAssets = argAssets.Split(',', ' ', ')', '(', '\'', '\"').Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
                assets = allAssets.Where(a => lstrAssets.Contains(a.Symbol)).ToList();
            }

            // Run library update prior to test; ensures all data is up to date
            Prompt.WriteLine($"Running library update to ensure data present for all requested symbols.");
            await Library.Update_TSD(assets);

            Prompt.WriteLine("\n\n\nAssembling test instructions.\n");

            foreach (Data.Strategy s in strategies) {
                foreach (Data.Asset a in assets) {
                    instructions.Add(new Data.Instruction() {
                        Name = $"cli__{s.Name}__{a.Symbol}",
                        Description = $"CLI {s.Name} {a.Symbol}",
                        Symbol = a.Symbol,
                        Strategy = s.Name,
                        Quantity = 1,
                        Active = true,
                        Format = Data.Format.Test,
                        Frequency = Data.Frequency.Daily
                    });
                }
            }

            List<Data.Test> tests = new List<Data.Test>();

            if (testType == Test.Discrete) {                        // Discrete testing tests one instruction at a time
                tests = await TestDiscrete_Daily(instructions, strategies, assets, argDays, argEndDate);
            } else if (testType == Test.Parallel) {                 // Parallel testing tests all instructions in the same setting
                DateTime startDate = argEndDate - new TimeSpan((int)((argDays / 5m) * 7m), 0, 0, 0);
                tests = await TestParallel_Daily(instructions, strategies, assets, startDate, argEndDate, argDollars, argDollarsPer);
            }

            Prompt.WriteLine($"Calculating metrics.");

            int counter = 1;
            Data.Daily data;
            foreach (Data.Asset asset in assets) {                  // Get each daily dataset, calculate ROC, place metric in tests
                Prompt.Write("." + (counter % 50 == 0 ? $"  {counter,6:0}\n" : ""));
                counter++;

                data = await Database.GetData_Daily(asset);
                if (data != null) {
                    RocResult[] roc = data.Prices.Count > argDays + 1 ? Indicator.GetRoc(data.Prices, argDays).ToArray() : null;
                    foreach (Data.Test test in tests.Where(t => t?.Asset.ID == asset.ID)) {
                        if (roc != null && roc.Length > 0) {
                            test.AssetRateOfChange = roc.Find(argEndDate).Roc ?? 0m;
                        }
                    }
                }
            }
            if (testType == Test.Discrete) {
                await Summary_Discrete(tests, strategies, argDays, argEndDate);
            } else if (testType == Test.Parallel) {
                await Summary_Parallel(tests, strategies, argDays, argEndDate, argDollars, argDollarsPer);
            }
        }

        public async Task<List<Data.Test>> TestDiscrete_Daily(List<Data.Instruction> instructions, List<Data.Strategy> strategies, List<Data.Asset> assets, int days, DateTime endDate) {
            List<Data.Test> listTests = new List<Data.Test>();

            for (int ins = 0; ins < instructions.Count; ins++) {
                Data.Instruction instruction = instructions[ins];
                Data.Strategy strategy = strategies.Find(s => s.Name == instruction.Strategy);
                Data.Asset asset = assets.Find(a => a.Symbol == instruction.Symbol);

                Prompt.WriteLine($"\n[{ins + 1:0000} / {instructions.Count:0000}] {instruction.Name} : "
                    + $"{instruction.Symbol} x {instruction.Quantity} @ {instruction.Strategy} ({instruction.Frequency})");

                if (strategy == null) {
                    Prompt.WriteLine($"Strategy '{instruction.Strategy}' not found in database. Aborting.\n");
                    return null;
                }

                if (asset == null) {
                    Prompt.WriteLine($"Asset '{instruction.Symbol}' not found in database. Aborting.\n");
                    return null;
                }

                Data.Daily allData = await Database.GetData_Daily(asset);     // Full dataset for pulling daily prices
                Data.Test test = new Data.Test() {                        // Collection of information for calculating test results
                    Asset = asset, Instruction = instruction, Strategy = strategy
                };

                // Calculate range of index to be simulating; do error checking
                int indexEnd = allData.Prices.FindIndex(p => p.Date == endDate);
                if (indexEnd < 0) {
                    Prompt.WriteLine($"Unable to find ending date in historical data- ensure ending date is a valid trading day.");
                    return null;
                } else if (indexEnd - days < 0) {
                    Prompt.WriteLine($"Insufficient historical data. {indexEnd} days of trading data available given ending date of {endDate:yyyy-MM-dd}");
                    return null;
                }

                int counter = 1;
                int weekdays = 1;
                for (int i = indexEnd - days; i <= indexEnd; i++) {
                    DateTime day = allData.Prices[i].Date;

                    bool? toBuy = await Database.QueryStrategy_Scalar(await Strategy.Interpret(strategy.Entry, instruction.Symbol, day));
                    bool? toSellGain = await Database.QueryStrategy_Scalar(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol, day));
                    bool? toSellLoss = await Database.QueryStrategy_Scalar(await Strategy.Interpret(strategy.ExitStopLoss, instruction.Symbol, day));

                    if (!toBuy.HasValue || !toSellGain.HasValue || !toSellLoss.HasValue) {
                        Prompt.WriteLine($"Error detected in SQL query. Please validate queries. Skipping.");
                        return null;
                    }

                    // Split output by weeks; account for holidays (missing days in dataset); skip weekends
                    if (day.DayOfWeek == DayOfWeek.Monday || counter == 1 || weekdays > 4) {
                        Prompt.Write($"\n  {day:yyyy-MM-dd}  ");
                        weekdays = 1;
                    } else {
                        weekdays++;
                    }

                    if (test.Shares > 0) {
                        test.DaysHeld++;                // Tally amount of days held in position
                    }

                    if (i == indexEnd) {
                        if (test.Shares > 0) {                                          // Last day, end the test
                            Prompt.Write("*liquidating");

                            Data.Daily.Price price = allData.Prices[indexEnd];
                            test.Trades.Add(new Data.Test.Trade() {
                                Timestamp = allData.Prices[indexEnd].Date,
                                Transaction = Data.Test.Trade.Direction.Sell,
                                Quantity = instruction.Quantity,
                                Price = price.Close,
                                Gain = price.Close * instruction.Quantity
                            });
                        } else {
                            Prompt.Write("..........");
                        }
                    } else if (toBuy.Value && (toSellGain.Value || toSellLoss.Value)) {   // Cannot simultaneously buy and sell ... erroneous queries?
                        Prompt.Write("?buysell??");
                        counter++;
                        continue;
                    } else if (test.Shares == 0 && toBuy.Value) {
                        Prompt.Write("buy*******");

                        test.Shares += instruction.Quantity;                                        // Buying a position
                        Data.Daily.Price price = allData.Prices.Find(p => p.Date.Date.CompareTo(day.Date) == 0);
                        test.Trades.Add(new Data.Test.Trade() {
                            Timestamp = day,
                            Transaction = Data.Test.Trade.Direction.Buy,
                            Quantity = instruction.Quantity,
                            Price = price.Close,
                            Gain = -price.Close * instruction.Quantity
                        });
                    } else if (test.Shares > 0 && (toSellGain.Value || toSellLoss.Value)) {
                        if (toSellGain.Value) {
                            Prompt.Write("sale-gain.");
                        } else if (toSellLoss.Value) {
                            Prompt.Write("sale-loss.");
                        }

                        Data.Daily.Price price = allData.Prices.Find(p => p.Date.Date.CompareTo(day.Date) == 0);
                        test.Trades.Add(new Data.Test.Trade() {
                            Timestamp = day,
                            Transaction = Data.Test.Trade.Direction.Sell,
                            Quantity = instruction.Quantity,
                            Price = price.Close,
                            Gain = price.Close * instruction.Quantity
                        });
                        test.Shares = 0;                                       // Selling the position
                    } else {
                        if (test.Shares > 0) {
                            Prompt.Write("**********");
                        } else {
                            Prompt.Write("..........");
                        }
                    }

                    counter++;
                }

                Prompt.WriteLine("\n");

                foreach (Data.Test.Trade trade in test.Trades) {
                    Prompt.WriteLine($"{trade.Timestamp:yyyy-MM-dd}:  {trade.Transaction} {instruction.Quantity} @ {trade.Price:n2} (${trade.Gain:n2})");
                    test.GainAmount += trade.Gain;
                }

                int positions = test.Trades.Count / 2;

                if (test.Trades.Count > 0) {
                    decimal begin = Math.Abs(test.Trades.First().Gain);
                    test.GainPercent = test.GainAmount / begin * 100;
                    test.GainPercentPerDay = test.DaysHeld > 0 ? (test.GainPercent / test.DaysHeld) : 0m;
                }

                decimal entry = test.Trades.Count > 0 ? test.Trades.First()?.Price ?? 0m : 0m;
                decimal exit = test.Trades.Count > 0 ? test.Trades.Last()?.Price ?? 0m : 0m;

                Prompt.WriteLine("");
                Prompt.WriteLine($"{allData.Prices[indexEnd - days].Date:yyyy-MM-dd} to {allData.Prices[indexEnd].Date:yyyy-MM-dd}");
                Prompt.WriteLine($"{positions} positions held; {test.DaysHeld} days in holding position; Entry at ${entry:n2}; Exit at ${exit:n2}");
                Prompt.WriteLine($"Total gain ${test.GainAmount:n2}; Total gain {test.GainPercent:0.00}%; Gain per day {test.GainPercentPerDay:0.00}%");
                Prompt.WriteLine("\n");

                listTests.Add(test);
            }

            return listTests;
        }

        public async Task<List<Data.Test>> TestParallel_Daily(
                List<Data.Instruction> instructions, List<Data.Strategy> strategies, List<Data.Asset> assets,
                DateTime startDate, DateTime endDate, decimal argDollars = 0m, decimal argDollarsPer = 0m) {
            List<Data.Test> listTests = new List<Data.Test>();

            bool dollarLimit = argDollars > 0;
            bool dollarPerLimit = argDollarsPer > 0;

            for (int iStrategy = 0; iStrategy < strategies.Count; iStrategy++) {
                Data.Strategy strategy = strategies[iStrategy];
                List<Data.Instruction> runInstructions = instructions.Where(i => i.Strategy == strategy.Name).ToList();
                decimal dollars = argDollars;                                   // Reset the funds every test

                Prompt.WriteLine($"\n[{iStrategy + 1:0000} / {strategies.Count:0000}] {strategy.Name}");

                List<Data.Test> splitTests = new List<Data.Test>();
                foreach (Data.Instruction instruction in runInstructions) {
                    splitTests.Add(new Data.Test() {
                        Instruction = instruction,
                        Strategy = strategy,
                        Asset = assets.Find(a => a.Symbol == instruction.Symbol)
                    });
                }

                for (DateTime day = startDate; day.CompareTo(endDate) <= 0; day += new TimeSpan(1, 0, 0, 0)) {
                    if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday) {
                        continue;
                    } else {
                        if (dollarLimit) {
                            Prompt.Write($"\n  {day:yyyy-MM-dd}  ${dollars,-7:0}  ");
                        } else {
                            Prompt.Write($"\n  {day:yyyy-MM-dd}  ");
                        }
                    }

                    // Sort instructions by strategy's SortBy query
                    List<string> sortedAssets = await Database.QueryStrategy_SortBy(strategy.SortBy, day);
                    List<string> selectedAssets = assets.Select(s => s.Symbol).ToList();                // Obtain list of assets selected for testing
                    sortedAssets.RemoveAll(s => !selectedAssets.Contains(s));                           // Remove assets not selected for testing
                    for (int newIndex = 0; newIndex < sortedAssets.Count; newIndex++) {                 // Iterate the sorting list
                        int oldIndex = runInstructions.FindIndex(r => r.Symbol == sortedAssets[newIndex]);
                        Data.Instruction moving = runInstructions[oldIndex];                            // Get item to move
                        if (oldIndex != newIndex) {
                            runInstructions.RemoveAt(oldIndex);                                         // Remove from old position
                            runInstructions.Insert(newIndex, moving);                                   // Insert at new index per sorting query outcomes
                        }
                    }

                    string[] sortedInstructions = runInstructions.Select(r => r.Symbol).ToArray();

                    Dictionary<string, decimal?> allPrices = await Database.GetPrices_Daily(assets, day);

                    for (int iInstruction = 0; iInstruction < runInstructions.Count; iInstruction++) {
                        Data.Instruction instruction = runInstructions[iInstruction];
                        Data.Test testData = splitTests[iInstruction];

                        bool? toBuy = await Database.QueryStrategy_Scalar(await Strategy.Interpret(strategy.Entry, instruction.Symbol, day));
                        bool? toSellGain = await Database.QueryStrategy_Scalar(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol, day));
                        bool? toSellLoss = await Database.QueryStrategy_Scalar(await Strategy.Interpret(strategy.ExitStopLoss, instruction.Symbol, day));

                        if (!toBuy.HasValue || !toSellGain.HasValue || !toSellLoss.HasValue) {
                            Prompt.WriteLine($"Error detected in SQL query. Please validate queries. Skipping.");
                            return null;
                        }

                        if (testData.Shares > 0) {
                            testData.DaysHeld++;                // Tally amount of days held in position
                        }

                        decimal? price = allPrices.ContainsKey(testData.Asset.ID) ? allPrices[testData.Asset.ID] : null;
                        if (!price.HasValue) {
                            Prompt.Write("-");
                            continue;
                        }

                        if (day.CompareTo(endDate) == 0) {                                     // Last day, end the test
                            if (testData.Shares > 0) {
                                // If position is held at the end of the test- liquidate shares into gains for calculations
                                Prompt.Write("^");

                                dollars += price.Value * testData.Shares;
                                testData.Trades.Add(new Data.Test.Trade() {
                                    Timestamp = day,
                                    Transaction = Data.Test.Trade.Direction.Sell,
                                    Quantity = testData.Shares,
                                    Price = price.Value,
                                    Gain = price.Value * testData.Shares
                                });
                            } else {
                                Prompt.Write(".");
                            }
                        } else if (toBuy.Value && (toSellGain.Value || toSellLoss.Value)) {   // Cannot simultaneously buy and sell ... erroneous queries?
                            Prompt.Write("?");

                            continue;
                        } else if (testData.Shares == 0 && toBuy.Value) {
                            if (dollarLimit && dollars < price.Value) {
                                Prompt.Write("b");
                            } else {
                                int quantity = 1;
                                if (dollarPerLimit) {
                                    quantity = (int)Math.Floor(Math.Min(dollars / price.Value, argDollarsPer / price.Value));
                                    if (quantity == 0) {
                                        Prompt.Write("b");
                                    } else {
                                        Prompt.Write("B");
                                    }
                                } else {
                                    Prompt.Write("B");
                                }

                                dollars -= quantity * price.Value;
                                testData.Shares += quantity;                                        // Buying a position
                                testData.Trades.Add(new Data.Test.Trade() {
                                    Timestamp = day,
                                    Transaction = Data.Test.Trade.Direction.Buy,
                                    Quantity = quantity,
                                    Price = price.Value,
                                    Gain = -price.Value * quantity
                                });
                            }
                        } else if (testData.Shares > 0 && (toSellGain.Value || toSellLoss.Value)) {
                            if (toSellGain.Value) {
                                Prompt.Write("S");
                            } else if (toSellLoss.Value) {
                                Prompt.Write("L");
                            }

                            dollars += price.Value * testData.Shares;
                            testData.Trades.Add(new Data.Test.Trade() {
                                Timestamp = day,
                                Transaction = Data.Test.Trade.Direction.Sell,
                                Quantity = testData.Shares,
                                Price = price.Value,
                                Gain = price.Value * testData.Shares
                            });
                            testData.Shares = 0;                                       // Selling the position
                        } else {
                            if (testData.Shares > 0) {
                                Prompt.Write("*");
                            } else {
                                Prompt.Write(".");
                            }
                        }
                    }
                }

                // Calculate metrics

                foreach (Data.Test test in splitTests) {
                    for (int i = 0; i < test.Trades.Count; i++) {
                        test.GainAmount += test.Trades[i].Gain;

                        if (test.Trades[i].Transaction == Data.Test.Trade.Direction.Sell) {
                            test.GainPercent = test.GainAmount / Math.Abs(test.Trades[0].Gain) * 100;
                            test.GainPercentPerDay = test.DaysHeld > 0 ? (test.GainPercent / test.DaysHeld) : 0m;
                        }
                    }
                }

                int positions = splitTests.Select(s => s.Trades.Count).Sum() / 2;
                int daysHeld = splitTests.Select(s => s.DaysHeld).Max();
                decimal gainAmount = splitTests.Where(s => s.Trades.Count > 0).Select(s => s.GainAmount).Sum();
                decimal gainPercent = splitTests.Where(s => s.Trades.Count > 0).Select(s => s.GainPercent).Average();
                decimal gainPercentPerDay = splitTests.Where(s => s.Trades.Count > 0).Select(s => s.GainPercentPerDay).Average();

                // Display individual metrics

                if (dollarLimit) {
                    Prompt.WriteLine($"\n\nStarting funds ${argDollars:0} -> Ending funds ${dollars:0}");
                }
                Prompt.WriteLine("\n");
                Prompt.WriteLine($"{startDate.Date:yyyy-MM-dd} to {endDate.Date:yyyy-MM-dd}");
                Prompt.WriteLine($"{positions} positions held; {daysHeld} days in holding position");
                Prompt.WriteLine($"Total gain ${gainAmount:n2}; Average gain {gainPercent:0.00}%; Gain per day {gainPercentPerDay:0.00}%");
                Prompt.WriteLine("\n");

                listTests.AddRange(splitTests);
            }

            return listTests;
        }

        public async Task Summary_Discrete(List<Data.Test> tests, List<Data.Strategy> strategies, int days, DateTime endDate) {
            Prompt.WriteLine($"\n\n\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine($">>> Completed testing simulation\n\n");

            Prompt.WriteLine($">>>>>>>>>> Results <<<<<<<<<<");                                // Summary output beginning

            int removed = tests.RemoveAll(t => t == null);
            if (removed > 0) {
                Prompt.WriteLine($"\nRemoved {removed} invalid test results. See test output for errors.");
            }

            Prompt.WriteLine($"\n  Tested {days} trading days prior to {endDate:yyyy-MM-dd}");

            Prompt.WriteLine("\n  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine("      Metrics per Individual Strategy per Symbol");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine($"  {"Gain %",10} \t {"Strategy",-20} {"Symbol",-10} {"ROC %",10}\t {"Days Held",10}\t {"Gain % / Day",10}");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");

            foreach (Data.Test t in tests.OrderBy(o => -o.GainPercent)) {
                decimal gain = t.Trades.Count > 0 ? Math.Abs(t.Trades.First()?.Gain ?? 0m) : 0m;
                decimal gainPerDay = t.DaysHeld > 0 ? (t.GainPercent / t.DaysHeld) : 0m;
                Prompt.WriteLine($"  {t.GainPercent,8:00.00} %\t {t.Strategy.Name,-20} {t.Instruction.Symbol,-10} {t.AssetRateOfChange,8:00.00} %\t {t.DaysHeld,10:0}\t {gainPerDay,10:00.00} %");
            }

            Prompt.WriteLine("\n");

            Prompt.WriteLine("\n  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine("     Mean Metrics per Strategy");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine($"     {"Strategy",-20} {"Mean Gain %",14}\t {"Days Held",10}\t {"Mean Gain % / Day",10}");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");

            foreach (Data.Strategy s in strategies) {
                int sumDaysHeld = 0;
                decimal sumGain = 0m;
                decimal sumGainPercent = 0m;

                List<Data.Test> lt = tests.Where(w => w.Strategy == s).ToList();
                foreach (Data.Test t in lt) {
                    sumGain += t.GainAmount;
                    sumDaysHeld += t.DaysHeld;
                    sumGainPercent += t.GainPercent;
                }

                decimal meanGainPercent = lt.Count > 0 ? (sumGainPercent / lt.Count) : 0m;
                decimal meanGainPercentPerDay = sumDaysHeld > 0 ? (meanGainPercent / sumDaysHeld) : 0m;

                Prompt.WriteLine($"     {s.Name,-20} {meanGainPercent,12:00.00} %\t {sumDaysHeld,10:0}\t {meanGainPercentPerDay,14:00.000}");
            }

            Prompt.WriteLine("\n");
        }

        public async Task Summary_Parallel(List<Data.Test> tests, List<Data.Strategy> strategies, int days, DateTime endDate, decimal dollars, decimal dollarsPer) {
            Prompt.WriteLine($"\n\n\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine($">>> Completed testing simulation\n\n");

            Prompt.WriteLine($">>>>>>>>>> Results <<<<<<<<<<");                                // Summary output beginning

            int removed = tests.RemoveAll(t => t == null);
            if (removed > 0) {
                Prompt.WriteLine($"\nRemoved {removed} invalid test results. See test output for errors.");
            }

            Prompt.WriteLine($"\n  Tested {days} trading days prior to {endDate:yyyy-MM-dd}");
            Prompt.WriteLine(dollars > 0 ? $"  Maximum trading funds utilized: ${dollars:n2}" : "  Unlimited trading funds utilized");
            Prompt.WriteLine(dollarsPer > 0 ? $"  Maximum funds per asset allocated: ${dollarsPer:n2}" : "  Unlimited funds per asset allocated");

            Prompt.WriteLine("\n  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine("      Metrics per Individual Strategy per Symbol");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine($"  {"Gain %",10} \t {"Strategy",-20} {"Symbol",-10} {"ROC %",10}\t {"Days Held",10}\t {"Gain % / Day",10}");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");

            foreach (Data.Test t in tests.OrderBy(o => -o.GainPercent)) {
                decimal gain = t.Trades.Count > 0 ? Math.Abs(t.Trades.First()?.Gain ?? 0m) : 0m;
                decimal gainPerDay = t.DaysHeld > 0 ? (t.GainPercent / t.DaysHeld) : 0m;
                Prompt.WriteLine($"  {t.GainPercent,8:00.00} %\t {t.Strategy.Name,-20} {t.Instruction.Symbol,-10} {t.AssetRateOfChange,8:00.00} %\t {t.DaysHeld,10:0}\t {gainPerDay,10:00.00} %");
            }

            Prompt.WriteLine("\n");

            Prompt.WriteLine("\n  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine("     Mean Metrics per Strategy");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");
            Prompt.WriteLine($"     {"Strategy",-20} {"Mean Gain %",14}\t {"Max Days Held",14}\t{"Mean Gain % / Day",20}");
            Prompt.WriteLine("  --------------------------------------------------------------------------------------------------------");

            foreach (Data.Strategy s in strategies) {
                List<Data.Test> lt = tests.Where(w => w.Strategy == s && w.Trades.Count > 0).ToList();

                int maxDaysHeld = lt.Select(t => t.DaysHeld).Max();
                decimal meanGainPercent = lt.Where(t => t.Trades.Count > 0).Select(t => t.GainPercent).Average();
                decimal meanGainPercentPerDay = lt.Where(t => t.Trades.Count > 0).Select(t => t.GainPercentPerDay).Average();
                Prompt.WriteLine($"     {s.Name,-20} {meanGainPercent,12:00.00} %\t {maxDaysHeld,14:0}\t{meanGainPercentPerDay,18:00.00} %");
            }

            Prompt.WriteLine("\n");
        }
    }
}