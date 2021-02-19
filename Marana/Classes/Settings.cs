﻿using System;
using System.IO;
using System.Threading.Tasks;

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

        public int Library_LimitDailyEntries { get; set; }
        public Option_DownloadSymbols Library_DownloadSymbols { get; set; }

        public enum Option_DownloadSymbols {
            All,
            Watchlist
        }

        public Settings() {
            Directory_Working = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");

            Database_Server = "localhost";
            Database_Port = 3306;
            Database_Schema = "Marana";

            Library_LimitDailyEntries = 500;
            Library_DownloadSymbols = Option_DownloadSymbols.Watchlist;
        }

        public static async Task<Settings> Init() {
            CreateConfigDirectory();

            if (File.Exists(GetConfigPath()))
                return await LoadConfig();
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

        public static async Task<bool> SaveConfig(Settings inc) {
            try {
                using (StreamWriter sw = new StreamWriter(GetConfigPath())) {
                    sw.WriteLine($"API_Alpaca_Live_Key: {inc?.API_Alpaca_Live_Key?.Trim()}");
                    sw.WriteLine($"API_Alpaca_Live_Secret: {inc?.API_Alpaca_Live_Secret?.Trim()}");
                    sw.WriteLine($"{Environment.NewLine}");
                    sw.WriteLine($"API_Alpaca_Paper_Key: {inc?.API_Alpaca_Paper_Key?.Trim()}");
                    sw.WriteLine($"API_Alpaca_Paper_Secret: {inc?.API_Alpaca_Paper_Secret?.Trim()}");
                    sw.WriteLine($"{Environment.NewLine}");
                    sw.WriteLine($"Directory_Working: {inc?.Directory_Working?.Trim()}");
                    sw.WriteLine($"{Environment.NewLine}");
                    sw.WriteLine($"Database_Server: {inc?.Database_Server?.Trim()}");
                    sw.WriteLine($"Database_Port: {inc?.Database_Port.ToString().Trim()}");
                    sw.WriteLine($"Database_Schema: {inc?.Database_Schema?.Trim()}");
                    sw.WriteLine($"Database_User: {inc?.Database_Username?.Trim()}");
                    sw.WriteLine($"Database_Password: {inc?.Database_Password?.Trim()}");
                    sw.WriteLine($"{Environment.NewLine}");
                    sw.WriteLine($"Library_DailyEntries: {inc?.Library_LimitDailyEntries.ToString().Trim()}");
                    sw.WriteLine($"Library_DownloadSymbols: {inc?.Library_DownloadSymbols.ToString()}");
                    sw.Close();
                    return true;
                }
            } catch (Exception ex) {
                await Error.Log("Database.cs, SaveConfig", ex.Message);
                return false;
            }
        }

        public static async Task<Settings> LoadConfig() {
            try {
                Settings oc = new Settings();
                using (StreamReader sr = new StreamReader(GetConfigPath())) {
                    string[] lines = sr.ReadToEnd().Split('\n', '\r');

                    foreach (string line in lines) {
                        if (line.Trim() == "" || line.IndexOf(':') == -1)
                            continue;

                        string key = line.Substring(0, line.IndexOf(':')),
                            value = line.Substring(line.IndexOf(':') + 1).Trim();

                        int resultInt;
                        object resultObject;
                        bool canParse = false;

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
                                canParse = int.TryParse(value, out resultInt);
                                oc.Database_Port = canParse ? resultInt : oc.Database_Port;
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

                            case "Library_DailyEntries":
                                canParse = int.TryParse(value, out resultInt);
                                oc.Library_LimitDailyEntries = canParse ? resultInt : oc.Library_LimitDailyEntries;
                                break;

                            case "Library_DownloadSymbols":
                                canParse = Enum.TryParse(typeof(Option_DownloadSymbols), value ?? "", out resultObject);
                                oc.Library_DownloadSymbols = canParse ? (Option_DownloadSymbols)resultObject : oc.Library_DownloadSymbols;
                                break;
                        }
                    }
                }

                return oc;
            } catch (Exception ex) {
                await Error.Log("Settings.cs, LoadConfig", ex.Message);
                return new Settings();
            }
        }
    }
}