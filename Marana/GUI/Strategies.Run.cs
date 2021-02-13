using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal partial class Strategies {

        public async Task Run(Marana.Settings settings, Database db) {
            Window window = Utility.SelectWindow(Main.WindowTypes.StrategiesRun);
            window.RemoveAll();

            Application.Refresh();
        }
    }
}