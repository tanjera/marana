using System;
using System.Collections.Generic;

namespace Marana {

    internal class Entry {

        public static void Main(string[] args) {
            Program p = new Program();

            if (args.Length == 0) {
                while (true) {
                    args = Prompt.Args();
                    if (p.Run(new List<string>(args)) == Program.ReturnCode.Exit)
                        return;
                }
            } else {
                if (p.Run(new List<string>(args)) == Program.ReturnCode.Exit)
                    return;
            }
        }
    }

    public class Program {

        public enum ReturnCode {
            Okay,
            Exit
        }

        public Settings _Settings;
        public Database _Database;

        public string TrimArgs(ref List<string> args) {
            if (args.Count > 0) {
                string outarg = args[0].ToLower();
                args.RemoveAt(0);
                return outarg;
            } else {
                return "";
            }
        }

        public ReturnCode Run(List<string> args) {
            _Settings = Config.Init();
            _Database = new Database(_Settings);

            // Validate the configuration file... if it doesn't exist, force entry into config edit mode
            if (!Config.Validate()) {
                Prompt.WriteLine("No configuration file found. Entering 'config edit' mode.");
                args = new List<string> { "config", "edit" };
            }

            // Parse command options for program functionality
            if (args.Count == 0) {
                Prompt.WriteLine("Use command 'marana help' for information on using Marana");
            } else if (args.Count > 0) {
                string opt0 = TrimArgs(ref args);

                // "config"
                if (opt0 == "config") {
                    if (args.Count == 0) {                              // "config"
                        Config.Info(_Settings);
                    } else {
                        string opt1 = TrimArgs(ref args);

                        if (opt1 == "edit") {                           // "config edit"
                            Config.Edit(ref _Settings);
                        }
                    }
                }

                // "library"
                else if (opt0 == "library") {
                    if (args.Count == 0) {                             // "library"
                        Library.Info(_Database);
                    } else {
                        string opt1 = TrimArgs(ref args);
                        if (opt1 == "clear") {                          // "library clear"
                            Library.Clear(_Database);
                        } else if (opt1 == "update") {                  // "library update"
                            Library.Update(args, _Settings, _Database);
                        }
                    }
                }

                // "analyze"
                else if (opt0 == "analyze") {
                    Analysis.Running(args, _Settings);
                }

                // "help"
                else if (opt0 == "help") {
                    Help.Info();
                }

                // "exit"
                else if (opt0 == "exit") {
                    Prompt.WriteLine("Exiting");
                    return ReturnCode.Exit;
                }

                // Default or blank option
                else {
                    Prompt.WriteLine("Invalid command. Use command 'marana help' for information on using Marana");
                }
            }

            return ReturnCode.Okay;
        }
    }
}