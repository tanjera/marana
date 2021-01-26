using System;
using System.IO;

namespace Marana {

    public class Settings {
        public string APIKey_AlphaVantage { get; set; }
        public string Directory_Library { get; set; }

        public string Directory_LibraryData {
            get { return Path.Combine(Directory_Library, "Data"); }
        }

        public string Directory_LibraryData_TSDA {
            get { return Path.Combine(Directory_LibraryData, "TSDA"); }
        }
    }
}