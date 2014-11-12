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

		internal static Board board;
	    
		internal static int[,] historyTable;
		internal static int[,] killerTable;
	    
		internal static moveAndEval result;
		internal static ulong nodesVisited;
	    internal static int researches;

	    internal static double failHigh;
	    internal static double failHighFirst;

	    internal static int ply;

	    internal static DoWorkEventArgs stopEvent;

	    internal static SearchInfo info;
	    internal static int moveTime;
	    internal static Stopwatch finishTimer;

	    internal static List<string> PVLine;

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// INITIALIZE SEARCH
		// Resets all of the static search variables, then initializes the search
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public static void initSearch(SearchInfo info, Board inputBoard, DoWorkEventArgs e) {

			board = inputBoard;

			Search.info = info;
			
			// Resets the history table and killer table
			historyTable = new int[13,64];
			killerTable = new int[Constants.MAX_DEPTH, 2];

			// Resets other variables
			result =  new moveAndEval();
		    
			nodesVisited = 0;
			researches = 0;
			failHigh = 0;
			failHighFirst = 0;
			ply = 0;

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

			// Set the target finish time
			moveTime = TimeControl.getFinishTime(info);
			result = new moveAndEval();
			finishTimer = Stopwatch.StartNew();

			int bookMove = OpeningBook.probeBookMove(board);
			
			// If there is a book move, then return it
			// Otherwise, start iterative deepening
			if (bookMove < 0) {//!= 0) {
				UCI_IO.plyOutOfBook = 0;
				result.move = bookMove;
			} else {
				UCI_IO.plyOutOfBook ++;
				// Initially set alpha and beta to - infinity and + infinity
				int alpha = -Constants.LARGE_INT;
				int beta = Constants.LARGE_INT;

				// Declare and initialize the aspiration window
				int currentWindow = Constants.ASP_WINDOW;

				Stopwatch iterationTimer = Stopwatch.StartNew();
				for (int i = 1; i <= Constants.MAX_DEPTH;) {

					// Before each search, it sees if it should quit
					if (quitSearch()) {
						return;
					}

					moveAndEval tempResult = PVSRoot(i, ply, alpha, beta);

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
						continue;
					} else if (tempResult.evaluationScore == beta) {
						currentWindow *= 2;
						beta += (int)(0.5 * currentWindow);
						researches++;
						continue;
					}
					result = tempResult;
					result.depthAchieved = i;
					result.time = iterationTimer.ElapsedMilliseconds;
					result.nodesVisited = nodesVisited;

					PVLine = UCI_IO.transpositionTable.getPVLine(Search.board, i);
					UCI_IO.printInfo(PVLine, i);
					failHighFirst = 0;
					failHigh = 0;

					// Reset the current window, and set alpha and beta for the next iteration
					currentWindow = Constants.ASP_WINDOW;
					alpha = result.evaluationScore - currentWindow;
					beta = result.evaluationScore + currentWindow;
					iterationTimer.Restart();
					nodesVisited = 0;
					researches = 0;
					ply = 0;
					i++;	
				}	
			}
	    }

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// PVS ROOT
		// Returns both the best move and the evaluation score
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

	    public static moveAndEval PVSRoot(int depth, int ply, int alpha, int beta) {
		    Zobrist zobristKey = Search.board.zobristKey;

			TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);

			// Make sure that the node is a PV node 
			if (entry.key == zobristKey && entry.depth >= depth && entry.flag == Constants.EXACT) {
				moveAndEval tableResult = new moveAndEval();
				tableResult.evaluationScore = entry.evaluationScore;
				tableResult.move = entry.move;
				return tableResult;
			}

			movePicker mPicker = new movePicker(Search.board, depth, ply);
		    int movesMade = 0;
			List<int> bestMoves = new List<int>();
			stateVariables restoreData = new stateVariables(Search.board);
			bool raisedAlpha = false;

			while (true) {

				int move = mPicker.getNextMove();
				int boardScore;

				if (move == 0) {
					break;
				}

				Search.board.makeMove(move);
				
				if (movesMade == 0) {
					boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, Constants.PV_NODE);

					// The first move is assumed to be the best move
					// If it failed low, that means that the rest of the moves will probably fail low, so don't bother searching them and return alpha right away (to start research)
					// Other approach is to wait until you search all moves to return
					if (boardScore < alpha) {
						moveAndEval failLowResult = new moveAndEval();
						failLowResult.evaluationScore = alpha;
						Search.board.unmakeMove(move, restoreData);
						return failLowResult;
					}
				} else {
					boardScore = -PVS(depth - 1, ply + 1, -alpha - 1, -alpha, true, Constants.NON_PV_NODE);
					
					if (boardScore > alpha) {
						boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, Constants.PV_NODE);
					}
				}
				Search.board.unmakeMove(move, restoreData);
				movesMade++;

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
			    TTEntry newEntry = new TTEntry(zobristKey, Constants.EXACT, depth, alpha, bestMoves[0]);
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

		public static int PVS(int depth, int ply, int alpha, int beta, bool doNull, int nodeType) {
		    
			// At the leaf nodes
			if (depth == 0) {

				return quiescence(depth, ply, alpha, beta, nodeType);

			} else {
				// return 0 if repetition or draw
				if (Search.board.fiftyMoveRule >= 100 || Search.board.getRepetitionNumber() > 1) {
					return 0;
				}

				// At the interior nodes
				// Probe the hash table and if the entry's depth is greater than or equal to current depth:
				Zobrist zobristKey = Search.board.zobristKey;
				TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);
				int TTmove = entry.move;

				// If we set it to entry.depth >= depth, then get a slightly different (perhaps move accurate?) result???
				if (entry.key == zobristKey && entry.depth >= depth) {
					// If it is a PV node, see if it is greater than beta (if so return beta), less than alpha (if so return alpha), or in between (if so return the exact score)
					// If it is a CUT node, see if this lower bound is greater than beta (if so return beta)
					// If it is an ALL node, see if this upper bound is less than alpha (is so return alpha)
					// Using a fail-hard here so need those extra conditions instead of just returning the score
					if (entry.flag == Constants.EXACT) {
						int evaluationScore = entry.evaluationScore;

						if (evaluationScore == -Constants.CHECKMATE) {
							return -Constants.CHECKMATE + ply;
						} else if (evaluationScore == Constants.STALEMATE) {
							return Constants.STALEMATE;
						}

						if (evaluationScore >= beta) {

							// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
							// Moves in the killer table shouldn't have a score, so don't need to take it out
							int flag = ((TTmove & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
							if (TTmove != 0 && (flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) && (TTmove != Search.killerTable[ply, 0])) {
								Search.killerTable[ply, 1] = Search.killerTable[ply, 0];
								Search.killerTable[ply, 0] = (TTmove & ~Constants.MOVE_SCORE_MASK);
							}
							return beta;
						} else if (evaluationScore <= alpha) {
							return alpha;
						} else if (evaluationScore > alpha && evaluationScore < beta) {
							return evaluationScore;
						}
					} else if (entry.flag == Constants.L_BOUND) {
						int evaluationScore = entry.evaluationScore;
						if (evaluationScore >= beta) {

							// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
							// Moves in the killer table shouldn't have a score, so don't need to take it out
							int flag = ((TTmove & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
							if (TTmove != 0 && (flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) && (TTmove != Search.killerTable[ply, 0])) {
								Search.killerTable[ply, 1] = Search.killerTable[ply, 0];
								Search.killerTable[ply, 0] = (TTmove & ~Constants.MOVE_SCORE_MASK);
							}

							return beta;
						}
					} else if (entry.flag == Constants.U_BOUND) {
						int evaluationScore = entry.evaluationScore;
						if (evaluationScore <= alpha) {
							return alpha;
						}
					}
				}

				// Null move pruning
				// The flag is set to true when a non-null search is called (regular PVS), and false when a null search is called
				// Do only if the doNull flag is true (so that two null moves aren't made in a row)
				// Do only if side to move is not in check (otherwise next move would lead to king capture)
				// Do only if depth is greater than or equal to (depth reduction + 1), otherwise we will get PVS called with depth = -1
				// DO only if it is a non-PV node (if PV node then beta != alpha + 1, if non-PV node then beta == alpha + 1)
				if (doNull == true && Search.board.isInCheck() == false && depth >= Constants.R + 1 && nodeType != Constants.PV_NODE) {
					Search.board.makeNullMove();
					int nullScore = -PVS(depth - 1 - Constants.R, ply + 1 + Constants.R, - beta, -beta + 1, false, nodeType);
					Search.board.unmakeNullMove();

					// Every 2047 nodes, check to see if there is a cancellation pending; if so then return 0
					if ((nodesVisited & 2047) == 0) {
						if (quitSearch()) {
							return 0;
						}
					}
					nodesVisited++;
					
					if (nullScore >= beta) {
						return beta;
					}
				}
				

				stateVariables restoreData = new stateVariables(Search.board);
				movePicker mPicker = new movePicker(Search.board, depth, ply);
				int bestMove = 0;
				int movesMade = 0;
				int boardScore = 0;
				// Keeps track to see whether or not alpha was raised (to see if we failed low or not); Necessary when storing entries in the transpositino table
				bool raisedAlpha = false;

				// Loops through all moves
				while (true) {
					int move = mPicker.getNextMove();

					// If the move picker returns a 0, then no more moves left, so break out of loop
					if (move == 0) {
						break;
					}

					Search.board.makeMove(move);

					// If it is the first move, search with a full window
					if (movesMade == 0) {
						boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, nodeType);
					}
						// Otherwise, search with a zero window search
					else {
						boardScore = -PVS(depth - 1, ply + 1, -alpha - 1, -alpha, true, Constants.NON_PV_NODE);
						
						// If failed high in ZWS and score > alpha + 1 (beta of ZWS), then we only know the lower bound (alpha + 1 or beta of ZWS)
						// Have to then do a full window search to determine exact value (to determine if the score is greater than beta)
						if (boardScore > alpha) {
							boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, Constants.PV_NODE);
						}
					}
					Search.board.unmakeMove(move, restoreData);
					movesMade++;

					// Every 2047 nodes, check to see if there is a cancellation pending; if so then return 0
					if ((nodesVisited & 2047) == 0) {
						if (quitSearch()) {
							return 0;
						}
					}
					nodesVisited++;

					if (boardScore > alpha) {
						raisedAlpha = true;

						// If the score was greater than beta, we have a beta cutoff (fail high)
						if (boardScore >= beta) {

							// Store the value in the transposition table
							TTEntry newEntry = new TTEntry(zobristKey, Constants.L_BOUND, depth, beta, move);
							UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);

							// Increment fail high first if first move produced cutoff, otherwise increment fail high
							if (movesMade == 1) {
								failHighFirst++;
							} else {
								failHigh++;
							}
							// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
							// Moves in the killer table shouldn't have a score, so don't need to take it out
							int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
							if ((flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) && (move != Search.killerTable[ply, 0])) {
								Search.killerTable[ply, 1] = Search.killerTable[ply, 0];
								Search.killerTable[ply, 0] = (move & ~Constants.MOVE_SCORE_MASK);
							}

							// return beta and not best score (fail-hard)
							return beta;
						}
						// If no beta cutoff but board score was higher than old alpha, then raise alpha
						bestMove = move;
						alpha = boardScore;
					}
				}

				// If number of legal moves made is 0, that means there is no legal moves, which means the side to move is either in checkmate or stalemate
				if (movesMade == 0) {
					if (Search.board.isInCheck() == true) {
						// Returns the mate score - number of moves made from root to mate 
						// Ensures that checkmates closer to the root will get a higher score, so that they will be played
						TTEntry newEntry = new TTEntry(zobristKey, Constants.EXACT, depth, -Constants.CHECKMATE);
						UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
						return -Constants.CHECKMATE + ply;
					} else {
						TTEntry newEntry = new TTEntry(zobristKey, Constants.EXACT, depth, Constants.STALEMATE);
						UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
						return Constants.STALEMATE;
					}
				}

				if (raisedAlpha == true) {
					TTEntry newEntry = new TTEntry(zobristKey, Constants.EXACT, depth, alpha, bestMove);
					UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
					UCI_IO.transpositionTable.storePVTTable(zobristKey, newEntry);
				} else if (raisedAlpha == false) {
					TTEntry newEntry = new TTEntry(zobristKey, Constants.U_BOUND, depth, alpha);
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

		private static int quiescence(int depth, int ply, int alpha, int beta, int nodeType) {

			// Probe the hash table, and if a match is found then return the score
			// (validate the key to prevent type 2 collision)
			/*Zobrist zobristKey = Search.cloneBoard.zobristKey;
			TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);
			if (entry.key == zobristKey && entry.depth >= 0) {
				return entry.evaluationScore;
			}*/

			// Otherwise, find the evaluation and store it in the table for future use
			//int evaluationScore = Evaluate.evaluationFunction(Search.cloneBoard);
			//TTEntry newEntry = new TTEntry(zobristKey, Constants.PV_NODE, 0, evaluationScore);
			//UCI_IO.hashTable.storeTTable(zobristKey, newEntry);
			
			// return 0 if repetition or draw
			if (Search.board.fiftyMoveRule >= 100 || Search.board.getRepetitionNumber() > 1) {
				return 0;
			}
			
			if (Search.board.isInCheck() == false) {
				int evaluationScore = Evaluate.evaluationFunction(Search.board);
				if (evaluationScore > alpha) {
					if (evaluationScore >= beta) {
						return beta;
					}
					alpha = evaluationScore;
				}
			}

			stateVariables restoreData = new stateVariables(Search.board);
			movePickerQuiescence mPickerQuiescence = new movePickerQuiescence(Search.board, depth, ply, Constants.CAP_AND_QUEEN_PROMO);
			int movesMade = 0;
			int boardScore = 0;
			bool firstMove = true;
			
			// Keeps track to see whether or not alpha was raised (to see if we failed low or not); Necessary when storing entries in the transpositino table
			bool raisedAlpha = false;

			// Loops through all moves
			while (true) {
				int move = mPickerQuiescence.getNextMove();

				// If the move picker returns a 0, then no more moves left, so break out of loop
				if (move == 0) {
					break;
				}

				Search.board.makeMove(move);

				boardScore = -quiescence(depth - 1, ply + 1, -beta, -alpha, nodeType);

				Search.board.unmakeMove(move, restoreData);
				movesMade++;

				// Every 2047 nodes, check to see if there is a cancellation pending; if so then return 0
				if ((nodesVisited & 2047) == 0) {
					if (quitSearch()) {
						return 0;
					}
				}
				nodesVisited++;

				if (boardScore > alpha) {
					raisedAlpha = true;

					// If the score was greater than beta, we have a beta cutoff (fail high)
					if (boardScore >= beta) {

						// Increment fail high first if first move produced cutoff, otherwise increment fail high
						if (firstMove == true) {
							failHighFirst++;
						} else {
							failHigh++;
						}
						// return beta and not best score (fail-hard)
						return beta;
					}
					// If no beta cutoff but board score was higher than old alpha, then raise alpha
					alpha = boardScore;
				}
				firstMove = false;
			}

			// If number of legal moves made is 0, that means there is no legal moves, which means the side to move is either in checkmate or stalemate
			if (movesMade == 0) {
				if (Search.board.isInCheck() == true) {
					return -Constants.CHECKMATE + ply;
				}
			}
			return alpha; //return alpha whether it was raised or not (fail-hard)	 
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
			else if (info.timeControl == true && finishTimer.ElapsedMilliseconds> Search.moveTime) { 
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
		private int ply;

		// Constructor
		public movePicker(Board inputBoard, int depth, int ply) {
			this.board = inputBoard;
			this.restoreData = new stateVariables(inputBoard);
			this.depth = depth;

			// If the side to move is not in check, then get list of moves from the almost legal move generator
			// Otherwise, get list of moves from the check evasion generator
			if (inputBoard.isInCheck() == false) {
				this.pseudoLegalMoveList = inputBoard.generateQuiescencelMoves(Constants.ALL_MOVES);
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
				// If the move is the same as the hash move, give it a value of 127
				// If the move is the same as the first or second killer, give it a value of 13 and 12 respectively (only in main search, not in quiescence since almost no quiet moves are played)
				if (move == 0) {
					break;
				} else if (move == hashMove) {
					
					pseudoLegalMoveList[i] |= (Constants.HASH_MOVE_SCORE << Constants.MOVE_SCORE_SHIFT);
					break;
				} else if (move == Search.killerTable[ply, 0]) {
					pseudoLegalMoveList[i] |= (Constants.KILLER_1_SCORE << Constants.MOVE_SCORE_SHIFT);
					Debug.Assert((Search.killerTable[ply, 0] & Constants.MOVE_SCORE_MASK) == 0);
					Debug.Assert((Search.killerTable[ply, 0]) != hashMove);
					break;
				} else if (move == Search.killerTable[ply, 1]) {
					pseudoLegalMoveList[i] |= (Constants.KILLER_2_SCORE << Constants.MOVE_SCORE_SHIFT);
					Debug.Assert((Search.killerTable[ply, 1] & Constants.MOVE_SCORE_MASK) == 0);
					Debug.Assert((Search.killerTable[ply, 1]) != hashMove);
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

					// Checks to see what piece moved, and what type of move was made
					// If the move is a king move or en-passant capture, then it does a legality check before returning the move
					// Otherwise it just returns the move
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
	// QUIESCENCE MOVE PICKER CLASS
	// Selects the move with the highest score and feeds it to the search function
	//--------------------------------------------------------------------------------------------------------------------------------------------
	//--------------------------------------------------------------------------------------------------------------------------------------------
	public sealed class movePickerQuiescence {

		internal Board board;
		internal stateVariables restoreData;

		internal int[] pseudoLegalMoveList;
		internal int index = 0;
		private int depth;
		private int flag;
		private int ply;

		// Constructor
		public movePickerQuiescence(Board inputBoard, int depth, int ply, int flag) {
			this.board = inputBoard;
			this.restoreData = new stateVariables(inputBoard);
			this.depth = depth;

			// If the side to move is not in check, then get list of moves from the almost legal move generator
			// Otherwise, get list of moves from the check evasion generator
			if (inputBoard.isInCheck() == false) {
				this.pseudoLegalMoveList = inputBoard.generateQuiescencelMoves(Constants.CAP_AND_QUEEN_PROMO);
			} else {
				this.pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
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
						tempIndex++;
					}

					// When not in check, we only want to consider good captures and capture promotions
					// Thus if highest score is less than the score of a good capture/capture promotion, then break
					if (pseudoLegalMoveList[index] >> Constants.MOVE_SCORE_SHIFT <= Constants.BAD_PROMOTION_CAPTURE_SCORE && board.isInCheck() == false) {
						return 0;
					}

					// Checks to see what piece moved, and what type of move was made
					// If the move is a king move or en-passant capture, then it does a legality check before returning the move
					// Otherwise it just returns the move
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
							index++;
						}
					} else {
						index++;
						return move;
					}
				}
			}
		}
	}


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
