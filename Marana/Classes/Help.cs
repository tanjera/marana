using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Marana {

    public class Help {

        public static void Default() {
            Prompt.Write(
                @"
Marana: Market Analytics and Trading, by Tanjera
Warning: Use at your own risk. Please read and understand end-user license agreement before use.

Command-line Usage:
Options:
    help, library, execute              Output this help menu

    library update                      Run library data update (update all symbol data)
    library update [start] [end]        Run library data update from symbol [start] to symbol [end]

    execute all                         Execute all automated trading instructions (live and paper)
    execute live                        Execute automated trading instructions for live account
    execute paper                       Execute automated trading instructions for paper account
");
        }
    }
}