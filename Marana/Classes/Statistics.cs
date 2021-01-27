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

            // Start at oldest value to take a running sum, then save calculations once reaching the period
            for (int i = 0; i < values.Count; i++) {
                runningsum += values[i].AdjustedClose;

                if (i > period - 2) {
                    switch (period) {
                        default: break;
                        case 7: values[i].SMA7 = runningsum / period; break;
                        case 20: values[i].SMA20 = runningsum / period; break;
                        case 50: values[i].SMA50 = runningsum / period; break;
                        case 100: values[i].SMA100 = runningsum / period; break;
                        case 200: values[i].SMA200 = runningsum / period; break;
                    }

                    // Remove the oldest value for the next iteration
                    runningsum -= values[i - period + 1].AdjustedClose;
                }
            }
        }

        // Calculates the moving standard deviation across a set of values
        public static void CalculateMSD20(ref List<DailyValue> values) {
            int period = 20;

            // No running sum used; just iterate beginning at period
            for (int i = period - 1; i < values.Count; i++) {
                decimal pSum = 0;
                decimal vSum = 0;

                for (int j = 0; j < period; j++) {
                    pSum += values[i - j].AdjustedClose;
                    vSum += values[i - j].Volume;
                }

                values[i].SMA20 = pSum / period;
                values[i].vSMA20 = vSum / period;

                double pSumSquaredVariance = 0;
                double vSumSquaredVariance = 0;

                for (int j = 0; j < period; j++) {
                    pSumSquaredVariance += Math.Pow((double)(values[i - j].AdjustedClose - values[i].SMA20), 2);
                    vSumSquaredVariance += Math.Pow((double)(values[i - j].Volume - values[i].vSMA20), 2);
                }

                values[i].MSD20 = (decimal)Math.Sqrt(pSumSquaredVariance / period);
                values[i].MSDr20 = values[i].MSD20 / values[i].SMA20;

                values[i].vMSD20 = (decimal)Math.Sqrt(vSumSquaredVariance / period);
            }
        }
    }
}