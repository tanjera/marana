using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Marana {

    public class Help {

        public static void Default() {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            Prompt.Write(
                $@"
Marana (v{version.Major}.{version.Minor}.{version.Build})

Market Analytics and Trading, by Tanjera
Warning: Use at your own risk. Please read and understand end-user license agreement before use.

Command-line Usage:
Options:
    help, library, execute              Output this help menu

    library update                      Run library data update (update all symbol data)
    library update [start] [end]        Run library data update from symbol [start] to symbol [end]

    backtest list <days> <strategies> <quantity> <symbols>
                                        Runs a backtest for amount of trading <days>
                                        against <strategies> (list encapsulated by quotation marks)
                                        for <quantity> of shares,
                                        against <symbols> (list encapsulated by quotations)

    backtest all <n>                    Run backtest against all trading instructions (live, paper, test)
                                        <n>: (Required) Amount of trading days to test
    backtest test <n>                   Run backtest against test instructions
    backtest paper <n>                  Run backtest against paper instructions
    backtest live <n>                   Run backtest against live instructions

    execute all [n]                     Execute all automated trading instructions (live and paper)
                                        [n]: (Optional) Use signals from [n] days ago (e.g. 1 for yesterday)
    execute live [n]                    Execute automated trading instructions for live account
    execute paper [n]                   Execute automated trading instructions for paper account
");
        }
    }
}