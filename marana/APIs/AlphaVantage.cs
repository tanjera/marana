using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;

namespace Marana.API {

    public class AlphaVantage {
        private Program Program;
        private Settings Settings => Program.Settings;

        public AlphaVantage(Program p) {
            Program = p;
        }

        public static async Task<bool> Validate_CSV(string text) {
            // Returns true if the returned CSV data starts with the proper headers
            return text.StartsWith("timestamp,open,high,low,close,adjusted_close,volume,dividend_amount,split_coefficient");
        }

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

        public async Task<object> GetData_Daily(Data.Asset asset, int limit = 500) {
            string output;
            output = await RequestData_Daily(asset.Symbol);

            if (output == "ERROR:INVALID"
                || output == "ERROR:INVALIDKEY"
                || output == "ERROR:EXCEEDEDCALLS"
                || output == "ERROR:EXCEPTION"
                || output.StartsWith("ERROR:WEBEXCEPTION:")) {
                return output;
            } else {
                return await ParseData_Daily(output, limit);
            }
        }

        public async Task CacheData_Daily(string symbol, string fpCache, string fpLockout, bool fullData = true) {
            string requestData = await RequestData_Daily(symbol, fullData);

            if (!Directory.Exists(Path.GetDirectoryName(fpCache)))
                Directory.CreateDirectory(Path.GetDirectoryName(fpCache));

            if (!Directory.Exists(Path.GetDirectoryName(fpLockout)))
                Directory.CreateDirectory(Path.GetDirectoryName(fpLockout));

            StreamWriter sw;

            sw = new StreamWriter(fpCache);
            await sw.WriteAsync(requestData);
            await sw.FlushAsync();
            sw.Close();
            await sw.DisposeAsync();

            sw = new StreamWriter(fpLockout);
            sw.Close();
            await sw.DisposeAsync();
        }

        private async Task<string> RequestData_Daily(string symbol, bool fullData = true) {
            try {
                HttpWebRequest request = WebRequest.Create(
                String.Format("https://www.alphavantage.co/query?function={0}&symbol={1}&outputsize={2}&datatype=csv&apikey={3}",
                "TIME_SERIES_DAILY_ADJUSTED", symbol, (fullData ? "full" : "compact"), Settings.API_AlphaVantage_Key)) as HttpWebRequest;
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string rte = await reader.ReadToEndAsync();
                response.Dispose();
                reader.Dispose();

                if (await Validate_ExceededCalls(rte)) {
                    return "ERROR:EXCEEDEDCALLS";
                } else if (await Validate_Error_Key(rte)) {
                    return "ERROR:INVALIDKEY";
                } else if (await Validate_Error(rte)) {
                    return "ERROR:INVALID";
                } else if (await Validate_CSV(rte)) {
                    return rte;
                } else {
                    return "ERROR:INVALID";
                }
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return "ERROR:EXCEPTION";
            }
        }

        public async Task<object> ParseData_Daily(string data, int maxrecords = -1) {
            Data.Daily dd = new Data.Daily();

            try {
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
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return null;
            }
        }
    }
}