using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Statistics {

        // Calculates the simple moving average across a set of values
        public static void CalculateSMA(ref List<DailyValue> values, int period) {
            decimal runningsum = 0;

            for (int i = values.Count - 1; i >= 0; i--) {
                runningsum += values[i].AdjustedClose;

                if (i <= values.Count - period) {
                    switch (period) {
                        default: break;
                        case 7: values[i].SMA7 = runningsum / period; break;
                        case 20: values[i].SMA20 = runningsum / period; break;
                        case 50: values[i].SMA50 = runningsum / period; break;
                        case 100: values[i].SMA100 = runningsum / period; break;
                        case 200: values[i].SMA200 = runningsum / period; break;
                    }

                    // Remove the oldest (i + (period - 1)) value for the next iteration
                    runningsum -= values[i + (period - 1)].AdjustedClose;
                }
            }
        }

        // Calculates the moving standard deviation across a set of values
        public static void CalculateMSD20(ref List<DailyValue> values) {
            int period = 20;

            for (int i = values.Count - period; i >= 0; i--) {
                decimal sum = 0;

                for (int j = i + period - 1; j >= i; j--) {
                    sum += values[j].AdjustedClose;
                }

                decimal meanOfSum = sum / period;
                double sumSquaredVariance = 0;

                for (int j = i + period - 1; j >= i; j--) {
                    sumSquaredVariance += Math.Pow((double)(values[j].AdjustedClose - meanOfSum), 2);
                }

                values[i].MSD20 = (decimal)Math.Sqrt(sumSquaredVariance / period);
                values[i].MSDr20 = values[i].MSD20 / meanOfSum;
            }
        }
    }
}