using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Calculations {

        // Calculates all the indicator metrics for a dataset
        public static void Metrics(ref Data.Daily dd) {
            // Prepare the data structures and tie Prices <-> Metrics
            dd.Prices.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            foreach (Data.Daily.Price p in dd.Prices) {
                Data.Daily.Metric m = new Data.Daily.Metric() { Price = p };
                p.Metric = m;
                dd.Metrics.Add(m);
            }

            Calculations.SMA(ref dd.Prices, 7);
            Calculations.SMA(ref dd.Prices, 20);
            Calculations.SMA(ref dd.Prices, 50);
            Calculations.SMA(ref dd.Prices, 100);
            Calculations.SMA(ref dd.Prices, 200);

            Calculations.MGR(ref dd.Prices, 50);
            Calculations.MGR(ref dd.Prices, 100);
            Calculations.MGR(ref dd.Prices, 200);

            Calculations.MSD20(ref dd.Prices);

            Calculations.RSI(ref dd.Prices);
        }

        // Calculates the simple moving average across a set of values
        public static void SMA(ref List<Data.Daily.Price> prices, int period) {
            decimal runningsum = 0;

            // Start at oldest value to take a running sum, then save calculations once reaching the period
            for (int i = 0; i < prices.Count; i++) {
                runningsum += prices[i].Close;

                if (i > period - 2) {
                    switch (period) {
                        default: break;
                        case 7: prices[i].Metric.SMA7 = runningsum / period; break;
                        case 20: prices[i].Metric.SMA20 = runningsum / period; break;
                        case 50: prices[i].Metric.SMA50 = runningsum / period; break;
                        case 100: prices[i].Metric.SMA100 = runningsum / period; break;
                        case 200: prices[i].Metric.SMA200 = runningsum / period; break;
                    }

                    // Remove the oldest value for the next iteration
                    runningsum -= prices[i - period + 1].Close;
                }
            }
        }

        // Calculates the moving growth rate (derived from simple moving average!) across a set of values
        public static void MGR(ref List<Data.Daily.Price> prices, int period) {
            for (int i = period; i < prices.Count; i++) {
                if (!prices[i].Metric.HasSMA7 || !prices[i - period].Metric.HasSMA7)
                    continue;

                switch (period) {
                    default: break;
                    case 50: prices[i].Metric.MGR50 = (prices[i].Metric.SMA7 - prices[i - period].Metric.SMA7) / prices[i - period].Metric.SMA7; break;
                    case 100: prices[i].Metric.MGR100 = (prices[i].Metric.SMA7 - prices[i - period].Metric.SMA7) / prices[i - period].Metric.SMA7; break;
                    case 200: prices[i].Metric.MGR200 = (prices[i].Metric.SMA7 - prices[i - period].Metric.SMA7) / prices[i - period].Metric.SMA7; break;
                }
            }
        }

        // Calculates the moving standard deviation across a set of values
        public static void MSD20(ref List<Data.Daily.Price> prices, int period = 20) {
            decimal pSum, vSum;
            double pSumSquaredVariance, vSumSquaredVariance;

            // No running sum used; just iterate beginning at period
            for (int i = period - 1; i < prices.Count; i++) {
                pSum = 0;
                vSum = 0;

                for (int j = 0; j < period; j++) {
                    pSum += prices[i - j].Close;
                    vSum += prices[i - j].Volume;
                }

                prices[i].Metric.SMA20 = pSum / period;
                prices[i].Metric.vSMA20 = vSum / period;

                pSumSquaredVariance = 0;
                vSumSquaredVariance = 0;

                for (int j = 0; j < period; j++) {
                    pSumSquaredVariance += Math.Pow((double)(prices[i - j].Close - prices[i].Metric.SMA20), 2);
                    vSumSquaredVariance += Math.Pow((double)(prices[i - j].Volume - prices[i].Metric.vSMA20), 2);
                }

                prices[i].Metric.MSD20 = (decimal)Math.Sqrt(pSumSquaredVariance / period);
                prices[i].Metric.MSDr20 = prices[i].Metric.MSD20 / prices[i].Metric.SMA20;

                prices[i].Metric.vMSD20 = (decimal)Math.Sqrt(vSumSquaredVariance / period);
            }
        }

        // Calculates the Relative Strength Indicator (RSI) using Wilder's smoothing.
        public static void RSI(ref List<Data.Daily.Price> prices, int period = 14) {
            decimal diff = 0;
            decimal avgGain = 0;
            decimal avgLoss = 0;
            decimal rs = 0;

            for (int i = period; i < prices.Count; i++) {
                if (i == period) {
                    decimal totalGain = 0;
                    decimal totalLoss = 0;
                    for (int j = 0; j < period; j++) {
                        diff = prices[i - j].Close - prices[i - j - 1].Close;
                        if (diff > 0)
                            totalGain += diff;
                        else if (diff < 0)
                            totalLoss += Math.Abs(diff);
                    }

                    avgGain = totalGain / period;
                    avgLoss = totalLoss / period;
                } else if (i > period) {
                    diff = prices[i].Close - prices[i - 1].Close;
                    // Using Wilder's smoothing
                    avgGain = ((avgGain * 13) + (diff > 0 ? diff : 0)) / period;
                    avgLoss = ((avgLoss * 13) + (diff < 0 ? Math.Abs(diff) : 0)) / period;
                }

                rs = avgGain / (avgLoss > 0 ? avgLoss : 0.0001M);
                prices[i].Metric.RSI = 100 - (100 / (1 + rs));
            }
        }

        public static Data.Signal.Directions HasCrossover(decimal new1, decimal new2, decimal old1, decimal old2) {
            /* All the metrics (prices, etc) should be positive numbers
             * Zero indicates data unavailable or inapplicable
             */

            if (new1 == 0 || new2 == 0 || old1 == 0 || old2 == 0)
                return Data.Signal.Directions.Same;

            decimal diff1 = new1 - new2;
            decimal diff2 = old1 - old2;

            if (diff1 == 0 && diff2 == 0)
                return Data.Signal.Directions.Same;
            else if (diff1 > 0 && diff2 > 0)
                return Data.Signal.Directions.Same;
            else if (diff1 < 0 && diff2 < 0)
                return Data.Signal.Directions.Same;
            else if (diff1 > 0 && diff2 <= 0)
                return Data.Signal.Directions.Up;
            else if (diff1 <= 0 && diff2 > 0)
                return Data.Signal.Directions.Down;
            else
                return Data.Signal.Directions.Same;
        }

        public static Data.Signal.Directions HasReversal(decimal num1, decimal num2, decimal num3) {
            /* All the metrics (prices, etc) should be positive numbers
             * Zero indicates data unavailable or inapplicable
             */

            if (num1 == 0 || num2 == 0 || num3 == 0)
                return Data.Signal.Directions.Same;

            decimal diff1 = num1 - num2;
            decimal diff2 = num2 - num3;

            /* Known issue: Will not detect reversal if ((diff1 || diff2) == 0)
             * ... it's a minor edge case, marginal effect on outcomes
             */

            if (diff1 == 0 && diff2 == 0)
                return Data.Signal.Directions.Same;
            else if (diff1 > 0 && diff2 > 0)
                return Data.Signal.Directions.Same;
            else if (diff1 < 0 && diff2 < 0)
                return Data.Signal.Directions.Same;
            else if (diff1 != 0 && diff2 == 0)
                return Data.Signal.Directions.Plateau;
            else if (diff1 > 0 && diff2 < 0)
                return Data.Signal.Directions.Trough;
            else if (diff1 < 0 && diff2 > 0)
                return Data.Signal.Directions.Peak;
            else
                return Data.Signal.Directions.Same;
        }
    }
}