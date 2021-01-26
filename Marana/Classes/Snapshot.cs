using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Marana {

    public class Snapshot {

        public static void Macro(List<string> args, Settings settings) {
            string filepath = Path.Combine(settings.Directory_Library, String.Format("Snapshot {0}.xlsx", DateTime.Now.ToString("yyyyMMdd-HHmmss")));

            SpreadsheetDocument ssdoc = SpreadsheetDocument.Create(filepath, SpreadsheetDocumentType.Workbook);

            WorkbookPart wbpart = ssdoc.AddWorkbookPart();
            wbpart.Workbook = new Workbook();

            WorksheetPart wspart = wbpart.AddNewPart<WorksheetPart>();
            SheetData sdata = new SheetData();
            wspart.Worksheet = new Worksheet(sdata);

            Sheets sheets = ssdoc.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            // Process each .json file to a data structure, then to a spreadsheet sheet
            DirectoryInfo ddir = new DirectoryInfo(settings.Directory_LibraryData);
            List<FileInfo> dfiles = new List<FileInfo>(ddir.GetFiles("*.json"));

            List<SymbolPair> pairs = API_NasdaqTrader.GetSymbolPairs();

            // Trim the list of symbols to parse based on user input
            if (args.Count > 0) {
                int si = 0, ei = 0;     // Start index, end index ;  for trimming

                string s = "", e = "";

                s = (from file in dfiles where file.Name.StartsWith(args[0].Trim().ToUpper()) select file.FullName).DefaultIfEmpty("").First();
                if (args.Count > 1)
                    e = (from file in dfiles where file.Name.StartsWith(args[1].Trim().ToUpper()) select file.FullName).DefaultIfEmpty("").First();

                si = dfiles.FindIndex(o => o.FullName == s);
                ei = dfiles.FindIndex(o => o.FullName == e) - si + 1;

                if (si > 0)     // Trim beginning and end of List<> per starting and ending indices (inclusive)
                    dfiles.RemoveRange(0, si);
                if (ei > 0)
                    dfiles.RemoveRange(ei, dfiles.Count - ei);
            }

            // Process each symbol to its own sheet

            /*
            for (int i = 0; i < dfiles.Count; i++) {
                string symbol = dfiles[i].Name.Substring(0, dfiles[i].Name.IndexOf(' ')).Trim();
                string name = (from pair in pairs where pair.Symbol == symbol select pair.Name).First();

                IEnumerable<DailyValue> ldv;
                using (StreamReader sr = new StreamReader(dfiles[i].FullName))
                    ldv = API_AlphaVantage.ProcessData_TimeSeriesDaily(sr.ReadToEnd()).OrderBy(obj => obj.Timestamp);

                Sheet sheet = new Sheet() {
                    Id = ssdoc.WorkbookPart.GetIdOfPart(wspart),
                    SheetId = (uint)(i + 1),
                    Name = symbol
                };

                sheets.Append(sheet);

                Prompt.WriteLine(String.Format("Formatting data for symbol {0}", symbol));

                for (int j = 0; j < ldv.Count<DailyValue>(); j++) {
                    DailyValue dv = ldv.ElementAt(j);

                    Row row = new Row();
                    row.AppendChild(new Cell() { CellValue = new CellValue(dv.Timestamp) });
                    row.AppendChild(new Cell() { CellValue = new CellValue(dv.Open) });
                    row.AppendChild(new Cell() { CellValue = new CellValue(dv.High) });
                    row.AppendChild(new Cell() { CellValue = new CellValue(dv.Low) });
                    row.AppendChild(new Cell() { CellValue = new CellValue(dv.Close) });
                    row.AppendChild(new Cell() { CellValue = new CellValue(dv.Volume) });

                    sdata.AppendChild(row);
                }
            }
            */

            wbpart.Workbook.Save();
            ssdoc.Close();
        }
    }
}