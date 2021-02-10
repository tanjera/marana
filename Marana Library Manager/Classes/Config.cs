using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Marana.Library {

    public static class Config {

        public static void Info(Settings config) {
            Prompt.NewLine();
            Prompt.WriteLine("Fixed Settings:");
            Prompt.WriteLine($"  Config Folder: {Config.GetConfigDirectory()}");
            Prompt.NewLine();
            Prompt.WriteLine("Current Settings:");
            Prompt.WriteLine($"  Working Directory Path: {config.Directory_Working}");
            Prompt.WriteLine($"  API -> Alpaca -> Key: {config.API_Alpaca_Key}");
            Prompt.WriteLine($"  API -> Alpaca -> Secret: {config.API_Alpaca_Secret}");
            Prompt.NewLine();
            Prompt.WriteLine($"  Database -> Server: {config.Database_Server}");
            Prompt.WriteLine($"  Database -> Port: {config.Database_Port}");
            Prompt.WriteLine($"  Database -> Name: {config.Database_Schema}");
            Prompt.WriteLine($"  Database -> User: {config.Database_User}");
            Prompt.NewLine();
            Prompt.WriteLine("To edit these settings, use the command 'marana config edit'");
        }

        public static void Edit(ref Settings settings) {
            string input;

            Prompt.NewLine();

            Prompt.Write($"Library Path [{settings.Directory_Working}]: ");
            input = Console.ReadLine().Trim();
            settings.Directory_Working = !String.IsNullOrEmpty(input) ? input
                : (settings.Directory_Working != null ? settings.Directory_Working : settings.Directory_Working);

            Prompt.Write($"API -> Alpaca -> Key [{settings.API_Alpaca_Key}]: ");
            input = Console.ReadLine().Trim();
            settings.API_Alpaca_Key = !String.IsNullOrEmpty(input) ? input
                : (settings.API_Alpaca_Key != null ? settings.API_Alpaca_Key : "");

            Prompt.Write($"API -> Alpaca -> Secret [{settings.API_Alpaca_Secret}]: ");
            input = Console.ReadLine().Trim();
            settings.API_Alpaca_Secret = !String.IsNullOrEmpty(input) ? input
                : (settings.API_Alpaca_Secret != null ? settings.API_Alpaca_Secret : "");

            Prompt.Write($"Database -> Server [{settings.Database_Server}]: ");
            input = Console.ReadLine().Trim();
            settings.Database_Server = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_Server != null ? settings.Database_Server : "");

            Prompt.Write($"Database -> Port [{settings.Database_Port}]: ");
            int result;
            if (int.TryParse(Console.ReadLine().Trim(), out result))
                settings.Database_Port = result;

            Prompt.Write($"Database -> Name [{settings.Database_Schema}]: ");
            input = Console.ReadLine().Trim();
            settings.Database_Schema = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_Schema != null ? settings.Database_Schema : "");

            Prompt.WriteLine("Note: Please do NOT use your regular username/password when setting database access.", ConsoleColor.Red);
            Prompt.WriteLine("This password will be stored in the Marana config file in the config folder.", ConsoleColor.Red);

            Prompt.Write($"Database -> User [{settings.Database_User}]: ");
            input = Console.ReadLine().Trim();
            settings.Database_User = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_User != null ? settings.Database_User : "");

            Prompt.Write($"Database -> Password [{settings.Database_Password}]: ");
            input = Console.ReadLine().Trim();
            settings.Database_Password = !String.IsNullOrEmpty(input) ? input
                : (settings.Database_Password != null ? settings.Database_Password : "");

            if (SaveConfig(settings))
                Prompt.WriteLine("Settings saved successfully.");
            else
                Prompt.WriteLine("Unable to save settings! Filesystem error?", ConsoleColor.Red);
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
                    sw.WriteLine($"API_Alpaca_Key: {inc.API_Alpaca_Key.Trim()}");
                    sw.WriteLine($"API_Alpaca_Secret: {inc.API_Alpaca_Secret.Trim()}");
                    sw.WriteLine($"Directory_Working: {inc.Directory_Working.Trim()}");

                    sw.WriteLine($"Database_Server: {inc.Database_Server.Trim()}");
                    sw.WriteLine($"Database_Port: {inc.Database_Port.ToString().Trim()}");
                    sw.WriteLine($"Database_Schema: {inc.Database_Schema.Trim()}");
                    sw.WriteLine($"Database_User: {inc.Database_User.Trim()}");
                    sw.WriteLine($"Database_Password: {inc.Database_Password.Trim()}");
                    sw.Close();
                    return true;
                }
            } catch (Exception ex) {
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
                        case "API_Alpaca_Key":
                            oc.API_Alpaca_Key = value;
                            break;

                        case "API_Alpaca_Secret":
                            oc.API_Alpaca_Secret = value;
                            break;

                        case "Directory_Working":
                            oc.Directory_Working = value;
                            break;

                        case "Database_Server":
                            oc.Database_Server = value;
                            break;

                        case "Database_Port":
                            oc.Database_Port = int.Parse(value);
                            break;

                        case "Database_Schema":
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