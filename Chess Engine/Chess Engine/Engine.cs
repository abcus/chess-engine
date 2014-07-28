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

            string test = "8/PP6/8/3k4/8/8/4Kppp/7Q b - - 0 3";
	        Board.FENToBoard(Constants.FEN_RANDOM);
	        InputOutput.drawBoard();
	        Test.kingInCheckTest(Board.getSideToMove());

            Stopwatch s = Stopwatch.StartNew();

            
           Console.WriteLine(Test.perft2(5));
          
            Console.WriteLine(s.Elapsed);

            //Test.perftSuite1();
            //Test.perftSuite2();


        }
    }
}
  
         