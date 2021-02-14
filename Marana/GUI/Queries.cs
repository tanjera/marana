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
            List<Data.Strategy> strategies = await db.GetStrategies();
            await lvStrategies.SetSourceAsync(strategies.Select(s => s.Name).ToArray());

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

        public async Task Execute(Database db, Data.Strategy strategy, ListView output) {
            Status = Statuses.Running;

            Status = Statuses.Inactive;
        }
    }
}