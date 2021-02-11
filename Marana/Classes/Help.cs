using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Marana {

    public class Help {

        public static void Default() {
            Prompt.Write(
                @"
Marana: Market Analytics Tools, by Tanjera

Usage: Can be run from the command line (e.g. 'marana help') or from the prompt (e.g. 'help')
Options:
    exit                                Exit the program
    help                                Output this help menu
    config                              Shows current configuration settings
    config edit                         Edit configuration settings

    library                             Show options list for library functions

");
        }

        public static void Library() {
            Prompt.Write(
                @"
Marana: Market Analytics Tools, by Tanjera

Library Options:

    library info                        Show information about current library data
    library clear                       Erase all library data
    library update                      Run library data update (update all symbol data)
    library update [start] [end]        Run library data update from symbol [start] to symbol [end]
");
        }
    }
}