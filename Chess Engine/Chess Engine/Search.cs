using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zobrist = System.UInt64;

namespace Chess_Engine {
    public static class Search {

		internal static Board cloneBoard;
	    
		internal static int[, ,] historyTable;
		internal static int[,] killerTable;
	    
		internal static moveAndEval result;
		internal static int initialDepth;
		internal static ulong nodesEvaluated;

	    internal static double failHigh;
	    internal static double failHighFirst;

	    internal static int numOfSuccessfulZWS;
	    
		internal static Stopwatch s;

		// Resets all of the static search variables, then initializes the search
		public static void initSearch(Board inputBoard, DoWorkEventArgs e) {
		    cloneBoard = new Board(inputBoard);
			
			historyTable = new int[64,64,13];
			killerTable = new int[2,42];

			result =  new moveAndEval();
		    initialDepth = 0;
		    nodesEvaluated = 0;
			failHigh = 0;
			failHighFirst = 0;

			numOfSuccessfulZWS = 0;
			
			s = Stopwatch.StartNew();

			runSearch(e);
	    }


		public static void runSearch (DoWorkEventArgs e) {
			
			result = new moveAndEval();

			// During iterative deepening if a search is interrupted before complete, then board will not be restored to original state
			// Clones the inputboard and operates on the clone so that this problem won't occur
			for (int i = 1; i <= 6; i++) {

				if (UCI_IO.searchWorker.CancellationPending) {
					e.Cancel = true;
					return;
				}

				initialDepth = i;
				result = PVSRoot(i);
				result.depthAchieved = i;	
			}
	    }

		// Negamax function called at the root 
		// Returns both the best move and the evaluation score
	    public static moveAndEval PVSRoot(int depth) {
		    int alpha = -Constants.LARGE_INT;
		    int beta = Constants.LARGE_INT;
		    Zobrist zobristKey = Search.cloneBoard.zobristKey;

			TTEntry entry = UCI_IO.hashTable.probeTTable(zobristKey);

			if (entry.key == zobristKey && entry.depth >= depth) {
				moveAndEval tableResult = new moveAndEval();
				tableResult.evaluationScore = entry.evaluationScore;
				tableResult.move = entry.move;
				return tableResult;
			}

			movePicker mPicker = new movePicker(Search.cloneBoard);
			List<int> bestMoves = new List<int>();
			stateVariables restoreData = new stateVariables(Search.cloneBoard);

			while (true) {

				int move = mPicker.getNextMove();

				if (move == 0) {
					break;
				}

				Search.cloneBoard.makeMove(move);
				int boardScore = -PVS(depth - 1, -beta, -alpha);

				Search.cloneBoard.unmakeMove(move, restoreData);
				
			    if (boardScore > alpha) {
				    alpha = boardScore;
				    bestMoves.Clear();
				    bestMoves.Add(move);
			    }
			    else if (boardScore == alpha) {
				    bestMoves.Add(move);
			    }
		    }
			TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, alpha, bestMoves[0]);
			UCI_IO.hashTable.storeTTable(zobristKey, newEntry);

		    moveAndEval result = new moveAndEval();
		    result.evaluationScore = alpha;
		    result.move = bestMoves[0];
		    return result;
	    }

		// Negamax function called from the root function
		// Returns the evaluation score
		// It is fail hard (if score > beta it returns beta, if score < alpha it returns alpha)
		public static int PVS(int depth, int alpha, int beta) {
		    
			// At the leaf nodes
		    if (depth <= 0) {

				// Probe the hash table, and if a match is found then return the score
				// (validate the key to prevent type 2 collision)
			    Zobrist zobristKey = Search.cloneBoard.zobristKey;
				TTEntry entry = UCI_IO.hashTable.probeTTable(zobristKey);
				// If we set it to depth >= 0, then get a slightly different result???
				if (entry.key == zobristKey && entry.depth == 0) {
					nodesEvaluated++;
					return entry.evaluationScore;
			    }

				// Otherwise, find the evaluation and store it in the table for future use
				int evaluationScore = Evaluate.evaluationFunction(Search.cloneBoard);
				TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, 0, evaluationScore);
				UCI_IO.hashTable.storeTTable(zobristKey, newEntry);

				nodesEvaluated++;
			    return evaluationScore;

			    // return quiescence(alpha, beta);
		    } 
			// At the interior nodes
			else {

				// Probe the hash table and if the entry's depth is greater than or equal to current depth:
			    Zobrist zobristKey = Search.cloneBoard.zobristKey;
			    TTEntry entry = UCI_IO.hashTable.probeTTable(zobristKey);

				// If we set it to entry.depth >= depth, then get a slightly different (perhaps move accurate?) result???
				if (entry.key == zobristKey && entry.depth == depth) {
					// If it is a PV node, see if it is greater than beta (if so return beta), less than alpha (if so return alpha), or in between (if so return the exact score)
					// If it is a CUT node, see if this lower bound is greater than beta (if so return beta)
					// If it is an ALL node, see if this upper bound is less than alpha (is so return alpha)
					// Using a fail-hard here so need those extra conditions instead of just returning the score
					if (entry.flag == Constants.PV_NODE) {
					    int evaluationScore = entry.evaluationScore;

						if (evaluationScore == -Constants.CHECKMATE) {
							return -Constants.CHECKMATE + (initialDepth - depth);
						} else if (evaluationScore == Constants.STALEMATE) {
							return Constants.STALEMATE;
						}

						if (evaluationScore >= beta) {
						    return beta;
					    } else if (evaluationScore <= alpha) {				    
						    return alpha;
					    } else if (evaluationScore > alpha && evaluationScore < beta) {
						    return evaluationScore;
					    }
				    } if (entry.flag == Constants.CUT_NODE) {
					    int evaluationScore = entry.evaluationScore;
					    if (evaluationScore >= beta) {
							return beta;
					    }
				    } else if (entry.flag == Constants.ALL_NODE) {
						int evaluationScore = entry.evaluationScore;
						if (evaluationScore <= alpha) {
							return alpha;
					    }
				    } 
			    }

				// Creates a move picker object
				movePicker mPicker = new movePicker(Search.cloneBoard);

			    int bestMove = 0;

				// Backs up the board's instance variables (that are restored during the unmake move)
				stateVariables restoreData = new stateVariables(Search.cloneBoard);
				
				// gets the first move from the move picker object
				// The first move is searched with a full window search; subsequent moves are searched with a zero window search
				int move = mPicker.getNextMove();
				
				// If the first move is 0, that means there is no legal moves, which means the side to move is either in checkmate or stalemate
				if (move == 0) {
					if (Search.cloneBoard.isInCheck() == true) {
						// Returns the mate score - number of moves made from root to mate 
						// Ensures that checkmates closer to the root will get a higher score, so that they will be played
						TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, -Constants.CHECKMATE);
						UCI_IO.hashTable.storeTTable(zobristKey, newEntry);
						return -Constants.CHECKMATE + (initialDepth - depth);
					} else {
						TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, Constants.STALEMATE);
						UCI_IO.hashTable.storeTTable(zobristKey, newEntry);
						return Constants.STALEMATE;
					}
				}
				// Keeps track to see whether or not alpha was raised (to see if we failed low or not)
				// Necessary when storing entries in the transpositino table
			    bool raisedAlpha = false;

				// Performs a full window search
				Search.cloneBoard.makeMove(move);
				int boardScore = -PVS(depth - 1, -beta, -alpha);
				Search.cloneBoard.unmakeMove(move, restoreData);

				if (boardScore > alpha) {
					raisedAlpha = true;
					
					// If the score was greater than beta, we have a beta cutoff (and move is too good to be played)
					if (boardScore >= beta) {

						// Store the value in the transposition table
						TTEntry newEntry = new TTEntry(zobristKey, Constants.CUT_NODE, depth, beta, move);
						UCI_IO.hashTable.storeTTable(zobristKey, newEntry);
						
						failHighFirst++;
						return beta; // return beta and not best score (fail-hard)
					}
					// If no beta cutoff but board score was higher than old alpha, then raise alpha
					bestMove = move;
					alpha = boardScore;
				}
				
				// Loop through all other moves and perform zero window search
				while (true) {
					move = mPicker.getNextMove();
					if (move == 0) {
						break;
					}

					Search.cloneBoard.makeMove(move);
				    boardScore = -PVS(depth - 1, -alpha - 1, -alpha);
					numOfSuccessfulZWS ++;

				    if (boardScore > alpha) {
					    numOfSuccessfulZWS --;
						boardScore = -PVS(depth - 1, -beta, -alpha);
				    }
					Search.cloneBoard.unmakeMove(move, restoreData);

					if (boardScore > alpha) {
						raisedAlpha = true;

						if (boardScore >= beta) {
							TTEntry newEntry = new TTEntry(zobristKey, Constants.CUT_NODE, depth, beta, move);
							UCI_IO.hashTable.storeTTable(zobristKey, newEntry);

							failHigh++;
							return beta; // return beta and not best score (fail-hard)
						}
						bestMove = move;
						alpha = boardScore;
					}
				}
			    if (raisedAlpha == true) {
					TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, alpha, bestMove);
					UCI_IO.hashTable.storeTTable(zobristKey, newEntry);
			    } else if (raisedAlpha == false) {
					TTEntry newEntry = new TTEntry(zobristKey, Constants.ALL_NODE, depth, alpha);
					UCI_IO.hashTable.storeTTable(zobristKey, newEntry);
			    }
				return alpha; //return alpha whether it was raised or not (fail-hard)
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
		internal Board board;

		public movePicker(Board inputBoard) {
			this.board = inputBoard;
			if (inputBoard.isInCheck() == false) {
				this.pseudoLegalMoveList = inputBoard.generateListOfAlmostLegalMoves();
			} else {
				this.pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
			}

			stateVariables restoreData = new stateVariables(inputBoard);

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
						inputBoard.unmakeMove(move, restoreData);
					} else {
						legalMoveList.Add(move);
					}
				}
			}
		}

		public int getNextMove() {

			if (legalMoveList.Count != 0) {
				int move = legalMoveList[0];
				legalMoveList.RemoveAt(0);
				return move;
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
