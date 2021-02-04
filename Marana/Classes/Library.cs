using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace Marana {

    public class Library {

        public static void Info(Database db) {
            decimal size = db.GetSize();

            Prompt.WriteLine(String.Format("Database size: {0} MB", size));
            Prompt.NewLine();
        }

        public static void Update(List<string> args, Settings settings, Database database) {
            database.Init();

            Update_TSDA(args, settings, database);
        }

        public static void Clear(Database db) {
            Prompt.Write("This will delete all current data in the database! Are you sure you want to continue?  ", ConsoleColor.Red);
            if (!Prompt.YesNo())
                return;

            db.Wipe();

            Prompt.WriteLine("Database cleared of all data and reinitialized.");
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

        public static void Update_TSDA(List<string> args, Settings settings, Database db) {
            List<SymbolPair> pairs = GetSymbols(db);

            Data.Select_Symbols(ref pairs, args);

            // Iterate all symbols in list (pairs), call API to download data, write to files in library
            for (int i = 0; i < pairs.Count; i++) {
                Prompt.Write(String.Format("{0} [{1:0000} / {2:0000}]:  {3}  :  ",
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i, pairs.Count, pairs[i].Symbol));

                /* Check validity- if less than 1 day old, data is valid!
                 */
                if (DateTime.UtcNow - db.GetValidity_TSDA(pairs[i]) < new TimeSpan(1, 0, 0, 0)) {
                    Prompt.WriteLine("Database current. Skipping.");
                    continue;
                }

                Prompt.Write("Requesting data.  ");
                string output = "";
                output = API.AlphaVantage.GetData_TSDA(settings.APIKey_AlphaVantage, pairs[i].Symbol);

                if (output == "ERROR:INVALID") {                        // Received invalid data (attempted invalid symbol?)
                    Prompt.WriteLine("Error: invalid API call.", ConsoleColor.Red);
                } else if (output == "ERROR:INVALIDKEY") {
                    Prompt.NewLine();
                    Prompt.WriteLine("Error: invalid API key- please enter a valid API key into config.", ConsoleColor.Red);
                    Prompt.WriteLine("Cancelling update.", ConsoleColor.Red);
                    return;
                } else if (output == "ERROR:EXCEEDEDCALLS") {           // Exceeded n amount of API calls in x amount of time (per API)
                    Prompt.WriteLine("Exceeded API calls; retrying in 1 minute", ConsoleColor.Yellow);
                    i--;
                    Thread.Sleep(60000);
                } else {                                                // Valid data received
                    Prompt.Write("Parsing. ");

                    DatasetTSDA ds = API.AlphaVantage.ParseData_TSDA(output);

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
                     */

                    Prompt.Write("Updating database. ");

                    db.AddData_TSDA(ds);

                    Prompt.WriteLine("Complete!", ConsoleColor.Green);
                }
            }

            Prompt.WriteLine(String.Format("{0}:  Library update complete!\n", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")), ConsoleColor.Green);
        }
    }
}