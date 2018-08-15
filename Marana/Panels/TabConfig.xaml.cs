using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace Marana {
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class TabConfig : UserControl {

        Main wdwMain;

        public TabConfig (Main m) {
            InitializeComponent ();

            wdwMain = m;

            txtAPIKey_AlphaVantage.Text = wdwMain.Config.APIKey_AlphaVantage;
            txtDirectoryLibrary.Text = wdwMain.Config.Directory_Library;
        }


        private void btnDirPathLibrary_Click (object sender, RoutedEventArgs e) {
            txtDirectoryLibrary.Text = Main.FileSelection_Dialog (true, null);
        }


        private void btnSaveConfig_Click (object sender, RoutedEventArgs e) {
            if (Configuration.SaveConfig(new Settings {
                APIKey_AlphaVantage = txtAPIKey_AlphaVantage.Text,
                Directory_Library = txtDirectoryLibrary.Text
            }))
                MessageBox.Show ("Configuration updated successfully!", "Configuration Saved");
            else
                MessageBox.Show ("Error: failed to save configuration file.", "Configuration Not Saved");
        }

    }
}
