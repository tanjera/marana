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

            Prompt.WriteLine(String.Format("Database size: {0} MB", size));
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
            if (DateTime.UtcNow - db.GetValidity_Assets() > new TimeSpan(1, 0, 0, 0))
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

            // Iterate all symbols in list (assets), call API to download data, write to files in library
            for (int i = 0; i < assets.Count; i++) {
                Prompt.Write(String.Format("{0} [{1:0000} / {2:0000}]  {3,-8}  ",
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i + 1, assets.Count, assets[i].Symbol));

                /* Check validity- if less than 1 day old, data is valid!
                 */
                if (DateTime.UtcNow - db.GetValidity_TSD(assets[i]) < new TimeSpan(1, 0, 0, 0)) {
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
                        Console.WriteLine("Error: {0}", output);
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
                Prompt.WriteLine(String.Format("{0}:  Completing {1} remaining background database tasks.",
                    DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), finishing));
                Thread.Sleep(5000);

                finishing = threads.FindAll(t => t.ThreadState == ThreadState.Running).Count;
            }

            Prompt.WriteLine(String.Format("{0}:  Library update complete!", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")), ConsoleColor.Green);
        }
    }
}