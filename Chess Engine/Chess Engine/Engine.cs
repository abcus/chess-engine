using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Engine {
        
        public void run() {
            Constants.initializeConstants();

            string test = "8/P7/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

            Board gameBoard = new Board(test);
            InputOutput.drawBoard(gameBoard);

            List<uint> temp = LegalMoveGenerator.generateListOfLegalMoves(gameBoard);
            Console.WriteLine(temp.Count);
        }
    }
}
  
         