using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Marana {

    public class Library {
        /* Status and Output variables, handler, and event triggering
         * For updating GUI with status and output text in an asynchronous manner
         */

        public Statuses Status;
        public List<string> Output;
        public bool CancelUpdate = false;

        public enum Statuses {
            Inactive,
            Updating
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

        public Library() {
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

        /* Library functionality:
         * Updating the data library, setting, and getting data
         */

        public async Task Update(List<string> args, Settings settings, Database database) {
            Status = Statuses.Updating;

            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine(">>> Library update starting\n");

            WriteLine("Initializing database.\n");
            await database.Init();

            // Gets all available assets; updates database as needed
            WriteLine("Querying database for list of ticker symbols.\n");
            List<Data.Asset> allAssets = await GetAssets(database);

            // Select assets to update data for
            // If args are used, they manually override the Library Settings
            WriteLine("Updating Time Series Dailies (TSD).\n");

            // Get assets to update based on command-line arguments and options/settings
            List<Data.Asset> updateAssets = new List<Data.Asset>();
            if (args.Count > 0) {                                                       // Update re: CLI args
                updateAssets = allAssets;
                Data.Select_Assets(ref updateAssets, args);
                WriteLine("\nPer command-line arguments, updating range.\n");
            } else {
                // Update assets used in automated instructions
                List<Data.Asset> instructionAssets = new List<Data.Asset>();
                List<Data.Instruction> instructions = await database.GetInstructions();

                if (instructions != null && instructions.Count > 0) {
                    foreach (Data.Instruction instruction in instructions) {            // Collect the symbols
                        instructionAssets.Add(allAssets.Find(a => a.Symbol == instruction.Symbol));
                    }

                    instructionAssets = instructionAssets.Distinct().ToList();          // Remove duplicates

                    if (instructionAssets.Count > 0) {                                  // Update library
                        WriteLine($"Updating assets with active automated trading instructions (live and paper).\n");

                        if (await Update_TSD(instructionAssets, settings, database) == ExitCode.Cancelled) {
                            await Update_Cancel();
                            return;
                        }
                    }
                }

                switch (settings.Library_DownloadSymbols) {
                    default: break;

                    case Settings.Option_DownloadSymbols.Watchlist:                     // (Option) Update watchlist only
                        List<string> wl = await database.GetWatchlist();
                        if (wl == null) {
                            WriteLine("Unable to retrieve Watchlist from database. Aborting.\n");
                            await Update_Cancel();
                            return;
                        }

                        updateAssets = allAssets.Where(a => wl.Contains(a.Symbol)).ToList();

                        WriteLine("\nPer options, updating watchlist only.\n");
                        break;

                    case Settings.Option_DownloadSymbols.All:                           // (Option) Update all symbols
                        updateAssets = allAssets;
                        WriteLine("\nPer options, updating all symbols.\n");
                        break;
                }
            }

            if (await Update_TSD(updateAssets, settings, database) == ExitCode.Cancelled) {
                await Update_Cancel();
                return;
            }

            await Update_Complete();
        }

        public async Task Update_Cancel() {
            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine(">>> Library update cancelled\n");
            Status = Statuses.Inactive;
            CancelUpdate = false;
        }

        public async Task Update_Complete() {
            WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            WriteLine(">>> Library update complete\n");
            Status = Statuses.Inactive;
        }

        public async Task<Data.Asset> GetAsset(Database db, string symbol) {
            List<Data.Asset> assets = await GetAssets(db);
            return assets.Find(a => a.Symbol == symbol);
        }

        public async Task<List<Data.Asset>> GetAssets(Database db) {
            // Update ticker symbols weekly
            if (DateTime.UtcNow - db.GetValidity_Assets().Result > new TimeSpan(7, 0, 0, 0))
                await Update_Symbols(db);

            return await db.GetAssets();
        }

        public async Task<Data.Daily.Price> GetLastPrice(Settings settings, Database db, Data.Asset asset) {
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);

            if (result is DateTime dt) {
                lastMarketClose = dt;
            } else {
                WriteLine("Unable to access market schedule/times via Alpaca API");
            }

            DateTime validity = await db.GetValidity_Daily(asset);
            if (validity.CompareTo(lastMarketClose) < 0) {
                await Update_TSD(new List<Data.Asset>() { asset }, settings, db);
                await Task.Delay(2000);                 // Allow database update thread to get ahead
            }

            Data.Daily dd = await db.GetData_Daily(asset);
            return dd.Prices.Last();
        }

        /// <summary>
        /// Gets the latest prices from the database (and updating per validity) of a list of assets
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="db"></param>
        /// <param name="assets">List of assets to get prices for</param>
        /// <returns>Dictionary of asset ID, closing price</returns>
        public async Task<Dictionary<string, decimal?>> GetLastPrices(Settings settings, Database db, List<Data.Asset> assets) {
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);

            if (result is DateTime dt) {
                lastMarketClose = dt;
            } else {
                WriteLine("Unable to access market schedule/times via Alpaca API");
            }

            Dictionary<string, DateTime> validities = await db.GetValidities();

            for (int i = 0; i < assets.Count; i++) {
                string validityKey = await db.GetValidityKey_Daily(assets[i]);
                DateTime validity = validities.ContainsKey(validityKey) ? validities[validityKey] : new DateTime();
                if (validity.CompareTo(lastMarketClose) < 0) {
                    await Update_TSD(new List<Data.Asset>() { assets[i] }, settings, db);
                }
            }

            await Task.Delay(2000);                 // Allow database update thread to get ahead

            return await db.GetPrices_Daily_Latest(assets);
        }

        public async Task<ExitCode> Update_Symbols(Database db) {
            Write("Updating list of ticker symbols. ");

            object output = await API.Alpaca.GetAssets(db._Settings);
            if (output is List<Data.Asset> list) {
                await db.SetAssets(list);
                WriteLine("Completed", ConsoleColor.Green);
                return ExitCode.Completed;
            } else {
                WriteLine("Error", ConsoleColor.Red);
                return ExitCode.Completed;
            }
        }

        public async Task<ExitCode> Update_TSD(List<Data.Asset> assets, Settings settings, Database db) {
            List<Task> threads = new List<Task>();

            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);
            if (result is DateTime dt) {
                lastMarketClose = dt;
            } else {
                WriteLine("Unable to access market schedule/times via Alpaca API");
            }

            // Iterate all symbols in list (assets), call API to download data, write to files in library
            for (int i = 0; i < assets.Count; i++) {
                if (CancelUpdate)
                    return ExitCode.Cancelled;

                Write($"  [{i + 1:0000} / {assets.Count:0000}]  {assets[i].Symbol,-8}  ");

                /* Check validity timestamp against last known market close
                 */

                DateTime validity = await db.GetValidity_Daily(assets[i]);
                if (validity.CompareTo(lastMarketClose) > 0) {
                    await Task.Delay(10);               // Allows GUI responsiveness
                    WriteLine("Database current. Skipping.");
                    continue;
                }

                Write("Requesting data. ");

                object output = null;
                Data.Daily dd = new Data.Daily();

                if (settings.Library_DataProvider == Settings.Option_DataProvider.Alpaca)
                    output = await API.Alpaca.GetData_Daily(settings, assets[i], settings.Library_LimitDailyEntries);
                else if (settings.Library_DataProvider == Settings.Option_DataProvider.AlphaVantage)
                    output = await API.AlphaVantage.GetData_Daily(settings, assets[i], settings.Library_LimitDailyEntries);

                if (output is Data.Daily pmdd)
                    dd = pmdd;
                else if (output is string pms) {
                    if (pms == "Too Many Requests"                  // Alpaca's return message for exceeding API calls
                            || pms == "ERROR:EXCEEDEDCALLS") {      // Alpha Vantage's return message for exceeding API calls
                        WriteLine("Exceeded API calls per minute- pausing for 30 seconds.");
                        await Task.Delay(30000);
                        i--;
                        continue;
                    } else {
                        WriteLine($"Error: {output}");
                        continue;
                    }
                }

                dd.Asset = assets[i];

                /* Calculate metrics, stock indicators
                 */
                Write("Calculating indicators. ");
                dd = await Calculate.Metrics(dd);

                /* Save to database
                 * Use threading for highly improved speed!
                 */

                WriteLine("Updating database.", ConsoleColor.Green);

                Task thread = new Task(async () => { await db.SetData_Daily(dd); });
                thread.Start();
                threads.Add(thread);

                int awaiting = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
                if (awaiting >= 10)
                    await Task.Delay(1000);
            }

            int finishing = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
            if (finishing > 0) {
                WriteLine($"  Completing {finishing} remaining background database tasks.");
                await Task.Delay(5000);

                finishing = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
            }

            return ExitCode.Completed;
        }
    }
}