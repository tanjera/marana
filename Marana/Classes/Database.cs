using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace Marana {

    public class Database {
        public Settings _Settings;

        public Database(Settings settings) {
            _Settings = settings;
        }

        public string ConnectionStr {
            get {
                return String.Format("server={0}; user={1}; database={2}; port={3}; password={4}",
                    _Settings.Database_Server, _Settings.Database_User, _Settings.Database_Schema, _Settings.Database_Port, _Settings.Database_Password); ;
            }
        }

        public void Init() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                using (MySqlCommand cmd = new MySqlCommand(
                        @"CREATE TABLE IF NOT EXISTS `_validity` (
                            `id` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `table` VARCHAR(64) NOT NULL,
                            `updated` DATETIME
                            ) AUTO_INCREMENT = 1;",
                        connection))
                    cmd.ExecuteNonQuery();

                connection.Close();
            }
        }

        public void Wipe() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                    cmd.ExecuteNonQuery();

                List<string> tables = new List<string>();
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                    @"SELECT table_name
                        FROM information_schema.tables
                        WHERE table_schema = '{0}';",
                    _Settings.Database_Schema), connection)) {
                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read())
                            tables.Add(rdr.GetString(0));
                    }
                }

                string droplist = String.Join(", ",
                    tables.Select(t => String.Format("`{0}`", MySqlHelper.EscapeString(t))));
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"DROP TABLE IF EXISTS {0};",
                        droplist), connection))
                    cmd.ExecuteNonQuery();

                using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                    cmd.ExecuteNonQuery();

                Init();

                connection.Close();
            }
        }

        public void AddData_Assets(List<Data.Asset> assets) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                // Drop the old table- easier than sorting and updating
                using (MySqlCommand cmd = new MySqlCommand(
                    @"DROP TABLE IF EXISTS `_assets`;",
                    connection))
                    cmd.ExecuteNonQuery();

                using (MySqlCommand cmd = new MySqlCommand(
                        @"CREATE TABLE IF NOT EXISTS `_assets` (
                            `id` VARCHAR(64) PRIMARY KEY,
                            `symbol` VARCHAR(10) NOT NULL,
                            `class` VARCHAR (16),
                            `exchange` VARCHAR (16),
                            `status` VARCHAR (16),
                            `tradeable` BOOLEAN,
                            `marginable` BOOLEAN,
                            `shortable` BOOLEAN,
                            `easytoborrow` BOOLEAN
                            );",
                        connection))
                    cmd.ExecuteNonQuery();

                string values = String.Join(", ",
                    assets.Select(a => String.Format("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}')",
                    MySqlHelper.EscapeString(a.ID),
                    MySqlHelper.EscapeString(a.Symbol),
                    MySqlHelper.EscapeString(a.Class),
                    MySqlHelper.EscapeString(a.Exchange),
                    MySqlHelper.EscapeString(a.Status),
                    MySqlHelper.EscapeString(a.Tradeable.GetHashCode().ToString()),
                    MySqlHelper.EscapeString(a.Marginable.GetHashCode().ToString()),
                    MySqlHelper.EscapeString(a.Shortable.GetHashCode().ToString()),
                    MySqlHelper.EscapeString(a.EasyToBorrow.GetHashCode().ToString()))));
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"INSERT INTO `_assets` ( id, symbol, class, exchange, status, tradeable, marginable, shortable, easytoborrow ) VALUES {0};",
                        values), connection)) {
                    cmd.ExecuteNonQuery();
                }

                UpdateValidity("_assets");

                connection.Close();
            }
        }

        public void AddData_TSD(object dataset)
            => AddData_TSD((Data.Daily)dataset);

        public void AddData_TSD(Data.Daily dataset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                string table = String.Format("TSD:{0}", dataset.Asset.ID);

                // Drop the old table- easier than sorting and updating
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"DROP TABLE IF EXISTS `{0}`;",
                        table), connection))
                    cmd.ExecuteNonQuery();

                // Create the new table
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"CREATE TABLE IF NOT EXISTS `{0}` (
                            `id` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `timestamp` DATE NOT NULL,
                            `open` DECIMAL(12, 4),
                            `high` DECIMAL(12, 4),
                            `low` DECIMAL(12, 4),
                            `close` DECIMAL(12, 4),
                            `volume` BIGINT
                            );",
                        table), connection))
                    cmd.ExecuteNonQuery();

                // Insert the data into the table

                if (dataset.Prices.Count == 0) {
                    UpdateValidity(table);
                    connection.Close();
                    return;
                }

                string values = String.Join(", ",
                    dataset.Prices.Select(v => String.Format(
                        "('{0}', '{1}', '{2}', '{3}', '{4}', '{5}')",
                    MySqlHelper.EscapeString(v.Timestamp.ToString("yyyy-MM-dd")),
                    MySqlHelper.EscapeString(v.Open.ToString()),
                    MySqlHelper.EscapeString(v.High.ToString()),
                    MySqlHelper.EscapeString(v.Low.ToString()),
                    MySqlHelper.EscapeString(v.Close.ToString()),
                    MySqlHelper.EscapeString(v.Volume.ToString()))));

                try {
                    using (MySqlCommand cmd = new MySqlCommand(String.Format(
                            @"INSERT INTO `{0}` (
                                    timestamp, open, high, low, close, volume
                                    ) VALUES {1};",
                            table, values), connection)) {
                        cmd.ExecuteNonQuery();
                    }

                    UpdateValidity(table);
                } catch (Exception ex) {
                    // TO-DO: log errors to error log
                } finally {
                    connection.Close();
                }
            }
        }

        public List<Data.Asset> GetData_Assets() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return new List<Data.Asset>();
                }

                List<Data.Asset> assets = new List<Data.Asset>();

                try {
                    using (MySqlCommand cmd = new MySqlCommand(
                           @"SELECT * FROM `_assets` ORDER BY symbol;",
                           connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read())
                                assets.Add(new Data.Asset() {
                                    ID = rdr.GetString("id"),
                                    Symbol = rdr.GetString("symbol"),
                                    Class = rdr.GetString("class"),
                                    Exchange = rdr.GetString("exchange"),
                                    Status = rdr.GetString("status"),
                                    Tradeable = rdr.GetBoolean("tradeable"),
                                    Marginable = rdr.GetBoolean("marginable"),
                                    Shortable = rdr.GetBoolean("shortable"),
                                    EasyToBorrow = rdr.GetBoolean("easytoborrow")
                                });
                        }
                    }

                    connection.Close();
                    return assets;
                } catch (Exception ex) {
                    connection.Close();
                    return new List<Data.Asset>();
                }
            }
        }

        public Data.Daily GetData_TSD(Data.Asset asset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return null;
                }

                string table = String.Format("TSD:{0}", asset.ID);
                Data.Daily ds = new Data.Daily() { Asset = asset };

                try {
                    using (MySqlCommand cmd = new MySqlCommand(String.Format(
                            @"SELECT * FROM `{0}`;",
                            table), connection)) {
                        using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                            while (rdr.Read()) {
                                ds.Prices.Add(new Data.Daily.Price() {
                                    Timestamp = rdr.GetDateTime("timestamp"),
                                    Open = rdr.GetDecimal("open"),
                                    High = rdr.GetDecimal("high"),
                                    Low = rdr.GetDecimal("low"),
                                    Close = rdr.GetDecimal("close"),
                                    Volume = rdr.GetInt64("volume")
                                });
                            }
                        }
                    }

                    connection.Close();
                    return ds;
                } catch (Exception ex) {
                    connection.Close();
                    return null;
                    // TO-DO: log errors to error log
                }
            }
        }

        public DateTime GetValidity(string table) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return new DateTime();
                }

                DateTime result = new DateTime();
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"SELECT `updated` FROM `_validity` WHERE `table` = '{0}';",
                        table), connection))
                    result = cmd.ExecuteScalar() != null ? (DateTime)(cmd.ExecuteScalar()) : new DateTime();

                connection.Close();
                return result;
            }
        }

        public DateTime GetValidity_Assets()
            => GetValidity("_assets");

        public DateTime GetValidity_TSD(Data.Asset asset)
            => GetValidity(String.Format("TSD:{0}", asset.ID));

        public decimal GetSize() {
            Init();                                     // Cannot get size of a schema with no tables!

            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return 0;
                }

                decimal size = 0;

                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                    @"SELECT table_schema `{0}`,
                            ROUND(SUM(data_length + index_length) / 1024 / 1024, 1) 'size'
                        FROM information_schema.tables
                        GROUP BY table_schema;",
                    _Settings.Database_Schema), connection)) {
                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read()) {
                            size += rdr.GetDecimal("size");
                        }
                    }
                }

                connection.Close();
                return size;
            }
        }

        public void UpdateValidity(string table) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                bool oldvalidity = false;
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"SELECT `id` FROM `_validity` WHERE `table` = '{0}';",
                        table), connection))
                    oldvalidity = cmd.ExecuteScalar() != null;

                // Use UTC time- in case client and server in different time zones

                if (oldvalidity) {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"UPDATE `_validity`
                                SET `updated` = ?updated
                                WHERE (`table` = ?table);",
                            connection)) {
                        cmd.Parameters.AddWithValue("?updated", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?table", table);
                        cmd.ExecuteNonQuery();
                    }
                } else {
                    using (MySqlCommand cmd = new MySqlCommand(
                            @"INSERT INTO `_validity` (
                                `table`, updated
                                ) VALUES (
                                ?table, ?updated
                                );",
                            connection)) {
                        cmd.Parameters.AddWithValue("?updated", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?table", table);
                        cmd.ExecuteNonQuery();
                    }
                }

                connection.Close();
            }
        }
    }
}