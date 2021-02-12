using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Library {

        public static void Erase(Marana.Settings settings, Database db) {
            Window window = Utility.SetWindow("Library: Erase Database");

            Label lblResult = new Label() {
                Width = Dim.Fill(), TextAlignment = TextAlignment.Centered, Y = Pos.Center()
            };

            Utility.OnButton confirmNo = new Utility.OnButton(() => {
                lblResult.Text = "Operation cancelled.";
            });
            Utility.OnButton confirmYes = new Utility.OnButton(() => {
                bool wipeResult = db.Wipe();
                lblResult.Text = wipeResult
                    ? "Operation completed. Database erased."
                    : "Operation failed. Attempt cancelled.";
            });

            Dialog dlgConfirm = Utility.DialogOption_YesNo("Are you sure you want to erase the database?",
                60, 7, window, confirmNo, confirmYes);

            dlgConfirm.ColorScheme = Colors.Error;

            window.Add(lblResult, dlgConfirm);

            Application.Run();
        }

        public static void Info(Marana.Settings settings, Database db) {
            Window window = Utility.SetWindow("Library Information");

            // Layout coordinates

            int x1 = 1;
            int x2 = 30;

            // Item labels

            window.Add(
                new Label("Database Size") { X = x1, Y = 1 },
                new Label($"{db.GetSize()} MB") { X = x2, Y = 1 }
            );

            Application.Run();
        }
    }
}