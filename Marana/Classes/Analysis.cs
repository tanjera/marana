using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Analysis {

        public static void Year(List<string> args, Settings settings) {
            // Process each .csv file to a data structure
            DirectoryInfo ddir = new DirectoryInfo(settings.Directory_LibraryData_TSDA);
            List<FileInfo> dfiles = new List<FileInfo>(ddir.GetFiles("*.csv"));

            List<SymbolPair> pairs = API_NasdaqTrader.GetSymbolPairs();

            Data.Select_Symbols(ref dfiles, args);

            for (int i = 0; i < dfiles.Count; i++) {
                string symbol = dfiles[i].Name.Substring(0, dfiles[i].Name.IndexOf(".csv")).Trim();
                string name = (from pair in pairs where pair.Symbol == symbol select pair.Name).First();

                DatasetTSDA ds = API.AlphaVantage.ParseData_TSDA(dfiles[i].FullName, 300);

                Prompt.Write(String.Format("Calculating statistics for {0}: ", symbol));

                Prompt.Write("SMA7");
                Statistics.CalculateSMA(ref ds.Values, 7);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA20");
                Statistics.CalculateSMA(ref ds.Values, 20);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA50");
                Statistics.CalculateSMA(ref ds.Values, 50);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA100");
                Statistics.CalculateSMA(ref ds.Values, 100);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("SMA200");
                Statistics.CalculateSMA(ref ds.Values, 200);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.Write("MDS20");
                Statistics.CalculateMSD20(ref ds.Values);
                Prompt.Write("! ", ConsoleColor.Green);

                Prompt.NewLine();

                string exportpath = Path.Combine(settings.Directory_Library, String.Format("{0}.csv", symbol));
                Export.TSDA_To_CSV(ds, exportpath);
            }
        }
    }
}