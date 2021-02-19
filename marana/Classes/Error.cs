using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    public class Error {

        public static string GetErrorPath() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Marana", "error.log");
        }

        public static async Task Log(string methodInfo, string message)
            => await Log($"{methodInfo}{Environment.NewLine}{message}");

        public static async Task Log(string message) {
            using (StreamWriter sw = new StreamWriter(GetErrorPath(), true)) {
                try {
                    await sw.WriteLineAsync($"{Environment.NewLine}{Environment.NewLine}{message}");
                    sw.Close();
                } catch {
                    sw.Close();
                }
            }
        }
    }
}