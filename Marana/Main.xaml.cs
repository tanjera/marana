using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

using OfficeOpenXml;

namespace Marana {

    public partial class Main : Window {

        private Configuration Config = new Configuration ();


        public Main () {
            InitializeComponent ();

            Config.Init ();
            ConfigTab_Update ();

            ChartTab_Init ();

            LibraryTab_Init ();

            //lvStocks.ItemsSource = API_NasdaqTrader.GetSymbolPairs ();
        }


        private void ConfigTab_Update () {
            txtAPIKey_AlphaVantage.Text = Config.APIKey_AlphaVantage;
            txtDirectoryLibrary.Text = Config.Directory_Library;
        }


        private void ChartTab_Init() {
            Series = new SeriesCollection ();

            XFormatter = val => new DateTime ((long)val).ToString ("MM/dd/yyyy");
            YFormatter = val => val.ToString ("C");

            DataContext = this;
        }


        private void LibraryTab_Init() {
            if (!String.IsNullOrEmpty(Config.Directory_Library))
                Directory.CreateDirectory (Config.Directory_Library);
        }



        private string FileSelection_Dialog (bool directory, CommonFileDialogFilter [] filters) {
            var dlg = new CommonOpenFileDialog ();
            if (filters != null)
                foreach (CommonFileDialogFilter filter in filters)
                    dlg.Filters.Add (filter);

            dlg.IsFolderPicker = directory;
            CommonFileDialogResult res = dlg.ShowDialog ();
            if (res == CommonFileDialogResult.Ok)
                return dlg.FileName;
            else
                return null;
        }



        private SeriesCollection Series { get; set; }

        private Func<double, string> XFormatter { get; set; }

        private Func<double, string> YFormatter { get; set; }

        private void Chart_DailyClose(string symbol) {
            Series.Add (new LineSeries {
                Title = String.Format ("{0}", symbol),
                Values = API_LiveCharts.DailyClose_To_Values (
                    API_AlphaVantage.ProcessData_TimeSeriesDaily (API_AlphaVantage.GetData_TimeSeriesDaily (Config.APIKey_AlphaVantage, symbol)))
            });
        }



        private void btnDirPathLibrary_Click (object sender, RoutedEventArgs e) {
            txtDirectoryLibrary.Text = FileSelection_Dialog (true, null);
        }


        private void btnSaveConfig_Click (object sender, RoutedEventArgs e) {
            Config = new Configuration {
                APIKey_AlphaVantage = txtAPIKey_AlphaVantage.Text,
                Directory_Library = txtDirectoryLibrary.Text
            };

            if (Config.SaveConfig ())
                MessageBox.Show ("Configuration updated successfully!", "Configuration Saved");
            else
                MessageBox.Show ("Error: failed to save configuration file.", "Configuration Not Saved");
        }


        private void btnFilepathAggregate_Click (object sender, RoutedEventArgs e) {
            txtFilepathAggregate.Text = FileSelection_Dialog (false,
                new CommonFileDialogFilter[] { new CommonFileDialogFilter ("Excel Spreadsheets", "*.xlsx") });
        }


        private void btnAggregateData_Click (object sender, RoutedEventArgs e) {

            /* Symbol Key:
             * x̅ : Mean
             * x̃ : Median
             * %Δ : % Change
             */

            DateTime YearsAgo_01 = DateTime.Today - new TimeSpan (365, 0, 0, 0, 0);
            DateTime YearsAgo_02 = DateTime.Today - new TimeSpan (730, 0, 0, 0, 0);
            DateTime YearsAgo_05 = DateTime.Today - new TimeSpan (1825, 0, 0, 0, 0);
            DateTime YearsAgo_10 = DateTime.Today - new TimeSpan (3650, 0, 0, 0, 0);
            DateTime YearsAgo_20 = DateTime.Today - new TimeSpan (7300, 0, 0, 0, 0);


            ExcelPackage ep = new ExcelPackage ();
            ExcelWorksheet epws = ep.Workbook.Worksheets.Add (String.Format ("Output {0}", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));

            // Spreadsheet title and row headers
            int headingOffset = 2;
            epws.SetValue (1, 2, String.Format ("Marana Aggregator: Analysis of Symbols, {0}", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));
            epws.SetValue (2, 1, "Symbol");
            epws.SetValue (2, 2, "Name");
            epws.SetValue (2, 3, "Oldest");
            epws.SetValue (2, 4, "Newest");
            epws.SetValue (2, 5, "Latest");
            epws.SetValue (2, 6, "YTD x̅");
            epws.SetValue (2, 7, "YTD x̃");
            epws.SetValue (2, 8, "YTD %Δ");
            epws.SetValue (2, 9, "-1Y x̅");
            epws.SetValue (2, 10, "-1Y x̃");
            epws.SetValue (2, 11, "-1Y %Δ");
            epws.SetValue (2, 12, "-2Y x̅");
            epws.SetValue (2, 13, "-2Y x̃");
            epws.SetValue (2, 14, "-2Y %Δ");
            epws.SetValue (2, 15, "-5Y x̅");
            epws.SetValue (2, 16, "-5Y x̃");
            epws.SetValue (2, 17, "-5Y %Δ");
            epws.SetValue (2, 18, "-10Y x̅");
            epws.SetValue (2, 19, "-10Y x̃");
            epws.SetValue (2, 20, "-10Y %Δ");
            epws.SetValue (2, 21, "-20Y x̅");
            epws.SetValue (2, 22, "-20Y x̃");
            epws.SetValue (2, 23, "-20Y %Δ");

            epws.Cells ["A1:W2"].Style.Font.Bold = true;
            epws.Cells ["A1:W2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            epws.Cells ["E3:W10000"].Style.Numberformat.Format = "#,##0.00";


            string [] files = Directory.GetFiles (Config.Directory_Library, "*.json");
            List<SymbolPair> pairs = API_NasdaqTrader.GetSymbolPairs ();

            for (int i = 0; i < files.Length; i++) {

                Console.WriteLine (String.Format ("Processing {0} of {1}: {2}", i, files.Length - 1, files [i]));

                IEnumerable<DailyValue> ldv;
                using (StreamReader sr = new StreamReader(files[i]))
                    ldv = API_AlphaVantage.ProcessData_TimeSeriesDaily (sr.ReadToEnd ()).OrderBy (obj => obj.Timestamp); ;

                string filename = files [i].Substring (files [i].LastIndexOf ('\\') + 1);
                string symbol = filename.Substring (0, filename.IndexOf (' ')).Trim ();
                string name = (from pair in pairs where pair.Symbol == symbol select pair.Name).First();

                epws.SetValue (i + headingOffset + 1, 1, symbol);                                           // Symbol
                epws.SetValue (i + headingOffset + 1, 2, name);                                             // Stock Name
                epws.SetValue (i + headingOffset + 1, 3, ldv.First ().Timestamp.ToString ("MM/dd/yyyy"));   // Oldest in Series
                epws.SetValue (i + headingOffset + 1, 4, ldv.Last ().Timestamp.ToString ("MM/dd/yyyy"));    // Newest in Series
                epws.SetValue (i + headingOffset + 1, 5, ldv.Last ().Close);                                // Latest Value

                // Isolate value groups (timespans) to run calculations on
                var ytd = from dv in ldv where dv.Timestamp.Year == DateTime.Today.Year select dv;
                var ya01 = from dv in ldv where dv.Timestamp.Year == YearsAgo_01.Year select dv;
                var ya02 = from dv in ldv where dv.Timestamp.Year == YearsAgo_02.Year select dv;
                var ya05 = from dv in ldv where dv.Timestamp.Year == YearsAgo_05.Year select dv;
                var ya10 = from dv in ldv where dv.Timestamp.Year == YearsAgo_10.Year select dv;
                var ya20 = from dv in ldv where dv.Timestamp.Year == YearsAgo_20.Year select dv;

                // Mean, median, and % growth (from median) for each timespan
                epws.SetValue (i + headingOffset + 1, 6, (from dv in ytd select dv.Close).DefaultIfEmpty (0).Average ());
                var lbuf = (from dv in ytd orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 7, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 9, (from dv in ya01 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya01 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 10, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 12, (from dv in ya02 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya02 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 13, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 15, (from dv in ya05 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya05 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 16, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 18, (from dv in ya10 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya10 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 19, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 21, (from dv in ya20 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya20 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 22, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));


                Console.Write (" ... data processed.\n");
            }

            for (int i = 1; i < 23; i++)
                epws.Column (i).AutoFit ();


            try {
                ep.SaveAs (new FileInfo (txtFilepathAggregate.Text));
                MessageBox.Show (String.Format ("Excel spreadsheet written to {0}", txtFilepathAggregate.Text));
            } catch {
                MessageBox.Show (String.Format ("Unable to save to selected destination."));
            }
        }


        private void btnUpdateLibrary_Click (object sender, RoutedEventArgs e) {

            List<SymbolPair> pairs = new List<SymbolPair>(API_NasdaqTrader.GetSymbolPairs ().OrderBy(obj => obj.Symbol).ToArray());
            string updateAt = (rbUpdateAt.IsChecked == true) ? txtUpdateAt.Text.Trim () : null;

            BackgroundWorker bgw = new BackgroundWorker ();
            bgw.WorkerReportsProgress = true;
            bgw.DoWork += new DoWorkEventHandler (delegate (object o, DoWorkEventArgs args) {

                int i = 0;
                if (updateAt != null)
                    i = Math.Max(0, pairs.FindIndex (obj => obj.Symbol == updateAt)) + 1;

                for (; i < pairs.Count; i++) {
                    string output = "";
                    BackgroundWorker bgwo = o as BackgroundWorker;
                    output = API_AlphaVantage.GetData_TimeSeriesDaily (Config.APIKey_AlphaVantage, pairs [i].Symbol, true);
                    bgwo.ReportProgress (i / pairs.Count, pairs [i].Symbol);
                    using (StreamWriter sw = new StreamWriter (String.Format ("{0}\\{1} {2}.json", Config.Directory_Library, pairs [i].Symbol, DateTime.Today.ToString ("yyyyMMdd")), false)) {
                        sw.Write (output);
                    }
                }
            });

            bgw.ProgressChanged += new ProgressChangedEventHandler (delegate (object o, ProgressChangedEventArgs args) {
                rtbLibrary.AppendText (String.Format ("{0}: Data request successful, data for {1} added to library!\n", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss"), args.UserState as string));
            });

            bgw.RunWorkerAsync ();
        }
    }
}
