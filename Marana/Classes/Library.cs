using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace Marana {

    public class Library {

        public static void Init(Settings config) {
            if (string.IsNullOrEmpty(config.Directory_Library)) {
                string default_library = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");
                Console.WriteLine(String.Format("Invalid Library Directory set; using default directory ({0})", default_library));
                config.Directory_Library = default_library;
                Configuration.SaveConfig(config);
            }

            if (!Directory.Exists(config.Directory_Library))
                Directory.CreateDirectory(config.Directory_Library);
        }

        public static void Update(string[] args, Settings config) {
            Console.WriteLine("Obtaining Symbol list from Nasdaq Trader... ");
            List<SymbolPair> pairs = new List<SymbolPair>(API_NasdaqTrader.GetSymbolPairs().OrderBy(obj => obj.Symbol).ToArray());
            Console.WriteLine("Completed.");

            if (args.Length > 1) {      // Need to trim the symbol list per input args
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                SymbolPair s = (from pair
                                in pairs
                                where pair.Symbol == args[1].Trim().ToUpper()
                                select pair)
                                    .DefaultIfEmpty(new SymbolPair()).First();

                SymbolPair e = (from pair
                                in pairs
                                where pair.Symbol == (args.Length > 2 ? args[2] : "").Trim().ToUpper()
                                select pair)
                                    .DefaultIfEmpty(new SymbolPair()).First();

                si = pairs.IndexOf(s);
                ei = pairs.IndexOf(e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    pairs.RemoveRange(0, si);
                if (ei > 0)
                    pairs.RemoveRange(ei, pairs.Count - ei);
            }

            for (int i = 0; i < pairs.Count; i++) {
                string output = "";
                output = API_AlphaVantage.GetData_TimeSeriesDaily(config.APIKey_AlphaVantage, pairs[i].Symbol, true);

                if (output == "ERROR:INVALID") {                        // Received invalid data (attempted invalid symbol?)
                    Console.WriteLine(String.Format("{0} [{1:0000} / {2:0000}]:  ERROR, invalid API call for {3}",
                    DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                    i,
                    pairs.Count,
                    pairs[i].Symbol));
                } else if (output == "ERROR:EXCEEDEDCALLS") {           // Exceeded n amount of API calls in x amount of time (per API)
                    Console.WriteLine(String.Format("{0} [{1:0000} / {2:0000}]:  WARNING, exceeded API calls; retrying in 1 minute",
                    DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                    i,
                    pairs.Count,
                    pairs[i].Symbol));

                    i--;
                    Thread.Sleep(60000);
                } else {                                                // Valid data received
                    Console.WriteLine(String.Format("{0} [{1:0000} / {2:0000}]:  Data for {3} added to library",
                        DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                        i,
                        pairs.Count,
                        pairs[i].Symbol));

                    using (StreamWriter sw = new StreamWriter(Path.Combine(config.Directory_Library, String.Format("{0} {1}.json", pairs[i].Symbol, DateTime.Today.ToString("yyyyMMdd"))), false)) {
                        sw.Write(output);
                    }
                }
            }

            Console.WriteLine(String.Format("{0}:  Library update complete!\n", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")));
        }
    }
}