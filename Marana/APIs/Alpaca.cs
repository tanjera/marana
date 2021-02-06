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

                List<Asset> output = new List<Asset>();
                foreach (var asset in filtered) {
                    output.Add(new Asset() {
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

        public static object GetData_TSD(Settings settings, Asset sp, int limit = 300) {
            return GetData_TSD_Async(settings, sp, limit).Result;
        }

        private static async Task<object> GetData_TSD_Async(Settings settings, Asset sp, int limit = 300) {
            try {
                var client = Environments.Paper.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Key, settings.API_Alpaca_Secret));

                // Maximum 1000 bars per API call
                var bars = await client.GetBarSetAsync(new BarSetRequest(sp.Symbol, TimeFrame.Day) { Limit = 300 });

                DatasetTSD ds = new DatasetTSD();
                foreach (var bar in bars[sp.Symbol]) {
                    if (bar.TimeUtc != null)
                        ds.TSDValues.Add(new TSDValue() {
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