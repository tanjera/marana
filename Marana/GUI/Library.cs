using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Library {

        public async Task Erase(Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryEraseDatabase);
            window.RemoveAll();

            Label lblResult = new Label() {
                Width = Dim.Fill(), TextAlignment = TextAlignment.Centered, Y = Pos.Center()
            };

            Utility.OnButton confirmNo = new Utility.OnButton(() => {
                lblResult.Text = "Operation cancelled.";
            });
            Utility.OnButton confirmYes = new Utility.OnButton(async () => {
                bool wipeResult = await db.Wipe();
                lblResult.Text = wipeResult
                    ? "Operation completed. Database erased."
                    : "Operation failed. Attempt cancelled.";
            });

            Dialog dlgConfirm = Utility.CreateDialog_OptionYesNo("Are you sure you want to erase the database?",
                60, 7, window, confirmNo, confirmYes);

            dlgConfirm.ColorScheme = Colors.Error;

            window.Add(lblResult, dlgConfirm);

            Application.Refresh();
        }

        public async Task Info(Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryInformation);
            window.RemoveAll();

            // Layout coordinates

            int x1 = 1;
            int x2 = 30;

            // "Connecting" screen

            window.Add(new Label("Connecting to database...") { X = x1, Y = 1 });
            Application.Refresh();

            // Connect to database, post results

            decimal size = await db.GetSize();

            window.RemoveAll();

            window.Add(
                new Label("Database Size") { X = x1, Y = 1 },
                new Label($"{size} MB") { X = x2, Y = 1 }
            );

            Application.Refresh();
        }

        public async Task Update(Marana.Library library, Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(Main.WindowTypes.LibraryUpdate);

            if (library.Status == Marana.Library.Statuses.Inactive) {
                window.RemoveAll();

                Button btnCancel = new Button("Start Update") { X = Pos.Center(), Y = 1 };

                btnCancel.Clicked += () => {
                    if (library.Status == Marana.Library.Statuses.Inactive) {
                        Action _update = async () => { await library.Update(new List<string>(), settings, db); };
                        _update.Invoke();

                        btnCancel.Text = "Cancel Update";
                    } else if (library.Status == Marana.Library.Statuses.Updating) {
                        library.CancelUpdate = true;
                        btnCancel.Text = "Start Update";
                    }
                };

                Label lblOutput = new Label() { X = 1, Y = 3, Width = Dim.Fill(), Height = Dim.Fill() };
                window.Add(btnCancel, lblOutput);

                library.StatusUpdate += (sender, e) => {
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