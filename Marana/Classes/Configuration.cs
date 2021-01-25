using System;
using System.IO;

namespace Marana {

    public class Settings {
        public string APIKey_AlphaVantage { get; set; }
        public string Directory_Library { get; set; }
    }

    public static class Configuration {

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