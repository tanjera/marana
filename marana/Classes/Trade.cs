using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Trade {

        public async Task RunAutomation(Settings settings, Database db, Data.Format format) {
            List<Data.Asset> assets = await db.GetAssets();
            List<Data.Instruction> instructions = (await db.GetInstructions()).Where(i => i.Format == format).ToList();
            List<Data.Strategy> strategies = await db.GetStrategies();

            Prompt.WriteLine($"Running {instructions.Count} automated rules for {format.ToString()} trades.");

            foreach (Data.Instruction instruction in instructions) {
                Data.Strategy strategy = strategies.Find(s => s.Name == instruction.Strategy);
                Data.Asset asset = assets.Find(a => a.Symbol == instruction.Symbol);

                if (strategy == null || asset == null) {
                    continue;
                }

                Prompt.WriteLine($"Processing {instruction.Description} ({instruction.Format.ToString()}): "
                    + $"{instruction.Symbol} x {instruction.Quantity} @ {instruction.Strategy} ({instruction.Frequency.ToString()})");

                if (instruction.Frequency == Data.Frequency.Daily) {
                    await RunAutomation_Daily(settings, db, instruction, strategy, asset);
                }
            }
        }

        public async Task RunAutomation_Daily(Settings settings, Database db,
            Data.Instruction instruction, Data.Strategy strategy, Data.Asset asset) {
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
                    Prompt.WriteLine("Buy trigger detected; placing Buy order.");
                    await API.Alpaca.PlaceOrder_BuyMarket(settings, instruction.Format, instruction.Symbol, instruction.Quantity);
                } else if (toSell) {
                    Prompt.WriteLine("Sell trigger detected; placing Sell order.");
                    await API.Alpaca.PlaceOrder_SellMarket(settings, instruction.Format, instruction.Symbol, instruction.Quantity);
                }
            }
        }
    }
}