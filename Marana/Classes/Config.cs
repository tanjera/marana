using System;
using System.IO;
using System.Security.Cryptography;

namespace Marana {

    public static class Config {

        public static void Info(Settings config) {
            Prompt.NewLine();
            Prompt.WriteLine("Fixed Settings:");
            Prompt.WriteLine(String.Format("  Config Folder: {0}", Config.GetConfigDirectory()));
            Prompt.NewLine();
            Prompt.WriteLine("Current Settings:");
            Prompt.WriteLine(String.Format("  Working Directory Path: {0}", config.Directory_Working));
            Prompt.WriteLine(String.Format("  API Key -> Alpha Vantage: {0}", config.APIKey_AlphaVantage));
            Prompt.NewLine();
            Prompt.WriteLine(String.Format("  Database -> Server: {0}", config.Database_Server));
            Prompt.WriteLine(String.Format("  Database -> Port: {0}", config.Database_Port));
            Prompt.WriteLine(String.Format("  Database -> Name: {0}", config.Database_Schema));
            Prompt.WriteLine(String.Format("  Database -> User: {0}", config.Database_User));
            Prompt.NewLine();
            Prompt.WriteLine("To edit these settings, use the command 'marana config edit'");
        }

        public static void Edit(ref Settings settings) {
            string input;

            Prompt.NewLine();

            Prompt.Write(String.Format("Library Path [{0}]: ", settings.Directory_Working));
            input = Console.ReadLine().Trim();
            settings.Directory_Working = !String.IsNullOrEmpty(input) ? input
                : (settings.Directory_Working != null ? settings.Directory_Working : settings.Directory_Working);

            Prompt.Write(String.Format("API Key -> Alpha Vantage [{0}]: ", settings.APIKey_AlphaVantage));
            input = Console.ReadLine().Trim();
            settings.APIKey_AlphaVantage = !String.IsNullOrEmpty(input) ? input
                : (settings.APIKey_AlphaVantage != null ? settings.APIKey_AlphaVantage : "");

            Prompt.Write(String.Format("Database -> Server [{0}]: ", settings.Database_Server));
            input = Console.ReadLine().Trim();
            settings.Database_Server = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_Server != null ? settings.Database_Server : "");

            Prompt.Write(String.Format("Database -> Port [{0}]: ", settings.Database_Port));
            int result;
            if (int.TryParse(Console.ReadLine().Trim(), out result))
                settings.Database_Port = result;

            Prompt.Write(String.Format("Database -> Name [{0}]: ", settings.Database_Schema));
            input = Console.ReadLine().Trim();
            settings.Database_Schema = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_Schema != null ? settings.Database_Schema : "");

            Prompt.WriteLine("Note: Please do NOT use your regular username/password when setting database access.", ConsoleColor.Red);
            Prompt.WriteLine("This password will be stored in the Marana config file in the config folder.", ConsoleColor.Red);

            Prompt.Write(String.Format("Database -> User [{0}]: ", settings.Database_User));
            input = Console.ReadLine().Trim();
            settings.Database_User = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_User != null ? settings.Database_User : "");

            Prompt.Write(String.Format("Database -> Password [{0}]: ", settings.Database_Password));
            input = Console.ReadLine().Trim();
            settings.Database_Password = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_Password != null ? settings.Database_Password : "");

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
                    sw.WriteLine(String.Format("Directory_Library: {0}", inc.Directory_Working.Trim()));

                    sw.WriteLine(String.Format("Database_Server: {0}", inc.Database_Server.Trim()));
                    sw.WriteLine(String.Format("Database_Port: {0}", inc.Database_Port.ToString().Trim()));
                    sw.WriteLine(String.Format("Database_Name: {0}", inc.Database_Schema.Trim()));
                    sw.WriteLine(String.Format("Database_User: {0}", inc.Database_User.Trim()));
                    sw.WriteLine(String.Format("Database_Password: {0}", inc.Database_Password.Trim()));
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
                            oc.Directory_Working = value;
                            break;

                        case "Database_Server":
                            oc.Database_Server = value;
                            break;

                        case "Database_Port":
                            oc.Database_Port = int.Parse(value);
                            break;

                        case "Database_Name":
                            oc.Database_Schema = value;
                            break;

                        case "Database_User":
                            oc.Database_User = value;
                            break;

                        case "Database_Password":
                            oc.Database_Password = value;
                            break;
                    }
                }
            }

            return oc;
        }
    }
}