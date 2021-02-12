using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Terminal.Gui;

namespace Marana {

    internal class Program {

        private static void Main(string[] args) {
            Settings settings = Settings.Init();
            Database db = new Database(settings);

            if (args.Length > 0)
                RunCommand(args, settings, db);
            else RunGUI(settings, db);
        }

        private static void RunGUI(Settings settings, Database db) {
            Marana.GUI.Main.Run(settings, db);
        }

        private static void RunCommand(string[] args, Settings settings, Database db) {
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
                            Library.Update(_args, settings, db);
                        }
                    }
                }

                // "help"
                else {
                    Help.Default();
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