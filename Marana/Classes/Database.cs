using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data;
using MySql.Data.MySqlClient;

namespace Marana {

    public class Database {
        private MySqlConnection Connection;

        public void Connect(Settings settings) {
            string connString = String.Format("server={0};user={1};database={2};port={3};password={4}",
                settings.Database_Server, settings.Database_User, settings.Database_Schema, settings.Database_Port, settings.Database_Password);

            Connection = new MySqlConnection(connString);

            try {
                Prompt.Write("Connecting to Database... ");
                Connection.Open();
                Prompt.WriteLine("success!");
            } catch (Exception ex) {
                Prompt.WriteLine(ex.Message, ConsoleColor.Red);
            }
        }

        public void Disconnect() {
            Connection.Close();
        }

        public void Setup() {
            MySqlCommand cmd;

            Prompt.WriteLine("Creating table: _symbols ");
            cmd = new MySqlCommand(
                @"CREATE TABLE IF NOT EXISTS `_symbols` (
                    `id` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    `symbol` VARCHAR(8) NOT NULL,
                    `name` VARCHAR(256) NULL
                    ) AUTO_INCREMENT = 1;",
                Connection);
            cmd.ExecuteNonQuery();

            Prompt.WriteLine("Creating table: _parity ");
            cmd = new MySqlCommand(
                @"CREATE TABLE IF NOT EXISTS `_parity` (
                    `id` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    `table` VARCHAR(64) NOT NULL,
                    `update_started` DATETIME,
                    `update_complete` DATETIME,
                    `client_started` VARCHAR(64),
                    `client_finished` VARCHAR(64)
                    ) AUTO_INCREMENT = 1;",
                Connection);
            cmd.ExecuteNonQuery();
        }

        public void Setup_TSDA(string symbol) {
            MySqlCommand cmd;
            Prompt.WriteLine(String.Format("Creating table: tsda_{0}", symbol));
            cmd = new MySqlCommand(String.Format(
                @"CREATE TABLE IF NOT EXISTS `{0}` (
                    `id` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    `timestamp` DATE NOT NULL,
                    `open` DECIMAL(6, 4),
                    `high` DECIMAL(6, 4),
                    `low` DECIMAL(6, 4),
                    `close` DECIMAL(6, 4),
                    `adjusted_close` DECIMAL(6, 4),
                    `volume` BIGINT,
                    `dividend_amount` DECIMAL(6, 4),
                    `split_coefficient` DECIMAL(6, 4),
                    `sma7` DECIMAL(6, 4),
                    `sma20` DECIMAL(6, 4),
                    `sma50` DECIMAL(6, 4),
                    `sma100` DECIMAL(6, 4),
                    `sma200` DECIMAL(6, 4),
                    `msd20` DECIMAL(6, 4),
                    `msdr20` DECIMAL(6, 6),
                    `vsma20` BIGINT,
                    `vmsd20` BIGINT
                );", symbol),
                Connection);
            cmd.ExecuteNonQuery();
        }
    }
}