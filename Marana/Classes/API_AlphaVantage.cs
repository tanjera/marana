using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;

namespace Marana {

    internal class API_AlphaVantage {

        public static bool Validate_Error(string text) {

            // Returns true if the returned JSON data has an "Error Message"
            return text.Contains("Error Message");
        }

        public static bool Validate_ExceededCalls(string text) {
            return text.Contains("Note") || text.Contains("Information");
        }

        public static string GetData_TimeSeriesDailyAdjusted(string apiKey, string symbol, bool fulldata = false) {
            HttpWebRequest request = WebRequest.Create(
                String.Format("https://www.alphavantage.co/query?function={0}&symbol={1}&outputsize={2}&datatype=csv&apikey={3}",
                "TIME_SERIES_DAILY_ADJUSTED", symbol, (fulldata ? "full" : "compact"), apiKey)) as HttpWebRequest;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string rte = reader.ReadToEnd();
            response.Dispose();
            reader.Dispose();

            if (Validate_ExceededCalls(rte)) {
                return "ERROR:EXCEEDEDCALLS";
            } else if (Validate_Error(rte)) {
                return "ERROR:INVALID";
            } else {
                return rte;
            }
        }
    }
}