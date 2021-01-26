using System;
using System.IO;

namespace Marana {

    public static class Config {

        public static void Info(Settings config) {
            Prompt.NewLine();
            Prompt.WriteLine("Fixed Settings:");
            Prompt.WriteLine(String.Format("  Config Folder: {0}", Config.GetConfigDirectory()));
            Prompt.NewLine();
            Prompt.WriteLine("Current Settings:");
            Prompt.WriteLine(String.Format("  Library Path: {0}", config.Directory_Library));
            Prompt.WriteLine(String.Format("  API Key -> Alpha Vantage: {0}", config.APIKey_AlphaVantage));
            Prompt.NewLine();
            Prompt.WriteLine("To edit these settings, use the command 'marana config edit'");
        }

        public static void Edit(ref Settings settings) {
            string input;

            Prompt.NewLine();

            string default_library = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");
            Prompt.Write(String.Format("Library Path [{0}]: ",
                settings.Directory_Library != null ? settings.Directory_Library : default_library));
            input = Console.ReadLine().Trim();
            settings.Directory_Library = !String.IsNullOrEmpty(input) ? input
                : (settings.Directory_Library != null ? settings.Directory_Library : default_library);

            Prompt.Write(String.Format("API Key -> Alpha Vantage [{0}]: ", settings.APIKey_AlphaVantage));
            input = Console.ReadLine().Trim();
            settings.APIKey_AlphaVantage = !String.IsNullOrEmpty(input) ? input
                : (settings.APIKey_AlphaVantage != null ? settings.APIKey_AlphaVantage : "");

            SaveConfig(settings);
        }

        public static Settings Init() {
            CreateConfigDirectory();

            if (File.Exists(GetConfigPath()))
                return LoadConfig();
            else
                return new Settings();
        }

        public static bool Validate() {
            if (!File.Exists(GetConfigPath()))
                return false;

            return true;
        }

        public static string GetConfigDirectory() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Marana");
        }

        public static string GetConfigPath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Marana", "config.cfg");
        }

        private static DirectoryInfo CreateConfigDirectory() {
            if (!Directory.Exists(GetConfigDirectory()))
                return Directory.CreateDirectory(GetConfigDirectory());
            else
                return new DirectoryInfo(GetConfigDirectory());
        }

        public static bool SaveConfig(Settings inc) {
            try {
                using (StreamWriter sw = new StreamWriter(GetConfigPath())) {
                    sw.WriteLine(String.Format("APIKey_AlphaVantage: {0}", inc.APIKey_AlphaVantage.Trim()));
                    sw.WriteLine(String.Format("Directory_Library: {0}", inc.Directory_Library.Trim()));
                    sw.Close();
                    return true;
                }
            } catch {
                return false;
            }
        }

        public static Settings LoadConfig() {
            Settings oc = new Settings();
            using (StreamReader sr = new StreamReader(GetConfigPath())) {
                string[] lines = sr.ReadToEnd().Split('\n', '\r');

                foreach (string line in lines) {
                    if (line.Trim() == "" || line.IndexOf(':') == -1)
                        continue;

                    string key = line.Substring(0, line.IndexOf(':')),
                        value = line.Substring(line.IndexOf(':') + 1).Trim();

                    switch (key) {
                        default: break;
                        case "APIKey_AlphaVantage":
                            oc.APIKey_AlphaVantage = value;
                            break;

                        case "Directory_Library":
                            oc.Directory_Library = value;
                            break;
                    }
                }
            }

            return oc;
        }
    }
}