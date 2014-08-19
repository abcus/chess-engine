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

		public static PolyglotEntry[] openingBook;
		
		public static void readPolyBook() {
			using (BinaryReader b = new BinaryReader(File.Open(@"C:\Users\Kevin\Desktop\chess\performance.bin", FileMode.Open))) {
				
				// Calculates the size of the polyglot entry structure (16 bytes)
				int sizeOfPolyStruct = System.Runtime.InteropServices.Marshal.SizeOf(typeof (PolyglotEntry));
				
				// Calculates the number of entries in the opening book by dividing its size by the size of the polyglot entry structure
				int length = (int) b.BaseStream.Length;
				int numEntries = length/sizeOfPolyStruct;
				openingBook = new PolyglotEntry[numEntries];

				// Set the initial position to 0
				int pos = 0;

				while (pos < numEntries) {
					
					// Create an array of bytes for the opening book entry to be read into
					byte[] entry = new byte[sizeOfPolyStruct];
					
					// Read the opening book into the array of bytes starting at element 0, and read 16 bytes (size of the polyglot struct)
					b.Read(entry, 0, sizeOfPolyStruct);

					// Use the byte array to create a new polyglot entry struct, and add the entry to the array 
					openingBook[pos] = new PolyglotEntry(entry);

					// Increment the position by 16 (the size of the struct)
					pos += 1;
				}
			}

			for (int k = 0; k < 100; k ++) {
				Console.WriteLine(openingBook[k].key);
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
