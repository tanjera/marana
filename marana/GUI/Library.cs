using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Library {

        public async Task Erase(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryEraseDatabase);
            window.RemoveAll();

            Label lblResult = new Label() {
                Width = Dim.Fill(), TextAlignment = TextAlignment.Centered, Y = Pos.Center()
            };

            Utility.OnButton confirmNo = new Utility.OnButton(() => {
                lblResult.Text = "Operation cancelled.";
            });
            Utility.OnButton confirmYes = new Utility.OnButton(async () => {
                bool wipeResult = await gm.Database.Wipe();
                lblResult.Text = wipeResult
                    ? "Operation completed. Market data erased from database."
                    : "Operation failed. Attempt cancelled.";
            });

            Dialog dlgConfirm = Utility.CreateDialog_OptionYesNo(
                "Are you sure you want to erase market data from the database?",
                70, 7, window, confirmNo, confirmYes);

            dlgConfirm.ColorScheme = Colors.Error;

            window.Add(lblResult, dlgConfirm);

            Application.Refresh();
        }

        public async Task Info(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryInformation);
            window.RemoveAll();

            // Layout coordinates

            int x1 = 1;
            int x2 = 30;

            // "Connecting" screen

            window.Add(new Label("Connecting to database...") { X = x1, Y = 1 });
            Application.Refresh();

            // Connect to database, post results

            decimal size = await gm.Database.GetSize();

            window.RemoveAll();

            window.Add(
                new Label("Database Size") { X = x1, Y = 1 },
                new Label($"{size} MB") { X = x2, Y = 1 }
            );

            Application.Refresh();
        }

        public async Task Options(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryOptions);
            window.RemoveAll();

            // Layout coordinates

            int x1 = 1;
            int x2 = 40, w2 = 10;

            // Item labels

            window.Add(
                new Label("Limit of daily entries to download:") { X = x1, Y = 1 },
                new Label("Download data for:") { X = x1, Y = 3 },
                new Label("Data Provider:") { X = x1, Y = 6 }

            );

            // Fields for text entry

            TextField tfDailyEntries = new TextField(gm.Settings.Library_LimitDailyEntries.ToString()) {
                X = x2, Y = 1, Width = w2
            };

            RadioGroup rgDownloadFor = new RadioGroup(
                new NStack.ustring[] { "All Symbols", "Symbols on Watchlist" },
                gm.Settings.Library_DownloadSymbols.GetHashCode()) {
                X = x2, Y = 3
            };

            RadioGroup rgDataProvider = new RadioGroup(
                new NStack.ustring[] { "Alpaca", "Alpha Vantage" },
                gm.Settings.Library_DataProvider.GetHashCode()) {
                X = x2, Y = 6
            };

            window.Add(
                tfDailyEntries,
                rgDownloadFor,
                rgDataProvider
                );

            // Dialog notification on save success

            Dialog dlgSaved = Utility.CreateDialog_NotificationOkay("Settings saved successfully.", 40, 7, window);

            // Button for saving settings

            Button btnSave = new Button("Save Settings") {
                X = Pos.Center(), Y = Pos.Bottom(rgDataProvider) + 2
            };

            btnSave.Clicked += async () => {
                int resultInt;
                bool canParse = false;

                canParse = int.TryParse(tfDailyEntries.Text.ToString(), out resultInt);
                gm.Settings.Library_LimitDailyEntries = canParse ? Math.Max(300, Math.Min(resultInt, 1000)) : gm.Settings.Library_LimitDailyEntries;
                tfDailyEntries.Text = gm.Settings.Library_LimitDailyEntries.ToString();

                gm.Settings.Library_DownloadSymbols = (Marana.Settings.Option_DownloadSymbols)rgDownloadFor.SelectedItem;
                gm.Settings.Library_DataProvider = (Marana.Settings.Option_DataProvider)rgDataProvider.SelectedItem;

                await Marana.Settings.SaveConfig(gm.Settings);

                window.Add(dlgSaved);
            };

            window.Add(btnSave);

            Application.Refresh();
        }

        public async Task Update(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryUpdate);

            if (gm.Library.Status == Marana.Library.Statuses.Inactive) {
                window.RemoveAll();

                int x1 = 1, x2 = 25;

                Button btnCancel = new Button("Start Update") { X = x1, Y = 1 };

                btnCancel.Clicked += async () => {
                    if (gm.Library.Status == Marana.Library.Statuses.Inactive) {
                        btnCancel.Text = "Cancel Update";
                        await gm.Library.Update(new List<string>(), gm.Settings, gm.Database);
                    } else if (gm.Library.Status == Marana.Library.Statuses.Updating) {
                        btnCancel.Text = "Start Update";
                        gm.Library.CancelUpdate = true;
                    }
                };

                TextView tvOutput = new TextView() {
                    X = x2, Y = 1, Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    ReadOnly = true
                };
                window.Add(btnCancel, tvOutput);

                gm.Library.StatusUpdate += (sender, e) => {
                    tvOutput.Text = String.Join("\n", e.Output.GetRange(
                        e.Output.Count < tvOutput.Bounds.Height ? 0 : e.Output.Count - tvOutput.Bounds.Height,
                        e.Output.Count < tvOutput.Bounds.Height ? e.Output.Count : tvOutput.Bounds.Height));

                    Application.Refresh();
                };
            }

            Application.Refresh();
        }
    }
}