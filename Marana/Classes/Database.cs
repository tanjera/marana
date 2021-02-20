using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;

using Skender.Stock.Indicators;

namespace Marana {

    public class Database {
        public Settings _Settings;

        public Database(Settings settings) {
            _Settings = settings;
        }

        public enum ColumnsAsset {
            ID,
            Symbol,
            Class,
            Exchange,
            Status,
            Tradeable,
            Marginable,
            Shortable,
            EasyToBorrow
        }

        public enum ColumnsDaily {
            ID,
            Asset,
            Symbol,
            Date,
            Open,
            High,
            Low,
            Close,
            Volume,

            // Moving Averages

            SMA7,
            SMA20,
            SMA50,
            SMA100,
            SMA200,
            EMA7,
            EMA20,
            EMA50,
            DEMA7,
            DEMA20,
            DEMA50,
            TEMA7,
            TEMA20,
            TEMA50,

            // Additional Indicators (alphabetic order)

            BollingerBands_Center,
            BollingerBands_Upper,
            BollingerBands_Lower,
            BollingerBands_Percent,
            BollingerBands_ZScore,
            BollingerBands_Width,

            Choppiness,

            MACD,
            MACD_Histogram,
            MACD_Signal,

            ROC14,
            RSI,

            Stochastic_Oscillator,
            Stochastic_Signal,
            Stochastic_PercentJ
        };

        public enum ColumnsInstructions {
            ID,
            Description,
            Active,
            Format,
            Symbol,
            Strategy,
            Quantity,
            Frequency
        };

        public enum ColumnsStrategy {
            Name,
            Entry,
            ExitGain,
            ExitLoss
        }

        public enum ColumnsValidity {
            Item,
            Updated
        };

        public enum ColumnsWatchlist {
            Symbol
        }

        public enum ColumnsQuotes {
            Symbol,
            Timestamp,
            Price
        }

        public string ConnectionStr {
            get {
                return $"server={_Settings.Database_Server}; user={_Settings.Database_Username}; "
                    + $"database={_Settings.Database_Schema}; port={_Settings.Database_Port}; "
                   + $"password={_Settings.Database_Password}";
            }
        }

        public async Task Init() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, Init", ex.Message);
                    return;
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"CREATE TABLE IF NOT EXISTS `Validity` (
                            `Item` VARCHAR(64) PRIMARY KEY,
                            `Updated` DATETIME
                            );",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand(
                            @"CREATE TABLE IF NOT EXISTS `Assets` (
                            `ID` VARCHAR(64) PRIMARY KEY,
                            `Symbol` VARCHAR(10) NOT NULL,
                            `Class` VARCHAR (16),
                            `Exchange` VARCHAR (16),
                            `Status` VARCHAR (16),
                            `Tradeable` BOOLEAN,
                            `Marginable` BOOLEAN,
                            `Shortable` BOOLEAN,
                            `EasyToBorrow` BOOLEAN
                            );",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"CREATE TABLE IF NOT EXISTS `Daily` (
                            `ID` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `Asset` VARCHAR(64),
                            `Symbol` VARCHAR(16),
                            `Date` DATE NOT NULL,
                            `Open` DECIMAL(16, 6),
                            `High` DECIMAL(16, 6),
                            `Low` DECIMAL(16, 6),
                            `Close` DECIMAL(16, 6),
                            `Volume` BIGINT,
                            `SMA7` DECIMAL(16, 6),
                            `SMA20` DECIMAL(16, 6),
                            `SMA50` DECIMAL(16, 6),
                            `SMA100` DECIMAL(16, 6),
                            `SMA200` DECIMAL(16, 6),
                            `EMA7` DECIMAL(16, 6),
                            `EMA20` DECIMAL(16, 6),
                            `EMA50` DECIMAL(16, 6),
                            `DEMA7` DECIMAL(16, 6),
                            `DEMA20` DECIMAL(16, 6),
                            `DEMA50` DECIMAL(16, 6),
                            `TEMA7` DECIMAL(16, 6),
                            `TEMA20` DECIMAL(16, 6),
                            `TEMA50` DECIMAL(16, 6),

                            `BollingerBands_Center` DECIMAL(16, 6),
                            `BollingerBands_Upper` DECIMAL(16, 6),
                            `BollingerBands_Lower` DECIMAL(16, 6),
                            `BollingerBands_Percent` DECIMAL(16, 6),
                            `BollingerBands_ZScore` DECIMAL(16, 6),
                            `BollingerBands_Width` DECIMAL(16, 6),

                            `Choppiness` DECIMAL(16, 6),

                            `MACD` DECIMAL(16, 6),
                            `MACD_Histogram` DECIMAL(16, 6),
                            `MACD_Signal` DECIMAL(16, 6),

                            `ROC14` DECIMAL(16, 6),
                            `RSI` DECIMAL(16, 6),

                            `Stochastic_Oscillator` DECIMAL(16, 6),
                            `Stochastic_Signal` DECIMAL(16, 6),
                            `Stochastic_PercentJ` DECIMAL(16, 6),
                            INDEX(`Asset`));",

                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"CREATE TABLE IF NOT EXISTS `Instructions` (
                            `ID` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `Description` VARCHAR(1028),
                            `Active` BOOLEAN,
                            `Format` VARCHAR (16),
                            `Symbol` VARCHAR(10),
                            `Strategy` VARCHAR(256),
                            `Quantity` INTEGER,
                            `Frequency` VARCHAR (16)
                            );",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"CREATE TABLE IF NOT EXISTS `Strategies` (
                            `Name` VARCHAR(256) PRIMARY KEY,
                            `Entry` TEXT,
                            `ExitGain` TEXT,
                            `ExitLoss` TEXT
                            );",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"CREATE TABLE IF NOT EXISTS `Watchlist` (
                            `Symbol` VARCHAR(10) NOT NULL PRIMARY KEY
                            );",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"CREATE TABLE IF NOT EXISTS `Quotes` (
                            `Symbol` VARCHAR(10) NOT NULL PRIMARY KEY,
                            `Timestamp` DATETIME,
                            `Price` DECIMAL(16, 6)
                            );",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    await connection.CloseAsync();
                } catch (Exception ex) {
                    await connection.CloseAsync();
                    await Error.Log("Database.cs, Init", ex.Message);
                    return;
                }
            }
        }

        public async Task<List<Data.Asset>> GetAssets() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetAssets", ex.Message);
                    return null;
                }

                List<Data.Asset> assets = new List<Data.Asset>();

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                           @"SELECT * FROM `Assets` ORDER BY Symbol;",
                           connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                Data.Asset a = new Data.Asset();
                                a.ID = rdr.IsDBNull(ColumnsAsset.ID.GetHashCode()) ? a.ID : rdr.GetString("ID");
                                a.Symbol = rdr.IsDBNull(ColumnsAsset.Symbol.GetHashCode()) ? a.Symbol : rdr.GetString("Symbol");
                                a.Class = rdr.IsDBNull(ColumnsAsset.Class.GetHashCode()) ? a.Class : rdr.GetString("Class");
                                a.Exchange = rdr.IsDBNull(ColumnsAsset.Exchange.GetHashCode()) ? a.Exchange : rdr.GetString("Exchange");
                                a.Status = rdr.IsDBNull(ColumnsAsset.Status.GetHashCode()) ? a.Status : rdr.GetString("Status");
                                a.Tradeable = rdr.IsDBNull(ColumnsAsset.Tradeable.GetHashCode()) ? a.Tradeable : rdr.GetBoolean("Tradeable");
                                a.Marginable = rdr.IsDBNull(ColumnsAsset.Marginable.GetHashCode()) ? a.Marginable : rdr.GetBoolean("Marginable");
                                a.Shortable = rdr.IsDBNull(ColumnsAsset.Shortable.GetHashCode()) ? a.Shortable : rdr.GetBoolean("Shortable");
                                a.EasyToBorrow = rdr.IsDBNull(ColumnsAsset.EasyToBorrow.GetHashCode()) ? a.EasyToBorrow : rdr.GetBoolean("EasyToBorrow");
                                assets.Add(a);
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return assets;
                } catch (Exception ex) {
                    await connection.CloseAsync();
                    await Error.Log("Database.cs, GetAssets", ex.Message);
                    return null;
                }
            }
        }

        public async Task<Data.Daily> GetData_Daily(Data.Asset asset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetData_Daily", ex.Message);
                    return null;
                }

                string table = $"Daily:{asset.ID}";
                Data.Daily ds = new Data.Daily() { Asset = asset };

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"SELECT * FROM `Daily` WHERE Asset = '{asset.ID}';",
                            connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                Data.Daily.Price p = new Data.Daily.Price();
                                p.Date = rdr.IsDBNull(ColumnsDaily.Date.GetHashCode()) ? p.Date : rdr.GetDateTime("Date");
                                p.Open = rdr.IsDBNull(ColumnsDaily.Open.GetHashCode()) ? p.Open : rdr.GetDecimal("Open");
                                p.High = rdr.IsDBNull(ColumnsDaily.High.GetHashCode()) ? p.High : rdr.GetDecimal("High");
                                p.Low = rdr.IsDBNull(ColumnsDaily.Low.GetHashCode()) ? p.Low : rdr.GetDecimal("Low");
                                p.Close = rdr.IsDBNull(ColumnsDaily.Close.GetHashCode()) ? p.Close : rdr.GetDecimal("Close");
                                p.Volume = rdr.IsDBNull(ColumnsDaily.Volume.GetHashCode()) ? p.Volume : rdr.GetInt64("Volume");

                                Data.Daily.Metric m = new Data.Daily.Metric();
                                m.SMA7 = rdr.IsDBNull(ColumnsDaily.SMA7.GetHashCode()) ? m.SMA7 : rdr.GetDecimal("SMA7");
                                m.SMA20 = rdr.IsDBNull(ColumnsDaily.SMA20.GetHashCode()) ? m.SMA20 : rdr.GetDecimal("SMA20");
                                m.SMA50 = rdr.IsDBNull(ColumnsDaily.SMA50.GetHashCode()) ? m.SMA50 : rdr.GetDecimal("SMA50");
                                m.SMA100 = rdr.IsDBNull(ColumnsDaily.SMA100.GetHashCode()) ? m.SMA100 : rdr.GetDecimal("SMA100");
                                m.SMA200 = rdr.IsDBNull(ColumnsDaily.SMA200.GetHashCode()) ? m.SMA200 : rdr.GetDecimal("SMA200");
                                m.EMA7 = rdr.IsDBNull(ColumnsDaily.EMA7.GetHashCode()) ? m.EMA7 : rdr.GetDecimal("EMA7");
                                m.EMA20 = rdr.IsDBNull(ColumnsDaily.EMA20.GetHashCode()) ? m.EMA20 : rdr.GetDecimal("EMA20");
                                m.EMA50 = rdr.IsDBNull(ColumnsDaily.EMA50.GetHashCode()) ? m.EMA50 : rdr.GetDecimal("EMA50");
                                m.DEMA7 = rdr.IsDBNull(ColumnsDaily.DEMA7.GetHashCode()) ? m.DEMA7 : rdr.GetDecimal("DEMA7");
                                m.DEMA20 = rdr.IsDBNull(ColumnsDaily.DEMA20.GetHashCode()) ? m.DEMA20 : rdr.GetDecimal("DEMA20");
                                m.DEMA50 = rdr.IsDBNull(ColumnsDaily.DEMA50.GetHashCode()) ? m.DEMA50 : rdr.GetDecimal("DEMA50");
                                m.TEMA7 = rdr.IsDBNull(ColumnsDaily.TEMA7.GetHashCode()) ? m.TEMA7 : rdr.GetDecimal("TEMA7");
                                m.TEMA20 = rdr.IsDBNull(ColumnsDaily.TEMA20.GetHashCode()) ? m.TEMA20 : rdr.GetDecimal("TEMA20");
                                m.TEMA50 = rdr.IsDBNull(ColumnsDaily.TEMA50.GetHashCode()) ? m.TEMA50 : rdr.GetDecimal("TEMA50");

                                m.BB = new BollingerBandsResult();
                                m.BB.Sma = rdr.IsDBNull(ColumnsDaily.BollingerBands_Center.GetHashCode()) ? m.BB.Sma : rdr.GetDecimal("BollingerBands_Center");
                                m.BB.UpperBand = rdr.IsDBNull(ColumnsDaily.BollingerBands_Upper.GetHashCode()) ? m.BB.UpperBand : rdr.GetDecimal("BollingerBands_Upper");
                                m.BB.LowerBand = rdr.IsDBNull(ColumnsDaily.BollingerBands_Lower.GetHashCode()) ? m.BB.LowerBand : rdr.GetDecimal("BollingerBands_Lower");
                                m.BB.PercentB = rdr.IsDBNull(ColumnsDaily.BollingerBands_Percent.GetHashCode()) ? m.BB.PercentB : rdr.GetDecimal("BollingerBands_Percent");
                                m.BB.ZScore = rdr.IsDBNull(ColumnsDaily.BollingerBands_ZScore.GetHashCode()) ? m.BB.ZScore : rdr.GetDecimal("BollingerBands_ZScore");
                                m.BB.Width = rdr.IsDBNull(ColumnsDaily.BollingerBands_Width.GetHashCode()) ? m.BB.Width : rdr.GetDecimal("BollingerBands_Width");

                                m.Choppiness = rdr.IsDBNull(ColumnsDaily.Choppiness.GetHashCode()) ? m.Choppiness : rdr.GetDecimal("Choppiness");

                                m.MACD = new MacdResult();
                                m.MACD.Macd = rdr.IsDBNull(ColumnsDaily.MACD.GetHashCode()) ? m.MACD.Macd : rdr.GetDecimal("MACD");
                                m.MACD.Histogram = rdr.IsDBNull(ColumnsDaily.MACD_Histogram.GetHashCode()) ? m.MACD.Histogram : rdr.GetDecimal("MACD_Histogram");
                                m.MACD.Signal = rdr.IsDBNull(ColumnsDaily.MACD_Signal.GetHashCode()) ? m.MACD.Signal : rdr.GetDecimal("MACD_Signal");

                                m.ROC14 = rdr.IsDBNull(ColumnsDaily.ROC14.GetHashCode()) ? m.ROC14 : rdr.GetDecimal("ROC14");
                                m.RSI = rdr.IsDBNull(ColumnsDaily.RSI.GetHashCode()) ? m.RSI : rdr.GetDecimal("RSI");

                                m.Stochastic = new StochResult();
                                m.Stochastic.Oscillator = rdr.IsDBNull(ColumnsDaily.Stochastic_Oscillator.GetHashCode()) ? m.Stochastic.Oscillator : rdr.GetDecimal("Stochastic_Oscillator");
                                m.Stochastic.Signal = rdr.IsDBNull(ColumnsDaily.Stochastic_Signal.GetHashCode()) ? m.Stochastic.Signal : rdr.GetDecimal("Stochastic_Signal");
                                m.Stochastic.PercentJ = rdr.IsDBNull(ColumnsDaily.Stochastic_PercentJ.GetHashCode()) ? m.Stochastic.PercentJ : rdr.GetDecimal("Stochastic_PercentJ");

                                p.Metric = m;
                                m.Price = p;

                                ds.Prices.Add(p);
                                ds.Metrics.Add(m);
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return ds;
                } catch (Exception ex) {
                    await connection.CloseAsync();
                    await Error.Log("Database.cs, GetData_Daily", ex.Message);
                    return null;
                }
            }
        }

        public async Task<List<Data.Instruction>> GetInstructions() {
            List<Data.Instruction> result = new List<Data.Instruction>();

            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetInstructions", ex.Message);
                    return new List<Data.Instruction>();
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"SELECT * FROM `Instructions`;",
                        connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                Data.Instruction i = new Data.Instruction();

                                i.Description = rdr.IsDBNull(ColumnsInstructions.Description.GetHashCode()) ? i.Description : rdr.GetString("Description");
                                i.Active = rdr.IsDBNull(ColumnsInstructions.Active.GetHashCode()) ? i.Active : rdr.GetBoolean("Active");
                                i.Format = rdr.IsDBNull(ColumnsInstructions.Format.GetHashCode()) ? i.Format : (Data.Format)Enum.Parse(typeof(Data.Format), rdr.GetString("Format"));
                                i.Symbol = rdr.IsDBNull(ColumnsInstructions.Symbol.GetHashCode()) ? "" : rdr.GetString("Symbol");
                                i.Strategy = rdr.IsDBNull(ColumnsInstructions.Strategy.GetHashCode()) ? i.Strategy : rdr.GetString("Strategy");
                                i.Quantity = rdr.IsDBNull(ColumnsInstructions.Quantity.GetHashCode()) ? i.Quantity : rdr.GetInt16("Quantity");
                                i.Frequency = rdr.IsDBNull(ColumnsInstructions.Frequency.GetHashCode()) ? i.Frequency : (Data.Frequency)Enum.Parse(typeof(Data.Frequency), rdr.GetString("Frequency"));

                                result.Add(i);
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return result;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, GetInstructions", ex.Message);
                    await connection.CloseAsync();
                    return null;
                }
            }
        }

        public async Task<List<Data.Strategy>> GetStrategies() {
            List<Data.Strategy> result = new List<Data.Strategy>();

            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetStrategies", ex.Message);
                    return new List<Data.Strategy>();
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"SELECT * FROM `Strategies`;",
                        connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                Data.Strategy s = new Data.Strategy();

                                s.Name = rdr.IsDBNull(ColumnsStrategy.Name.GetHashCode()) ? s.Name : rdr.GetString("Name");
                                s.Entry = rdr.IsDBNull(ColumnsStrategy.Entry.GetHashCode()) ? s.Entry : rdr.GetString("Entry");
                                s.ExitGain = rdr.IsDBNull(ColumnsStrategy.ExitGain.GetHashCode()) ? s.ExitGain : rdr.GetString("ExitGain");
                                s.ExitLoss = rdr.IsDBNull(ColumnsStrategy.ExitLoss.GetHashCode()) ? s.ExitLoss : rdr.GetString("ExitLoss");
                                result.Add(s);
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return result;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, GetStrategies", ex.Message);
                    await connection.CloseAsync();
                    return null;
                }
            }
        }

        public async Task<Data.Quote> GetLastQuote(string symbol) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetLastQuote", ex.Message);
                    return null;
                }

                try {
                    Data.Quote result = new Data.Quote() { Symbol = symbol };
                    using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT *
                            FROM `Quotes`
                            WHERE `Symbol` = '{symbol}';",
                        connection))
                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read()) {
                            result.Price = rdr.IsDBNull(ColumnsQuotes.Price.GetHashCode()) ? result.Price : rdr.GetDecimal("Price");
                            result.Timestamp = rdr.IsDBNull(ColumnsQuotes.Timestamp.GetHashCode()) ? result.Timestamp : rdr.GetDateTime("Timestamp");
                        }
                    }

                    await connection.CloseAsync();
                    return result;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, GetLastQuote", ex.Message);
                    await connection.CloseAsync();
                    return null;
                }
            }
        }

        public async Task<DateTime> GetValidity(string item) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetValidity", ex.Message);
                    return new DateTime();
                }

                try {
                    DateTime result = new DateTime();
                    using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT `Updated`
                            FROM `Validity`
                            WHERE `Item` = '{item}';",
                        connection))
                        result = await cmd.ExecuteScalarAsync() != null ? (DateTime)(await cmd.ExecuteScalarAsync()) : new DateTime();

                    await connection.CloseAsync();
                    return result;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, GetValidity", ex.Message);
                    await connection.CloseAsync();
                    return new DateTime();
                }
            }
        }

        public async Task<DateTime> GetValidity_Assets()
            => await GetValidity("Assets");

        public async Task<DateTime> GetValidity_Daily(Data.Asset asset)
            => await GetValidity($"Daily:{asset.ID}");

        public async Task<DateTime> GetValidity_Quote(string symbol)
            => await GetValidity($"Quote:{symbol}");

        public async Task<List<string>> GetWatchlist() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetWatchlist", ex.Message);
                    return null;
                }

                try {
                    List<string> watchlist = new List<string>();
                    using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT *
                            FROM `Watchlist`
                            ORDER BY `Symbol` ASC;",
                        connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                string read = rdr.IsDBNull(ColumnsWatchlist.Symbol.GetHashCode()) ? "" : rdr.GetString("Symbol");
                                if (!String.IsNullOrEmpty(read))
                                    watchlist.Add(read);
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return watchlist;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, GetWatchlist", ex.Message);
                    await connection.CloseAsync();
                    return null;
                }
            }
        }

        public async Task<decimal> GetSize() {
            await Init();                                     // Cannot get size of a schema with no tables!

            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, GetSize", ex.Message);
                    return 0;
                }

                try {
                    decimal size = 0;

                    List<string> tables = new List<string>();
                    using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT table_name
                        FROM information_schema.tables
                        WHERE table_schema = '{_Settings.Database_Schema}';",
                        connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read())
                                tables.Add(rdr.GetString(0));
                        }
                    }

                    string analyzelist = String.Join(", ",
                        tables.Select(t => $"`{MySqlHelper.EscapeString(_Settings.Database_Schema)}`.`{MySqlHelper.EscapeString(t)}`"));

                    using (MySqlCommand cmd = new MySqlCommand($@"ANALYZE TABLE {analyzelist};",
                        connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (MySqlCommand cmd = new MySqlCommand(
                        $@"SELECT table_schema `{_Settings.Database_Schema}`,
                            ROUND(SUM(data_length + index_length) / 1024 / 1024, 1) 'size'
                        FROM information_schema.tables
                        GROUP BY table_schema;",
                        connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                size += rdr.GetDecimal("size");
                            }
                        }
                    }

                    await connection.CloseAsync();
                    return size;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, GetSize", ex.Message);
                    await connection.CloseAsync();
                    return 0m;
                }
            }
        }

        public async Task<bool> ScalarQuery(string query) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, ScalarQuery", ex.Message);
                    return false;
                }

                try {
                    bool result = false;
                    try {
                        using (MySqlCommand cmd = new MySqlCommand(query, connection)) {
                            result = await cmd.ExecuteScalarAsync() != null;
                        }
                    } catch (Exception ex) {
                        await Error.Log("Database.cs, ScalarQuery", ex.Message);
                        return false;
                    }

                    await connection.CloseAsync();
                    return result;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, ScalarQuery", ex.Message);
                    await connection.CloseAsync();
                    return false;
                }
            }
        }

        public async Task SetAssets(List<Data.Asset> assets) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, SetAssets", ex.Message);
                    return;
                }

                try {
                    if (assets.Count == 0) {
                        await SetValidity("Assets");
                        await connection.CloseAsync();
                        return;
                    }

                    // Delete data that will be updated or rewritten

                    string deletes = String.Join(" OR ",
                        assets.Select(a =>
                        $"ID = '{a.ID}'"));
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"DELETE FROM `Assets` WHERE {deletes};",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Insert new or updated data

                    string inserts = String.Join(", ",
                        assets.Select(a =>
                        $"('{MySqlHelper.EscapeString(a.ID)}', "
                        + $"'{MySqlHelper.EscapeString(a.Symbol)}', "
                        + $"'{MySqlHelper.EscapeString(a.Class)}', "
                        + $"'{MySqlHelper.EscapeString(a.Exchange)}',"
                        + $"'{MySqlHelper.EscapeString(a.Status)}', "
                        + $"'{MySqlHelper.EscapeString(a.Tradeable.GetHashCode().ToString())}', "
                        + $"'{MySqlHelper.EscapeString(a.Marginable.GetHashCode().ToString())}', "
                        + $"'{MySqlHelper.EscapeString(a.Shortable.GetHashCode().ToString())}', "
                        + $"'{MySqlHelper.EscapeString(a.EasyToBorrow.GetHashCode().ToString())}')"));
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"INSERT INTO `Assets` ( ID, Symbol, Class, Exchange, Status, Tradeable, Marginable, Shortable, EasyToBorrow ) "
                            + $"VALUES {inserts};",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await SetValidity("Assets");
                    await connection.CloseAsync();
                } catch (Exception ex) {
                    await Error.Log("Database.cs, SetAssets", ex.Message);
                    await connection.CloseAsync();
                }
            }
        }

        public async Task SetData_Daily(object dataset)
            => await SetData_Daily((Data.Daily)dataset);

        public async Task SetData_Daily(Data.Daily dataset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, SetData_Daily", ex.Message);
                    return;
                }

                // Delete data that will be updated or rewritten

                using (MySqlCommand cmd = new MySqlCommand(
                        $@"DELETE FROM `Daily`
                            WHERE Asset = '{dataset.Asset.ID}';",
                        connection)) {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insert the data into the table

                if (dataset.Prices.Count == 0) {
                    await SetValidity($"Daily:{dataset.Asset.ID}");
                    await connection.CloseAsync();
                    return;
                }

                string values = String.Join(", ",
                    dataset.Prices.Select(v =>
                        $@"(
                        '{MySqlHelper.EscapeString(dataset.Asset.ID)}',
                        '{MySqlHelper.EscapeString(dataset.Asset.Symbol)}',
                        '{MySqlHelper.EscapeString(v.Date.ToString("yyyy-MM-dd"))}',
                        '{MySqlHelper.EscapeString(v.Open.ToString())}',
                        '{MySqlHelper.EscapeString(v.High.ToString())}',
                        '{MySqlHelper.EscapeString(v.Low.ToString())}',
                        '{MySqlHelper.EscapeString(v.Close.ToString())}',
                        '{MySqlHelper.EscapeString(v.Volume.ToString())}',
                        {(v.Metric?.SMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA7.ToString())}'" : "null")},
                        {(v.Metric?.SMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA20.ToString())}'" : "null")},
                        {(v.Metric?.SMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA50.ToString())}'" : "null")},
                        {(v.Metric?.SMA100 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA100.ToString())}'" : "null")},
                        {(v.Metric?.SMA200 != null ? $"'{MySqlHelper.EscapeString(v.Metric.SMA200.ToString())}'" : "null")},
                        {(v.Metric?.EMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.EMA7.ToString())}'" : "null")},
                        {(v.Metric?.EMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.EMA20.ToString())}'" : "null")},
                        {(v.Metric?.EMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.EMA50.ToString())}'" : "null")},
                        {(v.Metric?.DEMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.DEMA7.ToString())}'" : "null")},
                        {(v.Metric?.DEMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.DEMA20.ToString())}'" : "null")},
                        {(v.Metric?.DEMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.DEMA50.ToString())}'" : "null")},
                        {(v.Metric?.TEMA7 != null ? $"'{MySqlHelper.EscapeString(v.Metric.TEMA7.ToString())}'" : "null")},
                        {(v.Metric?.TEMA20 != null ? $"'{MySqlHelper.EscapeString(v.Metric.TEMA20.ToString())}'" : "null")},
                        {(v.Metric?.TEMA50 != null ? $"'{MySqlHelper.EscapeString(v.Metric.TEMA50.ToString())}'" : "null")},

                        {(v.Metric?.BB?.Sma != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.Sma.ToString())}'" : "null")},
                        {(v.Metric?.BB?.UpperBand != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.UpperBand.ToString())}'" : "null")},
                        {(v.Metric?.BB?.LowerBand != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.LowerBand.ToString())}'" : "null")},
                        {(v.Metric?.BB?.PercentB != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.PercentB.ToString())}'" : "null")},
                        {(v.Metric?.BB?.ZScore != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.ZScore.ToString())}'" : "null")},
                        {(v.Metric?.BB?.Width != null ? $"'{MySqlHelper.EscapeString(v.Metric.BB.Width.ToString())}'" : "null")},

                        {(v.Metric?.Choppiness != null ? $"'{MySqlHelper.EscapeString(v.Metric.Choppiness.ToString())}'" : "null")},

                        {(v.Metric?.MACD?.Macd != null ? $"'{MySqlHelper.EscapeString(v.Metric.MACD.Macd.ToString())}'" : "null")},
                        {(v.Metric?.MACD?.Histogram != null ? $"'{MySqlHelper.EscapeString(v.Metric.MACD.Histogram.ToString())}'" : "null")},
                        {(v.Metric?.MACD?.Signal != null ? $"'{MySqlHelper.EscapeString(v.Metric.MACD.Signal.ToString())}'" : "null")},

                        {(v.Metric?.ROC14 != null ? $"'{MySqlHelper.EscapeString(v.Metric.ROC14.ToString())}'" : "null")},
                        {(v.Metric?.RSI != null ? $"'{MySqlHelper.EscapeString(v.Metric.RSI.ToString())}'" : "null")},

                        {(v.Metric?.Stochastic?.Oscillator != null ? $"'{MySqlHelper.EscapeString(v.Metric.Stochastic.Oscillator.ToString())}'" : "null")},
                        {(v.Metric?.Stochastic?.Signal != null ? $"'{MySqlHelper.EscapeString(v.Metric.Stochastic.Signal.ToString())}'" : "null")},
                        {(v.Metric?.Stochastic?.PercentJ != null ? $"'{MySqlHelper.EscapeString(v.Metric.Stochastic.PercentJ.ToString())}'" : "null")}
                        )"));

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"INSERT INTO `Daily` (
                                    Asset, Symbol, Date, Open, High, Low, Close, Volume,
                                    SMA7, SMA20, SMA50, SMA100, SMA200,
                                    EMA7, EMA20, EMA50, DEMA7, DEMA20, DEMA50, TEMA7, TEMA20, TEMA50,

                                    BollingerBands_Center, BollingerBands_Upper, BollingerBands_Lower, BollingerBands_Percent, BollingerBands_ZScore, BollingerBands_Width,
                                    Choppiness,
                                    MACD, MACD_Histogram, MACD_Signal,
                                    ROC14,
                                    RSI,
                                    Stochastic_Oscillator, Stochastic_Signal, Stochastic_PercentJ
                                    ) VALUES {values};",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await SetValidity($"Daily:{dataset.Asset.ID}");
                    await connection.CloseAsync();
                } catch (Exception ex) {
                    await Error.Log("Database.cs, SetData_Daily", ex.Message);
                    await connection.CloseAsync();
                }
            }
        }

        public async Task SetLastQuote(Data.Quote quote) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, SetLastQuote", ex.Message);
                    return;
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"DELETE FROM `Quotes` WHERE `Symbol` = '{MySqlHelper.EscapeString(quote.Symbol)}';",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"INSERT INTO `Quotes` (
                            `Symbol`, `Timestamp`, `Price`
                            ) VALUES ( '{MySqlHelper.EscapeString(quote.Symbol)}', '{quote.Timestamp:yyyy-MM-dd HH:mm}', '{quote?.Price}' );",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await SetValidity($"Quote:{quote.Symbol}");
                    await connection.CloseAsync();
                } catch (Exception ex) {
                    await Error.Log("Database.cs, SetLastQuote", ex.Message);
                    await connection.CloseAsync();
                }
            }
        }

        public async Task SetValidity(string item) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, SetValidity", ex.Message);
                    return;
                }

                try {
                    bool oldvalidity = false;
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"SELECT `Item` FROM `Validity` WHERE `Item` = '{item}';",
                            connection))
                        oldvalidity = cmd.ExecuteScalar() != null;

                    // Use UTC time- in case client and server in different time zones

                    if (oldvalidity) {
                        using (MySqlCommand cmd = new MySqlCommand(
                                @"UPDATE `Validity`
                                SET `Updated` = ?updated
                                WHERE (`Item` = ?item);",
                                connection)) {
                            cmd.Parameters.AddWithValue("?updated", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("?item", item);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    } else {
                        using (MySqlCommand cmd = new MySqlCommand(
                                @"INSERT INTO `Validity` (
                                `Item`, Updated
                                ) VALUES (
                                ?item, ?updated
                                );",
                                connection)) {
                            cmd.Parameters.AddWithValue("?updated", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("?item", item);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    await connection.CloseAsync();
                } catch (Exception ex) {
                    await Error.Log("Database.cs, SetValidity", ex.Message);
                    await connection.CloseAsync();
                }
            }
        }

        public async Task SetWatchlist(List<string> watchlist) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync();
                } catch (Exception ex) {
                    Console.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    await Error.Log("Database.cs, SetWatchlist", ex.Message);
                    return;
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"DELETE FROM `Watchlist`;",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    string values = String.Join(", ",
                        watchlist.Select(s => $"('{MySqlHelper.EscapeString(s.Trim().ToUpper())}')"));

                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"INSERT INTO `Watchlist` (
                            `Symbol`
                            ) VALUES {values};",
                            connection)) {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await connection.CloseAsync();
                } catch (Exception ex) {
                    await Error.Log("Database.cs, SetWatchlist", ex.Message);
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<object> ValidateQuery(string query) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, ValidateQuery", ex.Message);
                    return ex.Message;
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(query, connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            // No need to read anything... just dispose/close and return
                        }
                    }
                } catch (Exception ex) {
                    // We expect lots of exceptions here, since this function tests user-written SQL queries for validity,
                    // and if invalid, cmd.ExecuteReader will throw an Exception- this will return the message to display
                    // for the user.
                    return ex.Message;
                }

                await connection.CloseAsync();
                return true;
            }
        }

        public async Task<bool> Wipe() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    await connection.OpenAsync(); ;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, Wipe", ex.Message);
                    return false;
                }

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                            $@"DROP TABLE IF EXISTS Validity, Assets, Daily, Quotes;",
                            connection))
                        await cmd.ExecuteNonQueryAsync();

                    await Init();
                    await connection.CloseAsync();
                    return true;
                } catch (Exception ex) {
                    await Error.Log("Database.cs, Wipe", ex.Message);
                    await connection.CloseAsync();
                    return false;
                }
            }
        }
    }
}