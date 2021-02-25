using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alpaca.Markets;

namespace Marana.API {

    public class Alpaca {

        public static async Task ClearOrders(Settings settings, Data.Format format) {
            IAlpacaTradingClient trading = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                await trading.DeleteAllOrdersAsync();
                return;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return;
            }
        }

        public static async Task<object> GetAssets(Settings settings) {
            try {
                IAlpacaTradingClient trading = null;

                if (!String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) && !String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (!String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) && !String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                } else {
                    return new ArgumentNullException();
                }

                var assets = await trading.ListAssetsAsync(new AssetsRequest { AssetStatus = AssetStatus.Active });

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
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return ex.Message;
            }
        }

        public static async Task<object> GetTradeableCash(Settings settings, Data.Format format) {
            IAlpacaTradingClient trading = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return null;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return null;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                var account = await trading.GetAccountAsync();

                return account.TradableCash;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return ex.Message;
            }
        }

        public static async Task<object> GetData_Daily(Settings settings, Data.Asset asset, int limit = 500) {
            try {
                IAlpacaDataClient data = null;

                if (!String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) && !String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                    data = Environments.Live.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (!String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) && !String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                    data = Environments.Paper.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                } else {
                    return new ArgumentNullException();
                }

                // Maximum 1000 bars per API call
                var bars = await data.GetBarSetAsync(new BarSetRequest(asset.Symbol, TimeFrame.Day) { Limit = limit });

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
                if (ex.Message != "Too Many Requests") {                    // This is handled elsewhere- does not need to be error logged
                    await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                }
                return ex.Message;
            }
        }

        public static async Task<object> GetPositions(Settings settings, Data.Format format) {
            IAlpacaTradingClient client = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return false;
                    }

                    client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return false;
                    }

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
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return null;
            }
        }

        public static async Task<object> GetTime_LastMarketClose(Settings settings) {
            IAlpacaTradingClient client = null;

            try {
                if (!String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) && !String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                    client = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (!String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) && !String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                    client = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                } else {
                    return new ArgumentNullException();
                }

                var calendars = client.ListCalendarAsync(new CalendarRequest().SetInclusiveTimeInterval(DateTime.UtcNow - new TimeSpan(30, 0, 0, 0), DateTime.UtcNow)).Result;
                return calendars.Where(c => c.TradingCloseTimeUtc.CompareTo(DateTime.UtcNow) <= 0).Last().TradingCloseTimeUtc;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return ex.Message;
            }
        }

        public static async Task<object> GetOrders_OpenBuy(Settings settings, Data.Format format) {
            IAlpacaTradingClient trading = null;
            IAlpacaDataClient data = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return null;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                    data = Environments.Live.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return null;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                    data = Environments.Paper.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                return (await trading.ListOrdersAsync(
                    new ListOrdersRequest() { OrderStatusFilter = OrderStatusFilter.Open, LimitOrderNumber = 1000 }))
                    .Where(o => o.OrderSide == OrderSide.Buy)
                    .Select<IOrder, Data.Order>((q) => { return new Data.Order() { Symbol = q.Symbol, Quantity = (int)q.Quantity }; })
                    .ToList();
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return ex.Message;
            }
        }

        public static async Task<Trading.OrderResult> PlaceOrder_BuyMarket(Settings settings, Database db, Data.Format format, string symbol, int shares,

            TimeInForce timeInForce = TimeInForce.Gtc, bool useMargin = false) {
            IAlpacaTradingClient trading = null;
            IAlpacaDataClient data = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                    data = Environments.Live.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                    data = Environments.Paper.GetAlpacaDataClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                var account = await trading.GetAccountAsync();

                if (trading == null || account == null
                    || account.IsAccountBlocked || account.IsTradingBlocked
                    || account.TradeSuspendedByUser)
                    return Trading.OrderResult.Fail;

                var order = await trading.PostOrderAsync(MarketOrder.Buy(symbol, shares).WithDuration(timeInForce));
                return Trading.OrderResult.Success;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return Trading.OrderResult.Fail;
            }
        }

        public static async Task<Trading.OrderResult> PlaceOrder_SellMarket(Settings settings, Data.Format format, string symbol, int shares,
            TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient trading = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                // Prevents exceptions or unwanted behavior with Alpaca API
                var account = await trading.GetAccountAsync();
                if (trading == null || account == null
                    || account.IsAccountBlocked || account.IsTradingBlocked
                    || account.TradeSuspendedByUser)
                    return Trading.OrderResult.Fail;

                // Prevents unintentionally short selling (selling into negative digits, the API interprets that as intent to short-sell)
                var positions = await trading.ListPositionsAsync();
                if (!positions.Any(p => p.Symbol == symbol))                // If there is no position for this symbol
                    return Trading.OrderResult.Fail;

                var position = await trading.GetPositionAsync(symbol);      // If there were no position, this would throw an Exception!
                if (position == null || position.Quantity < shares)         // If the current position doesn't have enough shares
                    return Trading.OrderResult.Fail;

                var order = await trading.PostOrderAsync(MarketOrder.Sell(symbol, shares).WithDuration(timeInForce));
                return Trading.OrderResult.Success;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return Trading.OrderResult.Fail;
            }
        }

        public static async Task<Trading.OrderResult> PlaceOrder_SellLimit(Settings settings, Data.Format format, string symbol, int shares, decimal limitPrice,
            TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient trading = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                // Prevents exceptions or unwanted behavior with Alpaca API
                var account = await trading.GetAccountAsync();
                if (trading == null || account == null
                    || account.IsAccountBlocked || account.IsTradingBlocked
                    || account.TradeSuspendedByUser)
                    return Trading.OrderResult.Fail;

                // Prevents unintentionally short selling (selling into negative digits, the API interprets that as intent to short-sell)
                var positions = await trading.ListPositionsAsync();
                if (!positions.Any(p => p.Symbol == symbol))                // If there is no position for this symbol
                    return Trading.OrderResult.Fail;

                var position = await trading.GetPositionAsync(symbol);      // If there were no position, this would throw an Exception!
                if (position == null || position.Quantity < shares)         // If the current position doesn't have enough shares
                    return Trading.OrderResult.Fail;

                var order = await trading.PostOrderAsync(LimitOrder.Sell(symbol, shares, limitPrice).WithDuration(timeInForce));
                return Trading.OrderResult.Success;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return Trading.OrderResult.Fail;
            }
        }

        public static async Task<Trading.OrderResult> PlaceOrder_SellStopLimit(Settings settings, Data.Format format, string symbol, int shares, decimal stopPrice, decimal limitPrice,
            TimeInForce timeInForce = TimeInForce.Gtc) {
            IAlpacaTradingClient trading = null;

            try {
                if (format == Data.Format.Live) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Live_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Live.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Live_Key, settings.API_Alpaca_Live_Secret));
                } else if (format == Data.Format.Paper) {
                    if (String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Key) || String.IsNullOrWhiteSpace(settings.API_Alpaca_Paper_Secret)) {
                        return Trading.OrderResult.Fail;
                    }

                    trading = Environments.Paper.GetAlpacaTradingClient(new SecretKey(settings.API_Alpaca_Paper_Key, settings.API_Alpaca_Paper_Secret));
                }

                // Prevents exceptions or unwanted behavior with Alpaca API
                var account = await trading.GetAccountAsync();
                if (trading == null || account == null
                    || account.IsAccountBlocked || account.IsTradingBlocked
                    || account.TradeSuspendedByUser)
                    return Trading.OrderResult.Fail;

                // Prevents unintentionally short selling (selling into negative digits, the API interprets that as intent to short-sell)
                var positions = await trading.ListPositionsAsync();
                if (!positions.Any(p => p.Symbol == symbol))                // If there is no position for this symbol
                    return Trading.OrderResult.Fail;

                var position = await trading.GetPositionAsync(symbol);      // If there were no position, this would throw an Exception!
                if (position == null || position.Quantity < shares)         // If the current position doesn't have enough shares
                    return Trading.OrderResult.Fail;

                var order = await trading.PostOrderAsync(StopLimitOrder.Sell(symbol, shares, stopPrice, limitPrice).WithDuration(timeInForce));
                return Trading.OrderResult.Success;
            } catch (Exception ex) {
                await Log.Error($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
                return Trading.OrderResult.Fail;
            }
        }
    }
}