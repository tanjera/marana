using System;
using System.IO;

namespace Marana {

    public class Settings {
        public string API_Alpaca_Key { get; set; }
        public string API_Alpaca_Secret { get; set; }

        public string Directory_Working { get; set; }

        public string Database_Server { get; set; }
        public int Database_Port { get; set; }
        public string Database_Schema { get; set; }
        public string Database_User { get; set; }
        public string Database_Password { get; set; }

        public string Directory_LibraryData {
            get { return Path.Combine(Directory_Working, "Data"); }
        }

        public Settings() {
            Directory_Working = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marana");

            Database_Server = "localhost";
            Database_Port = 3306;
            Database_Schema = "Marana";
        }
    }
}