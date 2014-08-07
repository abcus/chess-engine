using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {
    public static class Search {

        private static Board cloneBoard;

		public static moveAndEval runSearch (Board inputBoard, CancellationToken searchStopToken) {
			
			// During iterative deepening if a search is interrupted before complete, then board will not be restored to original state
			// Clones the inputboard and operates on the clone so that this problem won't occur
			cloneBoard = new Board(inputBoard);
			moveAndEval result = negaMaxRoot(cloneBoard, 6);
			return result;
	    }


	    public static moveAndEval negaMaxRoot(Board inputBoard, int depth) {
		    int alpha = -Constants.LARGE_INT;
		    int beta = Constants.LARGE_INT;

		    List<int> bestMoves = new List<int>();
		    List<int> legalMoves = inputBoard.getLegalMove();

			while (legalMoves.Count != 0) {
			    inputBoard.makeMove(legalMoves[0]);
				int boardScore = -negaMax(inputBoard, depth - 1, -beta, -alpha);
				
				inputBoard.unmakeMove(legalMoves[0]);
				
			    if (boardScore > alpha) {
				    alpha = boardScore;
				    bestMoves.Clear();
				    bestMoves.Add(legalMoves[0]);
			    }
			    else if (boardScore == alpha) {
				    bestMoves.Add(legalMoves[0]);
			    }
			    legalMoves.RemoveAt(0);
		    }
		    moveAndEval result = new moveAndEval();
		    result.evaluationScore = alpha;
		    result.move = bestMoves[0];
		    return result;
	    }


	    public static int negaMax(Board inputBoard, int depth, int alpha, int beta) {
		    
		    if (depth == 0) {
			    return Evaluate.evaluationFunction(inputBoard);
		    } else {
				List<int> legalMoves = inputBoard.getLegalMove();

			    if (legalMoves.Count == 0) {
				    if (inputBoard.isInCheck() == true) {
					    return -Constants.CHECKMATE;
				    } else {
					    return Constants.STALEMATE;
				    }
			    }

			    while (legalMoves.Count != 0) {
				    inputBoard.makeMove(legalMoves[0]);
				    int boardScore = -negaMax(inputBoard, depth - 1, -beta, -alpha);
					inputBoard.unmakeMove(legalMoves[0]);
				    if (boardScore >= beta) {
					    return beta;
				    }
				    if (boardScore > alpha) {
					    alpha = boardScore;
				    }
					legalMoves.RemoveAt(0);
			    }
			    return alpha;
		    }
	    }
    }

	

		
			
	    

	 


	public sealed class moveAndEval {
		internal int move;
		internal int evaluationScore;
	}
    
}
