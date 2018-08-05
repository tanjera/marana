using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Marana {

    public partial class Main : Window {

        private Configuration Config = new Configuration ();

        public Main () {
            InitializeComponent ();

            Config.Init ();
            ConfigTab_Update ();

            Chart_Init ();

            lvStocks.ItemsSource = API_NasdaqTrader.GetSymbolPairs ();
        }

        private string FileSelection_Dialog (bool directory, CommonFileDialogFilter [] filters) {
            var dlg = new CommonOpenFileDialog ();
            if (filters != null)
                foreach (CommonFileDialogFilter filter in filters)
                    dlg.Filters.Add(filter);

            dlg.IsFolderPicker = directory;
            CommonFileDialogResult res = dlg.ShowDialog ();
            if (res == CommonFileDialogResult.Ok)
                return dlg.FileName;
            else
                return null;
        }
        private void btnFilePathWorkbook_Click (object sender, RoutedEventArgs e) {
            txtFilePathWorkbook.Text = FileSelection_Dialog (false,
                new CommonFileDialogFilter [] {
                    new CommonFileDialogFilter ("Excel Spreadsheets (*.xlsx)", "*.xlsx"),
                    new CommonFileDialogFilter ("All files (*.*)", "*.*")});
        }

        private void btnDirPathLibrary_Click (object sender, RoutedEventArgs e) {
            txtDirectoryLibrary.Text = FileSelection_Dialog (true, null);
        }

        private void btnSaveConfig_Click (object sender, RoutedEventArgs e) {
            Config = new Configuration {
                APIKey_AlphaVantage = txtAPIKey_AlphaVantage.Text,
                FilePath_Aggregator = txtFilePathWorkbook.Text,
                Directory_Library = txtDirectoryLibrary.Text
            };

            if (Config.SaveConfig ())
                MessageBox.Show ("Configuration updated successfully!", "Configuration Saved");
            else
                MessageBox.Show ("Error: failed to save configuration file.", "Configuration Not Saved");
        }

        private void ConfigTab_Update () {
            txtAPIKey_AlphaVantage.Text = Config.APIKey_AlphaVantage;
            txtFilePathWorkbook.Text = Config.FilePath_Aggregator;
            txtDirectoryLibrary.Text = Config.Directory_Library;
        }

        private void Chart_Init() {
            Series = new SeriesCollection ();

            XFormatter = val => new DateTime ((long)val).ToString ("MM/dd/yyyy");
            YFormatter = val => val.ToString ("C");

            DataContext = this;
        }

        private SeriesCollection Series { get; set; }
        private Func<double, string> XFormatter { get; set; }
        private Func<double, string> YFormatter { get; set; }

        private void Chart_DailyClose(string symbol) {
            Series.Add (new LineSeries {
                Title = String.Format("{0}", symbol),
                Values = API_LiveCharts.DailyClose_To_Values (API_AlphaVantage.GetData_TimeSeriesDaily (Config.APIKey_AlphaVantage, symbol))
            });
        }

    }
}
