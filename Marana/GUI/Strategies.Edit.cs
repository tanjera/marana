using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal partial class Strategies {

        public async Task Edit(Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(Main.WindowTypes.StrategiesEdit);
            window.RemoveAll();

            // Status screen, while connecting to database

            window.Add(new Label("Connecting to database...") { X = 1, Y = 1 });
            Application.Refresh();

            // Set up the view items

            List<Data.Strategy> listStrategies = await db.GetStrategies();

            window.RemoveAll();

            Button btnAdd = new Button("Add a Strategy") {
                X = 1, Y = 1
            };

            ListView lvStrategies = new ListView() {
                X = 1, Y = Pos.Bottom(btnAdd) + 1,
                Width = 20, Height = Dim.Fill() - 4
            };

            Button btnDelete = new Button("Delete Strategy") {
                X = 1, Y = Pos.Bottom(lvStrategies) + 2,
                Width = 20, Height = 1
            };

            Label lblEditor = new Label("Editor:") {
                X = Pos.Right(lvStrategies) + 2, Y = 1
            };

            TextView tvEditor = new TextView() {
                X = Pos.Right(lvStrategies) + 2, Y = Pos.Bottom(lblEditor) + 1,
                Width = Dim.Fill(), Height = Dim.Fill() - 4
            };

            Button btnSave = new Button("Save Current Strategy") {
                X = Pos.Left(tvEditor), Y = Pos.Bottom(tvEditor) + 2,
                Width = Dim.Fill(), Height = 1
            };

            // Link view item functionality

            btnAdd.Clicked += async () => {
                Dialog dialog = new Dialog("Add Strategy", 50, 10) { X = Pos.Center(), Y = Pos.Center() };

                Label lblPrompt = new Label("Please enter a name for the strategy:") { X = Pos.Center(), Y = Pos.Center() - 2 };
                TextField tfName = new TextField() { X = Pos.Center(), Y = Pos.Bottom(lblPrompt) + 1, Width = 30, Height = 1 };
                Button btnSaved = new Button("Okay") { X = Pos.Center(), Y = Pos.Bottom(tfName) + 1 };

                btnSaved.Clicked += async () => {
                    window.Remove(dialog);
                    listStrategies.Add(new Data.Strategy() { Name = tfName.Text.ToString(), Query = "" });
                    await lvStrategies.SetSourceAsync(listStrategies.Select(o => { return o.Name; }).ToList());
                };

                dialog.Add(lblPrompt, tfName, btnSaved);
                window.Add(dialog);
            };

            await lvStrategies.SetSourceAsync(listStrategies.Select(o => { return o.Name; }).ToList());

            lvStrategies.SelectedItemChanged += (o) => {
                if (listStrategies.Count > 0) {
                    tvEditor.Text = listStrategies[o.Item].Query;
                }
            };

            lvStrategies.SelectedItem = 0;

            btnSave.Clicked += async () => {
                if (listStrategies.Count > lvStrategies.SelectedItem) {
                    listStrategies[lvStrategies.SelectedItem].Query = tvEditor.Text.ToString();
                    await db.SetStrategy(listStrategies[lvStrategies.SelectedItem]);
                    window.Add(Utility.CreateDialog_NotificationOkay("Strategy Saved", 40, 7, window));
                }
            };

            btnDelete.Clicked += async () => {
                window.Add(Utility.CreateDialog_OptionYesNo("Are you sure you want to delete this strategy?", 50, 7, window,
                    () => { },
                    async () => {
                        await db.DeleteStrategy(listStrategies[lvStrategies.SelectedItem]);
                        listStrategies.RemoveAt(lvStrategies.SelectedItem);

                        if (listStrategies.Count > 0) {
                            await lvStrategies.SetSourceAsync(listStrategies.Select(o => { return o.Name; }).ToList());
                            lvStrategies.SelectedItem = 0;
                        } else {
                            await lvStrategies.SetSourceAsync(new List<string>());
                            tvEditor.Text = "";
                        }

                        window.Add(Utility.CreateDialog_NotificationOkay("Strategy Deleted", 40, 7, window));
                    }));
            };

            // Add and display

            window.Add(
                btnAdd, lvStrategies, btnDelete,
                lblEditor, tvEditor, btnSave);

            Application.Refresh();
        }
    }
}