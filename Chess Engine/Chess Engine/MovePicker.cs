using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Chess_Engine {

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
				if (entry.key == board.zobristKey && entry.move != 0 && entry.depth > 0) {
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
				} else if (this.isMoveLegal(hashMove) == true) {
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
		internal int evaluationScore;
		internal int depthAchieved;
		internal long time;
		internal ulong nodesVisited;
	}
}

