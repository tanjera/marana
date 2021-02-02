using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

using CsvHelper;

namespace Marana.API {

    public class AlphaVantage {

        public static bool Validate_Error(string text) {
            // Returns true if the returned JSON data (error data always in JSON format) has an "Error Message"
            return text.Contains("Error Message");
        }

        public static bool Validate_ExceededCalls(string text) {
            // Returns true if the returned JSON data (error data always in JSON format) has a "Note" or Information
            // Indicative of too may API calls per minute/day (over limit)
            return text.Contains("Note") || text.Contains("Information");
        }

        public static string GetData_TSDA(string apiKey, string symbol, bool fulldata = true) {
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

        public static DatasetTSDA ParseData_TSDA(string filepath, int maxrecords = -1) {
            DatasetTSDA ds = new DatasetTSDA();

            using (StreamReader sr = new StreamReader(filepath)) {
                using (CsvReader csv = new CsvReader(sr, CultureInfo.InvariantCulture)) {
                    csv.Read();
                    csv.ReadHeader();
                    while (maxrecords != 0 && csv.Read()) {
                        ds.Values.Add(new DailyValue() {
                            Timestamp = csv.GetField<DateTime>("timestamp"),
                            Open = csv.GetField<decimal>("open"),
                            High = csv.GetField<decimal>("high"),
                            Low = csv.GetField<decimal>("low"),
                            Close = csv.GetField<decimal>("close"),
                            AdjustedClose = csv.GetField<decimal>("adjusted_close"),
                            Volume = csv.GetField<decimal>("volume"),
                            Dividend_Amount = csv.GetField<decimal>("dividend_amount"),
                            Split_Coefficient = csv.GetField<decimal>("split_coefficient")
                        });

                        maxrecords--;
                    }
                }
            }

            // Chronological order, oldest to newest
            ds.Values.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            return ds;
        }
    }
}