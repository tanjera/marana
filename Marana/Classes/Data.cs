using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Data {

        public static void Select_Symbols(ref List<FileInfo> files, List<string> args) {
            // Select symbol files to update (trim list) based on user input args
            if (args.Count == 0)
                return;

            if (args.Count > 0) {
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                string s = "", e = "";

                s = (from file
                     in files
                     where file.Name.StartsWith(args[0].Trim().ToUpper())
                     select file.FullName)
                     .DefaultIfEmpty("").First();

                if (args.Count > 1)
                    e = (from file
                         in files
                         where file.Name.StartsWith(args[1].Trim().ToUpper())
                         select file.FullName)
                         .DefaultIfEmpty("").First();

                si = files.FindIndex(o => o.FullName == s);
                ei = files.FindIndex(o => o.FullName == e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    files.RemoveRange(0, si);
                if (ei > 0)
                    files.RemoveRange(ei, files.Count - ei);
            }
        }

        public static void Select_Symbols(ref List<SymbolPair> pairs, List<string> args) {
            // Select symbols to update (trim list) based on user input args
            if (args.Count == 0)
                return;

            if (args.Count > 0) {       // Need to trim the symbol list per input args
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                SymbolPair s = null, e = null;

                s = (from pair
                     in pairs
                     where pair.Symbol == args[0].Trim().ToUpper()
                     select pair)
                     .DefaultIfEmpty(new SymbolPair()).First();

                if (args.Count > 1)
                    e = (from pair
                         in pairs
                         where pair.Symbol == (args.Count > 1 ? args[1] : "").Trim().ToUpper()
                         select pair)
                         .DefaultIfEmpty(new SymbolPair()).First();

                si = pairs.IndexOf(s);
                ei = pairs.IndexOf(e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    pairs.RemoveRange(0, si);
                if (ei > 0)
                    pairs.RemoveRange(ei, pairs.Count - ei);
            }
        }
    }
}