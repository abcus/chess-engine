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
using Microsoft.Win32;
using Zobrist = System.UInt64;

namespace Chess_Engine {
    public static class Search {

		internal static Board cloneBoard;
	    
		internal static int[,] historyTable;
		internal static int[,] killerTable;
	    
		internal static moveAndEval result;
		internal static int initialDepth;
		internal static ulong nodesVisited;
	    internal static int researches;

	    internal static double failHigh;
	    internal static double failHighFirst;

	    internal static int numOfSuccessfulZWS;
	    
	    internal static DoWorkEventArgs stopEvent;

	    internal static SearchInfo info;
	    internal static DateTime finishTime;

	    internal static List<string> PVLine;

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// INITIALIZE SEARCH
		// Resets all of the static search variables, then initializes the search
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public static void initSearch(SearchInfo info, Board inputBoard, DoWorkEventArgs e) {

			// Copies the reference for the input board into a static variable 
			cloneBoard = inputBoard;

			Search.info = info;
			
			// Resets the history table and killer table
			historyTable = new int[13,64];
			killerTable = new int[Constants.MAX_DEPTH, 2];

			// Resets other variables
			result =  new moveAndEval();
		    
			initialDepth = 0;
		    nodesVisited = 0;
			researches = 0;
			failHigh = 0;
			failHighFirst = 0;
			numOfSuccessfulZWS = 0;

			// Sets the stop search event
			stopEvent = e;

			// Resets the PV line string list
			PVLine = new List<string>();

			// Starts the iterative deepening
			runSearch();
	    }

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// ITERATIVE DEEPENING FUNCTION
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public static void runSearch () {

			// Set the target finish time (don't do it too early)
			finishTime = TimeControl.getFinishTime(info);
			result = new moveAndEval();
			
			int bookMove = OpeningBook.probeBookMove(cloneBoard);
			
			// If there is a book move, then return it
			// Otherwise, start iterative deepening
			if (bookMove != 0) {
				UCI_IO.plyOutOfBook = 0;
				result.move = bookMove;
			} else {
				UCI_IO.plyOutOfBook ++;
				// Initially set alpha and beta to - infinity and + infinity
				int alpha = -Constants.LARGE_INT;
				int beta = Constants.LARGE_INT;

				// Declare and initialize the aspiration window
				int currentWindow = Constants.ASP_WINDOW;

				DateTime iterationStartTime = DateTime.Now;
				for (int i = 1; i <= Constants.MAX_DEPTH;) {

					// sets the initial depth (for mate score calculation)
					initialDepth = i;

					// Before each search, it sees if it should quit
					if (quitSearch()) {
						return;
					}

					moveAndEval tempResult = PVSRoot(i, alpha, beta);

					// If PVSRoot at depth i returned null
					// that means that the search wasn't completed and time has run out, so terminate the thread
					// The result variable will be from the last completed iteration
					// If PVSRoot returned a value of alpha
					// That means we failed low (no move had a score that exceeded alpha)
					// Widen the window by a factor of 2, and search again (beta is untouched because otherwise it may lead to search instability)
					// If PVSRoot returned a value of beta
					// That means we failed high (there was a move whose score exceeded beta)
					// Widen the window by a factor of 2, and search again (alpha is untouched because otherwise it may lead to search instability)
					// If PVSRoot returned a value between alpha and beta, the search completed successfully
					// We set the result variable equal to the return value of the function
					if (tempResult == null) {
						return;
					} if (tempResult.evaluationScore == alpha) {
						currentWindow *= 2;
						alpha -= (int)(0.5 * currentWindow);
						researches++;
					} else if (tempResult.evaluationScore == beta) {
						currentWindow *= 2;
						beta += (int)(0.5 * currentWindow);
						researches++;
					} else {
						result = tempResult;
						result.depthAchieved = i;
						result.time = (long)(DateTime.Now - iterationStartTime).TotalMilliseconds;
						result.nodesVisited = nodesVisited;

						PVLine = UCI_IO.transpositionTable.getPVLine(Search.cloneBoard, i);
						UCI_IO.printInfo(PVLine, i);
						failHighFirst = 0;
						failHigh = 0;

						// Reset the current window, and set alpha and beta for the next iteration
						currentWindow = Constants.ASP_WINDOW;
						alpha = result.evaluationScore - currentWindow;
						beta = result.evaluationScore + currentWindow;
						iterationStartTime = DateTime.Now;
						nodesVisited = 0;
						researches = 0;
						i++;
					}
				}	
			}
	    }

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// PVS ROOT
		// Returns both the best move and the evaluation score
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

	    public static moveAndEval PVSRoot(int depth, int alpha, int beta) {
		    Zobrist zobristKey = Search.cloneBoard.zobristKey;

			TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);

			// Make sure that the node is a PV node 
			if (entry.key == zobristKey && entry.depth >= depth && entry.flag == Constants.PV_NODE) {
				moveAndEval tableResult = new moveAndEval();
				tableResult.evaluationScore = entry.evaluationScore;
				tableResult.move = entry.move;
				return tableResult;
			}

			movePicker mPicker = new movePicker(Search.cloneBoard, depth);
		    bool firstMove = true;
			List<int> bestMoves = new List<int>();
			stateVariables restoreData = new stateVariables(Search.cloneBoard);
			bool raisedAlpha = false;

			while (true) {

				int move = mPicker.getNextMove();
				int boardScore;

				if (move == 0) {
					break;
				}

				Search.cloneBoard.makeMove(move);
				
				if (firstMove == true) {
					boardScore = -PVS(depth - 1, -beta, -alpha);

					// The first move is assumed to be the best move
					// If it failed low, that means that the rest of the moves will probably fail low, so don't bother searching them and return alpha right away (to start research)
					if (boardScore < alpha) {
						moveAndEval failLowResult = new moveAndEval();
						failLowResult.evaluationScore = alpha;
						return failLowResult;
					}
				} else {
					boardScore = -PVS(depth - 1, -alpha - 1, -alpha);
					numOfSuccessfulZWS++;

					if (boardScore > alpha) {
						numOfSuccessfulZWS--;
						boardScore = -PVS(depth - 1, -beta, -alpha);
					}
				}
				Search.cloneBoard.unmakeMove(move, restoreData);
				firstMove = false;

				// At the end of every move, check to see if there is a cancellation pending; if so then return null 
				// A null value will signify that the search for that depth wasn't completed
				if (quitSearch() == true) {
					return null;
				}

				nodesVisited++;

				if (boardScore > alpha) {
					raisedAlpha = true;

					if (boardScore >= beta) {
						// return beta and not best score (fail-hard)
						 moveAndEval failHighResult = new moveAndEval();
						failHighResult.evaluationScore = beta;
						return failHighResult;
					}
					alpha = boardScore;
					bestMoves.Clear();
					bestMoves.Add(move);
				} else if (boardScore == alpha) {
				    bestMoves.Add(move);
			    }
		    }

			// If score was greater than alpha but less than beta, then it is a PV node and we can store the information
			// Otherwise, we failed low and return alpha without storing any information
		    if (raisedAlpha == true) {
			    TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, alpha, bestMoves[0]);
			    UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
				UCI_IO.transpositionTable.storePVTTable(zobristKey, newEntry);

			    moveAndEval result = new moveAndEval();
			    result.evaluationScore = alpha;
			    result.move = bestMoves[0];
			    return result;
		    }
		    else {
				// return beta and not best score (fail-hard)
				moveAndEval failLowResult = new moveAndEval();
				failLowResult.evaluationScore = alpha;
				return failLowResult;
		    }
	    }

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// PVS INTERIOR
		// Returns the evaluation score
		// It is fail hard (if score > beta it returns beta, if score < alpha it returns alpha)
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public static int PVS(int depth, int alpha, int beta) {
		    
			// At the leaf nodes
			if (depth == 0) {

				// Every 2047 nodes evaluated, check to see if there is a cancellation pending; if so then return 0
				if ((nodesVisited & 2047) == 0) {
					if (quitSearch()) {
						return 0;
					}
				}

				// return 0 if repetition or draw
				if (Search.cloneBoard.fiftyMoveRule >= 100) {
					return 0;
				}
				if (Search.cloneBoard.getRepetitionNumber() > 1) {
					return 0;
				}

				// Probe the hash table, and if a match is found then return the score
				// (validate the key to prevent type 2 collision)
				/*Zobrist zobristKey = Search.cloneBoard.zobristKey;
				TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);
				if (entry.key == zobristKey && entry.depth >= 0) {
					return entry.evaluationScore;
				}*/

				// Otherwise, find the evaluation and store it in the table for future use
				int evaluationScore = Evaluate.evaluationFunction(Search.cloneBoard);
				//TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, 0, evaluationScore);
				//UCI_IO.hashTable.storeTTable(zobristKey, newEntry);

				return evaluationScore;

				// return quiescence(alpha, beta);
			} else {
				// return 0 if repetition or draw
				if (Search.cloneBoard.fiftyMoveRule >= 100) {
					return 0;
				}
				if (Search.cloneBoard.getRepetitionNumber() > 1) {
					return 0;
				}

				// At the interior nodes
				// Probe the hash table and if the entry's depth is greater than or equal to current depth:
				Zobrist zobristKey = Search.cloneBoard.zobristKey;
				TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);
				int TTmove = entry.move;

				// If we set it to entry.depth >= depth, then get a slightly different (perhaps move accurate?) result???
				if (entry.key == zobristKey && entry.depth >= depth) {
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

							// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
							// Moves in the killer table shouldn't have a score, so don't need to take it out
							int flag = ((TTmove & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
							if (TTmove != 0 && (flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) && (TTmove != Search.killerTable[initialDepth - depth, 0])) {
								Search.killerTable[initialDepth - depth, 1] = Search.killerTable[initialDepth - depth, 0];
								Search.killerTable[initialDepth - depth, 0] = (TTmove & ~Constants.MOVE_SCORE_MASK);
							}
							return beta;
						} else if (evaluationScore <= alpha) {
							return alpha;
						} else if (evaluationScore > alpha && evaluationScore < beta) {
							return evaluationScore;
						}
					} else if (entry.flag == Constants.CUT_NODE) {
						int evaluationScore = entry.evaluationScore;
						if (evaluationScore >= beta) {

							// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
							// Moves in the killer table shouldn't have a score, so don't need to take it out
							int flag = ((TTmove & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
							if (TTmove != 0 && (flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) && (TTmove != Search.killerTable[initialDepth - depth, 0])) {
								Search.killerTable[initialDepth - depth, 1] = Search.killerTable[initialDepth - depth, 0];
								Search.killerTable[initialDepth - depth, 0] = (TTmove & ~Constants.MOVE_SCORE_MASK);
							}

							return beta;
						}
					} else if (entry.flag == Constants.ALL_NODE) {
						int evaluationScore = entry.evaluationScore;
						if (evaluationScore <= alpha) {
							return alpha;
						}
					}
				}

				stateVariables restoreData = new stateVariables(Search.cloneBoard);
				movePicker mPicker = new movePicker(Search.cloneBoard, depth);
				int bestMove = 0;
				int legalMovesMade = 0;
				int boardScore = 0;
				bool firstMove = true;
				// Keeps track to see whether or not alpha was raised (to see if we failed low or not); Necessary when storing entries in the transpositino table
				bool raisedAlpha = false;

				// Loops through all moves
				while (true) {
					int move = mPicker.getNextMove();

					// If the move picker returns a 0, then no more moves left, so break out of loop
					if (move == 0) {
						break;
					}

					Search.cloneBoard.makeMove(move);

					// If it is the first move, search with a full window
					if (firstMove) {
						boardScore = -PVS(depth - 1, -beta, -alpha);
					}
						// Otherwise, search with a zero window search
					else {
						boardScore = -PVS(depth - 1, -alpha - 1, -alpha);
						numOfSuccessfulZWS++;

						// If failed high in ZWS and score > alpha + 1 (beta of ZWS), then we only know the lower bound (alpha + 1 or beta of ZWS)
						// Have to then do a full window search to determine exact value (to determine if the score is greater than beta)
						if (boardScore > alpha) {
							numOfSuccessfulZWS--;
							boardScore = -PVS(depth - 1, -beta, -alpha);
						}
					}
					Search.cloneBoard.unmakeMove(move, restoreData);

					// Every 2047 nodes, check to see if there is a cancellation pending; if so then return 0
					if ((nodesVisited & 2047) == 0) {
						if (quitSearch()) {
							return 0;
						}
					}
					legalMovesMade++;
					nodesVisited++;

					if (boardScore > alpha) {
						raisedAlpha = true;

						// If the score was greater than beta, we have a beta cutoff (fail high)
						if (boardScore >= beta) {

							// Store the value in the transposition table
							TTEntry newEntry = new TTEntry(zobristKey, Constants.CUT_NODE, depth, beta, move);
							UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);

							// Increment fail high first if first move produced cutoff, otherwise increment fail high
							if (firstMove == true) {
								failHighFirst++;
							} else {
								failHigh++;
							}
							// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
							// Moves in the killer table shouldn't have a score, so don't need to take it out
							int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
							if ((flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) && (move != Search.killerTable[initialDepth - depth, 0])) {
								Search.killerTable[initialDepth - depth, 1] = Search.killerTable[initialDepth - depth, 0];
								Search.killerTable[initialDepth - depth, 0] = (move & ~Constants.MOVE_SCORE_MASK);
							}

							// return beta and not best score (fail-hard)
							return beta;
						}
						// If no beta cutoff but board score was higher than old alpha, then raise alpha
						bestMove = move;
						alpha = boardScore;
					}
					firstMove = false;
				}

				// If number of legal moves made is 0, that means there is no legal moves, which means the side to move is either in checkmate or stalemate
				if (legalMovesMade == 0) {
					if (Search.cloneBoard.isInCheck() == true) {
						// Returns the mate score - number of moves made from root to mate 
						// Ensures that checkmates closer to the root will get a higher score, so that they will be played
						TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, -Constants.CHECKMATE);
						UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
						return -Constants.CHECKMATE + (initialDepth - depth);
					} else {
						TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, Constants.STALEMATE);
						UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
						return Constants.STALEMATE;
					}
				}

				if (raisedAlpha == true) {
					TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, depth, alpha, bestMove);
					UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
					UCI_IO.transpositionTable.storePVTTable(zobristKey, newEntry);
				} else if (raisedAlpha == false) {
					TTEntry newEntry = new TTEntry(zobristKey, Constants.ALL_NODE, depth, alpha);
					UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
				}
				return alpha; //return alpha whether it was raised or not (fail-hard)	
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// QUIESCENCE SEARCH
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		private static int quiescence(int alpha, int beta) {
		    return 0;
	    }

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// CHECK STATUS
		// Called every 2048 nodes searched, and checks if time is up or if there is an interrupt form the GUI
		// When pondering, the time control and depth limit will be set to false
		// The only way that the engine will exit is if a "stop" command is received (even if search has exceeded the allocated time)
		// When the "ponderhit" command is received, the time control will be set to true
		// If the search has exceeded the allocated time it will return; otherwise it will keep thinking
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
	    private static bool quitSearch() {
		    // If the "stop" command was received, then set the stop event.Cancel (or "e") to true
			// Return true to the search functions so they can exit
			if (UCI_IO.searchWorker.CancellationPending) {
			    stopEvent.Cancel = true;
			    return true;
		    }  
			// If time control is on and the time has run out, then set the stop event.Cancel (or "e") to true
			// Return true to the search functions so they can exit
			else if (info.timeControl == true && DateTime.Now > Search.finishTime) { 
				stopEvent.Cancel = true;
			    return true;
		    } 
			// If the depth limit is on and the engine is trying to search deeper than the limit, set the stop event.Cancel (or "e") to true
			// Return true to the search functions so they can exit
			else if (info.depthLimit == true && initialDepth > info.depth) {
			    stopEvent.Cancel = true;
			    return true;
		    } 
			// If nothing is causing a cancellation, then return false to the search functions so they can keep searching
			else {
			    return false;
		    }
	    }
    }



	//--------------------------------------------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------------------------------------------
	// MOVE PICKER CLASS
	// Selects the move with the highest score and feeds it to the search function
	//--------------------------------------------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------------------------------------------
	public sealed class movePicker {
		
		internal Board board;
		internal stateVariables restoreData;
		
		internal int[] pseudoLegalMoveList;
		internal int index = 0;
		private int depth;

		// Constructor
		public movePicker(Board inputBoard, int depth) {
			this.board = inputBoard;
			this.restoreData = new stateVariables(inputBoard);
			this.depth = depth;

			// If the side to move is not in check, then get list of moves from the almost legal move generator
			// Otherwise, get list of moves from the check evasion generator
			if (inputBoard.isInCheck() == false) {
				this.pseudoLegalMoveList = inputBoard.generateAlmostLegalMoves();
			} else {
				this.pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
			}

			// retrieves the hash move from the transposition table
			// If the entry's key matches the board's current key, then probe the move
			// If the entry from the transposition table doesn't have a move, then probe the PV table
			// Have to remove any previous score from the hashmove
			int hashMove = 0;
			TTEntry entry = UCI_IO.transpositionTable.probeTTable(inputBoard.zobristKey);
			TTEntry PVTableEntry = UCI_IO.transpositionTable.probePVTTable(inputBoard.zobristKey);
			if (entry.key == inputBoard.zobristKey && entry.move != 0) {
				hashMove = (entry.move & ~Constants.MOVE_SCORE_MASK);
			} else if (PVTableEntry.key == inputBoard.zobristKey && PVTableEntry.move != 0) {
				hashMove = (PVTableEntry.move & ~Constants.MOVE_SCORE_MASK);
			}
			
			for (int i = 0; i < pseudoLegalMoveList.Length; i++) {
				// Have to remove any score from the move mask
				int move = pseudoLegalMoveList[i] & ~Constants.MOVE_SCORE_MASK;

				// Loop through all moves in the pseudo legal move list (until it hits a 0)
				if (move == 0) {
					break;
				} else if (move == hashMove) {
					// If the move is the same as the hash move, give it a value of 127
					pseudoLegalMoveList[i] |= (Constants.HASH_MOVE_SCORE << Constants.MOVE_SCORE_SHIFT);
					break;
				} else if (move == Search.killerTable[Search.initialDepth - depth, 0]) {
					pseudoLegalMoveList[i] |= (Constants.KILLER_1_SCORE << Constants.MOVE_SCORE_SHIFT);
					Debug.Assert((Search.killerTable[Search.initialDepth - depth, 0] & Constants.MOVE_SCORE_MASK) == 0);
					Debug.Assert((Search.killerTable[Search.initialDepth - depth, 0]) != hashMove);
					break;
				} else if (move == Search.killerTable[Search.initialDepth - depth, 1]) {
					pseudoLegalMoveList[i] |= (Constants.KILLER_2_SCORE << Constants.MOVE_SCORE_SHIFT);
					Debug.Assert((Search.killerTable[Search.initialDepth - depth, 1] & Constants.MOVE_SCORE_MASK) == 0);
					Debug.Assert((Search.killerTable[Search.initialDepth - depth, 1]) != hashMove);
					break;
				}
			}
		}

		public int getNextMove() {

			while (true) {
				int move = pseudoLegalMoveList[index];

				if (move == 0) {
					return 0;
				} else if (move != 0) {

					// Fix this later to swap only at the end of an interation
					int bestMoveScore = (move & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT;
					int tempIndex = index + 1;
					while (pseudoLegalMoveList[tempIndex] != 0) {
						if (((pseudoLegalMoveList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT) > bestMoveScore) {
							bestMoveScore = ((pseudoLegalMoveList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT);
							int bestMove = pseudoLegalMoveList[tempIndex];
							pseudoLegalMoveList[tempIndex] = pseudoLegalMoveList[index];
							pseudoLegalMoveList[index] = bestMove;
						}
						tempIndex ++;
					}

					move = pseudoLegalMoveList[index];
					int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
					int pieceMoved = (this.board.pieceArray[startSquare]);
					int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
					int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

					if (flag == Constants.EN_PASSANT_CAPTURE || pieceMoved == Constants.WHITE_KING || pieceMoved == Constants.BLACK_KING) {
						this.board.makeMove(move);
						if (this.board.isMoveLegal(sideToMove) == true) {
							board.unmakeMove(move, restoreData);
							index++;
							return move;
						} else {
							board.unmakeMove(move, restoreData);
							index ++;
						}
					} else {
						index++;
						return move;
					}	
				} 
			}
		}		
	}


	// Uses selection sort to pick the move with the highest score, and returns it
	// Ordering of moves are: hash moves (PV moves or refutation moves), good captures (SEE), promotions
	// killer moves (that caused beta-cutoffs at different positions at the same depth), history moves (that raised alpha)
	// Losing captures, all other moves

	//--------------------------------------------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------------------------------------------
	// MOVE AND EVAL STRUCT
	// Object that is returned by the PVS root method
	// Contains information on the best move, evaluation score, search depth, time, and nodes visited
	//--------------------------------------------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------------------------------------------

	public class moveAndEval {
		internal int move;
		internal int evaluationScore;
		internal int depthAchieved;
		internal long time;
		internal ulong nodesVisited;
	}
}
