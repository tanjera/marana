using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marana {

    internal class Prompts {

        public static string[] Args() {
            Console.Write("Enter args[] > ");
            string input = Console.ReadLine().Trim();
            return input.Split(' ');
        }

        public static void Key() {
            System.Console.WriteLine();
            System.Console.WriteLine();
            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }

        public static bool YesNo(string Message) {
            while (true) {
                System.Console.WriteLine(String.Format("{0}, [y/n]", Message));
                string input = System.Console.ReadLine().Trim();

                if (input.ToLower() == "y")
                    return true;
                else if (input.ToLower() == "n")
                    return false;
            }
        }
    }
}