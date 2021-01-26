using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace Marana {

    public class Library {

        public static void Init(Settings settings) {
            if (string.IsNullOrEmpty(settings.Directory_Library)) {
                string default_library = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");
                Prompt.WriteLine(String.Format("Invalid Library Directory set; using default directory ({0})", default_library), ConsoleColor.Yellow);
                settings.Directory_Library = default_library;
                Config.SaveConfig(settings);
            }

            if (!Directory.Exists(settings.Directory_Library))
                Directory.CreateDirectory(settings.Directory_Library);
            if (!Directory.Exists(settings.Directory_LibraryData))
                Directory.CreateDirectory(settings.Directory_LibraryData);
            if (!Directory.Exists(settings.Directory_LibraryData_TSDA))
                Directory.CreateDirectory(settings.Directory_LibraryData_TSDA);
        }

        public static void Info(Settings settings) {
            Init(settings);

            Prompt.WriteLine(String.Format("Library Data located at: {0}", settings.Directory_LibraryData));

            DirectoryInfo ld_tsda = new DirectoryInfo(settings.Directory_LibraryData_TSDA);
            FileInfo[] ld_tsda_gf = ld_tsda.GetFiles();

            long size = 0;
            foreach (FileInfo file in ld_tsda_gf)
                size += file.Length;

            Prompt.WriteLine(String.Format("Time Series data files: {0} files @ {1:0.00} Mb", ld_tsda_gf.Length, (size / 1048576f)));
            Prompt.WriteLine(String.Format("Last written to: {0}", ld_tsda.LastWriteTime.ToString("MM/dd/yyyy HH:mm")));
        }

        public static void Clear(Settings settings) {
            Library.Init(settings);

            Prompt.Write("This will delete all library data! Are you sure you want to continue?  ", ConsoleColor.Red);
            if (!Prompt.YesNo())
                return;

            // Delete all files in LibraryData
            DirectoryInfo ld = new DirectoryInfo(settings.Directory_LibraryData);

            FileInfo[] ld_gf = ld.GetFiles();
            Prompt.Write(String.Format("Deleting {0} files from {1} ... ", ld_gf.Length, settings.Directory_LibraryData));

            foreach (FileInfo file in ld_gf)
                file.Delete();

            Prompt.Write("Complete", ConsoleColor.Green);
            Prompt.NewLine();

            // Delete all files in LibraryData_TSDA
            DirectoryInfo ld_tsda = new DirectoryInfo(settings.Directory_LibraryData_TSDA);

            FileInfo[] ld_tsda_gf = ld_tsda.GetFiles();
            Prompt.Write(String.Format("Deleting {0} files from {1} ... ", ld_tsda_gf.Length, settings.Directory_LibraryData_TSDA));

            foreach (FileInfo file in ld_tsda_gf)
                file.Delete();

            Prompt.Write("Complete", ConsoleColor.Green);
            Prompt.NewLine();
        }

        public static void Update(List<string> args, Settings settings) {
            Init(settings);

            Update_TSDA(args, settings);
        }

        public static void Update_TSDA(List<string> args, Settings settings) {
            Prompt.Write("Obtaining Symbol list from Nasdaq Trader... ");
            List<SymbolPair> pairs = new List<SymbolPair>(API_NasdaqTrader.GetSymbolPairs().OrderBy(obj => obj.Symbol).ToArray());
            Prompt.Write("Completed", ConsoleColor.Green);
            Prompt.NewLine();

            Data.Select_Symbols(ref pairs, args);

            // Iterate all symbols in list (pairs), call API to download data, write to files in library
            for (int i = 0; i < pairs.Count; i++) {
                string output = "";
                output = API.AlphaVantage.GetData_TSDA(settings.APIKey_AlphaVantage, pairs[i].Symbol);

                if (output == "ERROR:INVALID") {                        // Received invalid data (attempted invalid symbol?)
                    Prompt.WriteLine(String.Format("{0} [{1:0000} / {2:0000}]:  Error, invalid API call for {3}",
                    DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i, pairs.Count, pairs[i].Symbol),
                    ConsoleColor.Red);
                } else if (output == "ERROR:EXCEEDEDCALLS") {           // Exceeded n amount of API calls in x amount of time (per API)
                    Prompt.WriteLine(String.Format("{0} [{1:0000} / {2:0000}]:  ... exceeded API calls; retrying in 1 minute",
                    DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i, pairs.Count, pairs[i].Symbol),
                    ConsoleColor.Yellow);

                    i--;
                    Thread.Sleep(60000);
                } else {                                                // Valid data received
                    using (StreamWriter sw = new StreamWriter(
                        Path.Combine(settings.Directory_LibraryData_TSDA, String.Format("{0}.csv", pairs[i].Symbol)), false)) {
                        sw.Write(output);

                        Prompt.WriteLine(String.Format("{0} [{1:0000} / {2:0000}]:  Data for {3} added to library",
                            DateTime.Now.ToString("MM/dd/yyyy HH:mm"), i, pairs.Count, pairs[i].Symbol));
                    }
                }
            }

            Prompt.WriteLine(String.Format("{0}:  Library update complete!\n", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")), ConsoleColor.Green);
        }
    }
}