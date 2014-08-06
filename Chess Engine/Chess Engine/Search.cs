using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {
    public class Search {

        static Random rnd = new Random();

        public static int alphaBetaRoot(Board inputBoard, CancellationToken ct) {
            
            int[] pseudoLegalMoveList;
            List<int> legalMoveList = new List<int>();
    
		    if (inputBoard.isInCheck() == false) {
		        pseudoLegalMoveList = inputBoard.generateListOfAlmostLegalMoves();
		    } else {
                pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
		    }
            
            foreach (int move in pseudoLegalMoveList) {
                int pieceMoved = ((move & Constants.PIECE_MOVED_MASK) >> 0);
                int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
                int flag = ((move & Constants.FLAG_MASK) >> 16);

                if (flag == Constants.EN_PASSANT_CAPTURE || pieceMoved == Constants.WHITE_KING ||
                    pieceMoved == Constants.BLACK_KING) {
                    inputBoard.makeMove(move);
                    if (inputBoard.isMoveLegal(sideToMove) == true) {
                        legalMoveList.Add(move);
                    }
                    inputBoard.unmakeMove(move);
                } else {
                    legalMoveList.Add(move);    
                }
            }

            return legalMoveList[0];
            
            while (!ct.IsCancellationRequested) {
               
            }
            
        }

    }

    class MoveSorter {
        
    }
}
