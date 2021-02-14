using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Queries {
        private Statuses Status;

        private enum Statuses {
            Inactive,
            Running
        }

        public async Task Run(Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(Main.WindowTypes.QueriesRun);
            window.RemoveAll();

            // Status screen, while connecting to database

            window.Add(new Label("Connecting to database...") { X = 1, Y = 1 });
            Application.Refresh();

            // Set up the view items

            window.RemoveAll();

            ListView lvStrategies = new ListView() {
                X = 1, Y = 1,
                Width = 20, Height = Dim.Fill() - 4
            };

            ListView lvResults = new ListView() {
                X = Pos.Right(lvStrategies) + 2, Y = 1,
                Width = Dim.Fill(), Height = Dim.Fill() - 4
            };

            Button btnRun = new Button("Run Current Strategy") {
                X = Pos.Left(lvResults), Y = Pos.Bottom(lvResults) + 2,
                Width = Dim.Fill(), Height = 1
            };

            // Link view item functionality
            List<string> strategies = Strategy.Listing.Select(o => { return o.Key; }).ToList();
            await lvStrategies.SetSourceAsync(strategies);

            btnRun.Clicked += async () => {
                if (Status == Statuses.Inactive && strategies.Count > lvStrategies.SelectedItem) {
                    await Execute(db, strategies[lvStrategies.SelectedItem], lvResults);
                }
            };

            // Add and display

            window.Add(
                lvStrategies,
                lvResults, btnRun);

            Application.Refresh();
        }

        public async Task Execute(Database db, string strategy, ListView output) {
            Status = Statuses.Running;

            List<Data.Asset> assets = await db.GetAssets();

            Func<Data.Daily, DateTime, bool> funcStrategy;
            if (!Strategy.Listing.TryGetValue(strategy, out funcStrategy)) {
                output.Text += "Unable to select strategy from Strategy listing.\nOperation Aborted";
                return;
            }

            List<Data.Asset> outList = new List<Data.Asset>();
            List<string> outBuffer = new List<string>() { "" };
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < assets.Count; i++) {
                outBuffer.Add($"[{i + 1:0000} / {assets.Count:0000}]  Querying and making determination on {assets[i].Symbol}.");
                await output.SetSourceAsync(outBuffer.GetRange(
                    Math.Max(0, outBuffer.Count - output.Bounds.Height),
                    Math.Min(output.Bounds.Height, outBuffer.Count)));
                Application.Refresh();

                Task t = new Task(async () => {
                    Data.Daily dd = await db.GetData_Daily(assets[i]);

                    if (dd == null || dd.Prices.Count == 0)
                        return;

                    if (funcStrategy(dd, new DateTime(2021, 02, 12)))
                        outList.Add(assets[i]);
                });

                while (tasks.FindAll(a => a.Status == TaskStatus.Running).Count >= 10)
                    await Task.Delay(100);

                t.Start();
                tasks.Add(t);
            }

            Status = Statuses.Inactive;
            outBuffer = new List<string>() { "Symbols matching this query:" };
            outBuffer.AddRange(outList.Select(s => s.Symbol));
            await output.SetSourceAsync(outBuffer);
            Application.Refresh();
        }
    }
}