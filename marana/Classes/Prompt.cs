using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    internal class Prompt {

        public static List<string> Args() {
            NewLine();
            Write($"{Settings.GetOSStyling("Marana")} > ", ConsoleColor.Cyan);
            string input = Console.ReadLine().Trim();
            return new List<string>(input.Split(' '));
        }

        public static void Key() {
            Prompt.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        public static bool YesNo() {
            while (true) {
                Prompt.Write(" [yes / no] ");

                string input = Console.ReadLine().Trim().ToLower();

                if (input == "y" || input == "yes")
                    return true;
                else
                    return false;
            }
        }

        public static void NewLine(int Lines = 1) {
            for (int i = 0; i < Lines; i++)
                Prompt.WriteLine("");
        }

        public static void Write(string Message, ConsoleColor ForeColor = ConsoleColor.White) {
            if (String.IsNullOrEmpty(Message))
                return;

            Console.ForegroundColor = ForeColor;
            Console.Write(Message);
            Console.ResetColor();
        }

        public static void WriteLine(string Message, ConsoleColor ForeColor = ConsoleColor.White) {
            Console.ForegroundColor = ForeColor;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
    }
}