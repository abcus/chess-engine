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
			OpeningBook.initOpeningBook();

	        while (true) {
                if (!UCI_IO.processGUIMessages(50)) {
                    break;
                }
            }
        }
    }
}
  
         