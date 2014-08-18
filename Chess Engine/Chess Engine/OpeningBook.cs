using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitboard = System.UInt64;
using Zobrist = System.UInt64;
using Score = System.UInt32;
using Move = System.UInt32;

namespace Chess_Engine {
	public static class OpeningBook {

		// Calculates the zobrist key
		public static Zobrist calculatePolyglotKey(Board inputBoard) {

			Zobrist polyglotKey = 0x0UL;

			// Updates the key with the piece locations
			for (int i = 0; i < 64; i++) {
				int pieceType = inputBoard.pieceArray[i];
				if (pieceType != 0) {
					polyglotKey ^= Constants.piecePolyglot[pieceType, i];
				}
			}

			// Updates the key with the en passant square
			if (inputBoard.enPassantSquare != 0) {
				int EPSquare = Constants.findFirstSet(inputBoard.enPassantSquare);

				// looking for white pawns on the 5th rank adjacent to the en passant square
				if (EPSquare >= Constants.H6 && EPSquare <= Constants.A6) {
					if (EPSquare == Constants.H6) {
						if (inputBoard.pieceArray[Constants.G5] == Constants.WHITE_PAWN) {
							polyglotKey ^= Constants.enPassantPolyglot[EPSquare];
						}
					} else if (EPSquare == Constants.A6) {
						if (inputBoard.pieceArray[Constants.B5] == Constants.WHITE_PAWN) {
							polyglotKey ^= Constants.enPassantPolyglot[EPSquare];
						}
					} else if (EPSquare > Constants.H6 && EPSquare < Constants.A6) {
						if (inputBoard.pieceArray[EPSquare - 7] == Constants.WHITE_PAWN || inputBoard.pieceArray[EPSquare - 9] == Constants.WHITE_PAWN) {
							polyglotKey ^= Constants.enPassantPolyglot[EPSquare];
						}
					}
				} else if (EPSquare >= Constants.H3 && EPSquare <= Constants.A3) {
					if (EPSquare == Constants.H3) {
						if (inputBoard.pieceArray[Constants.G4] == Constants.WHITE_PAWN) {
							polyglotKey ^= Constants.enPassantPolyglot[EPSquare];
						}
					} else if (EPSquare == Constants.A3) {
						if (inputBoard.pieceArray[Constants.B4] == Constants.WHITE_PAWN) {
							polyglotKey ^= Constants.enPassantPolyglot[EPSquare];
						}
					} else if (EPSquare > Constants.H3 && EPSquare < Constants.A3) {
						if (inputBoard.pieceArray[EPSquare + 7] == Constants.WHITE_PAWN || inputBoard.pieceArray[EPSquare + 9] == Constants.BLACK_PAWN) {
							polyglotKey ^= Constants.enPassantPolyglot[EPSquare];
						}
					}
				}	
			}

			// Updates the key with the castling rights
			if (inputBoard.whiteShortCastleRights == Constants.CAN_CASTLE) {
				polyglotKey ^= Constants.castlePolyglot[0];
			} if (inputBoard.whiteLongCastleRights == Constants.CAN_CASTLE) {
				polyglotKey ^= Constants.castlePolyglot[1];
			} if (inputBoard.blackShortCastleRights == Constants.CAN_CASTLE) {
				polyglotKey ^= Constants.castlePolyglot[2];
			} if (inputBoard.blackLongCastleRights == Constants.CAN_CASTLE) {
				polyglotKey ^= Constants.castlePolyglot[3];
			}

			// Updates the key with the side to move
			if (inputBoard.sideToMove == Constants.WHITE) {
				polyglotKey ^= Constants.sideToMovePolyglot[0];
			}

			return polyglotKey;
		}

		

		

	}
}
