using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace Marana {

    public partial class Main : Window {

        public Settings Config = new Settings ();

        public Main () {
            InitializeComponent ();

            Config = Configuration.Init ();
            tabConfiguration.Content = new TabConfig (this);
            tabCharts.Content = new TabCharts (this);
            tabAggregator.Content = new TabAggregator (this);
            tabLibrary.Content = new TabLibrary (this);
        }


        public static string FileSelection_Dialog (bool directory, CommonFileDialogFilter [] filters) {
            var dlg = new CommonOpenFileDialog ();
            if (filters != null)
                foreach (CommonFileDialogFilter filter in filters)
                    dlg.Filters.Add (filter);

            dlg.IsFolderPicker = directory;
            CommonFileDialogResult res = dlg.ShowDialog ();
            if (res == CommonFileDialogResult.Ok)
                return dlg.FileName;
            else
                return null;
        }

    }
}
