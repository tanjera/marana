using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OfficeOpenXml;


namespace Marana.Aggregator {

    /* Symbol Key:
     * x̅ : Mean
     * x̃ : Median
     * %Δ : % Change
     */

    class Aggregator {

        static DateTime YearsAgo_01 = DateTime.Today - new TimeSpan (365, 0, 0, 0, 0);
        static DateTime YearsAgo_02 = DateTime.Today - new TimeSpan (730, 0, 0, 0, 0);
        static DateTime YearsAgo_05 = DateTime.Today - new TimeSpan (1825, 0, 0, 0, 0);
        static DateTime YearsAgo_10 = DateTime.Today - new TimeSpan (3650, 0, 0, 0, 0);
        static DateTime YearsAgo_20 = DateTime.Today - new TimeSpan (7300, 0, 0, 0, 0);

        static void Main (string [] args) {

            ExcelPackage ep = new ExcelPackage ();
            ExcelWorksheet epws = ep.Workbook.Worksheets.Add (String.Format("Output {0}", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")));

            // Spreadsheet title and row headers
            int headingOffset = 2;
            epws.SetValue (1, 2, String.Format ("Marana Aggregator: Analysis of Symbols, {0}", DateTime.Now.ToString ("MM/dd/yyyy HH:mm:ss")));
            epws.SetValue (2, 1, "Symbol");
            epws.SetValue (2, 2, "Name");
            epws.SetValue (2, 3, "Oldest");
            epws.SetValue (2, 4, "Newest");
            epws.SetValue (2, 5, "Latest");
            epws.SetValue (2, 6, "YTD x̅");
            epws.SetValue (2, 7, "YTD x̃");
            epws.SetValue (2, 8, "YTD %Δ");
            epws.SetValue (2, 9, "-1Y x̅");
            epws.SetValue (2, 10, "-1Y x̃");
            epws.SetValue (2, 11, "-1Y %Δ");
            epws.SetValue (2, 12, "-2Y x̅");
            epws.SetValue (2, 13, "-2Y x̃");
            epws.SetValue (2, 14, "-2Y %Δ");
            epws.SetValue (2, 15, "-5Y x̅");
            epws.SetValue (2, 16, "-5Y x̃");
            epws.SetValue (2, 17, "-5Y %Δ");
            epws.SetValue (2, 18, "-10Y x̅");
            epws.SetValue (2, 19, "-10Y x̃");
            epws.SetValue (2, 20, "-10Y %Δ");
            epws.SetValue (2, 21, "-20Y x̅");
            epws.SetValue (2, 22, "-20Y x̃");
            epws.SetValue (2, 23, "-20Y %Δ");

            epws.Cells ["A1:W2"].Style.Font.Bold = true;
            epws.Cells ["A1:W2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            epws.Cells ["E3:W10000"].Style.Numberformat.Format = "#,##0.00";


            List<SymbolPair> pairs = API_NasdaqTrader.GetSymbolPairs ();

            for (int i = 0; i < 4 && i < pairs.Count; i++) {
                Console.Write (String.Format("Requesting data for {0}... ", pairs [i].Symbol));
                IEnumerable<DailyValue> ldv = API_AlphaVantage.GetData_TimeSeriesDaily (pairs [i].Symbol, true).OrderBy(obj => obj.Timestamp);
                Console.Write ("Request successful!");

                epws.SetValue (i + headingOffset + 1, 1, pairs [i].Symbol);                                 // Symbol
                epws.SetValue (i + headingOffset + 1, 2, pairs [i].Name);                                   // Stock Name
                epws.SetValue (i + headingOffset + 1, 3, ldv.First ().Timestamp.ToString("MM/dd/yyyy"));    // Oldest in Series
                epws.SetValue (i + headingOffset + 1, 4, ldv.Last ().Timestamp.ToString("MM/dd/yyyy"));     // Newest in Series
                epws.SetValue (i + headingOffset + 1, 5, ldv.Last ().Close);                                // Latest Value

                // Isolate value groups (timespans) to run calculations on
                var ytd = from dv in ldv where dv.Timestamp.Year == DateTime.Today.Year select dv;
                var ya01 = from dv in ldv where dv.Timestamp.Year == YearsAgo_01.Year select dv;
                var ya02 = from dv in ldv where dv.Timestamp.Year == YearsAgo_02.Year select dv;
                var ya05 = from dv in ldv where dv.Timestamp.Year == YearsAgo_05.Year select dv;
                var ya10 = from dv in ldv where dv.Timestamp.Year == YearsAgo_10.Year select dv;
                var ya20 = from dv in ldv where dv.Timestamp.Year == YearsAgo_20.Year select dv;

                // Mean, median, and % growth (from median) for each timespan
                epws.SetValue (i + headingOffset + 1, 6, (from dv in ytd select dv.Close).DefaultIfEmpty (0).Average ());
                var lbuf = (from dv in ytd orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 7, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 9, (from dv in ya01 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya01 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 10, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 12, (from dv in ya02 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya02 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 13, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 15, (from dv in ya05 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya05 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 16, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 18, (from dv in ya10 select dv.Close).DefaultIfEmpty (0).Average ());
                lbuf = (from dv in ya10 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 19, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));

                epws.SetValue (i + headingOffset + 1, 21, (from dv in ya20 select dv.Close).DefaultIfEmpty(0).Average ());
                lbuf = (from dv in ya20 orderby dv.Close select dv.Close).DefaultIfEmpty (0);
                epws.SetValue (i + headingOffset + 1, 22, (lbuf.Count () / 2 >= 0 ? lbuf.ElementAt (lbuf.Count () / 2) : 0));


                Console.Write (" ... data processed.\n");
            }

            for (int i = 1; i < 23; i++)
                epws.Column (i).AutoFit ();


            string filename = @"workbook.xlsx";
            try {
                ep.SaveAs (new FileInfo (filename));
                Console.WriteLine (String.Format ("Excel spreadsheet written to {0}", filename));
            } catch {
                Console.Write (String.Format ("Unable to save to {0}... Please enter a new filename: ", filename));
                string input = Console.ReadLine ();
                filename = input == "" ? filename : input;
                try {
                    ep.SaveAs (new FileInfo (filename));
                    Console.WriteLine (String.Format ("Excel spreadsheet written to {0}", filename));
                } catch {
                    throw new FileLoadException();
                }
            }


            Console.Write ("\n\rPress enter to continue.");
            Console.ReadLine ();

        }

    }
}
