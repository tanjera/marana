using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;

namespace Marana.API {

    public class AlphaVantage {

        public static async Task<bool> Validate_Error(string text) {
            // Returns true if the returned JSON data (error data always in JSON format) has an "Error Message"
            return text.Contains("Error Message");
        }

        public static async Task<bool> Validate_Error_Key(string text) {
            // Returns true if the returned JSON data (error data always in JSON format) has this specific error message.
            return text.Contains("the parameter apikey is invalid or missing");
        }

        public static async Task<bool> Validate_ExceededCalls(string text) {
            // Returns true if the returned JSON data (error data always in JSON format) has a "Note" or Information
            // Indicative of too may API calls per minute/day (over limit)
            return text.Contains("Note") || text.Contains("Information");
        }

        public static async Task<object> GetData_Daily(Settings settings, Data.Asset asset, int limit = 500) {
            string output;
            output = await RequestData_Daily(settings.API_AlphaVantage_Key, asset.Symbol);

            if (output == "ERROR:INVALID" || output == "ERROR:INVALIDKEY"
                || output == "ERROR:EXCEEDEDCALLS") {
                return output;
            } else {
                return await ParseData_Daily(output, limit);
            }
        }

        private static async Task<string> RequestData_Daily(string apiKey, string symbol, bool fulldata = true) {
            HttpWebRequest request = WebRequest.Create(
            String.Format("https://www.alphavantage.co/query?function={0}&symbol={1}&outputsize={2}&datatype=csv&apikey={3}",
            "TIME_SERIES_DAILY_ADJUSTED", symbol, (fulldata ? "full" : "compact"), apiKey)) as HttpWebRequest;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string rte = reader.ReadToEnd();
            response.Dispose();
            reader.Dispose();

            if (await Validate_ExceededCalls(rte)) {
                return "ERROR:EXCEEDEDCALLS";
            } else if (await Validate_Error_Key(rte)) {
                return "ERROR:INVALIDKEY";
            } else if (await Validate_Error(rte)) {
                return "ERROR:INVALID";
            } else {
                return rte;
            }
        }

        private static async Task<object> ParseData_Daily(string data, int maxrecords = -1) {
            Data.Daily dd = new Data.Daily();

            using (StringReader sr = new StringReader(data)) {
                using (CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture)) {
                    csv.Read();
                    csv.ReadHeader();
                    while (maxrecords != 0 && csv.Read()) {
                        dd.Prices.Add(new Data.Daily.Price() {
                            Date = csv.GetField<DateTime>("timestamp"),
                            Open = csv.GetField<decimal>("open"),
                            High = csv.GetField<decimal>("high"),
                            Low = csv.GetField<decimal>("low"),
                            Close = csv.GetField<decimal>("adjusted_close"),
                            Volume = csv.GetField<decimal>("volume")
                        });

                        maxrecords--;
                    }
                }
            }

            return dd;
        }
    }
}