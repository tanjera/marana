using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alpaca.Markets;

namespace Marana.API {

    public class Alpaca {

        public static async Task<object> GetAssets(Settings settings) {
            try {
                var client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                var assets = await client.ListAssetsAsync(new AssetsRequest { AssetStatus = AssetStatus.Active });

                List<Data.Asset> output = new List<Data.Asset>();
                foreach (var asset in assets) {
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

        public static async Task<object> GetData_Daily(Settings settings, Data.Asset asset, int limit = 500) {
            try {
                var client = Environments.Live.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                var poly = Environments.Live.GetPolygonDataClient(settings.API_Alpaca_Live_Key);

                // Maximum 1000 bars per API call
                var bars = await client.GetBarSetAsync(new BarSetRequest(asset.Symbol, TimeFrame.Day) { Limit = limit });

                Data.Daily ds = new Data.Daily();
                foreach (var bar in bars[asset.Symbol]) {
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

        public static async Task<object> GetPositions(Settings settings, Data.Format format) {
            IAlpacaTradingClient client = null;

            if (format == Data.Format.Live) {
                client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
            } else if (format == Data.Format.Paper) {
                client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
            }

            if (client == null)
                return false;

            var positions = await client.ListPositionsAsync();

            return positions.Select(p => new Data.Position() {
                ID = p.AssetId.ToString(),
                Symbol = p.Symbol,
                Quantity = p.Quantity
            }).ToList();
        }

        public static async Task<object> GetTime_LastMarketClose(Settings settings) {
            try {
                var client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                var calendars = client.ListCalendarAsync(new CalendarRequest().SetInclusiveTimeInterval(DateTime.UtcNow - new TimeSpan(30, 0, 0, 0), DateTime.UtcNow)).Result;
                return calendars.Where(c => c.TradingCloseTimeUtc.CompareTo(DateTime.UtcNow) <= 0).Last().TradingCloseTimeUtc;
            } catch (Exception ex) {
                return ex.Message;
            }
        }

        public static async Task<bool> PlaceOrder_BuyMarket(Settings settings, Data.Format format, string symbol, int shares, TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient client = null;

            if (format == Data.Format.Live) {
                client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
            } else if (format == Data.Format.Paper) {
                client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
            }

            if (client == null)
                return false;

            var order = await client.PostOrderAsync(MarketOrder.Buy(symbol, shares).WithDuration(timeInForce));
            return true;
        }

        public static async Task<bool> PlaceOrder_SellMarket(Settings settings, Data.Format format, string symbol, int shares, TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient client = null;

            if (format == Data.Format.Live) {
                client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
            } else if (format == Data.Format.Paper) {
                client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
            }

            if (client == null)
                return false;

            var order = await client.PostOrderAsync(MarketOrder.Sell(symbol, shares).WithDuration(timeInForce));
            return true;
        }

        public static async Task<bool> PlaceOrder_SellLimit(Settings settings, Data.Format format, string symbol, int shares, decimal limitPrice, TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient client = null;

            if (format == Data.Format.Live) {
                client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
            } else if (format == Data.Format.Paper) {
                client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
            }

            if (client == null)
                return false;

            var order = await client.PostOrderAsync(LimitOrder.Sell(symbol, shares, limitPrice).WithDuration(timeInForce));
            return true;
        }

        public static async Task<bool> PlaceOrder_SellStopLimit(Settings settings, Data.Format format, string symbol, int shares, decimal stopPrice, decimal limitPrice, TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient client = null;

            if (format == Data.Format.Live) {
                client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
            } else if (format == Data.Format.Paper) {
                client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
            }

            if (client == null)
                return false;

            var order = await client.PostOrderAsync(StopLimitOrder.Sell(symbol, shares, stopPrice, limitPrice).WithDuration(timeInForce));
            return true;
        }
    }
}