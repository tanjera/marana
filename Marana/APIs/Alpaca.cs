using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alpaca.Markets;

namespace Marana.API {

    public class Alpaca {

        public static object GetAssets(Settings settings) {
            return GetAssets_Async(settings).Result;
        }

        private static async Task<object> GetAssets_Async(Settings settings) {
            try {
                var client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Key, settings.API_Alpaca_Secret));
                var assets = await client.ListAssetsAsync(new AssetsRequest { AssetStatus = AssetStatus.Active });
                var filtered = assets.Where(asset => asset.IsTradable && (asset.Exchange == Exchange.Nasdaq || asset.Exchange == Exchange.Nyse));

                List<Data.Asset> output = new List<Data.Asset>();
                foreach (var asset in filtered) {
                    output.Add(new Data.Asset() {
                        ID = asset.AssetId.ToString(),
                        Class = asset.Class.ToString(),
                        Exchange = asset.Exchange.ToString(),
                        Symbol = asset.Symbol,
                        Status = asset.Status.ToString(),
                        Tradeable = asset.IsTradable,
                        Marginable = asset.Marginable,
                        Shortable = asset.Shortable,
                        EasyToBorrow = asset.EasyToBorrow
                    });
                }
                return output;
            } catch (Exception ex) {
                return ex.Message;
            }
        }

        public static async Task<object> GetData_TSD(Settings settings, Data.Asset sp, int limit = 300) {
            try {
                var client = Environments.Paper.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Key, settings.API_Alpaca_Secret));

                // Maximum 1000 bars per API call
                var bars = await client.GetBarSetAsync(new BarSetRequest(sp.Symbol, TimeFrame.Day) { Limit = limit });

                Data.Daily ds = new Data.Daily();
                foreach (var bar in bars[sp.Symbol]) {
                    if (bar.TimeUtc != null)
                        ds.Prices.Add(new Data.Daily.Price() {
                            Date = bar.TimeUtc ?? new DateTime(),
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

        public static object GetTime_LastMarketClose(Settings settings) {
            try {
                var client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Key, settings.API_Alpaca_Secret));
                var calendars = client.ListCalendarAsync(new CalendarRequest().SetInclusiveTimeInterval(DateTime.UtcNow - new TimeSpan(30, 0, 0, 0), DateTime.UtcNow)).Result;
                return calendars.Where(c => c.TradingCloseTimeUtc.CompareTo(DateTime.UtcNow) <= 0).Last().TradingCloseTimeUtc;
            } catch (Exception ex) {
                return ex.Message;
            }
        }
    }
}