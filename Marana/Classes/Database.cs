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

        public void AddData_Symbols(List<SymbolPair> pairs) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                // Drop the old table- easier than sorting and updating
                using (MySqlCommand cmd = new MySqlCommand(
                    @"DROP TABLE IF EXISTS `_symbols`;",
                    connection))
                    cmd.ExecuteNonQuery();

                using (MySqlCommand cmd = new MySqlCommand(
                        @"CREATE TABLE IF NOT EXISTS `_symbols` (
                            `id` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `symbol` VARCHAR(8) NOT NULL,
                            `name` VARCHAR(256) NULL
                            ) AUTO_INCREMENT = 1;",
                        connection))
                    cmd.ExecuteNonQuery();

                string values = String.Join(", ",
                    pairs.Select(p => String.Format("('{0}', '{1}')",
                    MySqlHelper.EscapeString(p.Symbol),
                    MySqlHelper.EscapeString(p.Name))));
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"INSERT INTO `_symbols` ( symbol, name ) VALUES {0};",
                        values), connection)) {
                    cmd.ExecuteNonQuery();
                }

                UpdateValidity("_symbols");

                connection.Close();
            }
        }

        public void AddData_TSD(object dataset)
            => AddData_TSD((DatasetTSD)dataset);

        public void AddData_TSD(DatasetTSD dataset) {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return;
                }

                string table = String.Format("tsd_{0}", dataset.Symbol);

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
                            `open` DECIMAL(10, 4),
                            `high` DECIMAL(10, 4),
                            `low` DECIMAL(10, 4),
                            `close` DECIMAL(10, 4),
                            `volume` BIGINT,
                            `sma7` DECIMAL(10, 4),
                            `sma20` DECIMAL(10, 4),
                            `sma50` DECIMAL(10, 4),
                            `sma100` DECIMAL(10, 4),
                            `sma200` DECIMAL(10, 4),
                            `msd20` DECIMAL(10, 4),
                            `msdr20` DECIMAL(10, 6),
                            `vsma20` BIGINT,
                            `vmsd20` BIGINT
                            );",
                        table), connection))
                    cmd.ExecuteNonQuery();

                // Insert the data into the table

                string values = String.Join(", ",
                    dataset.Values.Select(v => String.Format(
                        "('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}',"
                        + "'{11}', '{12}', '{13}', '{14}')",
                    MySqlHelper.EscapeString(v.Timestamp.ToString("yyyy-MM-dd")),
                    MySqlHelper.EscapeString(v.Open.ToString()),
                    MySqlHelper.EscapeString(v.High.ToString()),
                    MySqlHelper.EscapeString(v.Low.ToString()),
                    MySqlHelper.EscapeString(v.Close.ToString()),
                    MySqlHelper.EscapeString(v.Volume.ToString()),
                    MySqlHelper.EscapeString(v.SMA7.ToString()),
                    MySqlHelper.EscapeString(v.SMA20.ToString()),
                    MySqlHelper.EscapeString(v.SMA50.ToString()),
                    MySqlHelper.EscapeString(v.SMA100.ToString()),
                    MySqlHelper.EscapeString(v.SMA200.ToString()),
                    MySqlHelper.EscapeString(v.MSD20.ToString()),
                    MySqlHelper.EscapeString(v.MSDr20.ToString()),
                    MySqlHelper.EscapeString(v.vSMA20.ToString()),
                    MySqlHelper.EscapeString(v.vMSD20.ToString()))));

                try {
                    using (MySqlCommand cmd = new MySqlCommand(String.Format(
                            @"INSERT INTO `{0}` (
                                    timestamp, open, high, low, close, volume,
                                    sma7, sma20, sma50, sma100, sma200, msd20, msdr20, vsma20, vmsd20
                                    ) VALUES {1};",
                            table, values), connection)) {
                        cmd.ExecuteNonQuery();
                    }
                } catch (Exception ex) {
                    // TO-DO: log errors to MySQL _errors
                }

                UpdateValidity(table);

                connection.Close();
            }
        }

        public List<SymbolPair> GetData_Symbols() {
            using (MySqlConnection connection = new MySqlConnection(ConnectionStr)) {
                try {
                    connection.Open();
                } catch (Exception ex) {
                    Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                    return new List<SymbolPair>();
                }

                List<SymbolPair> pairs = new List<SymbolPair>();

                using (MySqlCommand cmd = new MySqlCommand(
                       @"SELECT `symbol`, `name` FROM `_symbols`;",
                       connection)) {
                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read())
                            pairs.Add(new SymbolPair() { Symbol = rdr.GetString("symbol"), Name = rdr.GetString("name") });
                    }
                }

                connection.Close();
                return pairs;
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

        public DateTime GetValidity_Symbols()
            => GetValidity("_symbols");

        public DateTime GetValidity_TSD(SymbolPair pair)
            => GetValidity(String.Format("tsd_{0}", pair.Symbol));

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