using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Trade {

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
            if (result is List<Data.Order> l) {
                orders = l;
            } else {
                return null;
            }

            decimal marked = 0m;
            Library library = new Library();
            foreach (Data.Order order in orders) {
                decimal? price = (await library.GetLastQuote(db, order.Symbol)).Price;
                if (price == null)
                    return null;

                marked += order.Quantity * price ?? 999999m;
            }

            return cash - marked;
        }

        public static async Task RunAutomation(Settings settings, Database db, Data.Format format) {
            Prompt.WriteLine($"\nRunning automated rules for {format} trades.");
            List<Data.Instruction> instructions = (await db.GetInstructions())?.Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await db.GetStrategies();

            List<Data.Asset> assets = await new Library().GetAssets(db);
            List<Data.Position> positions;

            object result = await API.Alpaca.GetPositions(settings, format);
            if (result is List<Data.Position>) {
                positions = result as List<Data.Position>;
            } else {
                Prompt.WriteLine("Unable to retrieve current trade positions from Alpaca API. Aborting.");
                return;
            }

            if (assets == null || assets.Count == 0) {
                Prompt.WriteLine("Unable to retrieve asset list from database.");
                return;
            }

            if (instructions == null || instructions.Count == 0) {
                Prompt.WriteLine("No automated instruction rules found in database. Aborting.");
                return;
            }

            if (strategies == null || strategies.Count == 0) {
                Prompt.WriteLine("No automation strategies found in database. Aborting.");
                return;
            }

            for (int i = 0; i < instructions.Count; i++) {
                Data.Strategy strategy = strategies.Find(s => s.Name == instructions[i].Strategy);
                Data.Asset asset = assets.Find(a => a.Symbol == instructions[i].Symbol);
                Data.Position position = positions.Find(p => p.Symbol == instructions[i].Symbol);

                Prompt.WriteLine($"\n[{i + 1:0000} / {instructions.Count:0000}] {instructions[i].Description} ({instructions[i].Format}): "
                    + $"{instructions[i].Symbol} x {instructions[i].Quantity} @ {instructions[i].Strategy} ({instructions[i].Frequency})");

                if (strategy == null) {
                    Prompt.WriteLine($"Strategy '{instructions[i].Strategy}' not found in database. Aborting.");
                    continue;
                }

                if (asset == null) {
                    Prompt.WriteLine($"Asset '{instructions[i].Symbol}' not found in database. Aborting.");
                    continue;
                }

                if (instructions[i].Frequency == Data.Frequency.Daily) {
                    await RunAutomation_Daily(settings, db, instructions[i], strategy, asset, position);
                }
            }
        }

        public static async Task RunAutomation_Daily(Settings settings, Database db,
            Data.Instruction instruction, Data.Strategy strategy, Data.Asset asset, Data.Position position) {
            // Get last market close to ensure most up-to-date data exists
            // And that today's potential data exists (since SQL query interprets to query for today's prices!)
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);
            if (result is DateTime r) {
                lastMarketClose = r;
            }

            // Check data validity (last update time) to ensure it is most recent
            DateTime validity = await db.GetValidity_Daily(asset);

            if (validity.CompareTo(lastMarketClose) < 0) {              // If data is invalid
                Prompt.WriteLine("Latest market data for this symbol needs updating. Updating now.");
                await new Library().Update_TSD(new List<Data.Asset>() { asset }, settings, db);
                await Task.Delay(10000);                         // Allow library update's database threads to get ahead
                validity = await db.GetValidity_Daily(asset);
            }

            if (validity.CompareTo(lastMarketClose) > 0) {       // If validity is current, data is valid
                bool toBuy = await db.ScalarQuery(await Strategy.Interpret(strategy.Entry, instruction.Symbol));
                bool toSell = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol))
                    || await db.ScalarQuery(await Strategy.Interpret(strategy.ExitLoss, instruction.Symbol));

                if (toBuy && toSell) {   // Cannot simultaneously buy and sell ... erroneous queries?
                    Prompt.WriteLine("Buy AND Sell triggers met- doing nothing. Check strategy for errors?");
                    return;
                }

                if (toBuy) {
                    // Warning: API buy/sell orders use negative quantity to indicate short positions
                    // And/or may just throw exceptions when attempting to sell to negative

                    if (position == null || position.Quantity <= 0) {
                        Prompt.WriteLine("Buy trigger detected; no current position owned; placing Buy order.");
                        if (await API.Alpaca.PlaceOrder_BuyMarket(settings, db, instruction.Format, instruction.Symbol, instruction.Quantity)) {
                            Prompt.WriteLine("Order successfully placed.", ConsoleColor.Green);
                        } else {
                            Prompt.WriteLine("Order placement unsuccessful.", ConsoleColor.Red);
                        }
                    } else if (position != null && position.Quantity > 0) {
                        Prompt.WriteLine("Buy trigger detected; active position already exists; doing nothing.");
                    }
                } else if (toSell) {
                    if (position == null || position.Quantity <= 0) {
                        Prompt.WriteLine("Sell trigger detected; no current position owned; doing nothing.");
                    } else if (position != null && position.Quantity > 0) {
                        Prompt.WriteLine("Sell trigger detected; active position found; placing Sell order.");
                        // Sell position.quantity in case position.Quantity != instruction.Quantity
                        if (await API.Alpaca.PlaceOrder_SellMarket(settings, instruction.Format, instruction.Symbol, position.Quantity)) {
                            Prompt.WriteLine("Order successfully placed.", ConsoleColor.Green);
                        } else {
                            Prompt.WriteLine("Order placement unsuccessful.", ConsoleColor.Red);
                        }
                    }
                } else {
                    Prompt.WriteLine("No triggers detected.");
                }
            }
        }
    }
}