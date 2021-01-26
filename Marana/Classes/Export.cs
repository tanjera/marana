using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

namespace Marana {

    public class Export {

        public static void TSDA_To_CSV(DatasetTSDA dataset, string filepath) {
            using (StreamWriter sw = new StreamWriter(filepath)) {
                using (CsvWriter csv = new CsvWriter(sw, CultureInfo.InvariantCulture)) {
                    csv.Context.RegisterClassMap<DailyValueMap>();

                    csv.WriteRecords(dataset.Values);
                }
            }
        }

        public class DailyValueMap : ClassMap<DailyValue> {

            public DailyValueMap() {
                Map(s => s.Timestamp).Index(0).Name("timestamp");
                Map(s => s.Open).Index(1).Name("open");
                Map(s => s.High).Index(2).Name("high");
                Map(s => s.Low).Index(3).Name("low");
                Map(s => s.Close).Index(4).Name("close");
                Map(s => s.AdjustedClose).Index(5).Name("adjusted_close");
                Map(s => s.Volume).Index(6).Name("volume");
                Map(s => s.Dividend_Amount).Index(7).Name("dividend_amount");
                Map(s => s.Split_Coefficient).Index(8).Name("split_coefficient");
                Map(s => s.SMA7).Index(9).Name("sma7");
                Map(s => s.SMA20).Index(10).Name("sma20");
                Map(s => s.SMA50).Index(11).Name("sma50");
                Map(s => s.SMA100).Index(12).Name("sma100");
                Map(s => s.SMA200).Index(13).Name("sma200");
                Map(s => s.MSD20).Index(14).Name("msd20");
                Map(s => s.MSDr20).Index(15).Name("msdr20");
            }
        }
    }
}