using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    public static class Engine {
        
        public static void run() {
            Constants.initializeConstants();

            //string test = "8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1";
	        Board.FENToBoard(Constants.FEN_RANDOM);
	        InputOutput.drawBoard();
	        Test.kingInCheckTest(Board.getSideToMove());

            //Test.perftSuite2();

            Test.printPerft(5);
            Test.perftSuite1();
        }
    }
}
  
         