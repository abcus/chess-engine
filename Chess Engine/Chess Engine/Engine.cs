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

			string test = "3k4/3p4/8/K1P4r/8/8/8/8 b - - 0 1";

			

            Board gameBoard = new Board(test);
            InputOutput.drawBoard(gameBoard);
            Test.kingInCheckTest(gameBoard, gameBoard.getSideToMove());

			
            Stopwatch s = Stopwatch.StartNew();
            Console.WriteLine(Test.perft(6, gameBoard));
	        //Test.perftDivide(5, gameBoard);
			Console.WriteLine(s.Elapsed);

        }
    }
}
  
         