using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Marana {

    class API_AlphaVantage {

        public static string GetData_TimeSeriesDaily (string apiKey, string symbol, bool fulldata = false) {
            bool success = false;

            while (!success) {
                HttpWebRequest request = WebRequest.Create (
                    String.Format ("https://www.alphavantage.co/query?function={0}&symbol={1}&outputsize={2}&apikey={3}",
                    "TIME_SERIES_DAILY", symbol, (fulldata ? "full" : "compact"), apiKey)) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse () as HttpWebResponse;
                StreamReader reader = new StreamReader (response.GetResponseStream ());
                string rte = reader.ReadToEnd ();

                if (rte == "{\n    \"Information\": \"Thank you for using Alpha Vantage! Please visit https://www.alphavantage.co/premium/ if you would like to have a higher API call volume.\"\n}") {
                    // API calls per minute exceeded... wait a minute then repeat request
                    Console.WriteLine ("Exceeded API calls per minute... waiting 1 minute...");
                    System.Threading.Thread.Sleep (15000);
                } else {
                    response.Dispose ();
                    reader.Dispose ();
                    return rte;
                }
            }

            return null;
        }

        public static List<DailyValue> ProcessData_TimeSeriesDaily (string rawData) {
            List<DailyValue> outList = new List<DailyValue> ();

            foreach (JToken day in JObject.Parse (rawData) ["Time Series (Daily)"].Children ().ToList ()) {
                DailyValue sd = new DailyValue ();
                sd.Timestamp = DateTime.Parse ((day as JProperty).Name);

                foreach (JToken item in day.Children ().ToList ()) {
                    sd.Open = item ["1. open"].Value<double> ();
                    sd.High = item ["2. high"].Value<double> ();
                    sd.Low = item ["3. low"].Value<double> ();
                    sd.Close = item ["4. close"].Value<double> ();
                    sd.Volume = item ["5. volume"].Value<double> ();
                }

                outList.Add (sd);
            }

            return outList;
        }
    }
}
