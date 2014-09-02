using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using Bitboard = System.UInt64;
using Zobrist = System.UInt64;
using Score = System.UInt32;
using Move = System.UInt32;

namespace Chess_Engine {

	public static class OpeningBook {

		public static PolyglotEntry[] polyglotOpeningBook;
		public static Random rnd = new Random();

		public static void initOpeningBook() {
			using (BinaryReader b = new BinaryReader(File.Open(@"Performance.bin", FileMode.Open))) {
				
				// Calculates the size of the polyglot entry structure (16 bytes)
				int sizeOfPolyStruct = System.Runtime.InteropServices.Marshal.SizeOf(typeof (PolyglotEntry));
				
				// Calculates the number of entries in the opening book by dividing its size by the size of the polyglot entry structure
				int length = (int) b.BaseStream.Length;
				int numEntries = length/sizeOfPolyStruct;
				polyglotOpeningBook = new PolyglotEntry[numEntries];
				// Console.WriteLine("Number of entries in opening book: " + numEntries);

				// Set the initial position to 0
				int pos = 0;

				while (pos < numEntries) {
					
					// Create an array of bytes for the opening book entry to be read into
					byte[] entry = new byte[sizeOfPolyStruct];
					
					// Read the opening book into the array of bytes starting at element 0, and read 16 bytes (size of the polyglot struct)
					b.Read(entry, 0, sizeOfPolyStruct);

					// Use the byte array to create a new polyglot entry struct, and add the entry to the array 
					polyglotOpeningBook[pos] = new PolyglotEntry(entry);

					// Increment the position by 16 (the size of the struct)
					pos += 1;
				}
			}
		}

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

		// Returns a move from the opening book (if any)
		public static int probeBookMove(Board inputBoard) {
			
			// Computes the input board's polyglot key
			Zobrist polyglotKey = OpeningBook.calculatePolyglotKey(inputBoard);
			int[] polyglotBookMoves = new int[Constants.MAX_BOOK_MOVES];
			int[] pseudoLegalMoveList = null;
			int[] engineBookMoves = new int[Constants.MAX_BOOK_MOVES];
			int engineBookMoveIndex = 0;

			// Searches through the opening book for a match of the position
			// if any then it stores the corresponding move from the opening book in an array
			int index = 0;
			for (int i = 0; i < OpeningBook.polyglotOpeningBook.Length; i++) {
				if (OpeningBook.polyglotOpeningBook[i].key == polyglotKey) {
					polyglotBookMoves[index] = OpeningBook.polyglotOpeningBook[i].move;
					index++;
				} 
				// Opening book entries are arranged from lowest key to highest key
				// If the key in the book is greater the board's key, then there will be no more entries in the book so the loop is exited
				else if (OpeningBook.polyglotOpeningBook[i].key > polyglotKey) {
					break;
				}
			}

			// Generates all pseudo legal moves from the position
			if (inputBoard.isInCheck() == false) {
				pseudoLegalMoveList = inputBoard.generateAlmostLegalMoves();
			} else {
				pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
			}

			// For each of the moves in the polyglot book move array, it converts the start square/destination square/promotion piece to the format used by the engine
			// It then compares these with a list of pseudo-legal moves for that position
			// If there is a match, it stores the full move int (generated by the move generator) in the engine book moves array
			for (int i = 0; (i < Constants.MAX_BOOK_MOVES && polyglotBookMoves[i] != 0); i++) {
				
				// Converts the polyglot start file and start rank into a start square used by the engine
				int startFile = 7-(polyglotBookMoves[i] >> 6 & 7);
				int startRank = (polyglotBookMoves[i] >> 9 & 7);
				int startSquare = 8*startRank + startFile;
				
				// Converts the polyglot destination file and destination rank into a destination square used by the engine
				int destinationFile = 7 - (polyglotBookMoves[i] >> 0 & 7);
				int destinationRank = (polyglotBookMoves[i] >> 3 & 7);
				int destinationSquare = 8*destinationRank + destinationFile;

				// Makes some adjustments to the destination square if the move is a castling move
				if (startSquare == Constants.E1 && destinationSquare == Constants.H1 && inputBoard.pieceArray[Constants.E1] == Constants.WHITE_KING) {
					destinationSquare = Constants.G1;
				} else if (startSquare == Constants.E1 && destinationSquare == Constants.A1 && inputBoard.pieceArray[Constants.E1] == Constants.WHITE_KING) {
					destinationSquare = Constants.C1;
				} else if (startSquare == Constants.E8 && destinationSquare == Constants.H8 && inputBoard.pieceArray[Constants.E8] == Constants.BLACK_KING) {
					destinationSquare = Constants.G8;
				} else if (startSquare == Constants.E8 && destinationSquare == Constants.A8 && inputBoard.pieceArray[Constants.E8] == Constants.BLACK_KING) {
					destinationSquare = Constants.C8;
				}

				// Converts the polyglot promotion number into a promotion piece used by the engine
				int promotionPiece = 0;
				if ((polyglotBookMoves[i] >> 12 & 15) != 0) {
					promotionPiece = (polyglotBookMoves[i] >> 12 & 7) + 1 + 6 * inputBoard.sideToMove;					
				}
				
				// compares the start square/destination square/promotion piece to each pseudo legal move geenrated by the engine
				// If they match, then store that pseudo legal move in the engine book moves array
				for (int j = 0; pseudoLegalMoveList[j] != 0; j++) {
					int engineMove = pseudoLegalMoveList[j];
					int engineStartSquare = ((engineMove & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
					int engineDestinationSquare = ((engineMove & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
					int enginePromotionPiece = ((engineMove & Constants.PIECE_PROMOTED_MASK) >> Constants.PIECE_PROMOTED_SHIFT);
					
					if (startSquare == engineStartSquare && destinationSquare == engineDestinationSquare && promotionPiece == enginePromotionPiece) {
						engineBookMoves[engineBookMoveIndex++] = pseudoLegalMoveList[j];
						break;
					}
				}
			}

			// Number of book moves
			int numBookMoves = 0;

			// Loops through the entire engine book moves array and sets the number of book moves = index (of last book moves found) + 1
			for (int i = 0; i < engineBookMoves.Length; i ++) {
				if (engineBookMoves[i] != 0) {
					numBookMoves = i + 1;
				}
			}

			// If the number of book moves isn't 0, it generates a random number between 0 and numBookMoves - 1 (inclusive)
			// It returns the move at that index
			// Otherwise if the number of book moves is 0, it returns 0
			if (numBookMoves != 0) {
				return engineBookMoves[rnd.Next(0, numBookMoves)];
			} else {
				return 0;
			}
		}
	}

	public struct PolyglotEntry {
		internal Zobrist key;
		internal UInt16 move;
		internal UInt16 weight;
		internal UInt32 learn;

		// Constructor
		public PolyglotEntry(byte[] entryArray) {
			
			// Read in the corresponding bytes from the byte array, swap the endian-ness, and store it in the instance variables
			this.key = Constants.swapUint64Endian(BitConverter.ToUInt64(entryArray, 0));
			this.move = Constants.swapUint16Endian(BitConverter.ToUInt16(entryArray, 8));
			this.weight = Constants.swapUint16Endian(BitConverter.ToUInt16(entryArray, 10));
			this.learn = Constants.swapUint32Endian(BitConverter.ToUInt32(entryArray, 12));
		}
	}
}
