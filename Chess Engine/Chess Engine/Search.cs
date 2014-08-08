using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {
    public class Search {

		
		internal Board cloneBoard;

		internal int[, ,] historyTable;
		internal int[,] killerTable;

		internal int ply;
		internal int startTime;
		internal int stopTime;
		internal int depth;
		internal int depthset;
		internal int timeset;
		internal int movesToGo;
		internal int infinite;
		internal static ulong nodes;
		internal int quit;
		internal int stopped;

		internal static moveAndEval result = new moveAndEval();

	    internal static int initialDepth;

		internal static Stopwatch s = Stopwatch.StartNew();

		// Constructor
	    public Search(Board inputBoard, DoWorkEventArgs e) {
		    cloneBoard = new Board(inputBoard);
			runSearch(e);
	    }


		public void runSearch (DoWorkEventArgs e) {
			
			result = new moveAndEval();

			// During iterative deepening if a search is interrupted before complete, then board will not be restored to original state
			// Clones the inputboard and operates on the clone so that this problem won't occur
			
			

			for (int i = 1; i <= 6; i++) {

				if (UCI_IO.searchWorker.CancellationPending) {
					e.Cancel = true;
					return;
				}

				initialDepth = i;
				result = negaMaxRoot(i);
				result.depthAchieved = i;	
			}
	    }

		// Negamax function called at the root 
		// Returns both the best move and the evaluation score
	    public moveAndEval negaMaxRoot(int depth) {
		    int alpha = -Constants.LARGE_INT;
		    int beta = Constants.LARGE_INT;

			movePicker mPicker = new movePicker(this.cloneBoard);

		    List<int> bestMoves = new List<int>();
			List<int> legalMoves = mPicker.legalMoveList;

			while (legalMoves.Count != 0) {
				this.cloneBoard.makeMove(legalMoves[0]);
				int boardScore = -negaMax(depth - 1, -beta, -alpha);

				this.cloneBoard.unmakeMove(legalMoves[0]);
				
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

		// Negamax function called from the root function
		// Returns the evaluation score
		public int negaMax(int depth, int alpha, int beta) {
		    
		    if (depth == 0) {
			    nodes ++;
				return Evaluate.evaluationFunction(this.cloneBoard);
		    } else {

				movePicker mPicker = new movePicker(this.cloneBoard);
				List<int> legalMoves = mPicker.legalMoveList;
			    int movesMade = 0;

			    while (legalMoves.Count != 0) {
					this.cloneBoard.makeMove(legalMoves[0]);
				    movesMade ++;
				    int boardScore = -negaMax(depth - 1, -beta, -alpha);
					this.cloneBoard.unmakeMove(legalMoves[0]);
				    
					if (boardScore >= beta) {
					    return beta;
				    }
				    if (boardScore > alpha) {
					    alpha = boardScore;
				    }
					legalMoves.RemoveAt(0);
			    }

				if (movesMade == 0) {
					if (this.cloneBoard.isInCheck() == true) {
						return -Constants.CHECKMATE + (initialDepth - depth);
					} else {
						return Constants.STALEMATE;
					}
				}

			    return alpha;
		    }
	    }

	    private static int quiescence(int alpha, int beta) {
		    return 0;
	    }

		// Called every 2048 nodes searched, and checks if time is up or if there is an interrupt form the GUI
	    private static void checkUp() {
		    
	    }
    }




	public sealed class movePicker {

		internal int[] pseudoLegalMoveList;
		internal List<int> legalMoveList = new List<int>(); 
		internal int index = 0;
		internal Board board;

		public movePicker(Board inputBoard) {
			this.board = inputBoard;
			if (inputBoard.isInCheck() == false) {
				this.pseudoLegalMoveList = inputBoard.generateListOfAlmostLegalMoves();
			} else {
				this.pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
			}

			foreach (int move in pseudoLegalMoveList) {

				if (move != 0) {
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
			}
		}

		public int getNextMove() {

			if (legalMoveList.Count != 0) {
				return legalMoveList[index++];
			} else {
				return 0;
			}

		}		
	}    

	 


	public struct moveAndEval {
		internal int move;
		internal int evaluationScore;
		internal int depthAchieved;
	}
    
}
