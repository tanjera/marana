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

            Output[Output.Count - 1] = $"{Output[Output.Count - 1]}{message}";
            OnStatusUpdate();

            Prompt.Write(message, color);
        }

        public void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray) {
            if (Output.Count == 0)
                Output.Add("");

            Output[Output.Count - 1] = $"{Output[Output.Count - 1]}{message}";
            Output.Add("");
            OnStatusUpdate();

            Prompt.WriteLine(message, color);
        }

        /* Library functionality:
         * Updating the data library, setting, and getting data
         */

        public async Task Update(List<string> args, Settings settings, Database database) {
            Status = Statuses.Updating;

            WriteLine("Initializing database.");
            await database.Init();

            // Gets all available assets; updates database as needed
            WriteLine("Querying database for list of ticker symbols.");
            List<Data.Asset> allAssets = await GetAssets(database);

            // Select assets to update data for
            // If args are used, they manually override the Library Settings
            Write("Updating Time Series Dailies (TSD). ");

            List<Data.Asset> updateAssets = new List<Data.Asset>();
            if (args.Count > 0) {
                updateAssets = allAssets;
                Data.Select_Assets(ref updateAssets, args);
                WriteLine("Per command-line arguments, updating range.");
            } else {
                switch (settings.Library_DownloadSymbols) {
                    default: break;

                    case Settings.Option_DownloadSymbols.Watchlist:
                        List<string> wl = await database.GetWatchlist();
                        updateAssets = allAssets.Where(a => wl.Contains(a.Symbol)).ToList();
                        WriteLine("Per options, updating watchlist only.");
                        break;

                    case Settings.Option_DownloadSymbols.All:
                        updateAssets = allAssets;
                        WriteLine("Per options, updating all symbols.");
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
            WriteLine("Library update cancelled.");
            Status = Statuses.Inactive;
            CancelUpdate = false;
        }

        public async Task Update_Complete() {
            WriteLine("Library update complete.");
            Status = Statuses.Inactive;
        }

        public async Task<List<Data.Asset>> GetAssets(Database db) {
            // Update ticker symbols weekly
            if (DateTime.UtcNow - db.GetValidity_Assets().Result > new TimeSpan(7, 0, 0, 0))
                await Update_Symbols(db);

            return await db.GetAssets();
        }

        public async Task<ExitCode> Update_Symbols(Database db) {
            Write("Updating list of ticker symbols. ");

            object output = await API.Alpaca.GetAssets(db._Settings);
            if (output is List<Data.Asset>) {
                await db.SetAssets((List<Data.Asset>)output);
                WriteLine("Completed", ConsoleColor.Green);
                return ExitCode.Completed;
            } else {
                WriteLine("Error", ConsoleColor.Red);
                return ExitCode.Completed;
            }
        }

        public async Task<ExitCode> Update_TSD(List<Data.Asset> assets, Settings settings, Database db) {
            List<Task> threads = new List<Task>();

            DateTime lastMarketClose = new DateTime();
            object result = await API.Alpaca.GetTime_LastMarketClose(settings);
            if (result is DateTime) {
                lastMarketClose = (DateTime)result;
            } else {
                lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            }

            // Iterate all symbols in list (assets), call API to download data, write to files in library
            for (int i = 0; i < assets.Count; i++) {
                if (CancelUpdate)
                    return ExitCode.Cancelled;

                Write($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm")} [{i + 1:0000} / {assets.Count:0000}]  {assets[i].Symbol,-8}  ");

                /* Check validity timestamp against last known market close
                 */

                DateTime validity = await db.GetValidity_Daily(assets[i]);
                if (validity.CompareTo(lastMarketClose) > 0) {
                    await Task.Delay(10);               // Allows GUI responsiveness
                    WriteLine("Database current. Skipping.");
                    continue;
                }

                Write("Requesting data. ");

                Data.Daily dd = new Data.Daily();
                object output = await API.Alpaca.GetData_Daily(settings, assets[i], settings.Library_LimitDailyEntries);

                if (output is Data.Daily)
                    dd = output as Data.Daily;
                else if (output is string) {
                    if (output as string == "Too Many Requests") {
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
                WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}:  Completing {finishing} remaining background database tasks.");
                await Task.Delay(5000);

                finishing = threads.FindAll(t => t.Status == TaskStatus.Running).Count;
            }

            return ExitCode.Completed;
        }
    }
}