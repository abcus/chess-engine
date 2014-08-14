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

            Constants.initConstants();
            Constants.initEvalConstants();
	        Constants.initZobrist();

			Board position = new Board("1k1r3q/1ppn3p/p4b2/4p3/8/P2N2P1/1PP1R1BP/2K1Q3 w - -");
			UCI_IO.drawBoard(position);
	        
			Console.WriteLine(position.staticExchangeEval(Constants.D3, Constants.E5, Constants.WHITE));
			

			while (true) {
                if (!UCI_IO.processGUIMessages(50)) {
                    break;
                }
            }
        }
    }
}
  
         