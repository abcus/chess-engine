using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Engine {
        
        public void run() {
            Constants.initializeConstants();

            string test = "k7/8/8/8/8/6n1/6P1/6RK w KQkq - 0 1";

            Board gameBoard = new Board(test);
            InputOutput.drawBoard(gameBoard);

            List<uint> temp = LegalMoveGenerator.generateListOfLegalMoves(gameBoard);
            Console.WriteLine("");
            Test.kingInCheckTest(gameBoard, gameBoard.getSideToMove());
            Console.WriteLine("Number of legal moves in this position: " + temp.Count);
        }
    }
}
  
         