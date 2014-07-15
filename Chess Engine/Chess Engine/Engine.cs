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

			string test = "n1n5/PPPk4/8/8/8/8/4Kppp/5N1N b - - 0 1";

			

            Board gameBoard = new Board(test);
            InputOutput.drawBoard(gameBoard);
            Test.kingInCheckTest(gameBoard, gameBoard.getSideToMove());

			

            Stopwatch s = Stopwatch.StartNew();
            Console.WriteLine(Test.perft(5, gameBoard));
            Console.WriteLine(s.Elapsed);

        }
    }
}
  
         