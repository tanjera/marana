using System;

namespace Marana {

    internal class Entry {

        public static void Main(string[] args) {
            args = Prompts.Args();               // For debugging; able to enter args

            // Run main program, outside of static Main method
            Run r = new Run();
            r.Init(args);

            Console.WriteLine();
        }
    }

    public class Run {
        public Settings Config = new Settings();

        public void Init(string[] args) {
            Config = Configuration.Init();

            // Validate the configuration file... if it doesn't exist, force entry into config edit mode
            if (!Configuration.Validate()) {
                Console.WriteLine("No configuration file found. Entering 'config edit' mode.");
                args = new string[2] { "config", "edit" };
            }

            // Parse command options for program functionality
            if (args.Length == 0) {
                Console.WriteLine("Use command 'marana help' for information on using Marana");
            } else if (args.Length > 0) {                         // Option "config"
                if (args[0].ToLower() == "config") {
                    if (args.Length > 1 && args[1].ToLower() == "edit") {
                        Config = Options.Config_Edit(Config);
                        Configuration.SaveConfig(Config);
                    } else {
                        Options.Config_Show(Config);
                    }
                } else if (args[0].ToLower() == "library") {      // Option "library"
                    Options.Library_Update(args, Config);
                } else {                                          // Option "help"
                    Options.Help();
                }
            }

            Prompts.Key();
        }
    }
}