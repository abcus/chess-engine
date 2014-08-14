using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Bitboard = System.UInt64;
using Zobrist = System.UInt64;
using Score = System.UInt32;
using Move = System.UInt32;

namespace Chess_Engine {

    // Class that stores the information necessary to restore the board back to its previous state after making a move
	// Other information stored by primitive types (e.g. zobrist key, material) is copied, while arrays are not (too slow)
    public sealed class stateVariables {
        internal int sideToMove;
        internal int whiteShortCastleRights;
        internal int whiteLongCastleRights;
        internal int blackShortCastleRights;
        internal int blackLongCastleRights;
        internal int fiftyMoveRule;
        internal int fullMoveNumber;
        internal int capturedPieceType;
        internal Bitboard enPassantSquare;
	    internal Zobrist zobristKey;

	    internal int whiteMidgameMaterial;
	    internal int whiteEndgameMaterial;
	    internal int blackMidgameMaterial;
	    internal int blackEndgameMaterial;


        // Constructor that sets all of the stateVariable's instance variables
        public stateVariables (Board inputBoard) {
            this.sideToMove = inputBoard.sideToMove;

            this.whiteShortCastleRights = inputBoard.whiteShortCastleRights;
            this.whiteLongCastleRights = inputBoard.whiteLongCastleRights;
            this.blackShortCastleRights = inputBoard.blackShortCastleRights;
            this.blackLongCastleRights = inputBoard.blackLongCastleRights;

            this.fiftyMoveRule = inputBoard.fiftyMoveRule;
            this.fullMoveNumber = inputBoard.fullMoveNumber;

            this.enPassantSquare = inputBoard.enPassantSquare;

	        this.zobristKey = inputBoard.zobristKey;

	        this.whiteMidgameMaterial = inputBoard.whiteMidgameMaterial;
	        this.whiteEndgameMaterial = inputBoard.whiteEndgameMaterial;
	        this.blackMidgameMaterial = inputBoard.blackMidgameMaterial;
	        this.blackEndgameMaterial = inputBoard.blackEndgameMaterial;
        } 
    }

    public class Board {

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //INSTANCE VARIABLES
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------

        //Variables that uniquely describe a board state

        //bitboards and array to hold bitboards
        internal Bitboard[] arrayOfBitboards = new Bitboard[13];
        
        //aggregate bitboards and array to hold aggregate bitboards
        internal Bitboard[] arrayOfAggregateBitboards = new Bitboard[3];
        
        //piece array that stores the pieces as integers in a 64-element array
        //Index 0 is H1, and element 63 is A8
        internal int[] pieceArray = new int[64];

        //side to move
        internal int sideToMove = 0;

        //castling rights
        internal int whiteShortCastleRights = 0;
        internal int whiteLongCastleRights = 0;
        internal int blackShortCastleRights = 0;
        internal int blackLongCastleRights = 0;

        //en passant state
        internal ulong enPassantSquare = 0x0UL;
        
        //move data
        internal int fullMoveNumber = 1;
        internal int fiftyMoveRule = 0;
        
        //Variables that can be calculated

        internal int[] pieceCount = new int[13];

        internal int whiteMidgameMaterial = 0;
        internal int whiteEndgameMaterial = 0;
        internal int blackMidgameMaterial = 0;
        internal int blackEndgameMaterial = 0;

		internal int[] midgamePSQ = new int[13];
        internal int[] endgamePSQ = new int[13];
        
        internal Zobrist zobristKey = 0x0UL;

	    internal List<Zobrist> gameHistory = new List<Zobrist>();

		internal int[] historyHash = new int[16381];

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        // CONSTRUCTORS
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------

        // Main constructor
        public Board(string FENString) {
            this.FENToBoard(FENString);
            this.materialCount();
            this.pieceSquareValue();
	        this.calculateZobristKey();
        }

        // Copy constructor
        public Board(Board inputBoard) {
            Array.Copy(inputBoard.arrayOfBitboards, this.arrayOfBitboards, inputBoard.arrayOfBitboards.Length);
            Array.Copy(inputBoard.arrayOfAggregateBitboards, this.arrayOfAggregateBitboards, inputBoard.arrayOfAggregateBitboards.Length);
            Array.Copy(inputBoard.pieceArray, this.pieceArray, inputBoard.pieceArray.Length);

            this.sideToMove = inputBoard.sideToMove;

            this.whiteShortCastleRights = inputBoard.whiteShortCastleRights;
            this.whiteLongCastleRights = inputBoard.whiteLongCastleRights;
            this.blackShortCastleRights = inputBoard.blackShortCastleRights;
            this.blackLongCastleRights = inputBoard.blackLongCastleRights;

            this.enPassantSquare = inputBoard.enPassantSquare;

            this.fullMoveNumber = inputBoard.fullMoveNumber;
            this.fiftyMoveRule = inputBoard.fiftyMoveRule;
            
            Array.Copy(inputBoard.pieceCount, this.pieceCount, inputBoard.pieceCount.Length);

            this.whiteMidgameMaterial = inputBoard.whiteMidgameMaterial;
            this.whiteEndgameMaterial = inputBoard.whiteEndgameMaterial;
            this.blackMidgameMaterial = inputBoard.blackMidgameMaterial;
            this.blackEndgameMaterial = inputBoard.blackEndgameMaterial;

	        Array.Copy(inputBoard.midgamePSQ, this.midgamePSQ, inputBoard.midgamePSQ.Length);
            Array.Copy(inputBoard.endgamePSQ, this.endgamePSQ, inputBoard.endgamePSQ.Length);

			this.zobristKey = inputBoard.zobristKey;

			// Copies the game history array list
	        this.gameHistory.AddRange(inputBoard.gameHistory);

			// Copies the history hash table
			Array.Copy(inputBoard.historyHash, this.historyHash, inputBoard.historyHash.Length);
        }
        
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
		// MAKE MOVE 
        // takes an int representing a move and makes it
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        public void makeMove(int moveRepresentationInput) {

            // Extracts information (piece moved, start square, destination square,  flag, piece captured, piece promoted) from the int encoding the move
            int pieceMoved = ((moveRepresentationInput & Constants.PIECE_MOVED_MASK) >> 0);
			int startSquare = ((moveRepresentationInput & Constants.START_SQUARE_MASK) >> 4);
			int destinationSquare = ((moveRepresentationInput & Constants.DESTINATION_SQUARE_MASK) >> 10);
			int flag = ((moveRepresentationInput & Constants.FLAG_MASK) >> 16);
			int pieceCaptured = ((moveRepresentationInput & Constants.PIECE_CAPTURED_MASK) >> 20);
            int piecePromoted = ((moveRepresentationInput & Constants.PIECE_PROMOTED_MASK) >> 24);
			
            // Calculates bitboards for removing piece from start square and adding piece to destionation square
			// "and" with ~startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
			ulong startSquareBitboard = (0x1UL << startSquare);
			ulong destinationSquareBitboard = (0x1UL << destinationSquare);

            // Sets the board's instance variables
            // sets the side to move to the other player (white = 0 and black = 1, so side ^ 1 = other side)
            this.sideToMove ^= 1;
	        this.zobristKey ^= Constants.sideToMoveZobrist[0];

            // If the move is anything but a double pawn push, sets the en passant square bitboard to 0x0UL;
		    if (flag != Constants.DOUBLE_PAWN_PUSH && this.enPassantSquare != 0x0UL) {
			    zobristKey ^= Constants.enPassantZobrist[Constants.findFirstSet(this.enPassantSquare)];
				this.enPassantSquare = 0x0UL;
		    }

            // Updates the en passant square instance variable if there was a double pawn push
		    if (flag == Constants.DOUBLE_PAWN_PUSH) {
		        if (pieceMoved == Constants.WHITE_PAWN) {
			        
					// If there was a previous en passant square, have to remove it from the zobrist key before adding in the new one
					if (this.enPassantSquare != 0) {
						this.zobristKey ^= Constants.enPassantZobrist[Constants.findFirstSet(this.enPassantSquare)];
			        }
					this.enPassantSquare = (0x1UL << (destinationSquare - 8));
					zobristKey ^= Constants.enPassantZobrist[Constants.findFirstSet(this.enPassantSquare)];
		        } else if (pieceMoved == Constants.BLACK_PAWN) {
			        if (this.enPassantSquare != 0) {
						zobristKey ^= Constants.enPassantZobrist[Constants.findFirstSet(this.enPassantSquare)];
			        }
					this.enPassantSquare = (0x1UL << (destinationSquare + 8));
					this.zobristKey ^= Constants.enPassantZobrist[Constants.findFirstSet(this.enPassantSquare)];
		        }
		    }
            // If the king moved, then set the castling rights to false
            if (pieceMoved == Constants.WHITE_KING) {

	            if (this.whiteShortCastleRights != Constants.CANNOT_CASTLE) {
		            this.zobristKey ^= Constants.castleZobrist[0];
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
	            } if (this.whiteLongCastleRights != Constants.CANNOT_CASTLE) {
					this.zobristKey ^= Constants.castleZobrist[1];
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
	            }
            } else if (pieceMoved == Constants.BLACK_KING) {

	            if (this.blackShortCastleRights != Constants.CANNOT_CASTLE) {
					this.zobristKey ^= Constants.castleZobrist[2];
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
	            } if (this.blackLongCastleRights != Constants.CANNOT_CASTLE) {
					this.zobristKey ^= Constants.castleZobrist[3];
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
	            }
            }
		    // If the start square or destination square is A1, H1, A8, or H8, then updates the castling rights
		    if (startSquare == Constants.A1 || destinationSquare == Constants.A1) {
			    if (this.whiteLongCastleRights != Constants.CANNOT_CASTLE) {
				    this.zobristKey ^= Constants.castleZobrist[1];
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
			    }
            } if (startSquare == Constants.H1 || destinationSquare == Constants.H1) {
				if (this.whiteShortCastleRights != Constants.CANNOT_CASTLE) {
					this.zobristKey ^= Constants.castleZobrist[0];
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
				}
            } if (startSquare == Constants.A8 || destinationSquare == Constants.A8) {
				if (this.blackLongCastleRights != Constants.CANNOT_CASTLE) {
					this.zobristKey ^= Constants.castleZobrist[3];
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
				}
            } if (startSquare == Constants.H8 || destinationSquare == Constants.H8) {
				if (this.blackShortCastleRights != Constants.CANNOT_CASTLE) {
					this.zobristKey ^= Constants.castleZobrist[2];
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
				}
            }

			// Increments the fullmove number (if it is black's turn to move)
	        if (this.sideToMove == Constants.BLACK) {
				this.fullMoveNumber++;
	        }

			// Increments the fifty move rule counter (actually counts plies and not moves)
	        this.fiftyMoveRule ++;

			// Maybe should clear the history hash here?? (speed might be an issue)
			if (flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE || flag == Constants.EN_PASSANT_CAPTURE) {
		        this.fiftyMoveRule = 0;
	        } if (flag == Constants.QUIET_MOVE && (pieceMoved == Constants.WHITE_PAWN || pieceMoved == Constants.BLACK_PAWN)) {
		        this.fiftyMoveRule = 0;
	        }

            // Updates the piece count and material fields
            if (flag == Constants.CAPTURE) {
                this.pieceCount[pieceCaptured]--;

                if (pieceMoved <= Constants.WHITE_KING) {
                    this.blackMidgameMaterial -= Constants.arrayOfPieceValuesMG[pieceCaptured];
                    this.blackEndgameMaterial -= Constants.arrayOfPieceValuesEG[pieceCaptured];
                } else {
                    this.whiteMidgameMaterial -= Constants.arrayOfPieceValuesMG[pieceCaptured];
                    this.whiteEndgameMaterial -= Constants.arrayOfPieceValuesEG[pieceCaptured]; 
                }
            } else if (flag == Constants.EN_PASSANT_CAPTURE) {
                this.pieceCount[pieceCaptured]--;
                
                if (pieceMoved == Constants.WHITE_PAWN) {
                    this.blackMidgameMaterial -= Constants.arrayOfPieceValuesMG[Constants.BLACK_PAWN];
                    this.blackEndgameMaterial -= Constants.arrayOfPieceValuesEG[Constants.BLACK_PAWN];
                } else {
                    this.whiteMidgameMaterial -= Constants.arrayOfPieceValuesMG[Constants.WHITE_PAWN];
                    this.whiteEndgameMaterial -= Constants.arrayOfPieceValuesEG[Constants.WHITE_PAWN];
                }
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.pieceCount[pieceMoved]--;
                this.pieceCount[piecePromoted]++;

                if (pieceMoved == Constants.WHITE_PAWN) {
                    this.whiteMidgameMaterial -= Constants.arrayOfPieceValuesMG[Constants.WHITE_PAWN];
                    this.whiteMidgameMaterial += Constants.arrayOfPieceValuesMG[piecePromoted];

                    this.whiteEndgameMaterial -= Constants.arrayOfPieceValuesEG[Constants.WHITE_PAWN];
                    this.whiteEndgameMaterial += Constants.arrayOfPieceValuesEG[piecePromoted];
                } else {
                    this.blackMidgameMaterial -= Constants.arrayOfPieceValuesMG[Constants.BLACK_PAWN];
                    this.blackMidgameMaterial += Constants.arrayOfPieceValuesMG[piecePromoted];

                    this.blackEndgameMaterial -= Constants.arrayOfPieceValuesEG[Constants.BLACK_PAWN];
                    this.blackEndgameMaterial += Constants.arrayOfPieceValuesEG[piecePromoted];
                }
                if (flag == Constants.PROMOTION_CAPTURE) {
                    this.pieceCount[pieceCaptured]--;

                    if (pieceMoved == Constants.WHITE_PAWN) {
                        this.blackMidgameMaterial -= Constants.arrayOfPieceValuesMG[pieceCaptured];
                        this.blackEndgameMaterial -= Constants.arrayOfPieceValuesEG[pieceCaptured];
                    } else {
                        this.whiteMidgameMaterial -= Constants.arrayOfPieceValuesMG[pieceCaptured];
                        this.whiteEndgameMaterial -= Constants.arrayOfPieceValuesEG[pieceCaptured]; 
                    }
                }  
            } 
            
            // Updates the piece square tables and zobrist key
            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                this.midgamePSQ[pieceMoved] -= Constants.arrayOfPSQMidgame[pieceMoved][startSquare];
                this.midgamePSQ[pieceMoved] += Constants.arrayOfPSQMidgame[pieceMoved][destinationSquare];

                this.endgamePSQ[pieceMoved] -= Constants.arrayOfPSQEndgame[pieceMoved][startSquare];
                this.endgamePSQ[pieceMoved] += Constants.arrayOfPSQEndgame[pieceMoved][destinationSquare];

	            this.zobristKey ^= Constants.pieceZobrist[pieceMoved, startSquare];
	            this.zobristKey ^= Constants.pieceZobrist[pieceMoved, destinationSquare];

            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.midgamePSQ[pieceMoved] -= Constants.arrayOfPSQMidgame[pieceMoved][startSquare];
                
                this.endgamePSQ[pieceMoved] -= Constants.arrayOfPSQEndgame[pieceMoved][startSquare];

				this.zobristKey ^= Constants.pieceZobrist[pieceMoved, startSquare];
            } 

            if (flag == Constants.CAPTURE) {
                this.midgamePSQ[pieceCaptured] -= Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare];
                
                this.endgamePSQ[pieceCaptured] -= Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare];

				this.zobristKey ^= Constants.pieceZobrist[pieceCaptured, destinationSquare];
            } else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    this.midgamePSQ[pieceCaptured] -= Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare - 8];
                    
                    this.endgamePSQ[pieceCaptured] -= Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare - 8];

	                this.zobristKey ^= Constants.pieceZobrist[pieceCaptured, destinationSquare - 8];
                } else {
                    this.midgamePSQ[pieceCaptured] -= Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare + 8];
                    
                    this.endgamePSQ[pieceCaptured] -= Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare + 8];

					this.zobristKey ^= Constants.pieceZobrist[pieceCaptured, destinationSquare + 8];
                }
            } else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.midgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.H1];
                    this.midgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.F1];

                    this.endgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.H1];
                    this.endgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.F1];

	                this.zobristKey ^= Constants.pieceZobrist[Constants.WHITE_ROOK, Constants.H1];
	                this.zobristKey ^= Constants.pieceZobrist[Constants.WHITE_ROOK, Constants.F1];
                } else {
                    this.midgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.H8];
                    this.midgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.F8];

                    this.endgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.H8];
                    this.endgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.F8];

					this.zobristKey ^= Constants.pieceZobrist[Constants.BLACK_ROOK, Constants.H8];
					this.zobristKey ^= Constants.pieceZobrist[Constants.BLACK_ROOK, Constants.F8];
				}
            } else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.midgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.A1];
                    this.midgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.D1];

                    this.endgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.A1];
                    this.endgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.D1];

					this.zobristKey ^= Constants.pieceZobrist[Constants.WHITE_ROOK, Constants.A1];
					this.zobristKey ^= Constants.pieceZobrist[Constants.WHITE_ROOK, Constants.D1];
				} else {
                    this.midgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.A8];
                    this.midgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.D8];

                    this.endgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.A8];
                    this.endgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.D8];

					this.zobristKey ^= Constants.pieceZobrist[Constants.BLACK_ROOK, Constants.A8];
					this.zobristKey ^= Constants.pieceZobrist[Constants.BLACK_ROOK, Constants.D8];
				}
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.midgamePSQ[piecePromoted] += Constants.arrayOfPSQMidgame[piecePromoted][destinationSquare];

                this.endgamePSQ[piecePromoted] += Constants.arrayOfPSQEndgame[piecePromoted][destinationSquare];

	            this.zobristKey ^= Constants.pieceZobrist[piecePromoted, destinationSquare];

                if (flag == Constants.PROMOTION_CAPTURE) {
                    this.midgamePSQ[pieceCaptured] -= Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare];

                    this.endgamePSQ[pieceCaptured] -= Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare];

	                this.zobristKey ^= Constants.pieceZobrist[pieceCaptured, destinationSquare];
                }
            }

			// Adds the zobrist key to the game history array list
			this.gameHistory.Add(this.zobristKey);

	        // Have a history hash of 2^14 = 16384 elements
			// Take the least significant 14 bits of the zobrist key to get an index, and increment the corresponding element in the history hash by 1
			// When checking for repetitions, convert the zobrist of the current position to an index and check the history hash 
			// if the element is 1, then no need to check for repetition 
			// If element is greater than 1, it could be that the position was seen before, or another position (which hashed to the same index) was seen before
			// If this is the case, then have to loop through the game history array list to check 
			// This hash table will never produce a false negative, but may produce a false positive
			// It saves a lot of unnecessary searching through the history list when there is no chance of a repetition

	        this.historyHash[(this.zobristKey % (ulong)this.historyHash.Length)] ++;

            // Updates the bitboards and arrays

            //If is any move except a promotion, then add a piece to the destination square
            //If it is a promotion don't add piece (pawn) to the destination square
            //Only remove it from the start square
            int colourOfPiece = 0;
            
            if (pieceMoved <= Constants.WHITE_KING) {
                colourOfPiece = Constants.WHITE;
            } else if (pieceMoved >= Constants.BLACK_PAWN) {
                colourOfPiece = Constants.BLACK;
            }

            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                
                //updates the bitboard and removes the int representing the piece from the start square of the piece array, and adds an int representing the piece to the destination square of the piece array
                this.arrayOfBitboards[pieceMoved] ^= (startSquareBitboard | destinationSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard | destinationSquareBitboard);

                this.pieceArray[startSquare] = Constants.EMPTY;
                this.pieceArray[destinationSquare] = pieceMoved;
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.arrayOfBitboards[pieceMoved] ^= (startSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard);

                this.pieceArray[startSquare] = Constants.EMPTY;     
            }

            //If there was a capture, also remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
			//Don't have to update the array because it was already overridden with the capturing piece
			if (flag == Constants.CAPTURE) {

                this.arrayOfBitboards[pieceCaptured] ^= (destinationSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);

			}
            //If there was an en-passant capture, remove the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
			//Update the array because the pawn destination square and captured pawn are on different squares
            else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    this.arrayOfBitboards[pieceCaptured] ^= (destinationSquareBitboard >> 8);
                    this.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard >> 8);

                    this.pieceArray[destinationSquare - 8] = Constants.EMPTY;

                    } else if (pieceMoved == Constants.BLACK_PAWN) {
                    this.arrayOfBitboards[pieceCaptured] ^= (destinationSquareBitboard << 8);
                    this.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard << 8);

                    this.pieceArray[destinationSquare + 8] = Constants.EMPTY;
                }	
			}
            //If short castle, then move the rook from H1 to F1
            //If short castle, then move the rook from H8 to F8
            else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.arrayOfBitboards[Constants.WHITE_ROOK] ^= (Constants.H1_BITBOARD | Constants.F1_BITBOARD);
                     this.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.H1_BITBOARD | Constants.F1_BITBOARD);

                    this.pieceArray[Constants.H1] = Constants.EMPTY;
                    this.pieceArray[Constants.F1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    this.arrayOfBitboards[Constants.BLACK_ROOK] ^= (Constants.H8_BITBOARD | Constants.F8_BITBOARD);
                    this.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.H8_BITBOARD | Constants.F8_BITBOARD);

                    this.pieceArray[Constants.H8] = Constants.EMPTY;
                    this.pieceArray[Constants.F8] = Constants.BLACK_ROOK;
                }
            }
            //If long castle, then move the rook from A1 to D1     
            //If long castle, then move the rook from A8 to D8
            else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.arrayOfBitboards[Constants.WHITE_ROOK] ^= (Constants.A1_BITBOARD | Constants.D1_BITBOARD);
                     this.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.A1_BITBOARD | Constants.D1_BITBOARD);

                    this.pieceArray[Constants.A1] = Constants.EMPTY;
                    this.pieceArray[Constants.D1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    this.arrayOfBitboards[Constants.BLACK_ROOK] ^= (Constants.A8_BITBOARD | Constants.D8_BITBOARD);
                    this.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.A8_BITBOARD | Constants.D8_BITBOARD);

                    this.pieceArray[Constants.A8] = Constants.EMPTY;
                    this.pieceArray[Constants.D8] = Constants.BLACK_ROOK;
                }       
            }
                //If regular promotion, updates the pawn's bitboard, the promoted piece bitboard, and the piece array
            else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.arrayOfBitboards[piecePromoted] ^= destinationSquareBitboard;
                this.arrayOfAggregateBitboards[colourOfPiece] ^= (destinationSquareBitboard);

                this.pieceArray[destinationSquare] = piecePromoted;

                //If there was a capture, removes the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                if (flag == Constants.PROMOTION_CAPTURE) {
                    this.arrayOfBitboards[pieceCaptured] ^= (destinationSquareBitboard);
                    this.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);
                }
            } 
            
			//updates the aggregate bitboards
            this.arrayOfAggregateBitboards[Constants.ALL] = ( this.arrayOfAggregateBitboards[Constants.WHITE] | this.arrayOfAggregateBitboards[Constants.BLACK]);

            //increments the full-move number (implement later)
            //Implement the half-move clock later (in the moves)
            //Also implement the repetitions later
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        // UNMAKE MOVE 
        // takes an int representing a move and unmakes it
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        public void unmakeMove(int unmoveRepresentationInput, stateVariables restoreData) {

            // Sets the side to move, white short/long castle rights, black short/long castle rights from the integer encoding the restore board data
            // Sets the repetition number, half-live clock (since last pawn push/capture), and move number from the integer encoding the restore board data
            this.sideToMove = restoreData.sideToMove;
            this.whiteShortCastleRights = restoreData.whiteShortCastleRights;
            this.whiteLongCastleRights = restoreData.whiteLongCastleRights;
            this.blackShortCastleRights = restoreData.blackShortCastleRights;
            this.blackLongCastleRights = restoreData.blackLongCastleRights;
            this.fiftyMoveRule = restoreData.fiftyMoveRule;
            this.fullMoveNumber = restoreData.fullMoveNumber;
	        this.zobristKey = restoreData.zobristKey;

			// Restores the history hash to its original value
			this.historyHash[(this.zobristKey % (ulong)this.historyHash.Length)]--;

            // Sets the en passant square bitboard from the integer encoding the restore board data (have to convert an int index to a ulong bitboard)
            this.enPassantSquare = restoreData.enPassantSquare;

	        this.whiteMidgameMaterial = restoreData.whiteMidgameMaterial;
	        this.whiteEndgameMaterial = restoreData.whiteEndgameMaterial;
	        this.blackMidgameMaterial = restoreData.blackMidgameMaterial;
	        this.blackEndgameMaterial = restoreData.blackEndgameMaterial;

		    //Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
		    int pieceMoved = ((unmoveRepresentationInput & Constants.PIECE_MOVED_MASK) >> 0);
		    int startSquare = ((unmoveRepresentationInput & Constants.START_SQUARE_MASK) >> 4);
		    int destinationSquare = ((unmoveRepresentationInput & Constants.DESTINATION_SQUARE_MASK) >> 10);
		    int flag = ((unmoveRepresentationInput & Constants.FLAG_MASK) >> 16);
		    int pieceCaptured = ((unmoveRepresentationInput & Constants.PIECE_CAPTURED_MASK) >> 20);
            int piecePromoted = ((unmoveRepresentationInput & Constants.PIECE_PROMOTED_MASK) >> 24);

		    //Calculates bitboards for removing piece from start square and adding piece to destionation square
		    //"and" with startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
		    ulong startSquareBitboard = (0x1UL << startSquare);
		    ulong destinationSquareBitboard = (0x1UL << destinationSquare);

            // Updates the piece count and material fields
            if (flag == Constants.CAPTURE) {
                this.pieceCount[pieceCaptured]++;
            } else if (flag == Constants.EN_PASSANT_CAPTURE) {
                this.pieceCount[pieceCaptured]++;

            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.pieceCount[pieceMoved]++;
                this.pieceCount[piecePromoted]--;

                if (flag == Constants.PROMOTION_CAPTURE) {
                    this.pieceCount[pieceCaptured]++;
                }
            }

			// Removes the last element of the game history list
			this.gameHistory.RemoveAt(this.gameHistory.Count-1);

            // Updates the piece square tables (faster to recalculate than to copy into the state variable object and copy back)
            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                this.midgamePSQ[pieceMoved] -= Constants.arrayOfPSQMidgame[pieceMoved][destinationSquare];
                this.midgamePSQ[pieceMoved] += Constants.arrayOfPSQMidgame[pieceMoved][startSquare];

                this.endgamePSQ[pieceMoved] -= Constants.arrayOfPSQEndgame[pieceMoved][destinationSquare];
                this.endgamePSQ[pieceMoved] += Constants.arrayOfPSQEndgame[pieceMoved][startSquare];

            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.midgamePSQ[pieceMoved] += Constants.arrayOfPSQMidgame[pieceMoved][startSquare];

                this.endgamePSQ[pieceMoved] += Constants.arrayOfPSQEndgame[pieceMoved][startSquare];
            }

            if (flag == Constants.CAPTURE) {
                this.midgamePSQ[pieceCaptured] += Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare];

                this.endgamePSQ[pieceCaptured] += Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare];
            } else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    this.midgamePSQ[pieceCaptured] += Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare - 8];

                    this.endgamePSQ[pieceCaptured] += Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare - 8];
                } else {
                    this.midgamePSQ[pieceCaptured] += Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare + 8];

                    this.endgamePSQ[pieceCaptured] += Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare + 8];
                }
            } else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.midgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.H1];
                    this.midgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.F1];

                    this.endgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.H1];
                    this.endgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.F1];
                } else {
                    this.midgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.H8];
                    this.midgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.F8];

                    this.endgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.H8];
                    this.endgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.F8];
                }
            } else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.midgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.A1];
                    this.midgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQMidgame[Constants.WHITE_ROOK][Constants.D1];

                    this.endgamePSQ[Constants.WHITE_ROOK] += Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.A1];
                    this.endgamePSQ[Constants.WHITE_ROOK] -= Constants.arrayOfPSQEndgame[Constants.WHITE_ROOK][Constants.D1];
                } else {
                    this.midgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.A8];
                    this.midgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQMidgame[Constants.BLACK_ROOK][Constants.D8];

                    this.endgamePSQ[Constants.BLACK_ROOK] += Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.A8];
                    this.endgamePSQ[Constants.BLACK_ROOK] -= Constants.arrayOfPSQEndgame[Constants.BLACK_ROOK][Constants.D8];
                }
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.midgamePSQ[piecePromoted] -= Constants.arrayOfPSQMidgame[piecePromoted][destinationSquare];

                this.endgamePSQ[piecePromoted] -= Constants.arrayOfPSQEndgame[piecePromoted][destinationSquare];

                if (flag == Constants.PROMOTION_CAPTURE) {
                    this.midgamePSQ[pieceCaptured] += Constants.arrayOfPSQMidgame[pieceCaptured][destinationSquare];

                    this.endgamePSQ[pieceCaptured] += Constants.arrayOfPSQEndgame[pieceCaptured][destinationSquare];
                }
            }

            //Removes the bit corresponding to the destination square, and adds a bit corresponding with the start square (to unmake move)
            //Removes the int representing the piece from the destination square of the piece array, and adds an int representing the piece to the start square of the piece array (to unmake move)
            //If it was a promotion, then don't have to remove the pawn from the destination square
            int colourOfPiece = 0;

            if (pieceMoved <= Constants.WHITE_KING) {
                colourOfPiece = Constants.WHITE;
            } else if (pieceMoved >= Constants.BLACK_PAWN) {
                colourOfPiece = Constants.BLACK;
            }
            
            
            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                this.arrayOfBitboards[pieceMoved] ^= (startSquareBitboard | destinationSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard | destinationSquareBitboard);

                this.pieceArray[destinationSquare] = Constants.EMPTY;
                this.pieceArray[startSquare] = pieceMoved; 
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.arrayOfBitboards[pieceMoved] ^= (startSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard);

                this.pieceArray[startSquare] = pieceMoved;    
            }

            //If there was a capture, add to the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
            //Also re-add the captured piece to the array
            if (flag == Constants.CAPTURE) {
                this.arrayOfBitboards[pieceCaptured] ^= (destinationSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);

                this.pieceArray[destinationSquare] = pieceCaptured;  
            }
            //If there was an en-passant capture, add the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
            //Also re-add teh captured pawn to the array 
            else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    this.arrayOfBitboards[Constants.BLACK_PAWN] ^= (destinationSquareBitboard >> 8);
                    this.arrayOfAggregateBitboards[Constants.BLACK] ^= (destinationSquareBitboard >> 8);

                    this.pieceArray[destinationSquare - 8] = Constants.BLACK_PAWN;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    this.arrayOfBitboards[Constants.WHITE_PAWN] ^= (destinationSquareBitboard << 8);
                     this.arrayOfAggregateBitboards[Constants.WHITE] ^= (destinationSquareBitboard << 8);

                    this.pieceArray[destinationSquare + 8] = Constants.WHITE_PAWN; 
                }  
            } 
            //If white king short castle, then move the rook from F1 to H1
            //If black king short castle, then move the rook from F8 to H8
            else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.arrayOfBitboards[Constants.WHITE_ROOK] ^= (Constants.F1_BITBOARD | Constants.H1_BITBOARD);
                     this.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.F1_BITBOARD | Constants.H1_BITBOARD);

                    this.pieceArray[Constants.F1] = Constants.EMPTY;
                    this.pieceArray[Constants.H1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    this.arrayOfBitboards[Constants.BLACK_ROOK] ^= (Constants.F8_BITBOARD | Constants.H8_BITBOARD);
                    this.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.F8_BITBOARD | Constants.H8_BITBOARD);

                    this.pieceArray[Constants.F8] = Constants.EMPTY;
                    this.pieceArray[Constants.H8] = Constants.BLACK_ROOK;
                }
            }
           //If king long castle, then move the rook from D1 to A1
            //If black king long castle, then move the rook from D8 to A8
            else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    this.arrayOfBitboards[Constants.WHITE_ROOK] ^= (Constants.D1_BITBOARD | Constants.A1_BITBOARD);
                     this.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.D1_BITBOARD | Constants.A1_BITBOARD);

                    this.pieceArray[Constants.D1] = Constants.EMPTY;
                    this.pieceArray[Constants.A1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    this.arrayOfBitboards[Constants.BLACK_ROOK] ^= (Constants.D8_BITBOARD | Constants.A8_BITBOARD);
                    this.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.D8_BITBOARD | Constants.A8_BITBOARD);

                    this.pieceArray[Constants.D8] = Constants.EMPTY;
                    this.pieceArray[Constants.A8] = Constants.BLACK_ROOK;
                }
            }
            //If there were promotions, update the promoted piece bitboard
            else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                this.arrayOfBitboards[piecePromoted] ^= (destinationSquareBitboard);
                this.arrayOfAggregateBitboards[colourOfPiece] ^= (destinationSquareBitboard);

                this.pieceArray[destinationSquare] = Constants.EMPTY;

                //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                //Also adds the captured piece back to the array
                if (flag == Constants.PROMOTION_CAPTURE) {
                    this.arrayOfBitboards[pieceCaptured] ^= (destinationSquareBitboard);
                    this.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);

                    this.pieceArray[destinationSquare] = pieceCaptured;
                }
            } 

	        //updates the aggregate bitboards
            this.arrayOfAggregateBitboards[Constants.ALL] = ( this.arrayOfAggregateBitboards[Constants.WHITE] | this.arrayOfAggregateBitboards[Constants.BLACK]);
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
		// ATTACK INFORMATION
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------

        public Bitboard getBitboardOfAttackers(int colourUnderAttack, int squareToCheck) {
            ulong bitboardOfAttackers = 0x0UL;

            if (colourUnderAttack == Constants.WHITE) {

                // Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL] & Constants.rookOccupancyMask[squareToCheck];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
                ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

                // Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL] & Constants.bishopOccupancyMask[squareToCheck];
                int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
                ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

                // Looks up knight attack set from square, and intersects with opponent's knight bitboard
                ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

                // Looks up white pawn attack set from square, and intersects with opponent's pawn bitboard
                ulong whitePawnMoveFromSquare = Constants.whiteCapturesAndCapturePromotions[squareToCheck];

                // Looks up king attack set from square, and intersects with opponent's king bitboard
                ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

                bitboardOfAttackers |= ((rookMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_ROOK])
                                    | (rookMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_QUEEN])
                                    | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_BISHOP])
                                    | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_QUEEN])
                                    | (knightMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_KNIGHT])
                                    | (whitePawnMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_PAWN])
                                    | (kingMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_KING]));

                return bitboardOfAttackers;

            } else {

                // Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL] & Constants.rookOccupancyMask[squareToCheck];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
                ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

                // Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL] & Constants.bishopOccupancyMask[squareToCheck];
                int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
                ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

                // Looks up knight attack set from square, and intersects with opponent's knight bitboard
                ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

                // Looks up black pawn attack set from square, and intersects with opponent's pawn bitboard
                ulong blackPawnMoveFromSquare = Constants.blackCapturesAndCapturePromotions[squareToCheck];

                // Looks up king attack set from square, and intersects with opponent's king bitboard
                ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

                bitboardOfAttackers = ((rookMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_ROOK])
                                       | (rookMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_QUEEN])
                                       | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_BISHOP])
                                       | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_QUEEN])
                                       | (knightMoveFromSquare & this.arrayOfBitboards[Constants.WHITE_KNIGHT])
                                       | (blackPawnMoveFromSquare & this.arrayOfBitboards[Constants.WHITE_PAWN])
                                       | (kingMoveFromSquare & this.arrayOfBitboards[Constants.WHITE_KING]));

                return bitboardOfAttackers;
            }
        }

        // Method determines how many times a square is attacked
        // Takes in square and colour, and returns the number of pieces of the opposite colour attacking that square
        public int timesSquareIsAttacked(int colourUnderAttack, int squareToCheck) {

            // Gets the bitboard of attackers for that square and returns a popcount
            Bitboard bitboardOfAttackers = getBitboardOfAttackers(colourUnderAttack, squareToCheck);
            
			if (bitboardOfAttackers == 0) {
                return 0;
            } else {
                return Constants.popcount(bitboardOfAttackers);
            }
		}

        // Method is called after you generate and make a move
        // Determines if the king (of the player who made the move) is attacked
        // Player who made move != player whose turn it is (if white player made move, then it will be black's turn)
        public bool isMoveLegal(int sideToMove) {

            bool isMoveLegal = false;

            // Finds the corresponding king, and checks to see if it is attacked
            int indexOfKing = Constants.findFirstSet(this.arrayOfBitboards[Constants.KING + 6 * sideToMove]);
            if (this.timesSquareIsAttacked(sideToMove, indexOfKing) == Constants.NOT_IN_CHECK) {
                isMoveLegal = true;
            }
            return isMoveLegal;
        }

        // Method is called before you generate moves 
        // Determines if the king (of the player whose turn it is) is attacked
        public bool isInCheck() {

            bool isInCheck = false;

            // Finds the corresponding king, and checks to see if it is attacked
            int indexOfKing = Constants.findFirstSet(this.arrayOfBitboards[Constants.KING + 6 * this.sideToMove]);
            if (this.timesSquareIsAttacked(this.sideToMove, indexOfKing) != Constants.NOT_IN_CHECK) {
                isInCheck = true;
            }
            return isInCheck;
        }

		// Method that returns the least valuable piece attacking or defending a square
	    public Bitboard getLVP(Bitboard attackersAndDefenders, int side) {
		    
			// Loops through all attacker (or defender) bitboards from least valuable to most valuable
			// As soon as an intersection is found with the bitboard of attackers and defenders, returns that intersection
			for (int i = Constants.WHITE_PAWN + 6 * side; i <= Constants.WHITE_KING + 6*side; i ++) {
			    Bitboard LVP = attackersAndDefenders & arrayOfBitboards[i];
			    if (LVP != 0) {
				    return LVP;
			    }
		    }
		    return 0;
	    }

	    public Bitboard getXRay(int destinationSquare, int frontAttackerSquare, Bitboard oldOccupiedBitboard, Bitboard newOccupiedBitboard, int sideToCapture) {
		    int frontAttackerPieceType = this.pieceArray[frontAttackerSquare];

		    if (sideToCapture == Constants.WHITE) {
				if (frontAttackerPieceType == Constants.WHITE_ROOK || frontAttackerPieceType == Constants.WHITE_QUEEN) {
					Bitboard rookAttacksFromDestionationSquareNew = generateRookMovesFromIndex(newOccupiedBitboard, destinationSquare);
					Bitboard rookAttacksFromFrontAttackerSquare = generateRookMovesFromIndex(newOccupiedBitboard, frontAttackerSquare);
					Bitboard rookAttacksFromDestinationSquareOld = generateRookMovesFromIndex(oldOccupiedBitboard, destinationSquare);
					Bitboard ray = rookAttacksFromDestionationSquareNew & rookAttacksFromFrontAttackerSquare & ~rookAttacksFromDestinationSquareOld;

					Bitboard potentialXRayAttackers = ray & this.arrayOfAggregateBitboards[Constants.WHITE];
					int potentialXRayPiece = this.pieceArray[Constants.findFirstSet(potentialXRayAttackers)];
					if (potentialXRayPiece == Constants.WHITE_ROOK || potentialXRayPiece == Constants.WHITE_QUEEN) {
						Debug.Assert(Constants.popcount(potentialXRayAttackers) == 1);
						return potentialXRayAttackers;
					}
				}

				if (frontAttackerPieceType == Constants.WHITE_PAWN || frontAttackerPieceType == Constants.WHITE_BISHOP || frontAttackerPieceType == Constants.WHITE_QUEEN) {
					Bitboard bishopAttacksFromDestionationSquare = generateBishopMovesFromIndex(newOccupiedBitboard, destinationSquare);
					Bitboard bishopAttacksFromFrontAttackerSquare = generateBishopMovesFromIndex(newOccupiedBitboard, frontAttackerSquare);
					Bitboard bishopAttacksFromDestionationSquareOld = generateBishopMovesFromIndex(oldOccupiedBitboard, destinationSquare);
					Bitboard ray = bishopAttacksFromDestionationSquare & bishopAttacksFromFrontAttackerSquare & ~bishopAttacksFromDestionationSquareOld;

					Bitboard potentialXRayAttackers = ray & this.arrayOfAggregateBitboards[Constants.WHITE];
					int potentialXRayPiece = this.pieceArray[Constants.findFirstSet(potentialXRayAttackers)];
					if (potentialXRayPiece == Constants.WHITE_BISHOP || potentialXRayPiece == Constants.WHITE_QUEEN) {
						Debug.Assert(Constants.popcount(potentialXRayAttackers) == 1);
						return potentialXRayAttackers;
					}
				}
		    } else {
				if (frontAttackerPieceType == Constants.BLACK_ROOK || frontAttackerPieceType == Constants.BLACK_QUEEN) {
					Bitboard rookAttacksFromDestionationSquare = generateRookMovesFromIndex(newOccupiedBitboard, destinationSquare);
					Bitboard rookAttacksFromFrontAttackerSquareNew = generateRookMovesFromIndex(newOccupiedBitboard, frontAttackerSquare);
					Bitboard rookAttacksFromDestinationSquareOld = generateRookMovesFromIndex(oldOccupiedBitboard, destinationSquare);
					Bitboard ray = rookAttacksFromDestionationSquare & rookAttacksFromFrontAttackerSquareNew & ~rookAttacksFromDestinationSquareOld;

					Bitboard potentialXRayAttackers = ray & this.arrayOfAggregateBitboards[Constants.BLACK];
					int potentialXRayPiece = this.pieceArray[Constants.findFirstSet(potentialXRayAttackers)];
					if (potentialXRayPiece == Constants.BLACK_ROOK|| potentialXRayPiece == Constants.BLACK_QUEEN) {
						Debug.Assert(Constants.popcount(potentialXRayAttackers) == 1);
						return potentialXRayAttackers;
					}
				}

				if (frontAttackerPieceType == Constants.BLACK_PAWN || frontAttackerPieceType == Constants.BLACK_BISHOP || frontAttackerPieceType == Constants.BLACK_QUEEN) {
					Bitboard bishopAttacksFromDestionationSquare = generateBishopMovesFromIndex(newOccupiedBitboard, destinationSquare);
					Bitboard bishopAttacksFromFrontAttackerSquare = generateBishopMovesFromIndex(newOccupiedBitboard, frontAttackerSquare);
					Bitboard bishopAttacksFromDestionationSquareOld = generateBishopMovesFromIndex(oldOccupiedBitboard, destinationSquare);
					Bitboard ray = bishopAttacksFromDestionationSquare & bishopAttacksFromFrontAttackerSquare & ~bishopAttacksFromDestionationSquareOld;

					Bitboard potentialXRayAttackers = ray & this.arrayOfAggregateBitboards[Constants.BLACK];
					int potentialXRayPiece = this.pieceArray[Constants.findFirstSet(potentialXRayAttackers)];
					if (potentialXRayPiece == Constants.BLACK_BISHOP || potentialXRayPiece == Constants.BLACK_QUEEN) {
						Debug.Assert(Constants.popcount(potentialXRayAttackers) == 1);
						return potentialXRayAttackers;
					}
				}
		    }
		    return 0;
	    }

		// Method that returns the expected evaluation value to be lost or gained after a series of exchanges on a single square
	    public int staticExchangeEval(int startSquare, int destinationSquare, int sideToCapture) {

			int[] gain = new int[32];
		    int depth = 0;
			Bitboard mayBeFrontAttacker = (this.arrayOfBitboards[Constants.WHITE_PAWN] | this.arrayOfBitboards[Constants.WHITE_BISHOP] |
		                                   this.arrayOfBitboards[Constants.WHITE_ROOK] | this.arrayOfBitboards[Constants.WHITE_QUEEN] |
		                                   this.arrayOfBitboards[Constants.BLACK_PAWN] | this.arrayOfBitboards[Constants.BLACK_BISHOP] |
		                                   this.arrayOfBitboards[Constants.BLACK_ROOK] | this.arrayOfBitboards[Constants.BLACK_QUEEN]);
			Bitboard startSquareBitboard = (0x1UL << startSquare);
			Bitboard occupiedBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
			Bitboard attackersAndDefenders = this.getBitboardOfAttackers(Constants.WHITE, destinationSquare) | this.getBitboardOfAttackers(Constants.BLACK, destinationSquare);
		    
			// Material win for side capturing (e.g. white) if the target piece (e.g. pawn) is en-prise
			gain[depth] = Constants.arrayOfPieceValueSEE[this.pieceArray[destinationSquare]];
			
			Console.WriteLine(gain[depth]);

			do {
			    depth++;
				
				
				// Material win for other side (e.g. black) if the piece that just captured (e.g. rook) is en-prise
				gain[depth] = Constants.arrayOfPieceValueSEE[this.pieceArray[startSquare]] - gain[depth - 1];
			    
				Console.WriteLine(gain[depth]);

				// Doesn't affect the sign of the result, but affects the magnitude so will leave out
				/*if (Math.Max(-gain[depth - 1], gain[depth]) < 0) {
				    break;
			    }*/
				
				

				// Add any hidden attackers
				if ((startSquareBitboard & mayBeFrontAttacker) != 0) {
					attackersAndDefenders |= getXRay(destinationSquare, startSquare, occupiedBitboard, occupiedBitboard ^ startSquareBitboard, sideToCapture);
				}

				// Remove the piece that just captured (e.g. rook) from the attackers/defenders and occupied bitboards
				attackersAndDefenders ^= startSquareBitboard;
				occupiedBitboard ^= startSquareBitboard;

				sideToCapture ^= 1;

				// Get the other side's (e.g. black) least valuable attacker
				startSquareBitboard = this.getLVP(attackersAndDefenders, sideToCapture);

				startSquare = Constants.findFirstSet(startSquareBitboard);

			// If there are no least valuable attackers, then break out of the loop
		    } while (startSquareBitboard != 0);

		    while (-- depth != 0) {
			    gain[depth - 1] = -Math.Max(-gain[depth - 1], gain[depth]);
		    }

		    return gain[0];
	    }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
		// ALMOST LEGAL MOVE GENERATOR
        // Only called when the king (of the player whose turn it is) is not attacked
        // For pieces that are in an absolute pin, only generates moves along the pin ray (including capture of pinner)
        // For castling, only generates moves that don't involve king passing through attacked square 
		// When testing for legality, only have to check king moves and en passant
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        
        public int[] generateAlmostLegalMoves() {

            if (this.sideToMove == Constants.WHITE) {
                
                //Gets the indices of all of the pieces
                Bitboard tempWhitePawnBitboard = this.arrayOfBitboards[Constants.WHITE_PAWN];
                Bitboard tempWhiteKnightBitboard = this.arrayOfBitboards[Constants.WHITE_KNIGHT];
                Bitboard tempWhiteBishopBitboard = this.arrayOfBitboards[Constants.WHITE_BISHOP];
                Bitboard tempWhiteRookBitboard = this.arrayOfBitboards[Constants.WHITE_ROOK];
                Bitboard tempWhiteQueenBitboard = this.arrayOfBitboards[Constants.WHITE_QUEEN];
                Bitboard tempWhiteKingBitboard = this.arrayOfBitboards[Constants.WHITE_KING];
                Bitboard tempAllPieceBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
                Bitboard tempBlackRookAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_ROOK] | this.arrayOfBitboards[Constants.BLACK_QUEEN]);
                Bitboard tempBlackBishopAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_BISHOP] | this.arrayOfBitboards[Constants.BLACK_QUEEN]);
                int kingIndex = Constants.findFirstSet(tempWhiteKingBitboard);

                int[] listOfAlmostLegalMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;

                // Finds rook moves from the king, and intersects with white (own) pieces to get bitboard of potentially pinned pieces
                Bitboard potentiallyPinnedPiecesByRook = ((this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex)) & this.arrayOfAggregateBitboards[Constants.WHITE]);

               // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                // Intersect with black rook and queen to get bitboard of potential pinners
                Bitboard tempAllPieceExceptPotentiallyPinnedByRookBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByRook);
                Bitboard rookMovesFromIndexWithoutPinned = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, kingIndex);
                Bitboard potentialPinners = (rookMovesFromIndexWithoutPinned & tempBlackRookAndQueenBitboard);

              // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                while (potentialPinners != 0) {
                    int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);

                    // Removes the potential pinner from the bitboard
                    potentialPinners &= (potentialPinners - 1);

                    Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                    Bitboard rookMovesFromPinnerIndex = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, indexOfPotentialPinner);

                    // If intersection with potentially pinned pieces is not zero, then piece is pinned
                    // Generates pin ray
                    Bitboard pinnedPiece = (rookMovesFromPinnerIndex & potentiallyPinnedPiecesByRook);
                    if (pinnedPiece != 0) {
                        Bitboard pinRay = (rookMovesFromIndexWithoutPinned & (rookMovesFromPinnerIndex | pinner));
                        
                        int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                        int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                        // If the pinned piece is a white pawn, then generate single and double pushes along the pin ray
                        
                        if (pinnedPieceType == Constants.WHITE_PAWN) {

                            //For pawns that are between the 2nd and 6th ranks, generate single pushes
                            if (indexOfPinnedPiece >= Constants.H2 && indexOfPinnedPiece <= Constants.A6) {
                                //Generates white pawn single moves
                                Bitboard possiblePawnSingleMoves = (Constants.whiteSinglePawnMovesAndPromotionMoves[indexOfPinnedPiece] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & pinRay);
                                index = this.generatePawnMove(indexOfPinnedPiece, possiblePawnSingleMoves, listOfAlmostLegalMoves, index, Constants.WHITE);
                            }
                            //For pawns that are on the 2nd rank, generate double pawn pushes
                            if (indexOfPinnedPiece >= Constants.H2 && indexOfPinnedPiece <= Constants.A2) {
                                Bitboard singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[indexOfPinnedPiece];
                                Bitboard doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[indexOfPinnedPiece + 8];
                                Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                                if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                    pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & pinRay);
                                }

                                index = this.generatePawnDoubleMove(indexOfPinnedPiece, pseudoLegalDoubleMoveFromIndex,listOfAlmostLegalMoves, index, Constants.WHITE);

                            }
                            // Removes the white pawn from the list of white pawns
                            tempWhitePawnBitboard &= (~pinnedPiece);
                        }
                        // If the pinned piece is a white rook, then generate moves along the pin ray
                        else if (pinnedPieceType == Constants.WHITE_ROOK) {
                            Bitboard legalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & (pinRay));
                            index = this.generateRookMoves(indexOfPinnedPiece, legalRookMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);

                            // Removes the white rook from the list of white rooks
                            tempWhiteRookBitboard &= (~pinnedPiece);
                        }
                        // If the pinned piece is a white queen, then generate moves along the pin ray (only rook moves)
                        else if (pinnedPieceType == Constants.WHITE_QUEEN) {
                            Bitboard legalQueenMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & (pinRay));
                            index = this.generateQueenMoves(indexOfPinnedPiece, legalQueenMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                            
                            // Removes the white queen from the list of white queens
                            tempWhiteQueenBitboard &= (~pinnedPiece);
                        }
                        // If pinned piece type is a white knight, then it isn't allowed to move
                        else if (pinnedPieceType == Constants.WHITE_KNIGHT) {
                            // Remove it from the knight list so that no night moves will be generated later on
                            tempWhiteKnightBitboard &= (~pinnedPiece);
                        }
                        // If pinned piece type is a white bishop, then it isn't allowed to move
                        else if (pinnedPieceType == Constants.WHITE_BISHOP) {
                            // Remove it from the bishop list so that no bishop moves will be generated later on
                            tempWhiteBishopBitboard &= (~pinnedPiece);
                        }
                        // Note that pawn captures, en-passant captures, promotions, promotion-captures, knight moves, and bishop moves will all be illegal
                    }
                    
                }
                // Finds bishop moves from the king, and intersects with white (own) pieces to get bitboard of potentially pinned pieces
                Bitboard potentiallyPinnedPiecesByBishop = (this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex) & this.arrayOfAggregateBitboards[Constants.WHITE]);

               // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                // Intersect with black rook and queen to get bitboard of potential pinners
                Bitboard tempAllPieceExceptPotentiallyPinnedByBishopBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByBishop);
                Bitboard bishopMovesFromIndexWithoutPinned = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, kingIndex);
                potentialPinners = (bishopMovesFromIndexWithoutPinned & (tempBlackBishopAndQueenBitboard));

                // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                while (potentialPinners != 0) {
                    int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);
                    // Removes the potential pinner from the black rook and queen bitboard
                    potentialPinners &= (potentialPinners - 1);
                    Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                    Bitboard bishopMovesFromPinnerIndex = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, indexOfPotentialPinner);

                   // If intersection with potentially pinned pieces is not zero, then piece is pinned
                    // Generates pin ray
                    Bitboard pinnedPiece = (bishopMovesFromPinnerIndex & potentiallyPinnedPiecesByBishop);
                    if (pinnedPiece != 0) {
                        Bitboard pinRay = (bishopMovesFromIndexWithoutPinned & (bishopMovesFromPinnerIndex | pinner));
                        
                        int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                        int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                        // If the pinned piece is a white pawn, then generate captures, en passant captures, and capture promotions
                        if (pinnedPieceType == Constants.WHITE_PAWN) {

                            //For pawns that are between the 2nd and 6th ranks, generate captures
                            if (indexOfPinnedPiece >= Constants.H2 && indexOfPinnedPiece <= Constants.A6) {

                                //Generates white pawn captures (will be a maximum of 1 along the pin ray)
                                Bitboard legalPawnCapturesFromIndex = (Constants.whiteCapturesAndCapturePromotions[indexOfPinnedPiece] & this.arrayOfAggregateBitboards[Constants.BLACK] & pinRay);
                                index = this.generatePawnCaptures(indexOfPinnedPiece, legalPawnCapturesFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                            }
                            //For pawns that are on the 5th rank, generate en passant captures
                            if ((this.enPassantSquare & Constants.RANK_6) != 0) {
                                if (indexOfPinnedPiece >= Constants.H5 && indexOfPinnedPiece <= Constants.A5) {
                                    Bitboard legalEnPassantFromIndex = (Constants.whiteCapturesAndCapturePromotions[indexOfPinnedPiece] & this.enPassantSquare & pinRay);
                                    index = this.generatePawnEnPassant(indexOfPinnedPiece, legalEnPassantFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                                    
                                }
                            }
                            //For pawns on the 7th rank, generate promotion captures
                            if (indexOfPinnedPiece >= Constants.H7 && indexOfPinnedPiece <= Constants.A7) {

                                //Generates white pawn capture promotions
                                Bitboard legalPawnCapturesFromIndex = (Constants.whiteCapturesAndCapturePromotions[indexOfPinnedPiece] & this.arrayOfAggregateBitboards[Constants.BLACK] & pinRay);
                                index = this.generatePawnPromotionCapture(indexOfPinnedPiece, legalPawnCapturesFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                            }
                            // Removes the white pawn from the list of white pawns
                            tempWhitePawnBitboard &= (~pinnedPiece);
                        }
                        // If the pinned piece is a white bishop, then generate moves along the pin ray
                        else if (pinnedPieceType == Constants.WHITE_BISHOP) {

                            Bitboard legalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & (pinRay));
                            index = this.generateBishopMoves(indexOfPinnedPiece, legalBishopMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                            
                            // Removes the white bishop from the list of white rooks
                            tempWhiteBishopBitboard &= (~pinnedPiece);
                        }
                        // If the pinned piece is a white queen, then generate moves along the pin ray
                        else if (pinnedPieceType == Constants.WHITE_QUEEN) {

                            Bitboard legalQueenMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & (pinRay));
                            index = this.generateQueenMoves(indexOfPinnedPiece, legalQueenMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);

                            // Removes the white queen from the list of white queens
                            tempWhiteQueenBitboard &= (~pinnedPiece);
                        }
                       // If pinned piece type is a white knight, then it isn't allowed to move
                       else if (pinnedPieceType == Constants.WHITE_KNIGHT) {
                            // Remove it from the knight list so that no night moves will be generated later on
                            tempWhiteKnightBitboard &= (~pinnedPiece);
                        }
                        // If pinned piece type is a white rook, then it isn't allowed to move
                        else if (pinnedPieceType == Constants.WHITE_ROOK) {
                            // Remove it from the bishop list so that no bishop moves will be generated later on
                            tempWhiteRookBitboard &= (~pinnedPiece);
                        }
                       // Note that single pawn pushes, double pawn pushes, promotions, promotion-captures, knight moves, and rook moves will all be illegal
                    }
                     
                }      
                // Loops through all pawns and generates white pawn moves, captures, and promotions
                while (tempWhitePawnBitboard != 0) {

                    // Finds the index of the first white pawn, then removes it from the temporary pawn bitboard
                    int pawnIndex = Constants.findFirstSet(tempWhitePawnBitboard);
                    tempWhitePawnBitboard &= (tempWhitePawnBitboard - 1);

                    //For pawns that are between the 2nd and 6th ranks, generate single pushes and captures
                    if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A6) {

                        // Passes a bitboard of possible pawn single moves to the generate move method (bitboard could be 0)
                        // Method reads bitboard of possible moves, encodes them, adds them to the list, and increments the index by 1
                        Bitboard possiblePawnSingleMoves = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        index = this.generatePawnMove(pawnIndex, possiblePawnSingleMoves, listOfAlmostLegalMoves, index, Constants.WHITE);

                        // Passes a bitboard of possible pawn captures to the generate move method (bitboard could be 0)
                        Bitboard possiblePawnCaptures = Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.BLACK]);
                        index = this.generatePawnCaptures(pawnIndex, possiblePawnCaptures, listOfAlmostLegalMoves, index, Constants.WHITE);
                    }
                    //For pawns that are on the 2nd rank, generate double pawn pushes
                    if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                        Bitboard singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                        Bitboard doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];
                        Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                        if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                            pseudoLegalDoubleMoveFromIndex = doublePawnMovementFromIndex;
                        }

                        index = this.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                    }
                    //If en passant is possible, For pawns that are on the 5th rank, generate en passant captures
                    if ((this.enPassantSquare & Constants.RANK_6) != 0) {
                        if (pawnIndex >= Constants.H5 && pawnIndex <= Constants.A5) {
                            Bitboard pseudoLegalEnPassantFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex] & this.enPassantSquare;
                            index = this.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                        }
                    }
                    //For pawns on the 7th rank, generate promotions and promotion captures
                    if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                        Bitboard pseudoLegalPromotionFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        index = this.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);

                        Bitboard pseudoLegalPromotionCaptureFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.BLACK]);
                        index = this.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                    }
                }
                //generates white knight moves and captures
                while (tempWhiteKnightBitboard != 0) {
                    int knightIndex = Constants.findFirstSet(tempWhiteKnightBitboard);
                    tempWhiteKnightBitboard &= (tempWhiteKnightBitboard - 1);
                    Bitboard pseudoLegalKnightMovementFromIndex = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[Constants.WHITE]);
                    index = this.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                }
                //generates white bishop moves and captures
                while (tempWhiteBishopBitboard != 0) {
                    int bishopIndex = Constants.findFirstSet(tempWhiteBishopBitboard);
                    tempWhiteBishopBitboard &= (tempWhiteBishopBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]));
                    index = this.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                }
                //generates white rook moves and captures
                while (tempWhiteRookBitboard != 0) {
                    int rookIndex = Constants.findFirstSet(tempWhiteRookBitboard);
                    tempWhiteRookBitboard &= (tempWhiteRookBitboard - 1);
                    Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]));
                    index = this.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                }
                //generates white queen moves and captures
                while (tempWhiteQueenBitboard != 0) {
                    int queenIndex = Constants.findFirstSet(tempWhiteQueenBitboard);
                    tempWhiteQueenBitboard &= (tempWhiteQueenBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]));
                    Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]));
                    Bitboard pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
                    index = this.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);
                }
                //generates white king moves and captures
                Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[Constants.WHITE]);
                index = this.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex, listOfAlmostLegalMoves, index, Constants.WHITE);

                //Generates white king castling moves (if the king is not in check)
                if ((this.whiteShortCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    int moveRepresentation = this.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.G1, Constants.SHORT_CASTLE, Constants.EMPTY);

                    if (this.timesSquareIsAttacked(Constants.WHITE, Constants.F1) == 0) {
                        listOfAlmostLegalMoves[index++] = moveRepresentation;
                    }
                }
                if ((this.whiteLongCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    int moveRepresentation = this.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.C1, Constants.LONG_CASTLE, Constants.EMPTY);

                    if (this.timesSquareIsAttacked(Constants.WHITE, Constants.D1) == 0) {
                        listOfAlmostLegalMoves[index++] = moveRepresentation;
                    }
                }


                return listOfAlmostLegalMoves;
            }

            else if (this.sideToMove == Constants.BLACK) {

                //Gets the indices of all of the pieces
                Bitboard tempBlackPawnBitboard = this.arrayOfBitboards[Constants.BLACK_PAWN];
                Bitboard tempBlackKnightBitboard = this.arrayOfBitboards[Constants.BLACK_KNIGHT];
                Bitboard tempBlackBishopBitboard = this.arrayOfBitboards[Constants.BLACK_BISHOP];
                Bitboard tempBlackRookBitboard = this.arrayOfBitboards[Constants.BLACK_ROOK];
                Bitboard tempBlackQueenBitboard = this.arrayOfBitboards[Constants.BLACK_QUEEN];
                Bitboard tempBlackKingBitboard = this.arrayOfBitboards[Constants.BLACK_KING];
                Bitboard tempAllPieceBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
                Bitboard tempWhiteRookAndQueenBitboard = (this.arrayOfBitboards[Constants.WHITE_ROOK] | this.arrayOfBitboards[Constants.WHITE_QUEEN]);
                Bitboard tempWhiteBishopAndQueenBitboard = (this.arrayOfBitboards[Constants.WHITE_BISHOP] | this.arrayOfBitboards[Constants.WHITE_QUEEN]);
                int kingIndex = Constants.findFirstSet(tempBlackKingBitboard);

                int[] listOfAlmostLegalMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;

                // Finds rook moves from the king, and intersects with black (own) pieces to get bitboard of potentially pinned pieces
                Bitboard potentiallyPinnedPiecesByRook = ((this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex)) & this.arrayOfAggregateBitboards[Constants.BLACK]);

                // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                // Intersect with white rook and queen to get bitboard of potential pinners
                Bitboard tempAllPieceExceptPotentiallyPinnedByRookBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByRook);
                Bitboard rookMovesFromIndexWithoutPinned = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, kingIndex);
                Bitboard potentialPinners = (rookMovesFromIndexWithoutPinned & tempWhiteRookAndQueenBitboard);

                // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                while (potentialPinners != 0) {
                    int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);

                    // Removes the potential pinner from the bitboard
                    potentialPinners &= (potentialPinners - 1);

                    Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                    Bitboard rookMovesFromPinnerIndex = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, indexOfPotentialPinner);

                    // If intersection with potentially pinned pieces is not zero, then piece is pinned
                    // Generates pin ray
                    Bitboard pinnedPiece = (rookMovesFromPinnerIndex & potentiallyPinnedPiecesByRook);
                    if (pinnedPiece != 0) {
                        Bitboard pinRay = (rookMovesFromIndexWithoutPinned & (rookMovesFromPinnerIndex | pinner));

                        int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                        int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                        // If the pinned piece is a black pawn, then generate single and double pushes along the pin ray

                        if (pinnedPieceType == Constants.BLACK_PAWN) {

                            //For pawns that are between the 3rd and 7th ranks, generate single pushes
                            if (indexOfPinnedPiece >= Constants.H3 && indexOfPinnedPiece <= Constants.A7) {
                                //Generates black pawn single moves
                                Bitboard possiblePawnSingleMoves = (Constants.blackSinglePawnMovesAndPromotionMoves[indexOfPinnedPiece] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & pinRay);
                                index = this.generatePawnMove(indexOfPinnedPiece, possiblePawnSingleMoves, listOfAlmostLegalMoves, index, Constants.BLACK);
                            }
                            //For pawns that are on the 7th rank, generate double pawn pushes
                            if (indexOfPinnedPiece >= Constants.H7 && indexOfPinnedPiece <= Constants.A7) {
                                Bitboard singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[indexOfPinnedPiece];
                                Bitboard doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[indexOfPinnedPiece - 8];
                                Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                                if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                    pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & pinRay);
                                }

                                index = this.generatePawnDoubleMove(indexOfPinnedPiece, pseudoLegalDoubleMoveFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                            }
                            // Removes the black pawn from the list of white pawns
                            tempBlackPawnBitboard &= (~pinnedPiece);
                        }
                            // If the pinned piece is a black rook, then generate moves along the pin ray
                        else if (pinnedPieceType == Constants.BLACK_ROOK) {
                            Bitboard legalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & (pinRay));
                            index = this.generateRookMoves(indexOfPinnedPiece, legalRookMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                            // Removes the black rook from the list of white rooks
                            tempBlackRookBitboard &= (~pinnedPiece);
                        }
                            // If the pinned piece is a black queen, then generate moves along the pin ray (only rook moves)
                        else if (pinnedPieceType == Constants.BLACK_QUEEN) {
                            Bitboard legalQueenMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & (pinRay));
                            index = this.generateQueenMoves(indexOfPinnedPiece, legalQueenMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                            // Removes the black queen from the list of white queens
                            tempBlackQueenBitboard &= (~pinnedPiece);
                        }
                            // If pinned piece type is a black knight, then it isn't allowed to move
                        else if (pinnedPieceType == Constants.BLACK_KNIGHT) {
                            // Remove it from the knight list so that no night moves will be generated later on
                            tempBlackKnightBitboard &= (~pinnedPiece);
                        }
                            // If pinned piece type is a black bishop, then it isn't allowed to move
                        else if (pinnedPieceType == Constants.BLACK_BISHOP) {
                            // Remove it from the bishop list so that no bishop moves will be generated later on
                            tempBlackBishopBitboard &= (~pinnedPiece);
                        }
                        // Note that pawn captures, en-passant captures, promotions, promotion-captures, knight moves, and bishop moves will all be illegal
                    }

                }
                // Finds bishop moves from the king, and intersects with black (own) pieces to get bitboard of potentially pinned pieces
                Bitboard potentiallyPinnedPiecesByBishop = (this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex) & this.arrayOfAggregateBitboards[Constants.BLACK]);

                // Removes potentially pinned pieces from the all pieces bitboard, and generates bishop moves from king again
                // Intersect with white bishop and queen to get bitboard of potential pinners
                Bitboard tempAllPieceExceptPotentiallyPinnedByBishopBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByBishop);
                Bitboard bishopMovesFromIndexWithoutPinned = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, kingIndex);
                potentialPinners = (bishopMovesFromIndexWithoutPinned & (tempWhiteBishopAndQueenBitboard));

                // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                while (potentialPinners != 0) {
                    int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);
                    // Removes the potential pinner from the black rook and queen bitboard
                    potentialPinners &= (potentialPinners - 1);
                    Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                    Bitboard bishopMovesFromPinnerIndex = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, indexOfPotentialPinner);

                    // If intersection with potentially pinned pieces is not zero, then piece is pinned
                    // Generates pin ray
                    Bitboard pinnedPiece = (bishopMovesFromPinnerIndex & potentiallyPinnedPiecesByBishop);
                    if (pinnedPiece != 0) {
                        Bitboard pinRay = (bishopMovesFromIndexWithoutPinned & (bishopMovesFromPinnerIndex | pinner));

                        int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                        int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                        // If the pinned piece is a black pawn, then generate captures, en passant captures, and capture promotions
                        if (pinnedPieceType == Constants.BLACK_PAWN) {

                            //For pawns that are between the 3rd and 7th ranks, generate captures
                            if (indexOfPinnedPiece >= Constants.H3 && indexOfPinnedPiece <= Constants.A7) {

                                //Generates black pawn captures (will be a maximum of 1 along the pin ray)
                                Bitboard legalPawnCapturesFromIndex = (Constants.blackCapturesAndCapturePromotions[indexOfPinnedPiece] & this.arrayOfAggregateBitboards[Constants.WHITE] & pinRay);
                                index = this.generatePawnCaptures(indexOfPinnedPiece, legalPawnCapturesFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                            }
                            //For pawns that are on the 4th rank, generate en passant captures
                            if ((this.enPassantSquare & Constants.RANK_3) != 0) {
                                if (indexOfPinnedPiece >= Constants.H4 && indexOfPinnedPiece <= Constants.A4) {
                                    Bitboard legalEnPassantFromIndex = (Constants.blackCapturesAndCapturePromotions[indexOfPinnedPiece] & this.enPassantSquare & pinRay);
                                    index = this.generatePawnEnPassant(indexOfPinnedPiece, legalEnPassantFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                                }
                            }
                            //For pawns on the 2nd rank, generate promotion captures
                            if (indexOfPinnedPiece >= Constants.H2 && indexOfPinnedPiece <= Constants.A2) {

                                //Generates black pawn capture promotions
                                Bitboard legalPawnCapturesFromIndex = (Constants.blackCapturesAndCapturePromotions[indexOfPinnedPiece] & this.arrayOfAggregateBitboards[Constants.WHITE] & pinRay);
                                index = this.generatePawnPromotionCapture(indexOfPinnedPiece, legalPawnCapturesFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                            }
                            // Removes the black pawn from the list of white pawns
                            tempBlackPawnBitboard &= (~pinnedPiece);
                        }
                            // If the pinned piece is a black bishop, then generate moves along the pin ray
                        else if (pinnedPieceType == Constants.BLACK_BISHOP) {

                            Bitboard legalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & (pinRay));
                            index = this.generateBishopMoves(indexOfPinnedPiece, legalBishopMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                            // Removes the white bishop from the list of white rooks
                            tempBlackBishopBitboard &= (~pinnedPiece);
                        }
                            // If the pinned piece is a black queen, then generate moves along the pin ray
                        else if (pinnedPieceType == Constants.BLACK_QUEEN) {

                            Bitboard legalQueenMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & (pinRay));
                            index = this.generateQueenMoves(indexOfPinnedPiece, legalQueenMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                            // Removes the blackqueen from the list of white queens
                            tempBlackQueenBitboard &= (~pinnedPiece);
                        }
                            // If pinned piece type is a black knight, then it isn't allowed to move
                       else if (pinnedPieceType == Constants.BLACK_KNIGHT) {
                            // Remove it from the knight list so that no night moves will be generated later on
                            tempBlackKnightBitboard &= (~pinnedPiece);
                        }
                            // If pinned piece type is a black rook, then it isn't allowed to move
                        else if (pinnedPieceType == Constants.BLACK_ROOK) {
                            // Remove it from the bishop list so that no rook moves will be generated later on
                            tempBlackRookBitboard &= (~pinnedPiece);
                        }
                        // Note that single pawn pushes, double pawn pushes, promotions, promotion-captures, knight moves, and rook moves will all be illegal
                    }

                }
                //Generates black pawn moves
                while (tempBlackPawnBitboard != 0) {
                    int pawnIndex = Constants.findFirstSet(tempBlackPawnBitboard);
                    tempBlackPawnBitboard &= (tempBlackPawnBitboard - 1);

                    if (pawnIndex >= Constants.H3 && pawnIndex <= Constants.A7) {

                        //Generates black pawn single moves
                        Bitboard pseudoLegalSinglePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        index = this.generatePawnMove(pawnIndex, pseudoLegalSinglePawnMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                        Bitboard pseudoLegalPawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.WHITE]);
                        index = this.generatePawnCaptures(pawnIndex, pseudoLegalPawnCapturesFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                    }

                    //Generates black pawn double moves
                    if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                        Bitboard singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                        Bitboard doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];
                        Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                        if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                            pseudoLegalDoubleMoveFromIndex = doublePawnMovementFromIndex;
                        }
                        index = this.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                    }

                    //Generates black pawn en passant captures
                    if ((this.enPassantSquare & Constants.RANK_3) != 0) {
                        if (pawnIndex >= Constants.H4 && pawnIndex <= Constants.A4) {
                            Bitboard pseudoLegalEnPassantFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex] & this.enPassantSquare;
                            index = this.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                        }
                    }
                    if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                        Bitboard pseudoLegalPromotionFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        index = this.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                        Bitboard pseudoLegalPromotionCaptureFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.WHITE]);
                        index = this.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                    }
                }

                //Generates black knight moves and captures
                while (tempBlackKnightBitboard != 0) {

                    int knightIndex = Constants.findFirstSet(tempBlackKnightBitboard);
                    tempBlackKnightBitboard &= (tempBlackKnightBitboard - 1);
                    Bitboard pseudoLegalKnightMovementFromIndex = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[Constants.BLACK]);
                    index = this.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                }

                //generates black bishop moves and captures
                while (tempBlackBishopBitboard != 0) {

                    int bishopIndex = Constants.findFirstSet(tempBlackBishopBitboard);
                    tempBlackBishopBitboard &= (tempBlackBishopBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]));
                    index = this.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                }
                //generates black rook moves and captures
                while (tempBlackRookBitboard != 0) {

                    int rookIndex = Constants.findFirstSet(tempBlackRookBitboard);
                    tempBlackRookBitboard &= (tempBlackRookBitboard - 1);
                    Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]));
                    index = this.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                }
                //generates black queen moves and captures
                while (tempBlackQueenBitboard != 0) {

                    int queenIndex = Constants.findFirstSet(tempBlackQueenBitboard);
                    tempBlackQueenBitboard &= (tempBlackQueenBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]));
                    Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]));
                    Bitboard pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
                    index = this.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);
                }

                //generates black king moves and captures
                Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[Constants.BLACK]);
                index = this.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex, listOfAlmostLegalMoves, index, Constants.BLACK);

                //Generates black king castling moves (if the king is not in check)
                if ((this.blackShortCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    int moveRepresentation = this.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.G8, Constants.SHORT_CASTLE, Constants.EMPTY);

                    if (this.timesSquareIsAttacked(Constants.BLACK, Constants.F8) == 0) {
                        listOfAlmostLegalMoves[index++] = moveRepresentation;
                    }
                }

                if ((this.blackLongCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    int moveRepresentation = this.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.C8, Constants.LONG_CASTLE, Constants.EMPTY);

                    if (this.timesSquareIsAttacked(Constants.BLACK, Constants.D8) == 0) {
                        listOfAlmostLegalMoves[index++] = moveRepresentation;
                    }
                }
                    
                
                //returns the list of legal moves
                return listOfAlmostLegalMoves;

            }

            return null;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        // CHECK EVASION MOVE GENERATOR
        // Only called when the king (of the player whose turn it is) is in check or double check
        // For single check, generates king moves, captures, and interpositions 
        // No moves are generated for pieces in absolute pins, so only king moves and en passants have to be checked for legality

        // For double check, only generates king moves
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
       
        
        public int[] checkEvasionGenerator() {

            if (this.sideToMove == Constants.WHITE) {
                // Gets temporary piece bitboards (they will be modified by removing the LSB until they equal 0, so can't use the actual piece bitboards)
                Bitboard tempWhitePawnBitboard = this.arrayOfBitboards[Constants.WHITE_PAWN];
                Bitboard tempWhiteKnightBitboard = this.arrayOfBitboards[Constants.WHITE_KNIGHT];
                Bitboard tempWhiteBishopBitboard = this.arrayOfBitboards[Constants.WHITE_BISHOP];
                Bitboard tempWhiteRookBitboard = this.arrayOfBitboards[Constants.WHITE_ROOK];
                Bitboard tempWhiteQueenBitboard = this.arrayOfBitboards[Constants.WHITE_QUEEN];
                Bitboard tempWhiteKingBitboard = this.arrayOfBitboards[Constants.WHITE_KING];
                Bitboard tempAllPieceBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
                Bitboard tempBlackRookAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_ROOK] | this.arrayOfBitboards[Constants.BLACK_QUEEN]);
                Bitboard tempBlackBishopAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_BISHOP] | this.arrayOfBitboards[Constants.BLACK_QUEEN]);

                int kingIndex = Constants.findFirstSet(tempWhiteKingBitboard);
                Bitboard bishopMovesFromKingPosition = this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex);
                Bitboard rookMovesFromKingPosition = this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex);

                // Initializes the list of pseudo legal moves (can only hold 220 moves), and initializes the array's index (so we know which element to add the move to)
                int[] listOfCheckEvasionMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;
                

                // Checks to see whether the king is in check or double check in the current position
                int kingCheckStatus = this.timesSquareIsAttacked(Constants.WHITE, kingIndex);

                // If the king is in check
                if (kingCheckStatus == Constants.CHECK) {

                    // Finds rook moves from the king, and intersects with white (own) pieces to get bitboard of potentially pinned pieces
                    Bitboard potentiallyPinnedPiecesByRook = ((this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex)) & this.arrayOfAggregateBitboards[Constants.WHITE]);

                    // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                    // Intersect with black rook and queen to get bitboard of potential pinners
                    Bitboard tempAllPieceExceptPotentiallyPinnedByRookBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByRook);
                    Bitboard rookMovesFromIndexWithoutPinned = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, kingIndex);
                    Bitboard potentialPinners = (rookMovesFromIndexWithoutPinned & tempBlackRookAndQueenBitboard);

                    // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                    while (potentialPinners != 0) {
                        int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);

                        // Removes the potential pinner from the bitboard
                        potentialPinners &= (potentialPinners - 1);

                        Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                        Bitboard rookMovesFromPinnerIndex = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, indexOfPotentialPinner);

                        // If intersection with potentially pinned pieces is not zero, then piece is pinned
                        // Generates pin ray
                        Bitboard pinnedPiece = (rookMovesFromPinnerIndex & potentiallyPinnedPiecesByRook);
                        if (pinnedPiece != 0) {
                            Bitboard pinRay = (rookMovesFromIndexWithoutPinned & (rookMovesFromPinnerIndex | pinner));

                            int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                            int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                            // If the pinned piece is a white pawn, then generate single and double pushes along the pin ray

                            if (pinnedPieceType == Constants.WHITE_PAWN) {
                                // Removes the white pawn from the list of white pawns
                                tempWhitePawnBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a white rook, then generate moves along the pin ray
                            else if (pinnedPieceType == Constants.WHITE_ROOK) {
                                // Removes the white rook from the list of white rooks
                                tempWhiteRookBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a white queen, then generate moves along the pin ray (only rook moves)
                            else if (pinnedPieceType == Constants.WHITE_QUEEN) {
                                // Removes the white queen from the list of white queens
                                tempWhiteQueenBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a white knight, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.WHITE_KNIGHT) {
                                // Remove it from the knight list so that no night moves will be generated later on
                                tempWhiteKnightBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a white bishop, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.WHITE_BISHOP) {
                                // Remove it from the bishop list so that no bishop moves will be generated later on
                                tempWhiteBishopBitboard &= (~pinnedPiece);
                            }
                            // Note that pawn captures, en-passant captures, promotions, promotion-captures, knight moves, and bishop moves will all be illegal
                        }

                    }
                    // Finds bishop moves from the king, and intersects with white (own) pieces to get bitboard of potentially pinned pieces
                    Bitboard potentiallyPinnedPiecesByBishop = (this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex) & this.arrayOfAggregateBitboards[Constants.WHITE]);

                    // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                    // Intersect with black rook and queen to get bitboard of potential pinners
                    Bitboard tempAllPieceExceptPotentiallyPinnedByBishopBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByBishop);
                    Bitboard bishopMovesFromIndexWithoutPinned = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, kingIndex);
                    potentialPinners = (bishopMovesFromIndexWithoutPinned & (tempBlackBishopAndQueenBitboard));

                    // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                    while (potentialPinners != 0) {
                        int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);
                        // Removes the potential pinner from the black rook and queen bitboard
                        potentialPinners &= (potentialPinners - 1);
                        Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                        Bitboard bishopMovesFromPinnerIndex = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, indexOfPotentialPinner);

                        // If intersection with potentially pinned pieces is not zero, then piece is pinned
                        // Generates pin ray
                        Bitboard pinnedPiece = (bishopMovesFromPinnerIndex & potentiallyPinnedPiecesByBishop);
                        if (pinnedPiece != 0) {
                            Bitboard pinRay = (bishopMovesFromIndexWithoutPinned & (bishopMovesFromPinnerIndex | pinner));

                            int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                            int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                            // If the pinned piece is a white pawn, then generate captures, en passant captures, and capture promotions
                            if (pinnedPieceType == Constants.WHITE_PAWN) {

                                // Removes the white pawn from the list of white pawns
                                tempWhitePawnBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a white bishop, then generate moves along the pin ray
                            else if (pinnedPieceType == Constants.WHITE_BISHOP) {

                                // Removes the white bishop from the list of white rooks
                                tempWhiteBishopBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a white queen, then generate moves along the pin ray
                            else if (pinnedPieceType == Constants.WHITE_QUEEN) {

                                // Removes the white queen from the list of white queens
                                tempWhiteQueenBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a white knight, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.WHITE_KNIGHT) {
                                // Remove it from the knight list so that no night moves will be generated later on
                                tempWhiteKnightBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a white rook, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.WHITE_ROOK) {
                                // Remove it from the bishop list so that no bishop moves will be generated later on
                                tempWhiteRookBitboard &= (~pinnedPiece);
                            }
                            // Note that single pawn pushes, double pawn pushes, promotions, promotion-captures, knight moves, and rook moves will all be illegal
                        }

                    }

                    //Calculates the squares that pieces can move to in order to capture or block the checking piece
                    Bitboard checkingPieceBitboard = this.getBitboardOfAttackers(Constants.WHITE, kingIndex);
                    int indexOfCheckingPiece = Constants.findFirstSet(checkingPieceBitboard);
                    Bitboard blockOrCaptureSquares = 0x0UL;

                    switch (pieceArray[indexOfCheckingPiece]) {
                            // If the checking piece is a black pawn, then can only capture (no interpositions)
                        case Constants.BLACK_PAWN: 
                            blockOrCaptureSquares = checkingPieceBitboard; break;
                            // If the checking piece is a black knight, then can only capture (no interpositions)
                        case Constants.BLACK_KNIGHT:
                            blockOrCaptureSquares = checkingPieceBitboard; break;
                            // If the checking piece is a black bishop, then can capture or interpose
                        case Constants.BLACK_BISHOP:
                            Bitboard bishopMovesFromChecker = this.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard)); break;
                            // If the checking piece is a black rook, then can capture or interpose
                        case Constants.BLACK_ROOK:
                            Bitboard rookMovesFromChecker = this.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard)); break;
                            // If the checking piece is a black queen, then can capture or interpose
                        case Constants.BLACK_QUEEN:
                            if ((bishopMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                bishopMovesFromChecker = this.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                                blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard));
                            } else if ((rookMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                rookMovesFromChecker = this.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                                blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard));
                            }
                            break;
                    }

                    // Generates moves as normal, but the piece's move squares are intersected with the block or capture squares
                    // Loops through all pawns and generates white pawn moves, captures, and promotions
                    while (tempWhitePawnBitboard != 0) {

                        int pawnIndex = Constants.findFirstSet(tempWhitePawnBitboard);
                        tempWhitePawnBitboard &= (tempWhitePawnBitboard - 1);

                        if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A6) {

                            Bitboard possiblePawnSingleMoves = (Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = this.generatePawnMove(pawnIndex, possiblePawnSingleMoves,listOfCheckEvasionMoves, index, Constants.WHITE);

                            Bitboard possiblePawnCaptures = (Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                            index = this.generatePawnCaptures(pawnIndex, possiblePawnCaptures,listOfCheckEvasionMoves, index, Constants.WHITE);
                        }
                        if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                            Bitboard singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                            Bitboard doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];
                            Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                            if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & blockOrCaptureSquares);
                            }

                            index = this.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                        }
                        if ((this.enPassantSquare & Constants.RANK_6) != 0) {
                            if (pawnIndex >= Constants.H5 && pawnIndex <= Constants.A5) {
                                Bitboard pseudoLegalEnPassantFromIndex = (Constants.whiteCapturesAndCapturePromotions[pawnIndex] & this.enPassantSquare);
                                index = this.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                            }
                        }
                        if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                            Bitboard pseudoLegalPromotionFromIndex = (Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = this.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);

                            Bitboard pseudoLegalPromotionCaptureFromIndex = (Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                            index = this.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                        }
                    }

                    //generates white knight moves and captures 
                    while (tempWhiteKnightBitboard != 0) {
                        int knightIndex = Constants.findFirstSet(tempWhiteKnightBitboard);
                        tempWhiteKnightBitboard &= (tempWhiteKnightBitboard - 1);
                        Bitboard pseudoLegalKnightMovementFromIndex = (Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = this.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white bishop moves and captures
                    while (tempWhiteBishopBitboard != 0) {
                        int bishopIndex = Constants.findFirstSet(tempWhiteBishopBitboard);
                        tempWhiteBishopBitboard &= (tempWhiteBishopBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = this.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white rook moves and captures
                    while (tempWhiteRookBitboard != 0) {
                        int rookIndex = Constants.findFirstSet(tempWhiteRookBitboard);
                        tempWhiteRookBitboard &= (tempWhiteRookBitboard - 1);
                        Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = this.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white queen moves and captures
                    while (tempWhiteQueenBitboard != 0) {
                        int queenIndex = Constants.findFirstSet(tempWhiteQueenBitboard);
                        tempWhiteQueenBitboard &= (tempWhiteQueenBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalQueenMovementFromIndex = ((pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & (~this.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = this.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white king moves and captures
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[Constants.WHITE]);
                    index = this.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);

                    //returns the list of legal moves
                    return listOfCheckEvasionMoves;

                }

                    // If the king is in double check
                else if (kingCheckStatus == Constants.DOUBLE_CHECK) {

                    // Only generates king moves
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[Constants.WHITE]);
                    index = this.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);

                    return listOfCheckEvasionMoves;
                }
            } 
                // If side to move is black
            else if (this.sideToMove == Constants.BLACK) {
                
                // Gets temporary piece bitboards (they will be modified by removing the LSB until they equal 0, so can't use the actual piece bitboards)
                Bitboard tempBlackPawnBitboard = this.arrayOfBitboards[Constants.BLACK_PAWN];
                Bitboard tempBlackKnightBitboard = this.arrayOfBitboards[Constants.BLACK_KNIGHT];
                Bitboard tempBlackBishopBitboard = this.arrayOfBitboards[Constants.BLACK_BISHOP];
                Bitboard tempBlackRookBitboard = this.arrayOfBitboards[Constants.BLACK_ROOK];
                Bitboard tempBlackQueenBitboard = this.arrayOfBitboards[Constants.BLACK_QUEEN];
                Bitboard tempBlackKingBitboard = this.arrayOfBitboards[Constants.BLACK_KING];
                Bitboard tempAllPieceBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
                Bitboard tempWhiteRookAndQueenBitboard = (this.arrayOfBitboards[Constants.WHITE_ROOK] | this.arrayOfBitboards[Constants.WHITE_QUEEN]);
                Bitboard tempWhiteBishopAndQueenBitboard = (this.arrayOfBitboards[Constants.WHITE_BISHOP] | this.arrayOfBitboards[Constants.WHITE_QUEEN]);

                int kingIndex = Constants.findFirstSet(tempBlackKingBitboard);
                Bitboard bishopMovesFromKingPosition = this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex);
                Bitboard rookMovesFromKingPosition = this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex);

                // Initializes the list of pseudo legal moves (can only hold 220 moves), and initializes the array's index (so we know which element to add the move to)
                int[]listOfCheckEvasionMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;

                // Checks to see whether the king is in check or double check in the current position
                int kingCheckStatus = this.timesSquareIsAttacked(Constants.BLACK, kingIndex);

                // If the king is in check
                if (kingCheckStatus == Constants.CHECK) {

                    // Finds rook moves from the king, and intersects with black (own) pieces to get bitboard of potentially pinned pieces
                    Bitboard potentiallyPinnedPiecesByRook = ((this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex)) & this.arrayOfAggregateBitboards[Constants.BLACK]);

                    // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                    // Intersect with white rook and queen to get bitboard of potential pinners
                    Bitboard tempAllPieceExceptPotentiallyPinnedByRookBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByRook);
                    Bitboard rookMovesFromIndexWithoutPinned = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, kingIndex);
                    Bitboard potentialPinners = (rookMovesFromIndexWithoutPinned & tempWhiteRookAndQueenBitboard);

                    // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                    while (potentialPinners != 0) {
                        int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);

                        // Removes the potential pinner from the bitboard
                        potentialPinners &= (potentialPinners - 1);

                        Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                        Bitboard rookMovesFromPinnerIndex = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, indexOfPotentialPinner);

                        // If intersection with potentially pinned pieces is not zero, then piece is pinned
                        // Generates pin ray
                        Bitboard pinnedPiece = (rookMovesFromPinnerIndex & potentiallyPinnedPiecesByRook);
                        if (pinnedPiece != 0) {
                            Bitboard pinRay = (rookMovesFromIndexWithoutPinned & (rookMovesFromPinnerIndex | pinner));

                            int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                            int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                            // If the pinned piece is a black pawn, then generate single and double pushes along the pin ray

                            if (pinnedPieceType == Constants.BLACK_PAWN) {

                                // Removes the black pawn from the list of white pawns
                                tempBlackPawnBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a black rook, then generate moves along the pin ray
                            else if (pinnedPieceType == Constants.BLACK_ROOK) {
                                // Removes the black rook from the list of white rooks
                                tempBlackRookBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a black queen, then generate moves along the pin ray (only rook moves)
                            else if (pinnedPieceType == Constants.BLACK_QUEEN) {
                               
                                // Removes the black queen from the list of white queens
                                tempBlackQueenBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a black knight, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.BLACK_KNIGHT) {
                                // Remove it from the knight list so that no night moves will be generated later on
                                tempBlackKnightBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a black bishop, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.BLACK_BISHOP) {
                                // Remove it from the bishop list so that no bishop moves will be generated later on
                                tempBlackBishopBitboard &= (~pinnedPiece);
                            }
                            // Note that pawn captures, en-passant captures, promotions, promotion-captures, knight moves, and bishop moves will all be illegal
                        }

                    }
                    // Finds bishop moves from the king, and intersects with black (own) pieces to get bitboard of potentially pinned pieces
                    Bitboard potentiallyPinnedPiecesByBishop = (this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex) & this.arrayOfAggregateBitboards[Constants.BLACK]);

                    // Removes potentially pinned pieces from the all pieces bitboard, and generates bishop moves from king again
                    // Intersect with white bishop and queen to get bitboard of potential pinners
                    Bitboard tempAllPieceExceptPotentiallyPinnedByBishopBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByBishop);
                    Bitboard bishopMovesFromIndexWithoutPinned = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, kingIndex);
                    potentialPinners = (bishopMovesFromIndexWithoutPinned & (tempWhiteBishopAndQueenBitboard));

                    // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                    while (potentialPinners != 0) {
                        int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);
                        // Removes the potential pinner from the black rook and queen bitboard
                        potentialPinners &= (potentialPinners - 1);
                        Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                        Bitboard bishopMovesFromPinnerIndex = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, indexOfPotentialPinner);

                        // If intersection with potentially pinned pieces is not zero, then piece is pinned
                        // Generates pin ray
                        Bitboard pinnedPiece = (bishopMovesFromPinnerIndex & potentiallyPinnedPiecesByBishop);
                        if (pinnedPiece != 0) {
                            Bitboard pinRay = (bishopMovesFromIndexWithoutPinned & (bishopMovesFromPinnerIndex | pinner));

                            int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                            int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                            // If the pinned piece is a black pawn, then generate captures, en passant captures, and capture promotions
                            if (pinnedPieceType == Constants.BLACK_PAWN) {
                                
                                // Removes the black pawn from the list of white pawns
                                tempBlackPawnBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a black bishop, then generate moves along the pin ray
                            else if (pinnedPieceType == Constants.BLACK_BISHOP) {

                              
                                // Removes the white bishop from the list of white rooks
                                tempBlackBishopBitboard &= (~pinnedPiece);
                            }
                                // If the pinned piece is a black queen, then generate moves along the pin ray
                            else if (pinnedPieceType == Constants.BLACK_QUEEN) {

                               
                                // Removes the blackqueen from the list of white queens
                                tempBlackQueenBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a black knight, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.BLACK_KNIGHT) {
                                // Remove it from the knight list so that no night moves will be generated later on
                                tempBlackKnightBitboard &= (~pinnedPiece);
                            }
                                // If pinned piece type is a black rook, then it isn't allowed to move
                            else if (pinnedPieceType == Constants.BLACK_ROOK) {
                                // Remove it from the bishop list so that no rook moves will be generated later on
                                tempBlackRookBitboard &= (~pinnedPiece);
                            }
                            // Note that single pawn pushes, double pawn pushes, promotions, promotion-captures, knight moves, and rook moves will all be illegal
                        }

                    }

                    //Calculates the squares that pieces can move to in order to capture or block the checking piece
                    Bitboard checkingPieceBitboard = this.getBitboardOfAttackers(Constants.BLACK, kingIndex);
                    int indexOfCheckingPiece = Constants.findFirstSet(checkingPieceBitboard);
                    Bitboard blockOrCaptureSquares = 0x0UL;

                    switch (pieceArray[indexOfCheckingPiece]) {
                            // If the checking piece is a white pawn, then can only capture (no interpositions)
                        case Constants.WHITE_PAWN:
                            blockOrCaptureSquares = checkingPieceBitboard; break;
                            // If the checking piece is a white knight, then can only capture (no interpositions)
                        case Constants.WHITE_KNIGHT:
                            blockOrCaptureSquares = checkingPieceBitboard; break;
                            // If the checking piece is a white bishop, then can capture or interpose
                        case Constants.WHITE_BISHOP:
                            Bitboard bishopMovesFromChecker = this.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard)); break;
                            // If the checking piece is a white rook, then can capture or interpose
                        case Constants.WHITE_ROOK:
                            Bitboard rookMovesFromChecker = this.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard)); break;
                            // If the checking piece is a white queen, then can capture or interpose
                        case Constants.WHITE_QUEEN:
                            if ((bishopMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                bishopMovesFromChecker = this.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                                blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard));
                            } else if ((rookMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                rookMovesFromChecker = this.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                                blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard));
                            }
                            break;
                    }

                    // Generates moves as normal, but the piece's move squares are intersected with the block or capture squares
                    // Loops through all pawns and generates white pawn moves, captures, and promotions
                    while (tempBlackPawnBitboard != 0) {

                        int pawnIndex = Constants.findFirstSet(tempBlackPawnBitboard);
                        tempBlackPawnBitboard &= (tempBlackPawnBitboard - 1);

                        if (pawnIndex >= Constants.H3 && pawnIndex <= Constants.A7) {

                            Bitboard possiblePawnSingleMoves = (Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = this.generatePawnMove(pawnIndex, possiblePawnSingleMoves,listOfCheckEvasionMoves, index, Constants.BLACK);

                            Bitboard possiblePawnCaptures = (Constants.blackCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                            index = this.generatePawnCaptures(pawnIndex, possiblePawnCaptures,listOfCheckEvasionMoves, index, Constants.BLACK);
                        }
                        if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                            Bitboard singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                            Bitboard doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];
                            Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                            if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & blockOrCaptureSquares);
                            }

                            index = this.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                        }
                        if ((this.enPassantSquare & Constants.RANK_3) != 0) {
                            if (pawnIndex >= Constants.H4 && pawnIndex <= Constants.A4) {
                                Bitboard pseudoLegalEnPassantFromIndex = (Constants.blackCapturesAndCapturePromotions[pawnIndex] & this.enPassantSquare);
                                index = this.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                            }
                        }
                        if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                            Bitboard pseudoLegalPromotionFromIndex = (Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = this.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);

                            Bitboard pseudoLegalPromotionCaptureFromIndex = (Constants.blackCapturesAndCapturePromotions[pawnIndex] & (this.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                            index = this.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                        }
                    }

                    //generates black knight moves and captures
                    while (tempBlackKnightBitboard != 0) {
                        int knightIndex = Constants.findFirstSet(tempBlackKnightBitboard);
                        tempBlackKnightBitboard &= (tempBlackKnightBitboard - 1);
                        Bitboard pseudoLegalKnightMovementFromIndex = (Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = this.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black bishop moves and captures
                    while (tempBlackBishopBitboard != 0) {
                        int bishopIndex = Constants.findFirstSet(tempBlackBishopBitboard);
                        tempBlackBishopBitboard &= (tempBlackBishopBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = this.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black rook moves and captures
                    while (tempBlackRookBitboard != 0) {
                        int rookIndex = Constants.findFirstSet(tempBlackRookBitboard);
                        tempBlackRookBitboard &= (tempBlackRookBitboard - 1);
                        Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = this.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black queen moves and captures
                    while (tempBlackQueenBitboard != 0) {
                        int queenIndex = Constants.findFirstSet(tempBlackQueenBitboard);
                        tempBlackQueenBitboard &= (tempBlackQueenBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalQueenMovementFromIndex = ((pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & (~this.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = this.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black king moves and captures
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[Constants.BLACK]);
                    index = this.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);

                    //returns the list of legal moves
                    return listOfCheckEvasionMoves;

                }

                    // If the king is in double check
                else if (kingCheckStatus == Constants.DOUBLE_CHECK) {

                    // Only generates king moves
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[Constants.BLACK]);
                    index = this.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);

                    return listOfCheckEvasionMoves;
                }
            }
            return null;
        }

		// Takes in the index of the pawn and the bitboard of all pieces, and generates single pawn pushes
        private int generatePawnMove(int pawnIndex, Bitboard pseudoLegalSinglePawnMoveFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            if (pseudoLegalSinglePawnMoveFromIndex != 0) {
                int indexOfWhitePawnSingleMoveFromIndex = Constants.findFirstSet(pseudoLegalSinglePawnMoveFromIndex);
                int moveRepresentation = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        
        private int generatePawnCaptures(int pawnIndex, Bitboard pseudoLegalPawnCapturesFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalPawnCapturesFromIndex != 0) {

                int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPawnCapturesFromIndex);
                pseudoLegalPawnCapturesFromIndex &= (pseudoLegalPawnCapturesFromIndex - 1);

                int moveRepresentation = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArray[pawnMoveIndex]);
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generatePawnDoubleMove(int pawnIndex, Bitboard pseudoLegalDoubleMoveFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            if (pseudoLegalDoubleMoveFromIndex != 0) {
                int indexOfWhitePawnDoubleMoveFromIndex = Constants.findFirstSet(pseudoLegalDoubleMoveFromIndex);
                int moveRepresentation = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnDoubleMoveFromIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generatePawnEnPassant(int pawnIndex, Bitboard pseudoLegalEnPassantFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            if (pseudoLegalEnPassantFromIndex != 0) {
                int indexOfWhiteEnPassantCaptureFromIndex = Constants.findFirstSet(pseudoLegalEnPassantFromIndex);
                int moveRepresentation = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhiteEnPassantCaptureFromIndex, Constants.EN_PASSANT_CAPTURE, (Constants.PAWN + 6 - 6 * pieceColour));
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generatePawnPromotion(int pawnIndex, Bitboard pseudoLegalPromotionFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            //Generates white pawn promotions
            if (pseudoLegalPromotionFromIndex != 0) {
                int indexOfWhitePawnSingleMoveFromIndex = Constants.findFirstSet(pseudoLegalPromotionFromIndex);
                int moveRepresentationKnightPromotion = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.KNIGHT + 6 * pieceColour));
                int moveRepresentationBishopPromotion = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.BISHOP + 6 * pieceColour));
                int moveRepresentationRookPromotion = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.ROOK + 6 * pieceColour));
                int moveRepresentationQueenPromotion = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.QUEEN + 6 * pieceColour));

                listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotion;
                listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotion;
                listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotion;
                listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotion;
            }
            return index;
        }

        private int generatePawnPromotionCapture(int pawnIndex, Bitboard pseudoLegalPromotionCaptureFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalPromotionCaptureFromIndex != 0) {

                int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPromotionCaptureFromIndex);
                pseudoLegalPromotionCaptureFromIndex &= (pseudoLegalPromotionCaptureFromIndex - 1);

                int moveRepresentationKnightPromotionCapture = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.KNIGHT + 6 * pieceColour));
                int moveRepresentationBishopPromotionCapture = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.BISHOP + 6 * pieceColour));
                int moveRepresentationRookPromotionCapture = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.ROOK + 6 * pieceColour));
                int moveRepresentationQueenPromotionCapture = this.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.QUEEN + 6 * pieceColour));

                listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotionCapture;
                listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotionCapture;
                listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotionCapture;
                listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotionCapture;
                
            }
            return index;
        }

        private int generateKnightMoves(int knightIndex, Bitboard pseudoLegalKnightMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColor) {
            
            while (pseudoLegalKnightMovementFromIndex != 0) {

                int knightMoveIndex = Constants.findFirstSet(pseudoLegalKnightMovementFromIndex);
                pseudoLegalKnightMovementFromIndex &= (pseudoLegalKnightMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (this.pieceArray[knightMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.KNIGHT + 6 * pieceColor), knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                } else if (this.pieceArray[knightMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.KNIGHT + 6 * pieceColor), knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArray[knightMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generateBishopMoves(int bishopIndex, ulong pseudoLegalBishopMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalBishopMovementFromIndex != 0) {

                int bishopMoveIndex = Constants.findFirstSet(pseudoLegalBishopMovementFromIndex);
                pseudoLegalBishopMovementFromIndex &= (pseudoLegalBishopMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (this.pieceArray[bishopMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.BISHOP + 6 * pieceColour), bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                }
                    //If not empty, then must be a black piece at that location, so generate a capture
                else if (this.pieceArray[bishopMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.BISHOP + 6 * pieceColour), bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArray[bishopMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generateRookMoves(int rookIndex, ulong pseudoLegalRookMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalRookMovementFromIndex != 0) {

                int rookMoveIndex = Constants.findFirstSet(pseudoLegalRookMovementFromIndex);
                pseudoLegalRookMovementFromIndex &= (pseudoLegalRookMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (this.pieceArray[rookMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.ROOK + 6 * pieceColour), rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                }
                    //If not empty, then must be a black piece at that location, so generate a capture
                else if (this.pieceArray[rookMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.ROOK + 6 * pieceColour), rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArray[rookMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generateQueenMoves(int queenIndex, ulong pseudoLegalQueenMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalQueenMovementFromIndex != 0) {

                int queenMoveIndex = Constants.findFirstSet(pseudoLegalQueenMovementFromIndex);
                pseudoLegalQueenMovementFromIndex &= (pseudoLegalQueenMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (this.pieceArray[queenMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.QUEEN + 6 * pieceColour), queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                }
                    //If not empty, then must be a black piece at that location, so generate a capture
                else if (this.pieceArray[queenMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.QUEEN + 6 * pieceColour), queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArray[queenMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private int generateKingMoves(int kingIndex, Bitboard pseudoLegalKingMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            while (pseudoLegalKingMovementFromIndex != 0) {

                int kingMoveIndex = Constants.findFirstSet(pseudoLegalKingMovementFromIndex);
                pseudoLegalKingMovementFromIndex &= (pseudoLegalKingMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (this.pieceArray[kingMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.KING + 6 * pieceColour), kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                } else if (this.pieceArray[kingMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = this.moveEncoder((Constants.KING + 6 * pieceColour), kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArray[kingMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

       //Generate rook moves from index
        private Bitboard generateRookMovesFromIndex(Bitboard allPieces, int index) {
            ulong horizontalVerticalOccupancy = allPieces & Constants.rookOccupancyMask[index];
            int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[index]) >> Constants.rookMagicShiftNumber[index]);
            return Constants.rookMoves[index][indexOfRookMoveBitboard];
        }

        //Generate bishop moves from index
        private Bitboard generateBishopMovesFromIndex(Bitboard allPieces, int index) {
            ulong diagonalOccupancy = allPieces & Constants.bishopOccupancyMask[index];
            int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[index]) >> Constants.bishopMagicShiftNumber[index]);
            return Constants.bishopMoves[index][indexOfBishopMoveBitboard];
        }


        //OTHER METHODS----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //takes in a FEN string, resets the board, and then sets all the instance variables based on it  
        public void FENToBoard(string FEN) {

            this.arrayOfBitboards = new ulong[13];
            this.arrayOfAggregateBitboards = new ulong[3];
            this.pieceArray = new int[64];
            this.sideToMove = 0;
            this.whiteShortCastleRights = this.whiteLongCastleRights = this.blackShortCastleRights = this.blackLongCastleRights = 0;
            this.enPassantSquare = 0x0UL;
            this.fullMoveNumber = 1;
            this.fiftyMoveRule = 0;
            
			this.pieceCount = new int[13];

	        this.whiteMidgameMaterial = 0;
	        this.whiteEndgameMaterial = 0;
	        this.blackMidgameMaterial = 0;
	        this.blackEndgameMaterial = 0;

			this.midgamePSQ = new int[13];
			this.endgamePSQ = new int[13];

            this.zobristKey = 0x0UL;
			this.gameHistory.Clear();

			this.historyHash = new int[16381];

            //Splits the FEN string into 6 fields
            string[] FENfields = FEN.Split(' ');

            //Splits the piece placement field into rows
            string[] pieceLocation = FENfields[0].Split('/');

            //Initializes the instance variables based on the contents of the field
            
            //sets the bitboards
            //loops through each of the 8 strings representing the rows, from the bottom row to the top
            for (int i = 0; i < 8; i++) {
                String row = pieceLocation[7 - i];

                //index for position in each row string
                int index = 0;

                //for each character in the row string, checks to see if there is a piece there
                //If there is, then it adds it to the appropriate bitboard
                //If there is a number, then it advances the index by that number
                foreach (char c in row) {
                    String binary = "00000000";
                    binary = binary.Substring(0, index) + "1" + binary.Substring(index + 1);
                    switch (c) {
                        case 'P': this.arrayOfBitboards[Constants.WHITE_PAWN] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'N': this.arrayOfBitboards[Constants.WHITE_KNIGHT]|= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'B': this.arrayOfBitboards[Constants.WHITE_BISHOP]|= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'R': this.arrayOfBitboards[Constants.WHITE_ROOK] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'Q': this.arrayOfBitboards[Constants.WHITE_QUEEN] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'K': this.arrayOfBitboards[Constants.WHITE_KING] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'p': this.arrayOfBitboards[Constants.BLACK_PAWN] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'n': this.arrayOfBitboards[Constants.BLACK_KNIGHT] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'b': this.arrayOfBitboards[Constants.BLACK_BISHOP] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'r': this.arrayOfBitboards[Constants.BLACK_ROOK] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'q': this.arrayOfBitboards[Constants.BLACK_QUEEN] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'k': this.arrayOfBitboards[Constants.BLACK_KING] |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case '1': index += 1; break;
                        case '2': index += 2; break;
                        case '3': index += 3; break;
                        case '4': index += 4; break;
                        case '5': index += 5; break;
                        case '6': index += 6; break;
                        case '7': index += 7; break;
                        case '8': index += 8; break;
                    }
                }
            }

            //sets the piece array
             //loops through each of the 8 strings representing the rows, from the bottom row to the top
            for (int i = 0; i < 8; i++) {
                String row = pieceLocation[7 - i];

                //index for position in each row string
                int index = 0;

                //for each character in the row string, checks to see if there is a piece there
                //If there is, then it adds it to the piece array
                //If there is a number, then it advances the index by that number
                foreach (char c in row) {
                    switch (c) {
						case 'P': this.pieceArray[7 + 8 * i - index] = Constants.WHITE_PAWN; index++; break;
						case 'N': this.pieceArray[7 + 8 * i - index] = Constants.WHITE_KNIGHT; index++; break;
						case 'B': this.pieceArray[7 + 8 * i - index] = Constants.WHITE_BISHOP; index++; break;
						case 'R': this.pieceArray[7 + 8 * i - index] = Constants.WHITE_ROOK; index++; break;
						case 'Q': this.pieceArray[7 + 8 * i - index] = Constants.WHITE_QUEEN; index++; break;
						case 'K': this.pieceArray[7 + 8 * i - index] = Constants.WHITE_KING; index++; break;
						case 'p': this.pieceArray[7 + 8 * i - index] = Constants.BLACK_PAWN; index++; break;
						case 'n': this.pieceArray[7 + 8 * i - index] = Constants.BLACK_KNIGHT; index++; break;
						case 'b': this.pieceArray[7 + 8 * i - index] = Constants.BLACK_BISHOP; index++; break;
						case 'r': this.pieceArray[7 + 8 * i - index] = Constants.BLACK_ROOK; index++; break;
						case 'q': this.pieceArray[7 + 8 * i - index] = Constants.BLACK_QUEEN; index++; break;
						case 'k': this.pieceArray[7 + 8 * i - index] = Constants.BLACK_KING; index++; break;
                        case '1': index += 1; break;
                        case '2': index += 2; break;
                        case '3': index += 3; break;
                        case '4': index += 4; break;
                        case '5': index += 5; break;
                        case '6': index += 6; break;
                        case '7': index += 7; break;
                        case '8': index += 8; break;
                    }
                }
            }

            //Sets the side to move variable
            foreach (char c in FENfields[1]) {
                if (c == 'w') {
					this.sideToMove = Constants.WHITE;
                } else if (c == 'b') {
					this.sideToMove = Constants.BLACK;
                }
            }
            
            //Sets the castling availability variables
            if (FENfields[2] == "-") {
				this.whiteShortCastleRights = 0;
				this.whiteLongCastleRights = 0;
				this.blackShortCastleRights = 0;
				this.blackLongCastleRights = 0;
            } else if (FENfields[2] != "-") {
                foreach (char c in FENfields[2]) {
                    if (c == 'K') {
						this.whiteShortCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'Q') {
						this.whiteLongCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'k') {
						this.blackShortCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'q') {
						this.blackLongCastleRights = Constants.CAN_CASTLE;
                    }
                }
            }
            
            //Sets the en Passant square variable
            if (FENfields[3] != "-") {

                int baseOfEPSquare = -1;
                int factorOfEPSquare = -1;

                foreach (char c in FENfields[3]) 
                if (char.IsLower(c) == true) {
                    baseOfEPSquare = 'h' - c;
                } else if (char.IsDigit(c) == true) {
                    factorOfEPSquare = ((int)Char.GetNumericValue(c) - 1);
                }
				this.enPassantSquare = 0x1UL << (baseOfEPSquare + factorOfEPSquare * 8);
            }
            
			//Checks to see if there is a halfmove clock or move number in the FEN string
            //If there isn't, then it sets the halfmove number clock and move number to 999;
            if (FENfields.Length >= 5) {
                //sets the halfmove clock since last capture or pawn move
				this.fiftyMoveRule = Convert.ToInt32(FENfields[4]);
				
                //sets the move number
	            this.fullMoveNumber = Convert.ToInt32(FENfields[5]);

            } else {
				this.fiftyMoveRule = 0;
				this.fullMoveNumber = 1;
            }

           //Computes the white pieces, black pieces, and occupied bitboard by using "or" on all the individual pieces
            for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_KING; i++) {
                this.arrayOfAggregateBitboards[0] |= arrayOfBitboards[i];
            }
            for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_KING; i++) {
                this.arrayOfAggregateBitboards[1] |= arrayOfBitboards[i];
            }
            
            this.arrayOfAggregateBitboards[2] = this.arrayOfAggregateBitboards[0] | this.arrayOfAggregateBitboards[1];

            // Sets the piece counter
            for (int i = Constants.WHITE_PAWN; i <= Constants.BLACK_KING; i++) {
                this.pieceCount[i] = Constants.popcount(this.arrayOfBitboards[i]);
            }
        }

		// Calculates the material fields
		private void materialCount() {

			// Calculates the number of pieces
			for (int i = Constants.WHITE_PAWN; i <= Constants.BLACK_KING; i++) {
				this.pieceCount[i] = Constants.popcount(this.arrayOfBitboards[i]);
			}

			// Multiplies the number of pieces by the material value
			for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_QUEEN; i++) {
				this.whiteMidgameMaterial += this.pieceCount[i] * Constants.arrayOfPieceValuesMG[i];
				this.whiteEndgameMaterial += this.pieceCount[i] * Constants.arrayOfPieceValuesEG[i];
			}

			for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_QUEEN; i++) {
				this.blackMidgameMaterial += this.pieceCount[i] * Constants.arrayOfPieceValuesMG[i];
				this.blackEndgameMaterial += this.pieceCount[i] * Constants.arrayOfPieceValuesEG[i];
			}
		}

		// calculates the value of the piece square table
		private void pieceSquareValue() {

			// loops through every square and finds the piece on that square
			// Sets the appropriate PSQ to the PSQ value for that square
			for (int i = 0; i < 64; i++) {
				int piece = pieceArray[i];

				if (piece >= Constants.WHITE_PAWN && piece <= Constants.BLACK_KING) {
					this.midgamePSQ[piece] += Constants.arrayOfPSQMidgame[piece][i];
					this.endgamePSQ[piece] += Constants.arrayOfPSQEndgame[piece][i];
				}
			}
		}

		// Calculates the zobrist key
		private void calculateZobristKey() {
			
			// Updates the key with the piece locations
			for (int i = 0; i < 64; i++) {
				int pieceType = this.pieceArray[i];
				if (pieceType != 0) {
					this.zobristKey ^= Constants.pieceZobrist[pieceType, i];
				}
			}

			// Updates the key with the en passant square
			if (this.enPassantSquare != 0) {
				this.zobristKey ^= Constants.enPassantZobrist[Constants.findFirstSet(this.enPassantSquare)];
			}

			// Updates the key with the castling rights
			if (this.whiteShortCastleRights == Constants.CAN_CASTLE) {
				this.zobristKey ^= Constants.castleZobrist[0];
			} if (this.whiteLongCastleRights == Constants.CAN_CASTLE) {
				this.zobristKey ^= Constants.castleZobrist[1];
			} if (this.blackShortCastleRights == Constants.CAN_CASTLE) {
				this.zobristKey ^= Constants.castleZobrist[2];
			} if (this.blackLongCastleRights == Constants.CAN_CASTLE) {
				this.zobristKey ^= Constants.castleZobrist[3];
			}

			// Updates the key with the side to move
			if (this.sideToMove == Constants.BLACK) {
				this.zobristKey ^= Constants.sideToMoveZobrist[0];
			}
			
			// Adds the initial position to the game history
			this.gameHistory.Add(this.zobristKey);

			// Adds the initial position to the history hash
			this.historyHash[(this.zobristKey % (ulong)this.historyHash.Length)]++;
	    }
		
        //Takes information on piece moved, start square, destination square, type of move, and piece captured
		//Creates a 32-bit unsigned integer representing this information
		//bits 0-3 store the piece moved, 4-9 stores start square, 10-15 stores destination square, 16-19 stores move type, 20-23 stores piece captured
		private int moveEncoder(int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured, int piecePromoted = 0x0) {
            int moveRepresentation = (pieceMoved | (startSquare << 4) | (destinationSquare << 10) | (flag << 16) | (pieceCaptured << 20) | (piecePromoted << 24));
            return moveRepresentation;
        }

		// Returns the number of times a given position has been repeated
		internal int getRepetitionNumber() {

			int possibleRepetitions = this.historyHash[(this.zobristKey % (ulong)this.historyHash.Length)];
			
			if (possibleRepetitions > 1) {
				int repetitionOfPosition = 0;

				for (int i = this.gameHistory.Count - 1 - this.fiftyMoveRule; i <= this.gameHistory.Count - 1; i += 2) {
					if (this.zobristKey == this.gameHistory[i]) {
						repetitionOfPosition++;
					}
				}
				return repetitionOfPosition;
			} else {
				return 1;
			}
		}
	}
}
