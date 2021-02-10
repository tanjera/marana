using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Marana {

    public class Library {

        public static void Info(Database db) {
            decimal size = db.GetSize();

            Prompt.WriteLine($"Database size: {size} MB");
        }

        public static void Update(List<string> args, Settings settings, Database database) {
            Prompt.WriteLine("Initializing database.");
            database.Init();

            Prompt.WriteLine("Querying database for list of ticker symbols.");
            List<Data.Asset> assets = GetAssets(database);
            Data.Select_Assets(ref assets, args);

            Prompt.WriteLine("Updating Time Series Dailies (TSD).");
            Update_TSD(assets, settings, database);
        }

        public static void Clear(Database db) {
            Prompt.Write("This will delete all current data in the database! Are you sure you want to continue?  ", ConsoleColor.Red);
            if (!Prompt.YesNo())
                return;

            db.Wipe();

            Prompt.WriteLine("Success- Database cleared of all data and reinitialized.");
        }

        public static List<Data.Asset> GetAssets(Database db) {
            // Update ticker symbols weekly
            if (DateTime.UtcNow - db.GetValidity_Assets() > new TimeSpan(7, 0, 0, 0))
                Update_Symbols(db);

            return db.GetData_Assets();
        }

        public static void Update_Symbols(Database db) {
            Prompt.Write("Updating list of ticker symbols. ");

            object output = API.Alpaca.GetAssets(db._Settings);
            if (output is List<Data.Asset>) {
                db.AddData_Assets((List<Data.Asset>)output);
                Prompt.WriteLine("Completed", ConsoleColor.Green);
            } else {
                Prompt.WriteLine("Error", ConsoleColor.Red);
            }
        }

        public static void Update_TSD(List<Data.Asset> assets, Settings settings, Database db) {
            List<Thread> threads = new List<Thread>();

            DateTime lastMarketClose = new DateTime();
            object result = API.Alpaca.GetTime_LastMarketClose(settings);
            if (result is DateTime) {
                lastMarketClose = (DateTime)result;
            } else {
                lastMarketClose = DateTime.UtcNow - new TimeSpan(1, 0, 0, 0);
            }

            // Iterate all symbols in list (assets), call API to download data, write to files in library
            for (int i = 0; i < assets.Count; i++) {
                Prompt.Write($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm")} [{i + 1:0000} / {assets.Count:0000}]  {assets[i].Symbol,-8}  ");

                /* Check validity timestamp against last known market close
                 */

                if (db.GetValidity_TSD(assets[i]).CompareTo(lastMarketClose) > 0) {
                    Prompt.WriteLine("Database current. Skipping.");
                    continue;
                }

                Prompt.Write("Requesting data. ");

                Data.Daily ds = new Data.Daily();
                object output = API.Alpaca.GetData_TSD(settings, assets[i]);

                if (output is Data.Daily)
                    ds = output as Data.Daily;
                else if (output is string) {
                    if (output as string == "Too Many Requests") {
                        Prompt.WriteLine("Exceeded API calls per minute- pausing for 30 seconds.");
                        Thread.Sleep(30000);
                        i--;
                        continue;
                    } else {
                        Prompt.WriteLine($"Error: {output}");
                        continue;
                    }
                }

                ds.Asset = assets[i];

                /* Save to database
                 * Use threading for highly improved speed!
                 */

                Prompt.WriteLine("Updating database.", ConsoleColor.Green);

                Thread thread = new Thread(new ParameterizedThreadStart(db.AddData_TSD));
                thread.Start(ds);
                threads.Add(thread);

                int awaiting = threads.FindAll(t => t.ThreadState == ThreadState.Running).Count;
                if (awaiting >= 10)
                    Thread.Sleep(1000);
            }

            int finishing = threads.FindAll(t => t.ThreadState == ThreadState.Running).Count;
            if (finishing > 0) {
                Prompt.WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}:  Completing {finishing} remaining background database tasks.");
                Thread.Sleep(5000);

                finishing = threads.FindAll(t => t.ThreadState == ThreadState.Running).Count;
                Prompt.WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}:  Library update complete!", ConsoleColor.Green);
            }
        }
    }
}