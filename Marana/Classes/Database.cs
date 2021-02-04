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

        public bool Connect(ref MySqlConnection connection) {
            string connString = String.Format("server={0};user={1};database={2};port={3};password={4}",
                _Settings.Database_Server, _Settings.Database_User, _Settings.Database_Schema, _Settings.Database_Port, _Settings.Database_Password);

            connection = new MySqlConnection(connString);

            try {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        public void Disconnect(ref MySqlConnection connection) {
            if (connection.State != System.Data.ConnectionState.Closed)
                connection.Close();

            connection.Dispose();
        }

        public void Init() {
            MySqlConnection connection = new MySqlConnection();

            if (!Connect(ref connection)) {
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

            Disconnect(ref connection);
        }

        public void Wipe() {
            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
                Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                return;
            }

            using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
                cmd.ExecuteNonQuery();

            List<string> tables = new List<string>();
            using (MySqlCommand cmd = new MySqlCommand(String.Format(
                @"SELECT
                    table_name
                FROM
                    information_schema.tables
                WHERE
                    table_schema = '{0}';",
                _Settings.Database_Schema), connection)) {
                using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                    while (rdr.Read())
                        tables.Add(rdr.GetString(0));
                }
            }

            foreach (string table in tables) {
                using (MySqlCommand cmd = new MySqlCommand(String.Format(
                        @"DROP TABLE IF EXISTS {0};",
                        table), connection))
                    cmd.ExecuteNonQuery();
            }

            using (MySqlCommand cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection))
                cmd.ExecuteNonQuery();

            Init();
            Disconnect(ref connection);
        }

        public void AddData_Symbols(List<SymbolPair> pairs) {
            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
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

            foreach (SymbolPair pair in pairs) {
                using (MySqlCommand cmd = new MySqlCommand(
                        @"INSERT INTO `_symbols` ( symbol, name ) VALUES ( ?symbol, ?name );",
                        connection)) {
                    cmd.Parameters.AddWithValue("?symbol", pair.Symbol);
                    cmd.Parameters.AddWithValue("?name", pair.Name);

                    cmd.ExecuteNonQuery();
                }
            }

            UpdateValidity("_symbols");

            Disconnect(ref connection);
        }

        public void AddData_TSDA(DatasetTSDA dataset) {
            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
                Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                return;
            }

            string table = String.Format("tsda_{0}", dataset.Symbol);

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
                        `adjusted_close` DECIMAL(10, 4),
                        `volume` BIGINT,
                        `dividend_amount` DECIMAL(10, 4),
                        `split_coefficient` DECIMAL(10, 4),
                        `sma7` DECIMAL(10, 4),
                        `sma20` DECIMAL(10, 4),
                        `sma50` DECIMAL(10, 4),
                        `sma100` DECIMAL(10, 4),
                        `sma200` DECIMAL(10, 4),
                        `msd20` DECIMAL(10, 4),
                        `msdr20` DECIMAL(10, 6),
                        `vsma20` BIGINT,
                        `vmsd20` BIGINT
                        );", table),
                    connection))
                cmd.ExecuteNonQuery();

            // Insert the data into the table
            foreach (DailyValue dv in dataset.Values) {
                try {
                    using (MySqlCommand cmd = new MySqlCommand(String.Format(
                            @"INSERT INTO `{0}` (
                        timestamp, open, high, low, close, adjusted_close, volume, dividend_amount, split_coefficient,
                        sma7, sma20, sma50, sma100, sma200, msd20, msdr20, vsma20, vmsd20
                        ) VALUES (
                        ?timestamp, ?open, ?high, ?low, ?close, ?adjusted_close, ?volume, ?dividend_amount, ?split_coefficient,
                        ?sma7, ?sma20, ?sma50, ?sma100, ?sma200, ?msd20, ?msdr20, ?vsma20, ?vmsd20
                        );", table),
                            connection)) {
                        cmd.Parameters.AddWithValue("?timestamp", dv.Timestamp);
                        cmd.Parameters.AddWithValue("?open", dv.Open);
                        cmd.Parameters.AddWithValue("?high", dv.High);
                        cmd.Parameters.AddWithValue("?low", dv.Low);
                        cmd.Parameters.AddWithValue("?close", dv.Close);
                        cmd.Parameters.AddWithValue("?adjusted_close", dv.AdjustedClose);
                        cmd.Parameters.AddWithValue("?volume", dv.Volume);
                        cmd.Parameters.AddWithValue("?dividend_amount", dv.Dividend_Amount);
                        cmd.Parameters.AddWithValue("?split_coefficient", dv.Split_Coefficient);
                        cmd.Parameters.AddWithValue("?sma7", dv.SMA7);
                        cmd.Parameters.AddWithValue("?sma20", dv.SMA20);
                        cmd.Parameters.AddWithValue("?sma50", dv.SMA50);
                        cmd.Parameters.AddWithValue("?sma100", dv.SMA100);
                        cmd.Parameters.AddWithValue("?sma200", dv.SMA200);
                        cmd.Parameters.AddWithValue("?msd20", dv.MSD20);
                        cmd.Parameters.AddWithValue("?msdr20", dv.MSDr20);
                        cmd.Parameters.AddWithValue("?vsma20", dv.vSMA20);
                        cmd.Parameters.AddWithValue("?vmsd20", dv.vMSD20);

                        cmd.ExecuteNonQuery();
                    }
                } catch (Exception ex) {
                    // TO-DO: log errors to MySQL _errors
                }
            }

            UpdateValidity(table);

            Disconnect(ref connection);
        }

        public List<SymbolPair> GetData_Symbols() {
            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
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

            Disconnect(ref connection);
            return pairs;
        }

        public DateTime GetValidity(string table) {
            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
                Prompt.WriteLine("Unable to connect to database. Please check your settings and your connection.");
                return new DateTime();
            }

            DateTime result = new DateTime();
            using (MySqlCommand cmd = new MySqlCommand(String.Format(
                    @"SELECT `updated` FROM `_validity` WHERE `table` = '{0}';",
                    table), connection))
                result = cmd.ExecuteScalar() != null ? (DateTime)(cmd.ExecuteScalar()) : new DateTime();

            Disconnect(ref connection);
            return result;
        }

        public DateTime GetValidity_Symbols()
            => GetValidity("_symbols");

        public DateTime GetValidity_TSDA(SymbolPair pair)
            => GetValidity(String.Format("tsda_{0}", pair.Symbol));

        public decimal GetSize() {
            Init();                                     // Cannot get size of a schema with no tables!

            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
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

            Disconnect(ref connection);

            return size;
        }

        public void UpdateValidity(string table) {
            MySqlConnection connection = new MySqlConnection();
            if (!Connect(ref connection)) {
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

            Disconnect(ref connection);
        }
    }
}