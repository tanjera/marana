using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marana {

    public class Program {
        public Settings Settings;
        public Database Database;

        public Help Help;
        public Library Library;
        public Strategy Strategy;
        public Trading Trading;
        public Testing Test;

        public API.Alpaca Alpaca;
        public API.AlphaVantage AlphaVantage;

        private static async Task Main(string[] args) {
            // Init Settings and Database
            Program Instance = new Program();
            await Instance.Init();

            List<string> _args = new List<string>(args);

            if (args.Length > 0) {              // Run single command then exit
                await Instance.RunCommand(_args);
                return;
            } else {                            // Enter command-line loop
                bool toLoop = true;
                while (toLoop) {
                    _args = Prompt.Args();
                    toLoop = await Instance.RunCommand(_args);
                }
            }
        }

        private async Task Init() {
            Settings = await Settings.Init();
            Database = new Database(Settings);

            Help = new Help();
            Library = new Library(this);
            Strategy = new Strategy(this);
            Trading = new Trading(this);
            Test = new Testing(this);

            Alpaca = new API.Alpaca(this);
            AlphaVantage = new API.AlphaVantage(this);
        }

        private async Task<bool> RunCommand(List<string> args) {
            // Validate the configuration file... if it doesn't exist, force entry into config edit mode
            if (!Settings.Exists()) {
                await Settings.SaveConfig(Settings);
                Prompt.WriteLine($"No configuration file found. Creating blank config file.\n");
                Prompt.WriteLine($"Please configure Marana by editing the file:");
                Prompt.WriteLine($"{Settings.GetConfigPath()}");
                return false;
            }

            // Consolidate args list based on quoted items (e.g. "AAPL, MSFT"
            for (int i = args.Count - 1; i > 0; i--) {
                if (args[i].EndsWith('\"') && !args[i].StartsWith('\"')) {
                    args[i - 1] = $"{args[i - 1]} {args[i]}";
                    args.RemoveAt(i);
                } else if (args[i].EndsWith('\"') && args[i].StartsWith('\"')) {
                    args[i] = args[i].Replace('\"', ' ').Trim();
                }
            }

            // Parse command options for program functionality

            // "library"
            if (args.Count > 0 && args[0] == "library") {
                if (args.Count > 1 && args[1] == "update") {
                    args.RemoveRange(0, 2);
                    await Library.Update(args);
                } else if (args.Count > 1 && args[1] == "erase") {
                    await Library.Erase();
                } else if (args.Count > 1 && args[1] == "info") {
                    await Library.GetInfo();
                } else {
                    Help.Library();
                }
            } else if (args.Count > 0 && args[0] == "execute") {
                if (args.Count > 1) {
                    DateTime day;
                    if (args.Count > 2) {
                        if (!DateTime.TryParse(args[2], out day)) {
                            Prompt.WriteLine("Invalid date provided. Unable to parse.");
                            return true;
                        }
                    } else {
                        day = DateTime.Today;
                    }

                    if (args[1] == "paper") {
                        await Trading.RunAutomation(Data.Format.Paper, day);
                    } else if (args[1] == "live") {
                        await Trading.RunAutomation(Data.Format.Live, day);
                    } else if (args[1] == "all") {
                        await Trading.RunAutomation(Data.Format.Live, day);
                        await Trading.RunAutomation(Data.Format.Paper, day);
                    }
                } else {
                    Help.Execute();
                }
            } else if (args.Count > 0 && args[0] == "test") {
                if (args.Count > 1 && args[1] == "strategies") {
                    await Strategy.Validate();
                } else if (args.Count > 1 && (args[1] == "discrete" || args[1] == "parallel")) {
                    if (args.Count < 5) {
                        Prompt.WriteLine("Insufficient arguments.");
                        return true;
                    }

                    // Process command line arguments
                    string strategies = args[2];
                    string symbols = args[3];

                    int days;
                    if (!int.TryParse(args[4], out days)) {
                        Prompt.WriteLine("Invalid arguments.");
                        return true;
                    }

                    DateTime ending = DateTime.Today;
                    if (args.Count > 5 && !DateTime.TryParse(args[5], out ending)) {
                        Prompt.WriteLine("Invalid ending date provided. Unable to parse.");
                        return true;
                    }

                    decimal dollars = 0m;
                    if (args.Count > 6 && !decimal.TryParse(args[6], out dollars)) {
                        Prompt.WriteLine("Invalid dollar limit provided. Unable to parse.");
                        return true;
                    }

                    decimal dollarsPer = 0m;
                    if (args.Count > 7 && !decimal.TryParse(args[7], out dollarsPer)) {
                        Prompt.WriteLine("Invalid dollar per stock limit provided. Unable to parse.");
                        return true;
                    }

                    switch (args[1]) {
                        default: break;
                        case "discrete": await Test.RunDiscrete(strategies, symbols, days, ending); break;
                        case "parallel": await Test.RunParallel(strategies, symbols, days, ending, dollars, dollarsPer); break;
                    }
                } else {
                    Help.Test();
                }
            } else if (args.Count > 0 && args[0] == "exit") {
                return false;
                //} else if (args.Count > 0 && args[0] == "debug") {
            } else {
                Help.Default();                     // "help"
            }

            return true;
        }
    }
}