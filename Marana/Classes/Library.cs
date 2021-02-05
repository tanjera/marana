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

            Prompt.WriteLine("Updating Time Series Dailies (TSD).");
            Update_TSD(args, settings, database);
        }

        public static void Clear(Database db) {
            Prompt.Write("This will delete all current data in the database! Are you sure you want to continue?  ", ConsoleColor.Red);
            if (!Prompt.YesNo())
                return;

            db.Wipe();

            Prompt.WriteLine("Success- Database cleared of all data and reinitialized.");
        }

        public static List<SymbolPair> GetSymbols(Database db) {
            if (DateTime.UtcNow - db.GetValidity_Symbols() > new TimeSpan(1, 0, 0, 0))
                Update_Symbols(db);

            return db.GetData_Symbols();
        }

        public static void Update_Symbols(Database db) {
            Prompt.Write("Updating list of ticker symbols from Nasdaq Trader... ");

            List<SymbolPair> pairs = new List<SymbolPair>(API.NasdaqTrader.GetSymbolPairs().OrderBy(obj => obj.Symbol).ToArray());
            db.AddData_Symbols(pairs);

            Prompt.Write("Completed", ConsoleColor.Green);
            Prompt.NewLine();
        }

        public static void Update_TSD(List<string> args, Settings settings, Database db) {
            Prompt.WriteLine("Querying database for list of ticker symbols.");

            List<SymbolPair> pairs = GetSymbols(db);

            Data.Select_Symbols(ref pairs, args);

            List<Thread> threads = new List<Thread>();

            // Iterate all symbols in list (pairs), call API to download data, write to files in library
            for (int i = 0; i < pairs.Count; i++) {
                Prompt.Write(String.Format("{0} [{1:0000} / {2:0000}]  {3,-8}  ",
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i + 1, pairs.Count, pairs[i].Symbol));

                /* Check validity- if less than 1 day old, data is valid!
                 */
                if (DateTime.UtcNow - db.GetValidity_TSD(pairs[i]) < new TimeSpan(1, 0, 0, 0)) {
                    Prompt.WriteLine("Database current. Skipping.");
                    continue;
                }

                Prompt.Write("Requesting data. ");

                DatasetTSD ds = new DatasetTSD();
                object output = API.Alpaca.GetData_TSD(settings, pairs[i]);

                if (output is DatasetTSD)
                    ds = output as DatasetTSD;
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

                ds.Symbol = pairs[i].Symbol;
                ds.CompanyName = pairs[i].Name;

                /* Calculate metrics
                 */

                Prompt.Write("Calculating metrics. ");

                Statistics.CalculateSMA(ref ds.Values, 7);
                Statistics.CalculateSMA(ref ds.Values, 20);
                Statistics.CalculateSMA(ref ds.Values, 50);
                Statistics.CalculateSMA(ref ds.Values, 100);
                Statistics.CalculateSMA(ref ds.Values, 200);
                Statistics.CalculateMSD20(ref ds.Values);

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