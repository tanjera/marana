using System;
using System.Collections.Generic;

namespace Marana {

    public class DatasetTSDA {
        public List<DailyValue> Values = new List<DailyValue>();
    }

    public class DailyValue {
        // Data received from outside API

        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal AdjustedClose { get; set; }
        public decimal Volume { get; set; }
        public decimal Dividend_Amount { get; set; }
        public decimal Split_Coefficient { get; set; }

        // Simple moving averages, various periods

        public decimal SMA7 { get; set; }
        public decimal SMA20 { get; set; }
        public decimal SMA50 { get; set; }
        public decimal SMA100 { get; set; }
        public decimal SMA200 { get; set; }

        // Moving standard deviation and according ratio

        public decimal MSD20 { get; set; }
        public decimal MSDr20 { get; set; }
    }

    public class SymbolPair {
        public string Symbol { get; set; }

        public string Name { get; set; }
    }
}