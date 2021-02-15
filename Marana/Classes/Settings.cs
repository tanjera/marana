using System;
using System.IO;

namespace Marana {

    public class Settings {
        public string API_Alpaca_Live_Key { get; set; }
        public string API_Alpaca_Live_Secret { get; set; }

        public string API_Alpaca_Paper_Key { get; set; }
        public string API_Alpaca_Paper_Secret { get; set; }

        public string Directory_Working { get; set; }

        public string Database_Server { get; set; }
        public int Database_Port { get; set; }
        public string Database_Schema { get; set; }
        public string Database_Username { get; set; }
        public string Database_Password { get; set; }

        public int Entries_TSD { get; set; }

        public Settings() {
            Directory_Working = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");

            Database_Server = "localhost";
            Database_Port = 3306;
            Database_Schema = "Marana";

            Entries_TSD = 1000;
        }

        public static Settings Init() {
            CreateConfigDirectory();

            if (File.Exists(GetConfigPath()))
                return LoadConfig();
            else
                return new Settings();
        }

        public static bool Exists() {
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
                    sw.WriteLine($"API_Alpaca_Live_Key: {inc?.API_Alpaca_Live_Key?.Trim()}");
                    sw.WriteLine($"API_Alpaca_Live_Secret: {inc?.API_Alpaca_Live_Secret?.Trim()}");

                    sw.WriteLine($"API_Alpaca_Paper_Key: {inc?.API_Alpaca_Paper_Key?.Trim()}");
                    sw.WriteLine($"API_Alpaca_Paper_Secret: {inc?.API_Alpaca_Paper_Secret?.Trim()}");

                    sw.WriteLine($"Directory_Working: {inc?.Directory_Working?.Trim()}");

                    sw.WriteLine($"Database_Server: {inc?.Database_Server?.Trim()}");
                    sw.WriteLine($"Database_Port: {inc?.Database_Port.ToString().Trim()}");
                    sw.WriteLine($"Database_Schema: {inc?.Database_Schema?.Trim()}");
                    sw.WriteLine($"Database_User: {inc?.Database_Username?.Trim()}");
                    sw.WriteLine($"Database_Password: {inc?.Database_Password?.Trim()}");

                    sw.WriteLine($"Entries_TSD: {inc?.Entries_TSD.ToString().Trim()}");
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
                        case "API_Alpaca_Live_Key":
                            oc.API_Alpaca_Live_Key = value;
                            break;

                        case "API_Alpaca_Live_Secret":
                            oc.API_Alpaca_Live_Secret = value;
                            break;

                        case "API_Alpaca_Paper_Key":
                            oc.API_Alpaca_Paper_Key = value;
                            break;

                        case "API_Alpaca_Paper_Secret":
                            oc.API_Alpaca_Paper_Secret = value;
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
                            oc.Database_Username = value;
                            break;

                        case "Database_Password":
                            oc.Database_Password = value;
                            break;

                        case "Entries_TSD":
                            oc.Entries_TSD = int.Parse(value);
                            break;
                    }
                }
            }

            return oc;
        }
    }
}