using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Trade {

        public async Task RunAutomation(Settings settings, Database db, Data.Format format) {
            Prompt.WriteLine($"Running automated rules for {format.ToString()} trades.");

            List<Data.Asset> assets = await db.GetAssets();
            List<Data.Instruction> instructions = (await db.GetInstructions())?.Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await db.GetStrategies();

            List<Data.Position> positions;
            object result = await API.Alpaca.GetPositions(settings, format);
            if (result is List<Data.Position>) {
                positions = result as List<Data.Position>;
            } else {
                Prompt.WriteLine("Unable to retrieve current trade positions from Alpaca API. Aborting.");
                return;
            }

            if (assets == null || assets.Count == 0) {
                Prompt.WriteLine("Unable to retrieve asset list from database. Aborting.");
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

                Prompt.WriteLine($"[{i + 1:0000} / {instructions.Count:0000}] {instructions[i].Description} ({instructions[i].Format.ToString()}): "
                    + $"{instructions[i].Symbol} x {instructions[i].Quantity} @ {instructions[i].Strategy} ({instructions[i].Frequency.ToString()})");

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

        public async Task RunAutomation_Daily(Settings settings, Database db,
            Data.Instruction instruction, Data.Strategy strategy, Data.Asset asset, Data.Position position) {
            // Get last market close to ensure most up-to-date data exists
            // And that today's potential data exists (since SQL query interprets to query for today's prices!)
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);
            if (result is DateTime) {
                lastMarketClose = (DateTime)result;
            }

            // Check data validity (last update time) to ensure it is most recent
            DateTime validity = await db.GetValidity_Daily(asset);

            if (validity.CompareTo(lastMarketClose) > 0) {
                bool toBuy = await db.ScalarQuery(await Strategy.Interpret(strategy.Entry, instruction.Symbol));
                bool toSell = await db.ScalarQuery(await Strategy.Interpret(strategy.ExitGain, instruction.Symbol))
                    || await db.ScalarQuery(await Strategy.Interpret(strategy.ExitLoss, instruction.Symbol));

                if (toBuy && toSell) {   // Cannot simultaneously buy and sell ... erroneous queries?
                    Prompt.WriteLine("Buy AND Sell triggers met- doing nothing. Check strategy for errors?");
                    return;
                }

                if (toBuy) {
                    if (position == null) {
                        Prompt.WriteLine("Buy trigger detected; no current position found; placing Buy order.");
                        await API.Alpaca.PlaceOrder_BuyMarket(settings, instruction.Format, instruction.Symbol, instruction.Quantity);
                    } else if (position != null) {
                        Prompt.WriteLine("Buy trigger detected; active position already exists; doing nothing.");
                    }
                } else if (toSell) {
                    if (position == null) {
                        Prompt.WriteLine("Sell trigger detected; no current position found; doing nothing.");
                    } else if (position != null)
                        Prompt.WriteLine("Sell trigger detected; active position found; placing Sell order.");
                    await API.Alpaca.PlaceOrder_SellMarket(settings, instruction.Format, instruction.Symbol, instruction.Quantity);
                } else {
                    Prompt.WriteLine("No triggers detected.");
                }
            }
        }
    }
}