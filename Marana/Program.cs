using System;

namespace Marana {

    internal class MainClass {

        public static void Main(string[] args) {
            Prompt_Key();
        }

        public static void Prompt_Key() {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}