using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Main {
        public Marana.Library _Library = new Marana.Library();

        public enum WindowTypes {
            Welcome,
            Settings,
            LibraryUpdate,
            LibraryInformation,
            LibraryEraseDatabase,
            Screen,
            TradeLive,
            TradePaper
        }

        private static string[] WindowTitles = new string[] {
            "Welcome to Marana",
            "Edit Settings",
            "Update the Data Library",
            "Data Library Information",
            "Erase Data Library's Database",
            "Asset Screening",
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

                new MenuBarItem ("_Edit Settings", "", () => { GUI.Settings.Edit(settings);  }),

                new MenuBarItem ("_Library", new MenuItem [] {
                    new MenuItem ("_Update", "", () => { new GUI.Library().Update(_Library, settings, db); }),
                    new MenuItem ("_Information", "", () => { GUI.Library.Info(settings, db);  }),
                    new MenuItem(),
                    new MenuItem ("_Erase Database", "", () => { GUI.Library.Erase(settings, db);  })
                }),

                new MenuBarItem("_Screen", new MenuItem[] { }),

                new MenuBarItem("_Trade", new MenuItem[] {
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