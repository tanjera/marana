using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public static class Strategy {

        public static Dictionary<string, Func<Data.Daily, DateTime, bool>> Listing = new Dictionary<string, Func<Data.Daily, DateTime, bool>>() {
            { "Flips: Entry", new Func<Data.Daily, DateTime, bool>(Strategies.Flips.Entry) }
        };

        public static async Task<List<Data.Asset>> Run(Database db, string strategy, DateTime target) {
            List<Data.Asset> output = new List<Data.Asset>();

            List<Data.Asset> assets = await db.GetAssets();

            Func<Data.Daily, DateTime, bool> funcStrategy;
            if (!Listing.TryGetValue(strategy, out funcStrategy))
                return new List<Data.Asset>();

            for (int i = 0; i < assets.Count; i++) {
                Data.Daily dd = await db.GetData_Daily(assets[i]);

                if (dd == null || dd.Prices.Count == 0)
                    continue;

                if (funcStrategy(dd, target))
                    output.Add(assets[i]);
            }

            return output;
        }
    }
}