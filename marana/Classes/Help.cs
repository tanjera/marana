using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Marana {

    public class Help {
        private Version version = Assembly.GetExecutingAssembly().GetName().Version;

        public string Header() {
            return $@"
Marana (v{version.Major}.{version.Minor}.{version.Build})

Market Analytics and Trading, by Tanjera
Warning: Use at your own risk. Please read and understand end-user license agreement before use
";
        }

        public void Default() {
            Prompt.Write(Header());
            Prompt.Write(
            @"
Command -line Usage:
Options:
    help                                Display this help menu

    execute                             Display the help menu for executing automated trades
    library                             Display the help menu for data library management
    test                                Display the help menu for simulation testing

    exit                                Exit Marana
");
        }

        public void Library() {
            Prompt.Write(Header());
            Prompt.Write(
                @"
    library erase                       Erase all market data from data library

    library info                        Display information about the data library

    library update                      Run library data update. Updates either all symbols or
                                        watchlist only, depending on configuration file.

    library update [start] [end]        Update all symbols from [start] to [end]
                                        [start]: (Optional) symbol to start at alphabetically
                                        [end]: (Optional) symbol to end at alphabetically

");
        }

        public void Test() {
            Prompt.Write(Header());
            Prompt.Write(
                @"
    test strategies                     Test all strategies for valid SQL syntax
                                        Interprets the following fields in the SQL queries:
                                        {SYMBOL} is interpreted to the symbol being queried
                                        {DATE} is interpreted to the current date

    test discrete <stra> <symb> <days> [date]
                                        Runs a discrete test ((1 strategy x 1 symbol) / days)
                                        Note: Accounts for weekends and holidays

    test parallel <stra> <symb> <days> [date] [$tot] [$per]
                                        Runs a parallel test ((1 strategy x all symbols) / days)
                                        Note: Accounts for weekends, not holidays

                                        Test arguments:
                                        <stra>: Strategies; 'all' or list in quotations
                                        <symb> Symbols; 'all', 'watchlist', or list in quotations
                                        <days>: # of days to test
                                        [date]: (Optional) date to end simulation (format yyyy-MM-dd).
                                                Default is today
                                        [$tot]: (Optional) total dollar limit to buying. Default is unlimited.
                                        [$per]: (Optional) dollar limit per asset purchase. Default is 1 share

");
        }

        public void Execute() {
            Prompt.Write(Header());
            Prompt.Write(
                @"
    execute all [date]                  Execute all automated trading instructions (live and paper)
                                        [n]: (Optional) date to look for signals (format yyyy-MM-dd).
                                             Default is today

    execute live [date]                 Execute automated trading instructions for live account

    execute paper [date]                Execute automated trading instructions for paper account
");
        }
    }
}