using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace Marana {

    public class Library {

        public static void Info(Database database) {
            decimal size = database.GetSize();

            Prompt.WriteLine(String.Format("Database size: {0} MB", size));
            Prompt.NewLine();
        }

        public static void Update(List<string> args, Settings settings, Database database) {
            database.Init();

            Update_TSDA(args, settings, database);
        }

        public static void Clear(Database database) {
            Prompt.Write("This will delete all current data in the database! Are you sure you want to continue?  ", ConsoleColor.Red);
            if (!Prompt.YesNo())
                return;

            database.Wipe();

            Prompt.Write("Database cleared of all data and reinitialized.");
        }

        public static void Update_TSDA(List<string> args, Settings settings, Database database) {
            Prompt.Write("Obtaining Symbol list from Nasdaq Trader... ");
            List<SymbolPair> pairs = new List<SymbolPair>(API.NasdaqTrader.GetSymbolPairs().OrderBy(obj => obj.Symbol).ToArray());
            Prompt.Write("Completed", ConsoleColor.Green);
            Prompt.NewLine();

            Data.Select_Symbols(ref pairs, args);

            // Iterate all symbols in list (pairs), call API to download data, write to files in library
            for (int i = 0; i < pairs.Count; i++) {
                string output = "";

                Prompt.Write(String.Format("{0} [{1:0000} / {2:0000}]:  {3}  :  Requesting data. ",
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i, pairs.Count, pairs[i].Symbol));

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

                    database.Add_TSDA(ds);

                    Prompt.WriteLine("Complete!", ConsoleColor.Green);
                }
            }

            Prompt.WriteLine(String.Format("{0}:  Library update complete!\n", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")), ConsoleColor.Green);
        }
    }
}