using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Marana
{
    public class Settings {

        public string APIKey_AlphaVantage { get; set; }
        public string Directory_Library { get; set; }
    }


    public static class Configuration {

        public static Settings Init() {
            CreateConfigDirectory ();

            if (File.Exists (GetConfigPath ()))
                return LoadConfig ();
            else
                return new Settings ();
        }

        private static string GetConfigDirectory () {
            return String.Format (@"{0}\Marana", Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData));
        }

        private static string GetConfigPath () {
            return String.Format (@"{0}\Marana\config.cfg", Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData));
        }

        private static DirectoryInfo CreateConfigDirectory () {
            if (!Directory.Exists(GetConfigDirectory()))
                return Directory.CreateDirectory(GetConfigDirectory());
            else
                return new DirectoryInfo (GetConfigDirectory ());
        }

        public static bool SaveConfig (Settings inc) {
            try {
                using (StreamWriter sw = new StreamWriter (GetConfigPath ())) {
                    sw.WriteLine (String.Format ("APIKey_AlphaVantage: {0}", inc.APIKey_AlphaVantage.Trim ()));
                    sw.WriteLine (String.Format ("Directory_Library: {0}", inc.Directory_Library.Trim ()));
                    sw.Close ();
                    return true;
                }
            } catch {
                return false;
            }
        }

        public static Settings LoadConfig () {
            Settings oc = new Settings ();
            using (StreamReader sr = new StreamReader (GetConfigPath())) {
                string [] lines = sr.ReadToEnd ().Split ('\n', '\r');

                foreach (string line in lines) {
                    if (line.Trim () == "" || line.IndexOf (':') == -1)
                        continue;

                    string key = line.Substring (0, line.IndexOf (':')),
                        value = line.Substring (line.IndexOf (':') + 1).Trim ();

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
