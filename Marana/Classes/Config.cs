using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Marana
{
    class Configuration {

        public string APIKey_AlphaVantage { get; set; }
        public string FilePath_Aggregator { get; set; }
        public string Directory_Library { get; set; }

        public void Init() {
            CreateConfigDirectory ();

            if (File.Exists (GetConfigPath ()))
                LoadConfig ();
        }

        private string GetConfigDirectory () {
            return String.Format (@"{0}\Marana", Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData));
        }

        private string GetConfigPath () {
            return String.Format (@"{0}\Marana\config.cfg", Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData));
        }

        private DirectoryInfo CreateConfigDirectory () {
            if (!Directory.Exists(GetConfigDirectory()))
                return Directory.CreateDirectory(GetConfigDirectory());
            else
                return new DirectoryInfo (GetConfigDirectory ());
        }

        public void SaveConfig () {
            using (StreamWriter sw = new StreamWriter (GetConfigPath ())) {
                sw.WriteLine (String.Format ("APIKey_AlphaVantage: {0}", APIKey_AlphaVantage));
                sw.WriteLine (String.Format ("FilePath_Aggregator: {0}", FilePath_Aggregator));
                sw.WriteLine (String.Format ("Directory_Library: {0}", Directory_Library));
                sw.Close ();
            }
        }

        public void LoadConfig () {
            using (StreamReader sr = new StreamReader (GetConfigPath())) {
                string [] lines = sr.ReadToEnd ().Split ('\n', '\r');

                foreach (string line in lines) {
                    if (line.Trim () == "" || line.IndexOf (':') == -1)
                        continue;

                    string key = line.Substring (0, line.IndexOf (':'));

                    switch (key) {
                        default: break;
                        case "APIKey_AlphaVantage":
                            APIKey_AlphaVantage = line.Substring (line.IndexOf (':') + 1);
                            break;
                        case "FilePath_Aggregator":
                            FilePath_Aggregator = line.Substring (line.IndexOf (':') + 1);
                            break;
                        case "Directory_Library":
                            Directory_Library = line.Substring (line.IndexOf (':') + 1);
                            break;
                    }
                }
            }
        }
    }
}
