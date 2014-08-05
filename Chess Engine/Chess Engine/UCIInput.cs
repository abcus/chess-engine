using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {

    class UCIInput {

        // Method that continuously accepts user input
        public static bool processGUIMessages(Board inputBoard) {
            
                if (Console.KeyAvailable) {
                    string input = Console.ReadLine();

                    if (input.Length > 0) {
                        return inputHandler(input, inputBoard);
                    }
                } else {
                    Thread.Sleep(50);
                }
                return true;
        }

        

        public static bool inputHandler(string input, Board inputBoard) {
            if (input == "quit") {
                return false;
            } else if (input == "perft") {
                Task perft = Task.Run(() => Test.printPerft(inputBoard, 5));
                return true;
            }
            return true;
        }

        public static void print(string name) {
            while (true) {
                Console.WriteLine("Hello " + name);
            }
        }
    }
}
