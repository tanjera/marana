using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marana {

    internal class Program {

        private static void Main(string[] args) {
            List<string> _args = new List<string>(args);

            while (true) {
                if (_args.Count == 0)
                    _args = Prompt.Args();

                Settings _Settings = Config.Init();
                Database _Database = new Database(_Settings);

                // Validate the configuration file... if it doesn't exist, force entry into config edit mode
                if (!Config.Validate()) {
                    Prompt.WriteLine("No configuration file found. Entering 'config edit' mode.");
                    _args = new List<string> { "config", "edit" };
                }

                // Parse command options for program functionality
                if (_args.Count == 0) {
                    Prompt.WriteLine("Use command 'marana help' for information on using Marana");
                } else if (_args.Count > 0) {
                    string opt0 = TrimArgs(ref _args);

                    // "config"
                    if (opt0 == "config") {
                        if (_args.Count == 0) {                              // "config"
                            Config.Info(_Settings);
                        } else {
                            string opt1 = TrimArgs(ref _args);

                            if (opt1 == "edit") {                           // "config edit"
                                Config.Edit(ref _Settings);
                            }
                        }
                    }

                    // "library"
                    else if (opt0 == "library") {
                        if (_args.Count == 0) {                             // "library"
                            Library.Info(_Database);
                        } else {
                            string opt1 = TrimArgs(ref _args);
                            if (opt1 == "clear") {                          // "library clear"
                                Library.Clear(_Database);
                            } else if (opt1 == "update") {                  // "library update"
                                Library.Update(_args, _Settings, _Database);
                            }
                        }
                    }

                    // "analyze"
                    else if (opt0 == "analyze") {
                        string opt1 = TrimArgs(ref _args);
                        if (opt1 == "daily") {
                            Analysis.Daily(_args, _Database, _Settings);
                        } else if (opt1 == "insert") {
                            string opt2 = TrimArgs(ref _args);
                            if (opt2 == "long") {
                                Analysis.Insert_Long(_args, _Database, _Settings);
                            } else if (opt2 == "short") {
                                Analysis.Insert_Short(_args, _Database, _Settings);
                            }
                        }
                    }

                    // "help"
                    else if (opt0 == "help") {
                        Help.Info();
                    }

                    // "exit"
                    else if (opt0 == "exit") {
                        Prompt.WriteLine("Exiting");
                        return;
                    }

                    // Default or blank option
                    else {
                        Prompt.WriteLine("Invalid command. Use command 'marana help' for information on using Marana");
                    }
                }

                _args.Clear();
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