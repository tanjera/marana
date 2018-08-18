using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.WindowsAPICodePack.Dialogs;

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;


namespace Marana {

    public partial class TabCharts : UserControl {

        Main wdwMain;

        private SeriesCollection Series { get; set; }
        private Func<double, string> XFormatter { get; set; }
        private Func<double, string> YFormatter { get; set; }


        public TabCharts (Main m) {
            InitializeComponent ();

            wdwMain = m;

            Series = new SeriesCollection ();
            XFormatter = val => new DateTime ((long)val).ToString ("MM/dd/yyyy");
            YFormatter = val => val.ToString ("C");
            DataContext = this;

            //lvStocks.ItemsSource = API_NasdaqTrader.GetSymbolPairs ();
        }


        private void Chart_DailyClose (string symbol) {
            Series.Add (new LineSeries {
                Title = String.Format ("{0}", symbol),
                Values = API_LiveCharts.DailyClose_To_Values (
                    API_AlphaVantage.ProcessData_TimeSeriesDaily (API_AlphaVantage.GetData_TimeSeriesDaily (wdwMain.Config.APIKey_AlphaVantage, symbol)))
            });
        }
    }
}
