using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Marana.Visualizer {

    public partial class Main : Window {

        public Main() {
            InitializeComponent();

            lvStocks.ItemsSource = API_NasdaqTrader.GetSymbolPairs ();

            Chart_Init ();
            //Chart_DailyClose ("MSFT");
        }



        public void Chart_Init() {
            Series = new SeriesCollection ();

            XFormatter = val => new DateTime ((long)val).ToString ("MM/dd/yyyy");
            YFormatter = val => val.ToString ("C");

            DataContext = this;
        }

        public SeriesCollection Series { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public Func<double, string> YFormatter { get; set; }


        public void Chart_DailyClose(string symbol) {
            Series.Add (new LineSeries {
                Title = String.Format("{0}", symbol),
                Values = API_LiveCharts.DailyClose_To_Values (API_AlphaVantage.GetData_TimeSeriesDaily (symbol))
            });
        }
    }
}
