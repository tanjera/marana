using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana.Strategies {

    public class Flips {

        public static bool Entry(Data.Daily dd, DateTime target) {
            int indexToday = dd.Prices.FindIndex(p => { return p.Date.Date == target.Date; });
            int indexYesterday = indexToday - 1;

            if (indexToday < 0 || indexYesterday < 0)
                return false;

            Data.Daily.Metric today = dd.Prices[indexToday].Metric;
            Data.Daily.Metric yesterday = dd.Prices[indexYesterday].Metric;

            if (today?.RSI == null
                || today?.SMA7 == null
                || today?.SMA20 == null
                || today?.MACD?.Macd == null || today?.MACD?.Signal == null
                || yesterday?.MACD?.Macd == null || yesterday?.MACD?.Signal == null)
                return false;

            return today.Price.Close > 10
                && today.Price.Close > (today.SMA7 * 1.005m)
                && today.RSI < 40
                && today.MACD.Macd > today.MACD.Signal
                && yesterday.MACD.Macd < yesterday.MACD.Signal;
        }
    }
}