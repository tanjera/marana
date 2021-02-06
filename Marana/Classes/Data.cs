using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Data {

        public static void Select_Assets(ref List<Asset> assets, List<string> args) {
            // Select symbols to update (trim list) based on user input args
            if (args.Count == 0)
                return;

            if (args.Count > 0) {       // Need to trim the symbol list per input args
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                Asset s = null, e = null;

                s = (from pair
                     in assets
                     where pair.Symbol == args[0].Trim().ToUpper()
                     select pair)
                     .DefaultIfEmpty(new Asset()).First();

                if (args.Count > 1)
                    e = (from pair
                         in assets
                         where pair.Symbol == (args.Count > 1 ? args[1] : "").Trim().ToUpper()
                         select pair)
                         .DefaultIfEmpty(new Asset()).First();

                si = assets.IndexOf(s);
                ei = assets.IndexOf(e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    assets.RemoveRange(0, si);
                if (ei > 0)
                    assets.RemoveRange(ei, assets.Count - ei);
            }
        }
    }
}