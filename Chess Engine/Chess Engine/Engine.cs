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

            string test = "r4k1r/p1pNqpb1/Bn2pnp1/3P4/1p2P3/2N2Q1p/PPPB1PPP/R3K2R b KQ - 0 2";
	        Board.FENToBoard(Constants.FEN_RANDOM);
	        InputOutput.drawBoard();
	        Test.kingInCheckTest(Board.getSideToMove());

            Test.printPerft(5);
            Test.perftSuite1();
            //Test.perftSuite2();
            
            
        }
    }
}
  
         