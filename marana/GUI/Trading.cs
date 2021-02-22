using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Trading {

        public async Task Execute(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.TradingExecute);

            if (gm.Trading.Status == Marana.Trading.Statuses.Inactive) {
                window.RemoveAll();

                int x1 = 1, x2 = 25;

                Label lblExecuteFormat = new Label("Format to Execute:") { X = x1, Y = 1 };

                RadioGroup rgExecuteFormat = new RadioGroup(
                    Enum.GetNames(typeof(Data.Format)).Select(s => NStack.ustring.Make(s)).ToArray()) {
                    X = x1, Y = Pos.Bottom(lblExecuteFormat)
                };

                Button btnCancel = new Button("Start Execution") { X = x1, Y = Pos.Bottom(rgExecuteFormat) + 2 };

                btnCancel.Clicked += async () => {
                    if (gm.Trading.Status == Marana.Trading.Statuses.Inactive) {
                        btnCancel.Text = "Cancel Execution";
                        await gm.Trading.RunAutomation(gm.Settings, gm.Database, (Data.Format)rgExecuteFormat.SelectedItem, DateTime.Today);
                    } else if (gm.Trading.Status == Marana.Trading.Statuses.Executing) {
                        btnCancel.Text = "Start Execution";
                        gm.Trading.CancelUpdate = true;
                    }
                };

                TextView tvOutput = new TextView() {
                    X = x2, Y = 1, Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    ReadOnly = true
                };

                window.Add(
                    lblExecuteFormat,
                    rgExecuteFormat,
                    btnCancel,
                    tvOutput);

                gm.Trading.StatusUpdate += (sender, e) => {
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