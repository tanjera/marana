using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Marana {
    class API_LiveCharts {

        public static ChartValues<DateTimePoint> DailyClose_To_Values (List<DailyValue> dailyvalues) {
            ChartValues<DateTimePoint> cv = new ChartValues<DateTimePoint> ();

            for (int i = dailyvalues.Count - 1; i >= 0; i--)
                cv.Add (new DateTimePoint(dailyvalues[i].Timestamp, dailyvalues[i].Close));

            return cv;
        }
    }
}
