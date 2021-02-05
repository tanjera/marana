using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alpaca.Markets;

namespace Marana.API {

    public class Alpaca {

        public static object GetData_TSD(Settings settings, SymbolPair sp) {
            return GetData_TSD_Async(settings, sp).Result;
        }

        private static async Task<object> GetData_TSD_Async(Settings settings, SymbolPair sp) {
            DatasetTSD ds = new DatasetTSD();

            try {
                var client = Environments.Paper.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Key, settings.API_Alpaca_Secret));

                // Maximum 1000 bars per API call
                var bars = await client.GetBarSetAsync(new BarSetRequest(sp.Symbol, TimeFrame.Day) { Limit = 1000 });

                foreach (var bar in bars[sp.Symbol]) {
                    if (bar.TimeUtc != null)
                        ds.Values.Add(new DailyValue() {
                            Timestamp = bar.TimeUtc ?? new DateTime(),
                            Open = bar.Open,
                            High = bar.High,
                            Low = bar.Low,
                            Close = bar.Close,
                            Volume = bar.Volume
                        });
                }

                return ds;
            } catch (Exception ex) {
                return ex.Message;
            }
        }
    }
}