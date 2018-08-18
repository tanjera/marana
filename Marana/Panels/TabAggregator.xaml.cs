using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Microsoft.WindowsAPICodePack.Dialogs;

using OfficeOpenXml;


namespace Marana {

    public partial class TabAggregator : UserControl {

        Main wdwMain;

        bool isRunning = false, isCancelled = false;
        BackgroundWorker bgw = new BackgroundWorker ();
        DateTime YearsAgo_01, YearsAgo_02, YearsAgo_05, YearsAgo_10, YearsAgo_20;

        ExcelPackage ep;
        ExcelWorksheet epws;
        int headingOffset = 2;


        public TabAggregator (Main m) {
            InitializeComponent ();

            wdwMain = m;
        }

        private void btnFilepath_Click (object sender, RoutedEventArgs e) {
            txtFilepath.Text = Main.FileSelection_Dialog (false,
                new CommonFileDialogFilter [] { new CommonFileDialogFilter ("Excel Spreadsheets", "*.xlsx") });

            if (!txtFilepath.Text.Trim ().EndsWith (".xlsx"))
                txtFilepath.AppendText (".xlsx");
        }

        private void btnAggregate_Click (object sender, RoutedEventArgs e) {
            if (!isRunning) {
                btnAggregate.Content = "Cancel Data Aggregation";
                Aggregate ();
            } else if (isRunning) {
                btnAggregate.Content = "Cancelling Data Aggregation...";
                btnAggregate.IsEnabled = false;
                bgw.CancelAsync ();
            }

            isRunning = !isRunning;
        }

        private void StopAggregation() {
            isRunning = false;
            btnAggregate.IsEnabled = true;
            btnAggregate.Content = "Aggregate Data to Spreadsheet";
        }


        private void Aggregate () {

            YearsAgo_01 = DateTime.Today - new TimeSpan (365, 0, 0, 0, 0);
            YearsAgo_02 = DateTime.Today - new TimeSpan (730, 0, 0, 0, 0);
            YearsAgo_05 = DateTime.Today - new TimeSpan (1825, 0, 0, 0, 0);
            YearsAgo_10 = DateTime.Today - new TimeSpan (3650, 0, 0, 0, 0);
            YearsAgo_20 = DateTime.Today - new TimeSpan (7300, 0, 0, 0, 0);

            ep = new ExcelPackage ();
            epws = ep.Workbook.Worksheets.Add (String.Format ("Output {0}", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));

            // Spreadsheet title and row headers
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

            List<string> files = new List<string> (Directory.GetFiles (wdwMain.Config.Directory_Library, "*.json"));
            List<SymbolPair> pairs = API_NasdaqTrader.GetSymbolPairs ();

            if (rbStartAt.IsChecked == true || rbFromTo.IsChecked == true) {
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                string s = "";
                if (rbStartAt.IsChecked == true)
                    s = (from file in files where file.Contains (String.Format ("\\{0} ", txtStartAt.Text.Trim ().ToUpper ())) select file).DefaultIfEmpty ("").First ();
                else if (rbFromTo.IsChecked == true)
                    s = (from file in files where file.Contains (String.Format ("\\{0} ", txtFromTo1.Text.Trim ().ToUpper ())) select file).DefaultIfEmpty ("").First ();
                string e = (from file in files where file.Contains (String.Format ("\\{0} ", txtFromTo2.Text.Trim ().ToUpper ())) select file).DefaultIfEmpty("").First ();

                si = files.IndexOf (s);
                ei = files.IndexOf (e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    files.RemoveRange (0, si);
                if (ei > 0)
                    files.RemoveRange (ei, files.Count - ei);
            }


            bgw.WorkerReportsProgress = true;
            bgw.WorkerSupportsCancellation = true;


            bgw.DoWork += new DoWorkEventHandler (delegate (object o, DoWorkEventArgs args) {
                for (int i = 0; i < files.Count; i++) {
                    if ((o as BackgroundWorker).CancellationPending == true) {
                        isCancelled = true;
                        return;
                    }

                    string filename = files [i].Substring (files [i].LastIndexOf ('\\') + 1);
                    string symbol = filename.Substring (0, filename.IndexOf (' ')).Trim ();
                    string name = (from pair in pairs where pair.Symbol == symbol select pair.Name).First ();

                    AggregateFile (i, files [i], symbol, name);
                    (o as BackgroundWorker).ReportProgress ((i * 100) / files.Count , symbol);
                }
            });


            bgw.ProgressChanged += new ProgressChangedEventHandler (delegate (object o, ProgressChangedEventArgs args) {
                rtbAggregator.AppendText (String.Format ("{0}: ({1:00}%), data aggregated for {2}!\n",
                    DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss"),
                    args.ProgressPercentage,
                    args.UserState as string));
            });


            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler (delegate (object o, RunWorkerCompletedEventArgs args) {
                if (isCancelled) {
                    rtbAggregator.AppendText (String.Format("{0}: Data aggregation cancelled.\n", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));
                } else {
                    for (int i = 1; i < 23; i++)
                        epws.Column (i).AutoFit ();

                    try {
                        ep.SaveAs (new FileInfo (txtFilepath.Text));
                        rtbAggregator.AppendText (String.Format ("Excel spreadsheet written to {0}\n", txtFilepath.Text));
                    } catch {
                        rtbAggregator.AppendText (String.Format ("Unable to save to selected destination.\n"));
                    }
                }

                isCancelled = false;
                StopAggregation ();

                ep.Dispose ();
                epws.Dispose ();
            });


            bgw.RunWorkerAsync ();
        }


        private void AggregateFile (int row, string file, string symbol, string name) {

            /* Symbol Key:
             * x̅ : Mean
             * x̃ : Median
             * %Δ : % Change
             */

            IEnumerable<DailyValue> ldv;
            using (StreamReader sr = new StreamReader (file))
                ldv = API_AlphaVantage.ProcessData_TimeSeriesDaily (sr.ReadToEnd ()).OrderBy (obj => obj.Timestamp);

            epws.SetValue (row + headingOffset + 1, 1, symbol);                                           // Symbol
            epws.SetValue (row + headingOffset + 1, 2, name);                                             // Stock Name
            epws.SetValue (row + headingOffset + 1, 3, ldv.First ().Timestamp.ToString ("MM/dd/yyyy"));   // Oldest in Series
            epws.SetValue (row + headingOffset + 1, 4, ldv.Last ().Timestamp.ToString ("MM/dd/yyyy"));    // Newest in Series
            epws.SetValue (row + headingOffset + 1, 5, ldv.Last ().Close);                                // Latest Value

            // Isolate value groups (timespans) to run calculations on
            var ytd = from dv in ldv where dv.Timestamp.Year == DateTime.Today.Year select dv;
            var ya01 = from dv in ldv where dv.Timestamp.Year == YearsAgo_01.Year select dv;
            var ya02 = from dv in ldv where dv.Timestamp.Year == YearsAgo_02.Year select dv;
            var ya05 = from dv in ldv where dv.Timestamp.Year == YearsAgo_05.Year select dv;
            var ya10 = from dv in ldv where dv.Timestamp.Year == YearsAgo_10.Year select dv;
            var ya20 = from dv in ldv where dv.Timestamp.Year == YearsAgo_20.Year select dv;

            // Mean, median, and % growth (from median) for each timespan
            epws.SetValue (row + headingOffset + 1, 6, (from dv in ytd select dv.Close).DefaultIfEmpty (0).Average ());
            var lbuf = (from dv in ytd orderby dv.Close select dv.Close).DefaultIfEmpty (0);
            epws.SetValue (row + headingOffset + 1, 7, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

            epws.SetValue (row + headingOffset + 1, 9, (from dv in ya01 select dv.Close).DefaultIfEmpty (0).Average ());
            lbuf = (from dv in ya01 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
            epws.SetValue (row + headingOffset + 1, 10, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

            epws.SetValue (row + headingOffset + 1, 12, (from dv in ya02 select dv.Close).DefaultIfEmpty (0).Average ());
            lbuf = (from dv in ya02 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
            epws.SetValue (row + headingOffset + 1, 13, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

            epws.SetValue (row + headingOffset + 1, 15, (from dv in ya05 select dv.Close).DefaultIfEmpty (0).Average ());
            lbuf = (from dv in ya05 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
            epws.SetValue (row + headingOffset + 1, 16, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

            epws.SetValue (row + headingOffset + 1, 18, (from dv in ya10 select dv.Close).DefaultIfEmpty (0).Average ());
            lbuf = (from dv in ya10 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
            epws.SetValue (row + headingOffset + 1, 19, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

            epws.SetValue (row + headingOffset + 1, 21, (from dv in ya20 select dv.Close).DefaultIfEmpty (0).Average ());
            lbuf = (from dv in ya20 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
            epws.SetValue (row + headingOffset + 1, 22, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));
        }
    }
}
