using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
					// First move is assumed to be the best move 
					// Search with full window
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
					// Other moves are assumed to not raise alpha (set by the first move)
					// Search with null window because it is faster than full-window search and only upper bound (alpha) is needed
					// If score > alpha, then score > alpha + 1 leading to a fast beta cutoff
					// Will have to re-search with full window to get exact score, and this node will be a PV node
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

			if (beta - alpha > 1) {
				Debug.Assert(nodeType == Constants.PV_NODE);
			}

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
				if (doNull == true 
					&& Search.board.isInCheck() == false 
					&& depth >= Constants.R + 1 
					&& nodeType != Constants.PV_NODE) {
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
				bool isInCheckBeforeMove = Search.board.isInCheck();
				// Keeps track to see whether or not alpha was raised (to see if we failed low or not); Necessary when storing entries in the transpositino table
				bool raisedAlpha = false;

				// Loops through all moves
				while (true) {
					int move = mPicker.getNextMove();

					// If the move picker returns a 0, then no more moves left, so break out of loop
					if (move == 0) {
						break;
					}

					// Make the move
					Search.board.makeMove(move);

					// If it is the first move, search with a full window
					if (movesMade == 0) {
						
						boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, nodeType);
					} else {
						// Late move reduction
						if (movesMade >= 4
						    && depth >= 3
						    && nodeType != Constants.PV_NODE
						    && Search.board.isInCheck() == false
						    && isInCheckBeforeMove == false
						    && ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.PROMOTION_CAPTURE
						    && ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.PROMOTION
						    && ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.CAPTURE
						    && ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.EN_PASSANT_CAPTURE) {

							
							boardScore = -PVS(depth - 2, ply + 1, -alpha - 1, -alpha, true, Constants.NON_PV_NODE);
						} else {
							boardScore = alpha + 1;
						}

						if (boardScore > alpha) {
							boardScore = -PVS(depth - 1, ply + 1, -alpha - 1, -alpha, true, Constants.NON_PV_NODE);

							// If failed high in ZWS and score > alpha + 1 (beta of ZWS), then we only know the lower bound (alpha + 1 or beta of ZWS)
							// Have to then do a full window search to determine exact value (to determine if the score is greater than beta)
							if (boardScore > alpha) {

								boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, nodeType);
							}
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

			Debug.Assert(depth <= 0);

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
			movePickerQuiescence mPickerQuiescence = new movePickerQuiescence(Search.board, depth, ply, 0);
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
		internal int depth;
		internal int ply;
		internal int phase = Constants.PHASE_HASH;

		internal int[] pseudoLegalCaptureList;
		internal int[] pseudoLegalQuietList;
		internal int[] pseudoLegalCheckEvasionList;
		internal int captureIndex = 0;
		internal int quietIndex = 0;
		internal int checkEvasionIndex = 0;

		internal int hashMove = 0;
		internal int killer1 = 0;
		internal int killer2 = 0;
		
		// Constructor
		public movePicker(Board inputBoard, int depth, int ply) {
			this.board = inputBoard;
			this.restoreData = new stateVariables(inputBoard);
			this.depth = depth;
			this.ply = ply;
		}

		// Method called by the 
		public int getNextMove() {

			// Start in the hash move phase
			if (this.phase == Constants.PHASE_HASH) {

				
				// retrieves the hash move from the transposition table
				// If the entry's key matches the board's current key, then probe the move
				// If the entry from the transposition table doesn't have a move, then probe the PV table
				// Have to remove any previous score from the hashmove
				TTEntry entry = UCI_IO.transpositionTable.probeTTable(board.zobristKey);
				TTEntry PVTableEntry = UCI_IO.transpositionTable.probePVTTable(board.zobristKey);
				if (entry.key == board.zobristKey && entry.move != 0) {
					this.hashMove = (entry.move & ~Constants.MOVE_SCORE_MASK);
				} else if (PVTableEntry.key == board.zobristKey && PVTableEntry.move != 0) {
					this.hashMove = (PVTableEntry.move & ~Constants.MOVE_SCORE_MASK);
				}

				// If no hash move, then set phase = PHASE_CAPTURE if not in check, and phase = PHASE_CHECK_EVADE if in check
				// If the hash move is illegal, then set phase = PHASE_CAPTURE if not in check, and phase = PHASE_CHECK_EVADE if in check
				// If the hash move is legal, then setphase = PHASE_CAPTURE if not in check, and phase = PHASE_CHECK_EVADE if in check and return the move
				if (this.hashMove == 0) {
					this.phase = board.isInCheck() ? Constants.PHASE_CHECK_EVADE : Constants.PHASE_GOOD_CAPTURE;
				} else if (this.isMoveLegal(hashMove) == false) {
					this.phase = board.isInCheck() ? Constants.PHASE_CHECK_EVADE : Constants.PHASE_GOOD_CAPTURE;
				} else {
					this.phase = board.isInCheck() ? Constants.PHASE_CHECK_EVADE : Constants.PHASE_GOOD_CAPTURE;
					return this.hashMove;
				}
				
			}

			// Capture phase
			if (this.phase == Constants.PHASE_GOOD_CAPTURE) {

				// The first time the move generator enters this phase, it generates the list of captures
				if (captureIndex == 0) {
					this.generateMoves();
				}
				
				while (true) {
					int move = pseudoLegalCaptureList[captureIndex];

					// If there is no move at the capture index, then set the phase = PHASE_KILLER and break
					// Otherwise, loop through the whole array and swap the move with the highest score to the front position
					if (move == 0) {
						this.phase = Constants.PHASE_KILLER_1;
						break;
					} else if (move != 0) {

						// Fix this later to swap only at the end of an interation
						int bestMoveScore = (move & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT;
						int tempIndex = captureIndex + 1;
						while (pseudoLegalCaptureList[tempIndex] != 0) {
							if (((pseudoLegalCaptureList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT) > bestMoveScore) {
								bestMoveScore = ((pseudoLegalCaptureList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT);
								int bestMove = pseudoLegalCaptureList[tempIndex];
								pseudoLegalCaptureList[tempIndex] = pseudoLegalCaptureList[captureIndex];
								pseudoLegalCaptureList[captureIndex] = bestMove;
							}
							tempIndex++;
						}

						// If the score goes into the bad captures, then break
						if (bestMoveScore < Constants.GOOD_PROMOTION_SCORE) {
							this.phase = Constants.PHASE_KILLER_1;
							break;
						}

						// Checks to see what piece moved, and what type of move was made
						// If the move is a king move or en-passant capture, then it does a legality check before returning the move
						// Otherwise it just returns the move
						move = pseudoLegalCaptureList[captureIndex];
						int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
						int pieceMoved = (this.board.pieceArray[startSquare]);
						int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
						int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

						if (flag == Constants.EN_PASSANT_CAPTURE || 
							pieceMoved == Constants.WHITE_KING || 
							pieceMoved == Constants.BLACK_KING) {
							this.board.makeMove(move);
							if (this.board.isMoveLegal(sideToMove) == true && move != this.hashMove) {
								board.unmakeMove(move, restoreData);
								captureIndex++;
								return move;
							} else {
								board.unmakeMove(move, restoreData);
								captureIndex++;
							}
						} else {
							if (move != this.hashMove) {
								captureIndex++;
								return move;
							} else {
								captureIndex++;
							}
						}
					}
				}
			}

			if (this.phase == Constants.PHASE_KILLER_1) {
				
				this.generateMoves();

				this.killer1 = Search.killerTable[ply, 0];

				// If the first killer move is null, then there are no killer moves and we move onto phase 3
				// If the first killer move is not null, then test it for legality
				if (this.killer1 == 0) {
					this.phase = Constants.PHASE_KILLER_2;
				} else {

					// If the first killer is legal and the same as a move in the move list, then set the killer position to point to the second killer move and return it
					// If the first killer is illegal, then set the killer position to point to the second killer move
					for (int i = 0; i < this.pseudoLegalQuietList.Length; i++) {
						if (this.pseudoLegalQuietList[i] == 0) {
							this.phase = Constants.PHASE_KILLER_2;
							break;
						} else if ((this.pseudoLegalQuietList[i] & ~Constants.MOVE_SCORE_MASK) == this.killer1 && this.isMoveLegal(killer1) == true) {
							
							this.phase = Constants.PHASE_KILLER_2;
							return killer1;
						} else {
							this.phase = Constants.PHASE_KILLER_2;
						}
					}
				}
			}

			if (this.phase == Constants.PHASE_KILLER_2) {
				
				this.killer2 = Search.killerTable[ply, 1];

				// If the second killer move is null, then set the phase = PHASE_QUIET
				// If the second killer move is not null, then test it for legality
				if (this.killer2 == 0) {
					this.phase = Constants.PHASE_QUIET;
				} else {

					// If the second killer is legal and is the same as a move in the move list, then set the phase = PHASE_QUIET and return it
					// If the second killer is illegal, then set the phase = PHASE_QUIET
					for (int i = 0; i < this.pseudoLegalQuietList.Length; i++) {
						if (this.pseudoLegalQuietList[i] == 0) {
							this.phase = Constants.PHASE_QUIET;
							break;
						} else if ((this.pseudoLegalQuietList[i] & ~Constants.MOVE_SCORE_MASK) == this.killer2 && this.isMoveLegal(killer2) == true) {
							this.phase = Constants.PHASE_QUIET;
							return killer2;
						} else {
							this.phase = Constants.PHASE_QUIET;
						}
					}
				}
			}

			// Quiet phase
			if (this.phase == Constants.PHASE_QUIET) {

				// The first time the move generator enters this phase, it generates the list of captures
				if (quietIndex == 0) {
					this.generateMoves();
				}

				while (true) {
					int move = pseudoLegalQuietList[quietIndex];

					// If there is no move at the quiet index, then return 0
					// Otherwise, loop through the whole array and swap the move with the highest score to the front position
					if (move == 0) {
						this.phase = Constants.PHASE_BAD_CAPTURE;
						break;
					} else if (move != 0) {

						// Fix this later to swap only at the end of an interation
						int bestMoveScore = (move & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT;
						int tempIndex = quietIndex + 1;
						while (pseudoLegalQuietList[tempIndex] != 0) {
							if (((pseudoLegalQuietList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT) > bestMoveScore) {
								bestMoveScore = ((pseudoLegalQuietList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT);
								int bestMove = pseudoLegalQuietList[tempIndex];
								pseudoLegalQuietList[tempIndex] = pseudoLegalQuietList[quietIndex];
								pseudoLegalQuietList[quietIndex] = bestMove;
							}
							tempIndex++;
						}

						// Checks to see what piece moved, and what type of move was made
						// If the move is a king move or en-passant capture, then it does a legality check before returning the move
						// Otherwise it just returns the move
						move = pseudoLegalQuietList[quietIndex];
						int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
						int pieceMoved = (this.board.pieceArray[startSquare]);
						int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
						int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

						if (flag == Constants.EN_PASSANT_CAPTURE ||
							pieceMoved == Constants.WHITE_KING ||
							pieceMoved == Constants.BLACK_KING) {
							this.board.makeMove(move);
							if (this.board.isMoveLegal(sideToMove) == true && move != this.hashMove && move != this.killer1 && move != this.killer2) {
								board.unmakeMove(move, restoreData);
								quietIndex++;
								return move;
							} else {
								board.unmakeMove(move, restoreData);
								quietIndex++;
							}
						} else {
							if (move != this.hashMove && move != this.killer1 && move != this.killer2) {
								quietIndex++;
								return move;
							} else {
								quietIndex++;
							}
						}
					}
				}
			}
			// Capture phase
			if (this.phase == Constants.PHASE_BAD_CAPTURE) {

				while (true) {
					int move = pseudoLegalCaptureList[captureIndex];

					// If there is no move at the capture index, then set the phase = PHASE_KILLER and break
					// Otherwise, loop through the whole array and swap the move with the highest score to the front position
					if (move == 0) {
						return 0;
					} else if (move != 0) {

						// Fix this later to swap only at the end of an interation
						int bestMoveScore = (move & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT;
						int tempIndex = captureIndex + 1;
						while (pseudoLegalCaptureList[tempIndex] != 0) {
							if (((pseudoLegalCaptureList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT) > bestMoveScore) {
								bestMoveScore = ((pseudoLegalCaptureList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT);
								int bestMove = pseudoLegalCaptureList[tempIndex];
								pseudoLegalCaptureList[tempIndex] = pseudoLegalCaptureList[captureIndex];
								pseudoLegalCaptureList[captureIndex] = bestMove;
							}
							tempIndex++;
						}
						// Checks to see what piece moved, and what type of move was made
						// If the move is a king move or en-passant capture, then it does a legality check before returning the move
						// Otherwise it just returns the move
						move = pseudoLegalCaptureList[captureIndex];
						int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
						int pieceMoved = (this.board.pieceArray[startSquare]);
						int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
						int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

						if (flag == Constants.EN_PASSANT_CAPTURE ||
							pieceMoved == Constants.WHITE_KING ||
							pieceMoved == Constants.BLACK_KING) {
							this.board.makeMove(move);
							if (this.board.isMoveLegal(sideToMove) == true && move != this.hashMove) {
								board.unmakeMove(move, restoreData);
								captureIndex++;
								return move;
							} else {
								board.unmakeMove(move, restoreData);
								captureIndex++;
							}
						} else {
							if (move != this.hashMove) {
								captureIndex++;
								return move;
							} else {
								captureIndex++;
							}
						}
					}
				}
			}


			if (this.phase == Constants.PHASE_CHECK_EVADE) {

				// The first time the move generator enters this phase, it generates the list of check evasions
				if (checkEvasionIndex == 0) {
					this.generateMoves();
				}

				while (true) {
					int move = pseudoLegalCheckEvasionList[checkEvasionIndex];

					if (move == 0) {
						return 0;
					} else if (move != 0) {

						// Fix this later to swap only at the end of an interation
						int bestMoveScore = (move & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT;
						int tempIndex = checkEvasionIndex + 1;
						while (pseudoLegalCheckEvasionList[tempIndex] != 0) {
							if (((pseudoLegalCheckEvasionList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT) > bestMoveScore) {
								bestMoveScore = ((pseudoLegalCheckEvasionList[tempIndex] & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT);
								int bestMove = pseudoLegalCheckEvasionList[tempIndex];
								pseudoLegalCheckEvasionList[tempIndex] = pseudoLegalCheckEvasionList[checkEvasionIndex];
								pseudoLegalCheckEvasionList[checkEvasionIndex] = bestMove;
							}
							tempIndex++;
						}

						// Checks to see what piece moved, and what type of move was made
						// If the move is a king move or en-passant capture, then it does a legality check before returning the move
						// Otherwise it just returns the move
						move = pseudoLegalCheckEvasionList[checkEvasionIndex];
						int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
						int pieceMoved = (this.board.pieceArray[startSquare]);
						int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
						int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

						if (flag == Constants.EN_PASSANT_CAPTURE ||
							pieceMoved == Constants.WHITE_KING ||
							pieceMoved == Constants.BLACK_KING) {
							this.board.makeMove(move);
							if (this.board.isMoveLegal(sideToMove) == true && move != this.hashMove) {
								board.unmakeMove(move, restoreData);
								checkEvasionIndex++;
								return move;
							} else {
								board.unmakeMove(move, restoreData);
								checkEvasionIndex++;
							}
						} else {
							if (move != this.hashMove) {
								checkEvasionIndex++;
								return move;
							} else {
								checkEvasionIndex++;
							}
						}
					}
				}
			}

			return 0;
		}

		private void generateMoves() {
			// If the side to move is not in check, then get list of moves from the almost legal move generator
			// Otherwise, get list of moves from the check evasion generator
			if (this.phase == Constants.PHASE_GOOD_CAPTURE) {
				this.pseudoLegalCaptureList = board.moveGenerator(Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO);

				for (int i = 0; i < pseudoLegalCaptureList.Length; i++) {
					// Have to remove any score from the move mask
					int move = pseudoLegalCaptureList[i] & ~Constants.MOVE_SCORE_MASK;

					// Loop through all moves in the pseudo legal move list (until it hits a 0)
					// If the move is the same as the first or second killer, give it a value of 13 and 12 respectively (only in main search, not in quiescence since almost no quiet moves are played)
					if (move == 0) {
						break;
					} else if (move == killer1 && move != this.hashMove) {
						pseudoLegalCaptureList[i] |= (Constants.KILLER_1_SCORE << Constants.MOVE_SCORE_SHIFT);
						Debug.Assert((killer1 & Constants.MOVE_SCORE_MASK) == 0);
						Debug.Assert((killer1) != hashMove);
						break;
					} else if (move == killer2 && move != this.hashMove) {
						pseudoLegalCaptureList[i] |= (Constants.KILLER_2_SCORE << Constants.MOVE_SCORE_SHIFT);
						Debug.Assert((killer2 & Constants.MOVE_SCORE_MASK) == 0);
						Debug.Assert((killer2) != hashMove);
						break;
					}
				}
			}

			else if (this.phase == Constants.PHASE_KILLER_1) {
				this.pseudoLegalQuietList = board.moveGenerator(Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS);

			} 

			else if (this.phase == Constants.PHASE_CHECK_EVADE) {
				this.pseudoLegalCheckEvasionList = board.checkEvasionGenerator();

				for (int i = 0; i < pseudoLegalCheckEvasionList.Length; i++) {
					// Have to remove any score from the move mask
					int move = pseudoLegalCheckEvasionList[i] & ~Constants.MOVE_SCORE_MASK;

					// Loop through all moves in the pseudo legal move list (until it hits a 0)
					// If the move is the same as the first or second killer, give it a value of 13 and 12 respectively (only in main search, not in quiescence since almost no quiet moves are played)
					if (move == 0) {
						break;
					} else if (move == Search.killerTable[ply, 0] && move != this.hashMove) {
						pseudoLegalCheckEvasionList[i] |= (Constants.KILLER_1_SCORE << Constants.MOVE_SCORE_SHIFT);
						Debug.Assert((Search.killerTable[ply, 0] & Constants.MOVE_SCORE_MASK) == 0);
						Debug.Assert((Search.killerTable[ply, 0]) != hashMove);
						break;
					} else if (move == Search.killerTable[ply, 1] && move != this.hashMove) {
						pseudoLegalCheckEvasionList[i] |= (Constants.KILLER_2_SCORE << Constants.MOVE_SCORE_SHIFT);
						Debug.Assert((Search.killerTable[ply, 1] & Constants.MOVE_SCORE_MASK) == 0);
						Debug.Assert((Search.killerTable[ply, 1]) != hashMove);
						break;
					}
				}
			}
		}

		// Checks to see if the hash move (or killer move) is legal
		// Starts by assuming that the move is legal, but will set it to illegal if any of several conditions are violaged
		private bool isMoveLegal(int move) {


			bool moveLegal = true;

			int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
			int destinationSquare = ((move & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
			int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
			int pieceCaptured = ((move & Constants.PIECE_CAPTURED_MASK) >> Constants.PIECE_CAPTURED_SHIFT);
			int pieceMoved = (this.board.pieceArray[startSquare]);
			int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
			int colourOfPieceOnSquare = (this.board.pieceArray[startSquare] <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;

			// If king is in check at the end of the move, then it is illegal
			
			board.makeMove(move);
			if (this.board.isMoveLegal(sideToMove) == false) {
				moveLegal = false;
			}
			board.unmakeMove(move, restoreData);

			// If the colour of the side to move is not the same as the colour of the piece on the start square, then it is illegal
			if (sideToMove != colourOfPieceOnSquare) {
				moveLegal = false;
			}

			// If move is a quiet move, double pawn push, short castle, long castle, en passant capture, or promotion and the destination square isn't empty, then it is illegal
			if (flag == Constants.QUIET_MOVE ||
				flag == Constants.DOUBLE_PAWN_PUSH ||
				flag == Constants.SHORT_CASTLE ||
				flag == Constants.LONG_CASTLE ||
				flag == Constants.EN_PASSANT_CAPTURE ||
				flag == Constants.PROMOTION) {
				if (this.board.pieceArray[destinationSquare] != Constants.EMPTY) {
					moveLegal = false;
				}
			}

			// If move is a capture or promotion capture and piece on the destination square is not the same as the piece captured (as specified by the move int), then it is illegal
			if (flag == Constants.CAPTURE ||
				flag == Constants.PROMOTION_CAPTURE) {
				if (this.board.pieceArray[destinationSquare] != pieceCaptured) {
					moveLegal = false;
				}
			}
			return moveLegal;
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
			if (inputBoard.isInCheck() == false && this.depth == 0) {
				this.pseudoLegalMoveList = inputBoard.moveGenerator(Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK);
			} else if (inputBoard.isInCheck() == false && this.depth < 0) {
				this.pseudoLegalMoveList = inputBoard.moveGenerator((Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO));
			} 
			else {
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
