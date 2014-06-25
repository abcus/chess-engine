using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Engine {
        
        public void run() {
            InputOutput.getFENString();
            Board gameBoard = new Board(Constants.BOARD_ARRAY);
            InputOutput.drawBoard(gameBoard);    
        }
    }
}
  
         