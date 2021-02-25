using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Log {

        public static async Task Error(string methodInfo, Exception ex)
            => await Error($"[{DateTime.Now:yyyy-MM-dd HH:mm}]  {methodInfo}{Environment.NewLine}{ex.Message} {Environment.NewLine}{ex.StackTrace}");

        public static async Task Error(string message) {
            try {
                using StreamWriter sw = new StreamWriter(Settings.GetErrorLogPath(), true);

                await sw.WriteLineAsync($"{Environment.NewLine}{Environment.NewLine}{message}");
                sw.Close();
            } catch (Exception ex) {
                Console.WriteLine($"{MethodBase.GetCurrentMethod().DeclaringType}: {MethodBase.GetCurrentMethod().Name}", ex);
            }
        }
    }
}