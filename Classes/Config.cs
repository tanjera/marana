using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Marana
{
    class Config {

        public static string LoadKey_APIAlphaVantage () {
            StreamReader sr = new StreamReader (@"config.txt");
            string[] lines = sr.ReadToEnd ().Split('\n', '\r');
            string[] ieKeys = (from line in lines where line.StartsWith ("APIKey_AlphaVantage: ") select line).ToArray();

            if (ieKeys.Length == 0)
                return null;
            else
                return ieKeys[0].Substring("APIKey_AlphaVantage: ".Length).Trim();
        }
    }
}
