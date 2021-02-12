using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Main {

        public static void Run(Marana.Settings settings, Database db) {
            Application.Init();

            MenuBar menuMain = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => {
                        Application.RequestStop ();
                    })
                }),

                new MenuBarItem ("_Settings", new MenuItem [] {
                    new MenuItem ("_Edit", "", () => { GUI.Settings.Edit(settings);  })
                }),

                new MenuBarItem ("_Library", new MenuItem [] {
                    new MenuItem ("_Information", "", () => { GUI.Library.Info(settings, db);  }),
                    new MenuItem(),
                    new MenuItem ("_Erase Database", "", () => { GUI.Library.Erase(settings, db);  })
                })
            });

            Window wdwMain = Utility.SetWindow("Marana");

            Label lblWelcome = new Label(
                $"Welcome to Marana {Environment.NewLine}"
                + $"Market Analytics Tools, by Tanjera {Environment.NewLine}"
                + $"{Environment.NewLine}"
                + "Please select an option from the Menu") {
                X = Pos.Center(),
                Y = Pos.Center()
            };

            wdwMain.Add(lblWelcome);

            Application.Top.Add(menuMain, wdwMain);
            Application.Run();
        }
    }
}