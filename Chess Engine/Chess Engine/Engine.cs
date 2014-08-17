using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Bitboard = System.UInt64;
using Zobrist = System.UInt64;
using Score = System.UInt32;
using Move = System.UInt32;

namespace Chess_Engine {
    
    public class Engine {

        // Constructor
        public Engine() {  
        }

        // Engine's run method
        public void run () {

            Constants.initBoardConstants();
            Constants.initEvalConstants();
	        Constants.initSearchConstants();

			/*Board temp = new Board("7k/8/8/4pP2/8/4r3/8/4Q2K w - - 0 1");
			UCI_IO.drawBoard(temp);
			Console.WriteLine(temp.staticExchangeEval(Constants.F5, Constants.E6, Constants.WHITE));*/

	        while (true) {
                if (!UCI_IO.processGUIMessages(50)) {
                    break;
                }
            }
        }
    }
}
  
         