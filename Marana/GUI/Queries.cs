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

        public async Task Test(GUI.Main gm) {
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

            Label lvResults = new Label() {
                X = Pos.Right(lvStrategies) + 2, Y = 1,
                Width = Dim.Fill(), Height = Dim.Fill() - 4
            };

            Button btnRun = new Button("Validate Strategy Queries") {
                X = Pos.Left(lvResults), Y = Pos.Bottom(lvResults) + 2,
                Width = Dim.Fill(), Height = 1
            };

            // Link view item functionality
            List<Data.Strategy> strategies = await gm.Database.GetStrategies();

            if (strategies == null)
                strategies = new List<Data.Strategy>();

            await lvStrategies.SetSourceAsync(strategies?.Select(s => s.Name).ToArray());

            btnRun.Clicked += async () => {
                if (Status == Statuses.Inactive && strategies.Count > lvStrategies.SelectedItem) {
                    await Execute(gm, strategies[lvStrategies.SelectedItem], lvResults);
                }
            };

            // Add and display

            window.Add(
                lvStrategies,
                lvResults, btnRun);

            Application.Refresh();
        }

        public async Task Execute(Main gm, Data.Strategy strategy, Label results) {
            Status = Statuses.Running;
            object result;

            results.Text = String.Concat(results.Text, $"Running Entry query {Environment.NewLine}");
            result = await gm.Database.ValidateQuery(
                await Strategy.Interpret(strategy.Entry, "SPY"));
            if (result is bool) {
                results.Text = String.Concat(results.Text, $"Successful query! {Environment.NewLine}");
            } else if (result is string) {
                results.Text = String.Concat(results.Text, $"{result} {Environment.NewLine}");
            }
            results.Text = string.Concat(results.Text, $"{Environment.NewLine}");

            results.Text = String.Concat(results.Text, $"Running Exit Gain query {Environment.NewLine}");
            result = await gm.Database.ValidateQuery(
               await Strategy.Interpret(strategy.ExitGain, "SPY"));
            if (result is bool) {
                results.Text = String.Concat(results.Text, $"Successful query! {Environment.NewLine}");
            } else if (result is string) {
                results.Text = String.Concat(results.Text, $"{result} {Environment.NewLine}");
            }
            results.Text = string.Concat(results.Text, $"{Environment.NewLine}");

            results.Text = String.Concat(results.Text, $"Running Exit Loss query {Environment.NewLine}");
            result = await gm.Database.ValidateQuery(
               await Strategy.Interpret(strategy.ExitLoss, "SPY"));
            if (result is bool) {
                results.Text = String.Concat(results.Text, $"Successful query! {Environment.NewLine}");
            } else if (result is string) {
                results.Text = String.Concat(results.Text, $"{result} {Environment.NewLine}");
            }

            Status = Statuses.Inactive;
        }
    }
}