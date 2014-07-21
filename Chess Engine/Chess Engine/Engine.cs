using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    public static class Engine {
        
        public static void run() {
            Constants.initializeConstants();

	        Test.perftSuite();
			
			//string test = "8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1";
	        Board.FENToBoard(Constants.FEN_START);
	        InputOutput.drawBoard();
	        Test.kingInCheckTest(Board.getSideToMove());

            //Test.perftDivide(6, gameBoard);

            Stopwatch s = Stopwatch.StartNew();
            int numberOfNodes = Test.perft(6);
            Console.WriteLine(numberOfNodes);
	        Console.WriteLine(s.Elapsed);
            Console.WriteLine("Nodes per second:" + (numberOfNodes)/(s.ElapsedMilliseconds/1000));

        }
    }
}
  
         