using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Main {
        private Marana.Library _Library = new Marana.Library();
        private GUI.Settings guiSettings = new Settings();
        private GUI.Library guiLibrary = new GUI.Library();
        private GUI.Strategies guiStrategies = new Strategies();

        public enum WindowTypes {
            Welcome,
            Settings,
            LibraryUpdate,
            LibraryInformation,
            LibraryEraseDatabase,
            StrategiesEdit,
            StrategiesRun,
            TradingLive,
            TradingPaper
        }

        private static string[] WindowTitles = new string[] {
            "Welcome to Marana",
            "Edit Settings",
            "Update the Data Library",
            "Data Library Information",
            "Erase Data Library's Database",
            "Edit Strategy Queries",
            "Run Strategy Queries",
            "Live Trading",
            "Paper Trading"
        };

        public async Task Init(Marana.Settings settings, Database db) {
            Application.Init();

            // Initialize all Windows in Application.Top.Subviews stack
            string[] wTypes = Enum.GetNames(typeof(WindowTypes));
            for (int i = 0; i < wTypes.Length; i++)
                Application.Top.Add(Utility.CreateWindow(wTypes[i], WindowTitles[i]));

            await Welcome(settings, db);

            Application.Run();
        }

        public async Task Welcome(Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(WindowTypes.Welcome);
            window.RemoveAll();

            MenuBar menuMain = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => {
                        Application.RequestStop ();
                    })
                }),

                new MenuBarItem ("_Settings", new MenuItem[] {
                    new MenuItem("_Edit Settings", "", async () => { await guiSettings.Edit(settings);  })
                    }),

                new MenuBarItem ("_Library", new MenuItem [] {
                    new MenuItem ("_Update", "", async () => { await guiLibrary.Update(_Library, settings, db); }),
                    new MenuItem ("_Information", "", async () => { await guiLibrary.Info(settings, db);  }),
                    new MenuItem(),
                    new MenuItem ("_Erase Database", "", async () => { await guiLibrary.Erase(settings, db);  })
                }),

                new MenuBarItem("St_rategies", new MenuItem[] {
                    new MenuItem ("_Edit Queries", "", async () => { await guiStrategies.Edit(settings, db); }),
                    new MenuItem ("_Run Queries", "", async () => { await guiStrategies.Run(settings, db); }),
                }),

                new MenuBarItem("_Trading", new MenuItem[] {
                    new MenuItem("_Live", "", () => { throw new NotImplementedException(); }),
                    new MenuItem("_Paper", "", () => { throw new NotImplementedException(); })
                })
            });

            Label lblWelcome = new Label(
                $"Welcome to Marana {Environment.NewLine}"
                + $"Market Analytics Tools, by Tanjera {Environment.NewLine}"
                + $"{Environment.NewLine}"
                + "Please select an option from the Menu") {
                X = Pos.Center(),
                Y = Pos.Center()
            };

            window.Add(lblWelcome);

            Application.Top.Add(menuMain);

            Application.Refresh();
        }
    }
}