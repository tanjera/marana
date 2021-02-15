using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Main {
        public Marana.Library Library = new Marana.Library();
        public Marana.Settings Settings = new Marana.Settings();
        public Marana.Database Database;

        private GUI.Settings guiSettings = new Settings();
        private GUI.Library guiLibrary = new Library();
        private GUI.Queries guiStrategies = new Queries();
        private GUI.Trading guiTrading = new Trading();

        public enum WindowTypes {
            Welcome,
            Settings,
            LibraryUpdate,
            LibraryInformation,
            LibraryEraseDatabase,
            QueriesRun,
            TradingEnterAutomation
        }

        private static string[] WindowTitles = new string[] {
            "Welcome to Marana",
            "Edit Settings",
            "Update the Data Library",
            "Data Library Information",
            "Erase Market Data from Library",
            "Test Strategy Queries",
            "Enter Automated Trade",
        };

        public async Task Init(Marana.Settings settings, Database db) {
            Application.Init();

            Settings = settings;
            Database = db;

            // Initialize all Windows in Application.Top.Subviews stack
            string[] wTypes = Enum.GetNames(typeof(WindowTypes));
            for (int i = 0; i < wTypes.Length; i++)
                Application.Top.Add(Utility.CreateWindow(wTypes[i], WindowTitles[i]));

            await Welcome();

            Application.Run();
        }

        public async Task Welcome() {
            Window window = Utility.SelectWindow(WindowTypes.Welcome);
            window.RemoveAll();

            MenuBar menuMain = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => {
                        Application.RequestStop ();
                    })
                }),

                new MenuBarItem ("_Settings", new MenuItem[] {
                    new MenuItem("_Edit Settings", "", new Action(SettingsEdit))
                    }),

                new MenuBarItem ("_Library", new MenuItem [] {
                    new MenuItem ("_Update", "", new Action(LibraryUpdate)),
                    new MenuItem ("_Information", "", new Action(LibraryInfo)),
                    new MenuItem(),
                    new MenuItem ("_Erase Market Data", "", new Action(LibraryErase)),
                }),

                new MenuBarItem("S_trategies", new MenuItem[] {
                    new MenuItem ("_Test Queries", "", new Action(StrategiesTest)),
                }),

                new MenuBarItem("Tra_ding", new MenuItem[] {
                    new MenuItem("_Enter Automated Trade", "", new Action(TradingEnterAutomation)),
                })
            });

            Label lblWelcome = new Label(
                $"Welcome to Marana{Environment.NewLine}"
                + $"Market Analytics Tools and Trading, by Tanjera{Environment.NewLine}"
                + $"{Environment.NewLine}{Environment.NewLine}"
                + $"This program is provided as-is without any warranty{Environment.NewLine}"
                + $"or liability for any losses you may incur.{Environment.NewLine}"
                + "Please read and understand the distribution license before use."
                ) {
                X = Pos.Center(), Y = Pos.Center(),
                TextAlignment = TextAlignment.Centered
            };

            window.Add(lblWelcome);

            Application.Top.Add(menuMain);

            Application.Refresh();
        }

        private async void LibraryUpdate()
            => await guiLibrary.Update(this);

        private async void LibraryInfo()
            => await guiLibrary.Info(this);

        private async void LibraryErase()
            => await guiLibrary.Erase(this);

        private async void SettingsEdit()
            => await guiSettings.Edit(this);

        private async void StrategiesTest()
            => await guiStrategies.Test(this);

        private async void TradingEnterAutomation()
            => await guiTrading.EnterAutomation(this);
    }
}