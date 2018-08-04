﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Marana {

    class API_NasdaqTrader {

        public static string GetList () {
            FtpWebRequest request = WebRequest.Create ("ftp://ftp.nasdaqtrader.com/symboldirectory/nasdaqlisted.txt") as FtpWebRequest;

            using (FtpWebResponse response = request.GetResponse () as FtpWebResponse) {
                StreamReader reader = new StreamReader (response.GetResponseStream ());
                return reader.ReadToEnd ();
            }
        }


        public static string GetSymbols () {
            string list = GetList ();
            StringBuilder output = new StringBuilder (); ;

            foreach (string eachline in list.Split('\n', '\r')) {
                if (eachline == "" || eachline.StartsWith ("Symbol") || eachline.StartsWith ("File Creation Time"))
                    continue;
                else
                    output.AppendLine (eachline.Substring (0, eachline.IndexOf ('|')));
            }

            return output.ToString();
        }


        public static List<SymbolPair> GetSymbolPairs () {
            string list = GetList ();
            List<SymbolPair> output = new List<SymbolPair> ();

            foreach (string eachline in list.Split ('\n', '\r')) {
                if (eachline == "" || eachline.StartsWith ("Symbol") || eachline.StartsWith ("File Creation Time"))
                    continue;
                else {
                    int first = eachline.IndexOf ('|'),
                        second = eachline.IndexOf ('|', first + 1) - first;
                    output.Add (new SymbolPair {
                        Symbol = eachline.Substring (0, first),
                        Name = eachline.Substring (first + 1, second - 1)
                    });
                }
            }

            return output;
        }

    }
}