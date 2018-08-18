using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace Marana {

    public partial class TabLibrary : UserControl {

        Main wdwMain;

        bool isRunning = false, isCancelled = false;
        BackgroundWorker bgw = new BackgroundWorker ();


        public TabLibrary (Main m) {
            InitializeComponent ();

            wdwMain = m;

            if (!String.IsNullOrEmpty (wdwMain.Config.Directory_Library))
                Directory.CreateDirectory (wdwMain.Config.Directory_Library);
        }


        private void btnUpdate_Click (object sender, RoutedEventArgs e) {
            if (!isRunning) {
                btnUpdate.Content = "Cancel Update";
                Update ();
            } else if (isRunning) {
                btnUpdate.Content = "Cancelling Update...";
                btnUpdate.IsEnabled = false;
                bgw.CancelAsync ();
            }

            isRunning = !isRunning;
        }

        private void StopUpdate () {
            isRunning = false;
            btnUpdate.IsEnabled = true;
            btnUpdate.Content = "Update Library";
        }


        private void Update () {

            List<SymbolPair> pairs = new List<SymbolPair> (API_NasdaqTrader.GetSymbolPairs ().OrderBy (obj => obj.Symbol).ToArray ());

            if (rbStartAt.IsChecked == true || rbFromTo.IsChecked == true) {
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                SymbolPair s = new SymbolPair ();
                if (rbStartAt.IsChecked == true)
                    s = (from pair in pairs where pair.Symbol == txtStartAt.Text.Trim ().ToUpper () select pair).DefaultIfEmpty (new SymbolPair()).First ();
                else if (rbFromTo.IsChecked == true)
                    s = (from pair in pairs where pair.Symbol == txtFromTo1.Text.Trim ().ToUpper () select pair).DefaultIfEmpty (new SymbolPair ()).First ();
                SymbolPair e = (from pair in pairs where pair.Symbol == txtFromTo2.Text.Trim ().ToUpper () select pair).DefaultIfEmpty (new SymbolPair ()).First ();

                si = pairs.IndexOf (s);
                ei = pairs.IndexOf (e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    pairs.RemoveRange (0, si);
                if (ei > 0)
                    pairs.RemoveRange (ei, pairs.Count - ei);
            }

            bgw.WorkerReportsProgress = true;
            bgw.WorkerSupportsCancellation = true;


            bgw.DoWork += new DoWorkEventHandler (delegate (object o, DoWorkEventArgs args) {
                for (int i = 0; i < pairs.Count; i++) {
                    if ((o as BackgroundWorker).CancellationPending == true) {
                        isCancelled = true;
                        return;
                    }

                    string output = "";
                    output = API_AlphaVantage.GetData_TimeSeriesDaily (wdwMain.Config.APIKey_AlphaVantage, pairs [i].Symbol, true);

                    (o as BackgroundWorker).ReportProgress ((i * 100) / pairs.Count, pairs [i].Symbol);
                    using (StreamWriter sw = new StreamWriter (String.Format ("{0}\\{1} {2}.json", wdwMain.Config.Directory_Library, pairs [i].Symbol, DateTime.Today.ToString ("yyyyMMdd")), false)) {
                        sw.Write (output);
                    }
                }
            });

            bgw.ProgressChanged += new ProgressChangedEventHandler (delegate (object o, ProgressChangedEventArgs args) {
                rtbLibrary.AppendText (String.Format ("{0}: ({1:00}%), data for {2} added to library!\n",
                    DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss"),
                    args.ProgressPercentage,
                    args.UserState as string));
            });

            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler (delegate (object o, RunWorkerCompletedEventArgs args) {
                if (isCancelled)
                    rtbLibrary.AppendText (String.Format ("{0}: Library update cancelled.\n", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));
                else
                    rtbLibrary.AppendText (String.Format ("{0}: Library update complete!\n", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));

                isCancelled = false;
                StopUpdate ();
            });


            bgw.RunWorkerAsync ();
        }
    }
}
