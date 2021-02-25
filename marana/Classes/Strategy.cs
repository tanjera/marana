using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Strategy {
        private Program Program;
        private Database Database => Program.Database;

        public Strategy(Program p) {
            Program = p;
        }

        public static async Task<string> Interpret(string query, string symbol, DateTime day) {
            return query?
                .Replace("{SYMBOL}", symbol)
                .Replace("{DATE}", day.ToString("yyyy-MM-dd"));
        }

        public async Task Validate() {
            // Link view item functionality
            List<Data.Strategy> strategies = await Database.GetStrategies();

            if (strategies == null) {
                Prompt.WriteLine("\nNo strategies found in database to validate.");
                return;
            }

            Prompt.WriteLine($"\nFound {strategies.Count} strategies in database. Testing.\n");

            foreach (Data.Strategy strategy in strategies) {
                object result;
                Prompt.WriteLine($"\nTesting strategy: {strategy.Name}\n");

                Prompt.Write($"  Running Entry query: \t\t");
                result = await Database.ValidateQuery(
                    await Strategy.Interpret(strategy.Entry, "SPY", DateTime.Today));
                if (result is bool) {
                    Prompt.WriteLine($"Successful!", ConsoleColor.Green);
                } else if (result is string) {
                    Prompt.WriteLine($"\n{result}\n");
                }

                Prompt.Write($"  Running Exit Gain query: \t");
                result = await Database.ValidateQuery(
                   await Strategy.Interpret(strategy.ExitGain, "SPY", DateTime.Today));
                if (result is bool) {
                    Prompt.WriteLine($"Successful!", ConsoleColor.Green);
                } else if (result is string) {
                    Prompt.WriteLine($"\n{result}\n");
                }

                Prompt.Write($"  Running Stop Loss query: \t");
                result = await Database.ValidateQuery(
                   await Strategy.Interpret(strategy.ExitStopLoss, "SPY", DateTime.Today));
                if (result is bool) {
                    Prompt.WriteLine($"Successful!", ConsoleColor.Green);
                } else if (result is string) {
                    Prompt.WriteLine($"\n{result}\n");
                }

                Prompt.WriteLine("");
            }
        }
    }
}