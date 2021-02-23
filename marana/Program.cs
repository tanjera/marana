using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana {

    internal class Program {

        private static async Task Main(string[] args) {
            Settings settings = await Settings.Init();
            Database db = new Database(settings);

            if (args.Length > 0)
                await RunCommand(args, settings, db);
            else
                await RunGUI(settings, db);
        }

        private static async Task RunGUI(Settings settings, Database db) {
            await new Marana.GUI.Main().Init(settings, db);
        }

        private static async Task RunCommand(string[] args, Settings settings, Database db) {
            List<string> _args = new List<string>(args);

            // Validate the configuration file... if it doesn't exist, force entry into config edit mode
            if (!Settings.Exists()) {
                Prompt.WriteLine("No configuration file found. Please configure Marana!");
            }

            // Parse command options for program functionality
            if (_args.Count > 0) {
                string opt0 = TrimArgs(ref _args);

                // "library"
                if (opt0 == "library") {
                    if (_args.Count == 0) {
                        Help.Default();
                    } else {
                        string opt1 = TrimArgs(ref _args);

                        if (opt1 == "update") {
                            await new Library().Update(_args, settings, db);
                        }
                    }
                } else if (opt0 == "execute") {
                    if (_args.Count == 0) {
                        Help.Default();
                    } else {
                        string opt1 = TrimArgs(ref _args);

                        int priordays;      // For executing instructions to process signals from previous days
                        int.TryParse(TrimArgs(ref _args), out priordays);
                        DateTime day = DateTime.Today - new TimeSpan(Math.Abs(priordays), 0, 0, 0);

                        if (opt1 == "paper") {
                            await new Trading().RunAutomation(settings, db, Data.Format.Paper, day);
                        } else if (opt1 == "live") {
                            await new Trading().RunAutomation(settings, db, Data.Format.Live, day);
                        } else if (opt1 == "all") {
                            await new Trading().RunAutomation(settings, db, Data.Format.Live, day);
                            await new Trading().RunAutomation(settings, db, Data.Format.Paper, day);
                        }
                    }
                } else if (opt0 == "backtest") {
                    if (_args.Count == 0) {
                        Help.Default();
                    } else {
                        string opt1 = TrimArgs(ref _args);

                        if (opt1 == "paper" || opt1 == "live" || opt1 == "test" || opt1 == "all") {
                            int priordays;      // For executing instructions to process signals from previous days
                            int.TryParse(TrimArgs(ref _args), out priordays);

                            switch (opt1) {
                                case "paper":
                                    await new Backtest().RunBacktest(settings, db, Data.Format.Paper, priordays);
                                    break;

                                case "live":
                                    await new Backtest().RunBacktest(settings, db, Data.Format.Live, priordays);
                                    break;

                                case "test":
                                    await new Backtest().RunBacktest(settings, db, Data.Format.Test, priordays);
                                    break;

                                case "all":
                                    await new Backtest().RunBacktest(settings, db, Data.Format.Live, priordays);
                                    await new Backtest().RunBacktest(settings, db, Data.Format.Paper, priordays);
                                    await new Backtest().RunBacktest(settings, db, Data.Format.Test, priordays);
                                    break;
                            }
                        } else if (opt1 == "list") {
                            if (_args.Count < 4) {
                                Console.WriteLine("Insufficient arguments for 'backtest list <days> <strategy> <quantity> <symbols>'");
                                return;
                            }

                            int days, quantity;
                            if (!int.TryParse(_args[0], out days) || !int.TryParse(_args[2], out quantity)) {
                                Console.WriteLine("Invalid arguments for 'backtest list <days> <strategy> <quantity> <symbols>'");
                                return;
                            }

                            await new Backtest().RunBacktest(settings, db, days, _args[1], quantity, _args[3]);
                        }
                    }
                } else {
                    if (opt0 == "debug") {
                        // space for debugging individual methods
                    }

                    Help.Default();                     // "help"
                }
            }
        }

        public static string TrimArgs(ref List<string> args) {
            if (args.Count > 0) {
                string outarg = args[0].ToLower();
                args.RemoveAt(0);
                return outarg;
            } else {
                return "";
            }
        }
    }
}