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

        public async Task Update(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryUpdate);

            if (gm.Library.Status == Marana.Library.Statuses.Inactive) {
                window.RemoveAll();

                Button btnCancel = new Button("Start Update") { X = Pos.Center(), Y = 1 };

                btnCancel.Clicked += async () => {
                    if (gm.Library.Status == Marana.Library.Statuses.Inactive) {
                        btnCancel.Text = "Cancel Update";
                        await gm.Library.Update(new List<string>(), gm.Settings, gm.Database);
                    } else if (gm.Library.Status == Marana.Library.Statuses.Updating) {
                        btnCancel.Text = "Start Update";
                        gm.Library.CancelUpdate = true;
                    }
                };

                Label lblOutput = new Label() { X = 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill() };
                window.Add(btnCancel, lblOutput);

                gm.Library.StatusUpdate += (sender, e) => {
                    lblOutput.Text = String.Join(Environment.NewLine, e.Output.GetRange(
                        e.Output.Count < lblOutput.Bounds.Height ? 0 : e.Output.Count - lblOutput.Bounds.Height,
                        e.Output.Count < lblOutput.Bounds.Height ? e.Output.Count : lblOutput.Bounds.Height));

                    Application.Refresh();
                };
            }

            Application.Refresh();
        }
    }
}