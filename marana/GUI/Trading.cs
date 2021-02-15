using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana.GUI {

    internal class Trading {

        public async Task EnterAutomation(GUI.Main gm) {
            Window window = Utility.SelectWindow(Main.WindowTypes.TradingEnterAutomation);
            window.RemoveAll();

            Application.Refresh();
        }
    }
}