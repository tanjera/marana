using System;

namespace Marana {

    public class DailyValue {
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
    }

    public class SymbolPair {
        public string Symbol { get; set; }
        public string Name { get; set; }
    }
}