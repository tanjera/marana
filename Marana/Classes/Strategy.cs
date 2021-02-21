using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public static class Strategy {

        public static async Task<string> Interpret(string query, string symbol, DateTime day) {
            return query?
                .Replace("{SYMBOL}", symbol)
                .Replace("{DATE}", day.ToString("yyyy-MM-dd"));
        }
    }
}