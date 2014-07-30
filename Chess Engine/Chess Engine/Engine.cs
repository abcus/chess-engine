using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    public class Engine {

        public Engine() {
            
        }

        public void run() {
            string test = "8/PP6/8/3k4/8/8/4Kppp/7Q b - - 0 3";
            Constants.initializeConstants();

            Board position = new Board(Constants.FEN_RANDOM);
            InputOutput.drawBoard(position);
	        Test.kingInCheckTest(position, position.getSideToMove());

            //Test.printPerft(position, 5);

            //Test.perftSuite1(position);
            Test.perftSuite2(position);


        }
    }
}
  
         