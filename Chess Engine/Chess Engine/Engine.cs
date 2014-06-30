using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Engine {
        
        public void run() {
            Constants.initializeConstants();
            Board gameBoard = new Board(Constants.FEN_START);
            InputOutput.drawBoard(gameBoard);    
        }
    }
}
  
         