using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Marana {

    internal class API_AlphaVantage {

        public static bool Validate_Error(string json) {

            // Returns true if the returned JSON data has an "Error Message"
            return JObject.Parse(json)["Error Message"] != null;
        }

        public static bool Validate_ExceededCalls(string json) {

            // Returns true if the returned JSON data has an "Note"
            return JObject.Parse(json)["Note"] != null;
        }

        public static string GetData_TimeSeriesDaily(string apiKey, string symbol, bool fulldata = false) {
            HttpWebRequest request = WebRequest.Create(
                String.Format("https://www.alphavantage.co/query?function={0}&symbol={1}&outputsize={2}&apikey={3}",
                "TIME_SERIES_DAILY", symbol, (fulldata ? "full" : "compact"), apiKey)) as HttpWebRequest;
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

        public static List<DailyValue> ProcessData_TimeSeriesDaily(string rawData) {
            List<DailyValue> outList = new List<DailyValue>();

            foreach (JToken day in JObject.Parse(rawData)["Time Series (Daily)"].Children().ToList()) {
                DailyValue sd = new DailyValue();
                sd.Timestamp = DateTime.Parse((day as JProperty).Name);

                foreach (JToken item in day.Children().ToList()) {
                    sd.Open = item["1. open"].Value<double>();
                    sd.High = item["2. high"].Value<double>();
                    sd.Low = item["3. low"].Value<double>();
                    sd.Close = item["4. close"].Value<double>();
                    sd.Volume = item["5. volume"].Value<double>();
                }

                outList.Add(sd);
            }

            return outList;
        }
    }
}