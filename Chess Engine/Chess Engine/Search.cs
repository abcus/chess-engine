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

	    internal static List<int> PVLine;

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
			PVLine = new List<int>();

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
				PVLine.Add(bookMove);
			} else {
				UCI_IO.plyOutOfBook ++;
				// Initially set alpha and beta to - infinity and + infinity
				int alpha = -Constants.LARGE_INT;
				int beta = Constants.LARGE_INT;

				Stopwatch iterationTimer = Stopwatch.StartNew();
				for (int i = 1; i <= Constants.MAX_DEPTH; i++) {

                    // Declare and initialize the aspiration window
                    int currentWindow = Constants.ASP_WINDOW;

				    while (true) {
                        // Before each search, it sees if it should quit
                        if (quitSearch()) {
                            return;
                        }

                        int tempResult = PVS(i, ply, alpha, beta, true, Constants.ROOT);

                        // If PVSRoot at depth i returned null
                        // that means that the search wasn't completed and time has run out, so terminate the thread and the result variable will be from the last completed iteration
                        // If PVSRoot returned a value of alpha or lower, then failed low
                        // Widen the window by a factor of 2, and search again (beta is untouched because otherwise it may lead to search instability)
                        // If PVSRoot returned a value of beta or higher, then failed high
                        // Widen the window by a factor of 2, and search again (alpha is untouched because otherwise it may lead to search instability)
                        // If PVSRoot returned a value between alpha and beta, the search completed successfully
                        // We set the result variable equal to the return value of the function
                        if (tempResult == Constants.SEARCH_ABORTED) {
                            return;
                        } if (tempResult <= alpha) {
                            currentWindow *= 2;
                            alpha -= (int)(0.5 * currentWindow);
                            researches++;
                        } else if (tempResult >= beta) {
                            currentWindow *= 2;
                            beta += (int)(0.5 * currentWindow);
                            researches++;
                        } else if (tempResult > alpha && tempResult < beta) {
                            result.evaluationScore = tempResult;
                            result.depthAchieved = i;
                            result.time = iterationTimer.ElapsedMilliseconds;
                            result.nodesVisited = nodesVisited;

                            PVLine = UCI_IO.transpositionTable.getPVLine(Search.board, i);
                            UCI_IO.printInfo(PVLine, i);
                            failHighFirst = 0;
                            failHigh = 0;

                            // Reset the current window, and set alpha and beta for the next depth
                            currentWindow = Constants.ASP_WINDOW;
                            alpha = result.evaluationScore - currentWindow;
                            beta = result.evaluationScore + currentWindow;
                            iterationTimer.Restart();
                            nodesVisited = 0;
                            researches = 0;
                            ply = 0;
                            break;
                        }
				    }
				}	
			}
	    }

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// PVS 
		// Returns the evaluation score (fail soft)
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public static int PVS(int depth, int ply, int alpha, int beta, bool doNull, int nodeTypeRootOrNot) {

			Debug.Assert(alpha < beta);
			int nodeType = beta > alpha + 1 ? Constants.PV_NODE : Constants.NON_PV_NODE;

			// At the leaf nodes
			if (depth <= 0 || ply >= Constants.MAX_DEPTH) {
				return quiescence(depth, ply, alpha, beta);
			}
			// return 0 if repetition or draw
			if (Search.board.isDraw() == true) {
				return Constants.DRAW;
			}

			// At the interior nodes
			// Probe the hash table and if the entry's depth is greater than or equal to current depth:
			Zobrist zobristKey = Search.board.zobristKey;
			TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);
			
			if (nodeType != Constants.PV_NODE && Search.canReturnTT(entry, depth, alpha, beta, zobristKey) && nodeTypeRootOrNot == Constants.NON_ROOT) {
				int evaluationScore = Search.scoreFromTT(entry.evaluationScore, ply);
                int TTmove = entry.move;
				if (evaluationScore > alpha) {
					if (evaluationScore >= beta) {
						updateKillers(TTmove, ply);
						return beta;
					}
					return evaluationScore;
				}
				return alpha;
            } else if (entry.key == zobristKey && entry.depth >= depth && entry.flag == Constants.EXACT && nodeType == Constants.ROOT) {
                return entry.evaluationScore;
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
				&& nodeType != Constants.PV_NODE
                && nodeTypeRootOrNot != Constants.ROOT) {
				Search.board.makeNullMove();
                int nullScore = -PVS(depth - 1 - Constants.R, ply + 1 + Constants.R, -beta, -beta + 1, false, Constants.NON_ROOT);
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
            List<int> bestMoves = new List<int>();
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

				// If it is the first move, it is assumed to be the best move so search with a full window
				if (movesMade == 0) {
                    boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, Constants.NON_ROOT);

                    // The first move is assumed to be the best move
                    // If it failed low, that means that the rest of the moves will probably fail low, so don't bother searching them and return alpha right away (to start research)
                    // Other approach is to wait until you search all moves to return
                    if (boardScore < alpha && nodeTypeRootOrNot == Constants.ROOT) {
				        Search.board.unmakeMove(move, restoreData);
				        return alpha;
				    }
				} else {
					// Late move reduction
					if (movesMade >= 4
						&& depth >= 3
						&& nodeType != Constants.PV_NODE
                        && nodeTypeRootOrNot != Constants.ROOT
						&& Search.board.isInCheck() == false
						&& isInCheckBeforeMove == false
						&& ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.PROMOTION_CAPTURE
						&& ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.PROMOTION
						&& ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.CAPTURE
						&& ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT) != Constants.EN_PASSANT_CAPTURE
						&& move != Search.killerTable[ply, 0]
						&& move != Search.killerTable[ply, 1]) {

                            boardScore = -PVS(depth - 2, ply + 1, -alpha - 1, -alpha, true, Constants.NON_ROOT);
					} else {
						boardScore = alpha + 1;
					}

					if (boardScore > alpha) {
                        // Other moves are assumed to not raise alpha (set by the first move)
                        // Search with null window because it is faster than full-window search and only upper bound (alpha) is needed
                        // If score > alpha, then score > alpha + 1 leading to a fast beta cutoff
                        // Will have to re-search with full window to get exact score, and this node will be a PV node
                        boardScore = -PVS(depth - 1, ply + 1, -alpha - 1, -alpha, true, Constants.NON_ROOT);

						// If failed high in ZWS and score > alpha + 1 (beta of ZWS), then we only know the lower bound (alpha + 1 or beta of ZWS)
						// Have to then do a full window search to determine exact value (to determine if the score is greater than beta)
						if (boardScore > alpha) {
                            boardScore = -PVS(depth - 1, ply + 1, -beta, -alpha, true, Constants.NON_ROOT);

						}
					}
				}
				Search.board.unmakeMove(move, restoreData);
				movesMade++;

				// Every 2047 nodes, check to see if there is a cancellation pending; if so then return a constant signifying an aborted search
				if ((nodesVisited & 2047) == 0) {
					if (quitSearch()) {
						return Constants.SEARCH_ABORTED;
					}
				}
				nodesVisited++;

				if (boardScore > alpha) {
					raisedAlpha = true;

					// If the score was greater than beta, we have a beta cutoff (fail high)
					if (boardScore >= beta) {

                        // If root node, then return straight away
					    if (nodeTypeRootOrNot == Constants.ROOT) {
					        return beta;
					    }

						// Store the value in the transposition table
						TTEntry newEntry = new TTEntry(zobristKey, Constants.L_BOUND, depth, beta, move);
						UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);

						// Increment fail high first if first move produced cutoff, otherwise increment fail high
						if (movesMade == 1) {
							failHighFirst++;
						} else {
							failHigh++;
						}
						updateKillers(move, ply);
						// return beta and not best score (fail-hard)
						return beta;
					}
					// If no beta cutoff but board score was higher than old alpha, then raise alpha
					bestMove = move;
					alpha = boardScore;
				    bestMoves.Clear();
                    bestMoves.Add(move);
				} else if (boardScore == alpha && nodeTypeRootOrNot == Constants.ROOT) {
				    bestMoves.Add(move); //multiple PV
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
			} else if (raisedAlpha == false && nodeTypeRootOrNot != Constants.ROOT) {
				TTEntry newEntry = new TTEntry(zobristKey, Constants.U_BOUND, depth, alpha);
				UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
			}
			return alpha; //return alpha whether it was raised or not (fail-hard)		
		}

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// QUIESCENCE SEARCH
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		private static int quiescence(int depth, int ply, int alpha, int beta) {

			int nodeType = beta > alpha + 1 ? Constants.PV_NODE : Constants.NON_PV_NODE;
			Debug.Assert(depth <= 0);
			Debug.Assert(alpha < beta);

			// return 0 if repetition or draw
			if (Search.board.fiftyMoveRule >= 100 || Search.board.getRepetitionNumber() > 1) {
				return 0;
			}
			
			// Probe the hash table, and if a match is found then return the score
			// Probe the hash table and if the entry's depth is greater than or equal to current depth:
			// At PV nodes, we don't use the TT for pruning, but only for move ordering
			Zobrist zobristKey = Search.board.zobristKey;
			TTEntry entry = UCI_IO.transpositionTable.probeTTable(zobristKey);

			if (nodeType != Constants.PV_NODE && Search.canReturnTT(entry, depth, alpha, beta, zobristKey)) {
				int evaluationScore = Search.scoreFromTT(entry.evaluationScore, ply);

				if (evaluationScore > alpha) {
					if (evaluationScore >= beta) {
						return beta;
					}
					return evaluationScore;
				}
				return alpha;
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
			int bestMove = 0;
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

				boardScore = -quiescence(depth - 1, ply + 1, -beta, -alpha);

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

						TTEntry newEntry = new TTEntry(zobristKey, Constants.L_BOUND, depth, beta, move);
						UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
						
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
					bestMove = move;
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
			if (raisedAlpha == true) {
				TTEntry newEntry = new TTEntry(zobristKey, Constants.EXACT, depth, alpha, bestMove);
				UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
			} else if (raisedAlpha == false) {
				TTEntry newEntry = new TTEntry(zobristKey, Constants.U_BOUND, depth, alpha);
				UCI_IO.transpositionTable.storeTTable(zobristKey, newEntry);
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

	    static int mateIn(int ply) {
		    return Constants.CHECKMATE - ply;
	    }

	    static int matedIn(int ply) {
		    return -Constants.CHECKMATE + ply;
	    }


		// Converts a score from the search to a score that can be stored in the transposition table
		// Have to adjust mate (+) and mated (-) scores from "plies to mate from root" to "plies to mate from current position"
		static int scoreToTT(int score, int ply) {
			if (score >= mateIn(Constants.MAX_PLY)) {
				return score + ply;
			}
			if (score <= matedIn(Constants.MAX_PLY)) {
				return score - ply;
			}
			return score;
	    }

		// Converts a score from the transpositino table to a score that can be used in the search
		// Have to adjust mate (+) and mated (-) scores from "plies to mate from current position" to "plies to mate from root"
	    static int scoreFromTT(int score, int ply) {
		    if (score >= mateIn(Constants.MAX_PLY)) {
			    return score - ply;
		    }
		    if (score <= matedIn(Constants.MAX_PLY)) {
			    return score + ply;
		    }
		    return score;
	    }
		// Returns a bool do determine whether or not a TT score can be used to cut off the search
	    static bool canReturnTT(TTEntry entry, int depth, int alpha, int beta, Zobrist key) {
		    if (entry.depth < depth || (entry.key != key)) {
			    return false;
		    }
		    int ttScore = entry.evaluationScore;
		    return (entry.flag == Constants.EXACT
					|| (entry.flag == Constants.L_BOUND && ttScore >= beta)
					|| (entry.flag == Constants.U_BOUND && ttScore <= alpha));
	    }

		// Stores the move in the killer table if it is not a capture, en passant capture, promotion, capture promotion and if it's not already in the killer table
		// Have to remove the score before storing it in the table
	    static void updateKillers(int move, int ply) {
			int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
			if ((flag == Constants.QUIET_MOVE
				|| flag == Constants.DOUBLE_PAWN_PUSH
				|| flag == Constants.SHORT_CASTLE
				|| flag == Constants.LONG_CASTLE)
				&& move != Search.killerTable[ply, 0]
				&& move != 0) {
				Search.killerTable[ply, 1] = Search.killerTable[ply, 0];
				Search.killerTable[ply, 0] = (move & ~Constants.MOVE_SCORE_MASK);
			}
		}
    }
}


	
