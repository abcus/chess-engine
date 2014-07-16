using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Engine {
        
        public void run() {
            Constants.initializeConstants();

			string test = "n1n5/1Pk5/8/8/8/8/5Kp1/5N1N w - - 0 1";

			

            Board gameBoard = new Board(test);
            InputOutput.drawBoard(gameBoard);
            Test.kingInCheckTest(gameBoard, gameBoard.getSideToMove());

			
            Stopwatch s = Stopwatch.StartNew();
            Console.WriteLine(Test.perft(5, gameBoard));
            Console.WriteLine(s.Elapsed);

        }
    }
}
  
         