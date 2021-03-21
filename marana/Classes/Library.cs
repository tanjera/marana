using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Marana {

    public class Library {
        private Program Program;
        private Settings Settings => Program.Settings;
        private Database Database => Program.Database;

        private API.Alpaca Alpaca => Program.Alpaca;
        private API.AlphaVantage AlphaVantage => Program.AlphaVantage;

        public Library(Program p) {
            Program = p;
        }

        public async Task Erase() {
            Prompt.Write("Are you sure you want to erase market data from the Database?");

            bool confirm = Prompt.YesNo();

            if (confirm) {
                bool wipeResult = await Database.Erase();
                Prompt.WriteLine(wipeResult
                    ? "Operation completed. Market data erased from Database."
                    : "Operation failed. Attempt cancelled.");
            } else {
                Prompt.WriteLine("Operation cancelled.");
            }
        }

        public async Task Update(List<string> args) {
            Prompt.WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine(">>> Library update starting\n");

            Prompt.WriteLine("Initializing Database.\n");
            await Database.Init();

            // Gets all available assets; updates Database as needed
            Prompt.WriteLine("Querying Database for list of ticker symbols.\n");
            List<Data.Asset> allAssets = await GetAssets();

            // Select assets to update data for
            // If args are used, they manually override the Library Settings
            Prompt.WriteLine("Updating Time Series Dailies (TSD).\n");

            // Get assets to update based on command-line arguments and options/Settings
            List<Data.Asset> updateAssets = new List<Data.Asset>();
            if (args.Count > 0) {                                                       // Update re: CLI args
                updateAssets = allAssets;
                Data.Select_Assets(ref updateAssets, args);
                Prompt.WriteLine("\nPer command-line arguments, updating range.\n");
            } else {
                // Update assets used in automated instructions
                List<Data.Asset> instructionAssets = new List<Data.Asset>();
                List<Data.Instruction> instructions = await Database.GetInstructions();

                if (instructions != null && instructions.Count > 0) {
                    foreach (Data.Instruction instruction in instructions) {            // Collect the symbols
                        instructionAssets.Add(allAssets.Find(a => a.Symbol == instruction.Symbol));
                    }

                    instructionAssets = instructionAssets.Distinct().ToList();          // Remove duplicates

                    if (instructionAssets.Count > 0) {                                  // Update library
                        Prompt.WriteLine($"Updating assets with active automated trading instructions (live and paper).\n");
                        await Update_TSD(instructionAssets);
                    }
                }

                switch (Settings.Library_DownloadSymbols) {
                    default: break;

                    case Settings.Option_DownloadSymbols.Watchlist:                     // (Option) Update watchlist only
                        List<string> wl = await Database.GetWatchlist();
                        if (wl == null) {
                            Prompt.WriteLine("Unable to retrieve Watchlist from Database. Aborting.\n");
                            return;
                        }

                        updateAssets = allAssets.Where(a => wl.Contains(a.Symbol)).ToList();

                        Prompt.WriteLine("\nPer options, updating watchlist only.\n");
                        break;

                    case Settings.Option_DownloadSymbols.All:                           // (Option) Update all symbols
                        updateAssets = allAssets;
                        Prompt.WriteLine("\nPer options, updating all symbols.\n");
                        break;
                }
            }

            await Update_TSD(updateAssets);

            Prompt.WriteLine($"\n{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
            Prompt.WriteLine(">>> Library update complete\n");
        }

        public async Task<Data.Asset> GetAsset(string symbol) {
            List<Data.Asset> assets = await GetAssets();
            return assets.Find(a => a.Symbol == symbol);
        }

        public async Task<List<Data.Asset>> GetAssets() {
            // Update ticker symbols weekly
            if (DateTime.UtcNow - Database.GetValidity_Assets().Result > new TimeSpan(7, 0, 0, 0)) {
                await Update_Symbols();
            }

            return await Database.GetAssets();
        }

        public async Task<Data.Daily.Price> GetLastPrice(Data.Asset asset) {
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await Alpaca.GetTime_LastMarketClose();

            if (result is DateTime dt) {
                lastMarketClose = dt;
            } else {
                Prompt.WriteLine("Unable to access market schedule/times via Alpaca API");
            }

            DateTime validity = await Database.GetValidity_Daily(asset);
            if (validity.CompareTo(lastMarketClose) < 0) {
                await Update_TSD(new List<Data.Asset>() { asset });
                await Task.Delay(2000);                 // Allow Database update thread to get ahead
            }

            Data.Daily dd = await Database.GetData_Daily(asset);
            return dd.Prices.Last();
        }

        /// <summary>
        /// Gets the latest prices from the Database (and updating per validity) of a list of assets
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="db"></param>
        /// <param name="assets">List of assets to get prices for</param>
        /// <returns>Dictionary of asset ID, closing price</returns>
        public async Task<Dictionary<string, decimal?>> GetLastPrices(List<Data.Asset> assets) {
            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await Alpaca.GetTime_LastMarketClose();

            if (result is DateTime dt) {
                lastMarketClose = dt;
            } else {
                Prompt.WriteLine("Unable to access market schedule/times via Alpaca API");
            }

            Dictionary<string, DateTime> validities = await Database.GetValidities();

            for (int i = 0; i < assets.Count; i++) {
                string validityKey = await Database.GetValidityKey_Daily(assets[i]);
                DateTime validity = validities.ContainsKey(validityKey) ? validities[validityKey] : new DateTime();
                if (validity.CompareTo(lastMarketClose) < 0) {
                    await Update_TSD(new List<Data.Asset>() { assets[i] });
                }
            }

            await Task.Delay(2000);                 // Allow Database update thread to get ahead

            return await Database.GetPrices_Daily_Last(assets);
        }

        public async Task GetInfo() {
            Prompt.WriteLine("Connecting to Database...");

            // Connect to Database, post results

            decimal size = await Database.GetSize();
            Prompt.WriteLine($"Database Size: {size} MB\n");
        }

        public async Task Update_Symbols() {
            Prompt.Write("Updating list of ticker symbols. ");

            object output = await Alpaca.GetAssets();
            if (output is List<Data.Asset> list) {
                await Database.SetAssets(list);
                Prompt.WriteLine("Completed", ConsoleColor.Green);
            } else {
                Prompt.WriteLine("Error", ConsoleColor.Red);
            }
        }

        public async Task Update_TSD(List<Data.Asset> assets) {
            List<Task> threads = new List<Task>();

            DateTime lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            object result = await Alpaca.GetTime_LastMarketClose();
            if (result is DateTime dt) {
                lastMarketClose = dt;
            } else {
                Prompt.WriteLine("Unable to access market schedule/times via Alpaca API");
            }

            int retryCounter = 0;

            await Settings.ClearCache();                // Routine emptying of cache directory

            // Iterate all symbols in list (assets), call API to download data, write to files in library
            for (int i = 0; i < assets.Count; i++) {
                Prompt.Write($"  [{i + 1:0000} / {assets.Count:0000}]  {assets[i].Symbol,-8}  ");

                /* Check validity timestamp against last known market close
                 */

                DateTime validity = await Database.GetValidity_Daily(assets[i]);
                if (validity.CompareTo(lastMarketClose) > 0) {
                    await Task.Delay(10);               // Allows GUI responsiveness
                    Prompt.WriteLine("Database current. Skipping.");
                    continue;
                }

                Prompt.Write("Requesting data. ");

                object output = null;
                Data.Daily dd = new Data.Daily();

                if (Settings.Library_DataProvider == Settings.Option_DataProvider.Alpaca) {
                    output = await Alpaca.GetData_Daily(assets[i], Settings.Library_LimitDailyEntries);
                } else if (Settings.Library_DataProvider == Settings.Option_DataProvider.AlphaVantage) {
                    string apiOutput = "";
                    string apiFilePath = Path.Combine(Settings.GetCacheDirectory(), Path.GetRandomFileName());
                    string apiCache = $"{apiFilePath}.cache";
                    string apiLockout = $"{apiFilePath}.lockout";

                    DateTime timeoutTime = DateTime.Now + new TimeSpan(0, 1, 0);        // 1 minute timeout
                    _ = AlphaVantage.CacheData_Daily(assets[i].Symbol, apiCache, apiLockout);

                    while (!File.Exists(apiLockout) && DateTime.Now <= timeoutTime) {
                        await Task.Delay(500);
                    }

                    if (File.Exists(apiLockout)) {
                        if (!File.Exists(apiCache)) {
                            apiOutput = "ERROR:TIMEOUT";
                        } else {
                            StreamReader sr = new StreamReader(apiCache);
                            apiOutput = await sr.ReadToEndAsync();
                            sr.Close();
                            sr.Dispose();
                        }

                        try {
                            if (File.Exists(apiCache))
                                File.Delete(apiCache);

                            if (File.Exists(apiLockout))
                                File.Delete(apiLockout);
                        } catch (Exception ex) {
                            await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                        }
                    } else {
                        apiOutput = "ERROR:TIMEOUT";
                    }

                    if (apiOutput == "ERROR:INVALID"
                        || apiOutput == "ERROR:INVALIDKEY"
                        || apiOutput == "ERROR:EXCEEDEDCALLS"
                        || apiOutput == "ERROR:EXCEPTION"
                        || apiOutput == "ERROR:TIMEOUT"
                        || apiOutput.StartsWith("ERROR:WEBEXCEPTION:")) {
                        output = apiOutput;
                    } else {
                        Prompt.Write("Parsing. ");
                        output = await AlphaVantage.ParseData_Daily(apiOutput, Settings.Library_LimitDailyEntries);
                    }
                }

                if (output is Data.Daily pmdd) {
                    dd = pmdd;
                    retryCounter = 0;
                } else if (output is string pms) {
                    if (pms == "Too Many Requests"                  // Alpaca's return message for exceeding API calls
                            || pms == "ERROR:EXCEEDEDCALLS") {      // Alpha Vantage's return message for exceeding API calls
                        Prompt.WriteLine("Exceeded API calls per minute- pausing for 30 seconds.");

                        await Task.Delay(30000);

                        i--;
                        retryCounter = 0;
                        continue;
                    } else if (pms == "ERROR:EXCEPTION" || pms == "ERROR:TIMEOUT") {
                        if (pms == "ERROR:EXCEPTION")
                            Prompt.WriteLine($"Error, Attempt #{retryCounter + 1}");
                        else if (pms == "ERROR:TIMEOUT")
                            Prompt.WriteLine($"Timeout, Attempt #{retryCounter + 1}");

                        if (retryCounter < 4) {
                            i--;
                            retryCounter++;
                        } else
                            retryCounter = 0;

                        continue;
                    } else {
                        Prompt.WriteLine($"Error: {output}");
                        retryCounter = 0;
                        continue;
                    }
                } else if (output == null) {
                    Prompt.WriteLine($"Error: See Error Log.");
                    retryCounter = 0;
                    continue;
                }

                dd.Asset = assets[i];

                /* Calculate metrics, stock indicators
                 */
                Prompt.Write("Calculating indicators. ");
                dd = await Calculate.Metrics(dd);

                /* Save to Database
                 * Use threading for highly improved speed!
                 */

                Prompt.WriteLine("Updating Database.", ConsoleColor.Green);

                Task thread = new Task(async () => { await Database.SetData_Daily(dd); });
                thread.Start();
                threads.Add(thread);

                int awaiting = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
                if (awaiting >= 10)
                    await Task.Delay(1000);
            }

            int finishing = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
            if (finishing > 0) {
                Prompt.WriteLine($"  Completing {finishing} remaining background Database tasks.");
                await Task.Delay(5000);

                finishing = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
            }
        }
    }
}