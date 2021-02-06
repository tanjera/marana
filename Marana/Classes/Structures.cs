using System;
using System.Collections.Generic;

namespace Marana {

    public class DatasetTSD {
        public Asset Asset = new Asset();
        public List<TSDValue> TSDValues = new List<TSDValue>();
        public List<Signal> Signals = new List<Signal>();
    }

    public class TSDValue {
        // Data received from outside API

        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

        // Simple moving averages, various periods, for price

        public decimal SMA7 { get; set; }
        public decimal SMA20 { get; set; }
        public decimal SMA50 { get; set; }
        public decimal SMA100 { get; set; }
        public decimal SMA200 { get; set; }

        // Moving standard deviation and according ratio (percentage) for price

        public decimal MSD20 { get; set; }
        public decimal MSDr20 { get; set; }

        // Metrics for analyzing trading volume

        public decimal vSMA20 { get; set; }
        public decimal vMSD20 { get; set; }
    }

    public class Signal {
        public DateTime Timestamp;
        public Types Type;
        public Directions Direction;
        public Metrics Metric;

        // General conditions around the signal event

        public int HasAlignment = 0;
        public bool HasResistance = false;

        public enum Types {
            Crossover,
            Reversal,
            Variation
        }

        public enum Directions {
            Same,
            Up,
            Down,
            Peak,
            Trough,
            Plateau,
            Increase,
            Decrease
        }

        public enum Metrics {
            SMA7,
            SMA20,
            SMA50,
            SMA100,
            SMA200,
            SMA7_20,
            SMA20_50,
            SMA50_100,
            SMA100_200,
        }

        public Signal(DateTime timestamp, Types type, Directions direction, Metrics metric) {
            Timestamp = timestamp;
            Type = type;
            Direction = direction;
            Metric = metric;
        }
    }

    public class Asset {
        public string ID { get; set; }
        public string Symbol { get; set; }
        public string Class { get; set; }
        public string Exchange { get; set; }
        public string Status { get; set; }
        public bool Tradeable { get; set; }
        public bool Marginable { get; set; }
        public bool Shortable { get; set; }
        public bool EasyToBorrow { get; set; }
    }
}