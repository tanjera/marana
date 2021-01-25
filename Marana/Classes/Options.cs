using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Marana {

    public class Options {

        public static void Help() {
            Console.WriteLine(
                @"
Marana: Market Analytics Tools, by Tanjera

Usage: 'marana [option]
Options:
    help                        Output this help menu
    config                      Shows current configuration settings
    config edit                 Edit configuration settings

    library                     Run library update (update all symbols)
    library [start]             Run library update starting at symbol [start]
    library [start] [end]       Run library update from symbol [start] to symbol [end]

");
        }

        public static void Config_Show(Settings config) {
            Console.WriteLine();
            Console.WriteLine("Fixed Settings:");
            Console.WriteLine(String.Format("  Config Folder: {0}", Configuration.GetConfigDirectory()));
            Console.WriteLine();
            Console.WriteLine("Current Settings:");
            Console.WriteLine(String.Format("  Library Path: {0}", config.Directory_Library));
            Console.WriteLine(String.Format("  API Key -> Alpha Vantage: {0}", config.APIKey_AlphaVantage));
            Console.WriteLine();
            Console.WriteLine("To edit these settings, use the command 'marana config edit'");
        }

        public static Settings Config_Edit(Settings conf_in) {
            string input;
            Settings conf_out = new Settings();

            Console.WriteLine();

            string default_library = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");
            Console.Write(String.Format("Library Path [{0}]: ",
                conf_in.Directory_Library != null ? conf_in.Directory_Library : default_library));
            input = Console.ReadLine().Trim();
            conf_out.Directory_Library = !String.IsNullOrEmpty(input) ? input
                : (conf_in.Directory_Library != null ? conf_in.Directory_Library : default_library);

            Console.Write(String.Format("API Key -> Alpha Vantage [{0}]: ", conf_in.APIKey_AlphaVantage));
            input = Console.ReadLine().Trim();
            conf_out.APIKey_AlphaVantage = !String.IsNullOrEmpty(input) ? input
                : (conf_in.APIKey_AlphaVantage != null ? conf_in.APIKey_AlphaVantage : "");

            return conf_out;
        }

        public static void Library_Update(string[] args, Settings config) {
            Library.Init(config);

            Library.Update(args, config);
        }
    }
}