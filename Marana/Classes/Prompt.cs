using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    internal class Prompt {

        public static string[] Args() {
            NewLine();
            Write("Marana > ", ConsoleColor.Blue);
            string input = Console.ReadLine().Trim();
            return input.Split(' ');
        }

        public static void Key() {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static bool YesNo() {
            while (true) {
                Console.Write(" [y/n] ");

                string input = Console.ReadLine().Trim().ToLower();

                if (input == "y")
                    return true;
                else if (input == "n")
                    return false;
            }
        }

        public static void NewLine(int Lines = 1) {
            for (int i = 0; i < Lines; i++)
                Console.WriteLine();
        }

        public static void Write(string Message, ConsoleColor ForeColor = ConsoleColor.Gray) {
            if (String.IsNullOrEmpty(Message))
                return;

            Console.ForegroundColor = ForeColor;
            Console.Write(Message);
            Console.ResetColor();
        }

        public static void WriteLine(string Message, ConsoleColor ForeColor = ConsoleColor.Gray) {
            Console.ForegroundColor = ForeColor;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
    }
}