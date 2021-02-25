using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Trading {

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

        public Trading() {
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

        /* Trading functionality:
         */

        public static async Task<decimal?> GetAvailableCash(Settings settings, Database db, Data.Format format) {
            object result;

            decimal cash;
            result = await API.Alpaca.GetTradeableCash(settings, format);
            if (result is decimal r) {
                cash = r;
            } else {
                return null;
            }

            List<Data.Order> orders;
            result = await API.Alpaca.GetOrders_OpenBuy(settings, format);
            if (result is List<Data.Order> pml) {
                orders = pml;
            } else {
                return null;
            }

            decimal marked = 0m;
            Library library = new Library();

            List<Data.Asset> assets = (await library.GetAssets(db))?.Where(a => orders.Any(o => o.Symbol == a.Symbol)).ToList();
            Dictionary<string, decimal?> prices = await library.GetLastPrices(settings, db, assets);

            for (int i = 0; i < orders.Count; i++) {
                Data.Asset asset = assets.Find(a => a.Symbol == orders[i].Symbol);

                if (asset == null || prices == null || !prices.ContainsKey(asset.ID))
                    return null;

                marked += orders[i].Quantity * prices[asset.ID] ?? 999999m;
            }

            return cash - marked;
        }

        public async Task RunAutomation(Settings settings, Database db, Data.Format format, DateTime day) {
            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Running automated rules for {format} instructions\n");

            List<Data.Instruction> instructions = (await db.GetInstructions())?.Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await db.GetStrategies();

            List<Data.Asset> assets = await new Library().GetAssets(db);
            List<Data.Position> positions;
            List<Data.Order> orders;

            object result;

            result = await API.Alpaca.GetPositions(settings, format);
            if (result is List<Data.Position> pmldp) {
                positions = pmldp;
            } else {
                WriteLine("Unable to retrieve current trade positions from Alpaca API. Aborting.\n");
                return;
            }

            result = await API.Alpaca.GetOrders_OpenBuy(settings, format);
            if (result is List<Data.Order> pmldo) {
                orders = pmldo;
            } else {
                WriteLine("Unable to retrieve current open orders from Alpaca API. Aborting.\n");
                return;
            }

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
                Data.Position position = positions.Find(p => p.Symbol == instructions[i].Symbol);
                Data.Order order = orders.Find(o => o.Symbol == instructions[i].Symbol && o.Quantity == instructions[i].Quantity);

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
                    if (!instructions[i].Active) {
                        WriteLine($"Instruction marked as 'Inactive'. Skipping.\n");
                    } else if (instructions[i].Active) {
                        await RunAutomation_Daily(settings, db, format, instructions[i], strategy, day, asset, position, order);
                    }
                }
            }

            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine($">>> Completed running automated rules for {format} instructions\n");
        }

        public async Task RunAutomation_Daily(Settings settings, Database db,
                Data.Format format, Data.Instruction instruction, Data.Strategy strategy,
                DateTime day, Data.Asset asset, Data.Position position, Data.Order order,
                bool useMargin = false) {
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
                bool? toBuy = await db.ScalarQuery(await Strategy.Interpret(strategy.Entry, instruction.Symbol, day));
                bool? toSellGain = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol, day));
                bool? toSellLoss = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitStopLoss, instruction.Symbol, day));

                if (!toBuy.HasValue || !toSellGain.HasValue || !toSellLoss.HasValue) {
                    WriteLine($"Error detected in SQL query. Please validate queries. Skipping.");
                    return;
                }

                if (toBuy.Value && (toSellGain.Value || toSellLoss.Value)) {   // Cannot simultaneously buy and sell ... erroneous queries?
                    WriteLine("Buy AND Sell triggers met- doing nothing. Check strategy for errors?");
                    return;
                }

                if (toBuy.Value) {
                    // Warning: API buy/sell orders use negative quantity to indicate short positions
                    // And/or may just throw exceptions when attempting to sell to negative

                    if (position != null && position.Quantity > 0) {
                        WriteLine("  Buy trigger detected; active position already exists; doing nothing.");
                    } else if (order != null) {
                        WriteLine("  Buy trigger detected; identical open buy order already exists; doing nothing.");
                    } else if (order == null && (position == null || position.Quantity <= 0)) {
                        WriteLine("  Buy trigger detected; no current position owned; placing Buy order.");

                        // If not using margin trading
                        // Ensure there is (as best as can be approximated) enough cash in account for transaction

                        decimal? availableCash = await GetAvailableCash(settings, db, format);

                        if (!useMargin && availableCash == null) {
                            WriteLine("    Error calculating available cash; instructed not to trade on margin; aborting.");
                            return;
                        }

                        decimal? lastPrice = (await library.GetLastPrice(settings, db, asset)).Close;
                        decimal? orderPrice = instruction.Quantity * lastPrice;

                        if (!useMargin && (lastPrice == null || orderPrice == null)) {
                            WriteLine("    Error calculating estimated cost of buy order; unable to determine if margin trading needed; aborting.");
                            return;
                        }

                        if (!useMargin && (availableCash < orderPrice)) {
                            WriteLine($"    Available cash ${availableCash:n0} insufficient for buy order ${orderPrice:n0}; aborting.");
                            return;
                        }

                        WriteLine($"    Available cash ${availableCash:n0} sufficient for buy order ${orderPrice:n0}.");

                        switch (await API.Alpaca.PlaceOrder_BuyMarket(settings, db, instruction.Format, instruction.Symbol, instruction.Quantity)) {
                            case OrderResult.Success:
                                WriteLine(">> Order successfully placed.", ConsoleColor.Green);
                                break;

                            case OrderResult.Fail:
                                WriteLine(">> Order placement unsuccessful.", ConsoleColor.Red);
                                break;

                            case OrderResult.FailInsufficientFunds:
                                WriteLine(">> Order placement unsuccessful; Insufficient available funds.", ConsoleColor.Red);
                                break;
                        }
                    }
                } else if (toSellGain.Value || toSellLoss.Value) {
                    string msgSellQuery = toSellGain.Value && toSellLoss.Value ? "Both" : (toSellGain.Value ? "Gain" : (toSellLoss.Value ? "Stop-Loss" : ""));

                    if (position == null || position.Quantity <= 0) {
                        WriteLine($"  Sell trigger detected; {msgSellQuery}; no current position owned; doing nothing.");
                    } else if (position != null && position.Quantity > 0) {
                        WriteLine($"  Sell trigger detected; {msgSellQuery}; active position found; placing Sell order.");
                        // Sell position.quantity in case position.Quantity != instruction.Quantity
                        switch (await API.Alpaca.PlaceOrder_SellMarket(settings, instruction.Format, instruction.Symbol, position.Quantity)) {
                            case OrderResult.Success:
                                WriteLine(">> Order successfully placed.", ConsoleColor.Green);
                                break;

                            case OrderResult.Fail:
                                WriteLine(">> Order placement unsuccessful.", ConsoleColor.Red);
                                break;
                        }
                    }
                } else {
                    WriteLine("  No triggers detected.");
                }
            }
        }
    }
}