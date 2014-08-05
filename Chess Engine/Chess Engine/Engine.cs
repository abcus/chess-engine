using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    public class Engine {

        public Engine() {
            
        }

        public void run () {
            
            string test = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            Constants.init();
            Constants.initEvalConstants();

            Board position = new Board(Constants.FEN_RANDOM);
            InputOutput.drawBoard(position);
            Test.kingInCheckTest(position, position.getSideToMove());

            while (true) {
                if (UCIInput.processGUIMessages(position) == false) {
                    break;
                }
            }
        }
    }
}
  
         