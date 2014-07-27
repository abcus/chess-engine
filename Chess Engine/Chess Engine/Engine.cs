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

            string test = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -";
	        Board.FENToBoard(test);
	        InputOutput.drawBoard();
	        Test.kingInCheckTest(Board.getSideToMove());

            //Test.perftDivide(1);

            //Test.printPerft(7);
            Test.perftSuite1();
            //Test.perftSuite2();
            
            
        }
    }
}
  
         