using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Settings {

        public async Task Edit(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.Settings);
            window.RemoveAll();

            // Layout coordinates

            int x1 = 1;
            int x2 = 30, w2 = 50;

            // Item labels

            window.Add(
                new Label("Config Directory") { X = x1, Y = 1 },
                new Label(Marana.Settings.GetConfigDirectory()) { X = x2, Y = 1 },

                new Label("Working Directory") { X = x1, Y = 2 },

                new Label("Alpaca API Live Key") { X = x1, Y = 4 },
                new Label("Alpaca API Live Secret") { X = x1, Y = 5 },

                new Label("Alpaca API Paper Key") { X = x1, Y = 7 },
                new Label("Alpaca API Paper Secret") { X = x1, Y = 8 },

                new Label("Database Server") { X = x1, Y = 10 },
                new Label("Database Port") { X = x1, Y = 11 },
                new Label("Database Schema") { X = x1, Y = 12 },
                new Label("Database Username") { X = x1, Y = 13 },
                new Label("Database Password") { X = x1, Y = 14 }
            );

            // Fields for text entry

            TextField tfWorkingDir = new TextField(gm.Settings.Directory_Working ?? "") {
                X = x2, Y = 2, Width = w2
            };

            TextField tfAlpacaLiveKey = new TextField(gm.Settings.API_Alpaca_Live_Key ?? "") {
                X = x2, Y = Pos.Bottom(tfWorkingDir) + 1, Width = w2
            };
            TextField tfAlpacaLiveSecret = new TextField(gm.Settings.API_Alpaca_Live_Secret ?? "") {
                X = x2, Y = Pos.Bottom(tfAlpacaLiveKey), Width = w2
            };

            TextField tfAlpacaPaperKey = new TextField(gm.Settings.API_Alpaca_Paper_Key ?? "") {
                X = x2, Y = Pos.Bottom(tfAlpacaLiveSecret) + 1, Width = w2
            };
            TextField tfAlpacaPaperSecret = new TextField(gm.Settings.API_Alpaca_Paper_Secret ?? "") {
                X = x2, Y = Pos.Bottom(tfAlpacaPaperKey), Width = w2
            };

            TextField tfDbServer = new TextField(gm.Settings.Database_Server ?? "") {
                X = x2, Y = Pos.Bottom(tfAlpacaPaperSecret) + 1, Width = w2
            };
            TextField tfDbPort = new TextField(gm.Settings.Database_Port.ToString() ?? "") {
                X = x2, Y = Pos.Bottom(tfDbServer), Width = w2
            };
            TextField tfDbSchema = new TextField(gm.Settings.Database_Schema ?? "") {
                X = x2, Y = Pos.Bottom(tfDbPort), Width = w2
            };
            TextField tfDbUsername = new TextField(gm.Settings.Database_Username ?? "") {
                X = x2, Y = Pos.Bottom(tfDbSchema), Width = w2
            };
            TextField tfDbPassword = new TextField(gm.Settings.Database_Password ?? "") {
                X = x2, Y = Pos.Bottom(tfDbUsername), Width = w2
            };

            window.Add(
                tfWorkingDir,
                tfAlpacaLiveKey,
                tfAlpacaLiveSecret,
                tfAlpacaPaperKey,
                tfAlpacaPaperSecret,
                tfDbServer,
                tfDbPort,
                tfDbSchema,
                tfDbUsername,
                tfDbPassword
                );

            // Dialog notification on save success

            Dialog dlgSaved = Utility.CreateDialog_NotificationOkay("Settings saved successfully.", 40, 7, window);

            // Button for saving settings

            Button btnSave = new Button("Save Settings") {
                X = Pos.Center(), Y = Pos.Bottom(tfDbPassword) + 2
            };

            btnSave.Clicked += () => {
                gm.Settings.Directory_Working = tfWorkingDir.Text.ToString().Trim();

                gm.Settings.API_Alpaca_Live_Key = tfAlpacaLiveKey.Text.ToString().Trim();
                gm.Settings.API_Alpaca_Live_Secret = tfAlpacaLiveSecret.Text.ToString().Trim();

                gm.Settings.API_Alpaca_Paper_Key = tfAlpacaPaperKey.Text.ToString().Trim();
                gm.Settings.API_Alpaca_Paper_Secret = tfAlpacaPaperSecret.Text.ToString().Trim();

                gm.Settings.Database_Server = tfDbServer.Text.ToString().Trim();
                gm.Settings.Database_Schema = tfDbSchema.Text.ToString().Trim();
                gm.Settings.Database_Username = tfDbUsername.Text.ToString().Trim();
                gm.Settings.Database_Password = tfDbPassword.Text.ToString().Trim();

                int portResult;
                bool portParse = int.TryParse(tfDbPort.Text.ToString(), out portResult);
                gm.Settings.Database_Port = portParse ? portResult : gm.Settings.Database_Port;

                Marana.Settings.SaveConfig(gm.Settings);

                window.Add(dlgSaved);
            };

            window.Add(btnSave);

            Application.Refresh();
        }
    }
}