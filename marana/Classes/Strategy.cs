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

        /// <summary>
        /// Interprets a SQL query to replace {DATE}
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <param name="day">Date to interpret {DATE} to</param>
        /// <returns></returns>
        public static async Task<string> Interpret(string query, DateTime day) {
            return query?
                .Replace("{DATE}", day.ToString("yyyy-MM-dd"));
        }

        /// <summary>
        /// Interprets a SQL query to replace {SYMBOL}, {DATE}
        /// </summary>
        /// <param name="query">SQL query</param>
        /// <param name="symbol">Symbol to interpret {SYMBOL} to</param>
        /// <param name="day">Date to interpret {DATE} to</param>
        /// <returns></returns>
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

                Prompt.Write($"  Running Sort By query: \t");
                result = await Database.ValidateQuery_SortBy(strategy.SortBy);
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