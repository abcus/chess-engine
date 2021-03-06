﻿using System;
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

        internal int material_PSQ_Value;

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

            this.material_PSQ_Value = inputBoard.material_PSQ_Value;
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

        internal Int32 material_PSQ_Value = 0;

        // Material value arrays indexed by piece
        internal static readonly Int32[] pieceValueMidgame = {
            0,
            Constants.PAWN_VALUE_MG, Constants.KNIGHT_VALUE_MG, Constants.BISHOP_VALUE_MG, Constants.ROOK_VALUE_MG, Constants.QUEEN_VALUE_MG, 0,
            Constants.PAWN_VALUE_MG, Constants.KNIGHT_VALUE_MG, Constants.BISHOP_VALUE_MG, Constants.ROOK_VALUE_MG, Constants.QUEEN_VALUE_MG, 0
        };

        internal static readonly Int32[] pieceValueEndgame = {
            0,
            Constants.PAWN_VALUE_EG, Constants.KNIGHT_VALUE_EG, Constants.BISHOP_VALUE_EG, Constants.ROOK_VALUE_EG, Constants.QUEEN_VALUE_EG, 0,
            Constants.PAWN_VALUE_EG, Constants.KNIGHT_VALUE_EG, Constants.BISHOP_VALUE_EG, Constants.ROOK_VALUE_EG, Constants.QUEEN_VALUE_EG, 0
        };

        internal static readonly Int32[][] pieceSquareTable = new Int32[13][];

        internal Zobrist zobristKey = 0x0UL;

	    internal List<Zobrist> gameHistory = new List<Zobrist>();
		internal List<int> moveHistory = new List<int>(); 

		// History hash is used to determine whether or not a check of game history is needed
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
            this.computeMatPSQScore();
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

            this.material_PSQ_Value = inputBoard.material_PSQ_Value;

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
            int startSquare = ((moveRepresentationInput & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
			int destinationSquare = ((moveRepresentationInput & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
			int flag = ((moveRepresentationInput & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
			int pieceCaptured = ((moveRepresentationInput & Constants.PIECE_CAPTURED_MASK) >> Constants.PIECE_CAPTURED_SHIFT);
            int piecePromoted = ((moveRepresentationInput & Constants.PIECE_PROMOTED_MASK) >> Constants.PIECE_PROMOTED_SHIFT);
	        int pieceMoved = this.pieceArray[startSquare];


            // Calculates bitboards for removing piece from start square and adding piece to destionation square
			// "and" with ~startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
			ulong startSquareBitboard = (0x1UL << startSquare);
			ulong destinationSquareBitboard = (0x1UL << destinationSquare);

            // Sets the board's instance variables
            
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

			// resets the 50 move rule
			if (flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE || flag == Constants.EN_PASSANT_CAPTURE) {
		        this.fiftyMoveRule = 0;
	        } if (flag == Constants.QUIET_MOVE && (pieceMoved == Constants.WHITE_PAWN || pieceMoved == Constants.BLACK_PAWN)) {
		        this.fiftyMoveRule = 0;
	        }

			// sets the side to move to the other player (white = 0 and black = 1, so side ^ 1 = other side)
			this.sideToMove ^= 1;
			this.zobristKey ^= Constants.sideToMoveZobrist[0];





            // If a piece was captured (move was a capture, EP capture, or promotion capture), then update the material balance
            if (pieceCaptured != 0) {

                Int32 captureSquare = destinationSquare;

                if (pieceCaptured == Constants.WHITE_PAWN || pieceCaptured == Constants.BLACK_PAWN) {
                    if (flag == Constants.EN_PASSANT_CAPTURE) {
                        captureSquare += this.sideToMove == Constants.WHITE ? -8 : 8; // Adjust the capture square if it is an en-passant (different than destination square)
                    }
                }
                this.material_PSQ_Value -= pieceSquareTable[pieceCaptured][captureSquare];
            }
            // If the piece moved was a pawn
            if (pieceMoved == Constants.WHITE_PAWN || pieceMoved == Constants.BLACK_PAWN) {

                if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                    this.material_PSQ_Value += pieceSquareTable[piecePromoted][destinationSquare] - pieceSquareTable[pieceMoved][destinationSquare]; // pieceSquareTable[PAWN][destination] is added later on, so have to subtract it here
                }
            }

            // If the move was a short castle
            if (flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                Int32 rookStart = flag == Constants.SHORT_CASTLE ? Constants.H1 : Constants.A1;
                Int32 rookFinish = flag == Constants.SHORT_CASTLE ? Constants.F1 : Constants.D1;

                // If side to move is black, then flip the rook start and finish squares 
                if (this.sideToMove == Constants.BLACK) {
                    rookStart = Utilities.flipSquare(rookStart);
                    rookFinish = Utilities.flipSquare(rookFinish);
                }
                Int32 piece = this.sideToMove == Constants.WHITE ? Constants.WHITE_ROOK : Constants.BLACK_ROOK;

                this.material_PSQ_Value += updatePieceSquare(piece, rookStart, rookFinish);
            }

            this.material_PSQ_Value += pieceSquareTable[pieceMoved][destinationSquare] - pieceSquareTable[pieceMoved][startSquare];






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

			this.moveHistory.Add(moveRepresentationInput);

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

            this.material_PSQ_Value = restoreData.material_PSQ_Value;

		    //Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
		    int startSquare = ((unmoveRepresentationInput & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
		    int destinationSquare = ((unmoveRepresentationInput & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
		    int flag = ((unmoveRepresentationInput & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
		    int pieceCaptured = ((unmoveRepresentationInput & Constants.PIECE_CAPTURED_MASK) >> Constants.PIECE_CAPTURED_SHIFT);
            int piecePromoted = ((unmoveRepresentationInput & Constants.PIECE_PROMOTED_MASK) >> Constants.PIECE_PROMOTED_SHIFT);

			int pieceMoved = this.pieceArray[destinationSquare];
	        if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
		        if (destinationSquare >= Constants.H8) {
			        pieceMoved = Constants.WHITE_PAWN;
		        } else if (destinationSquare <= Constants.A1) {
			        pieceMoved = Constants.BLACK_PAWN;
		        }
	        }


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

			this.moveHistory.RemoveAt(this.moveHistory.Count-1);

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

	    public void makeNullMove() {
			// Sets the board's instance variables
			// sets the side to move to the other player (white = 0 and black = 1, so side ^ 1 = other side)
			this.sideToMove ^= 1;
			this.zobristKey ^= Constants.sideToMoveZobrist[0];

			this.gameHistory.Add(this.zobristKey);
			this.historyHash[(this.zobristKey % (ulong)this.historyHash.Length)]++;
	    }

	    public void unmakeNullMove() {
			this.sideToMove ^= 1;
			this.zobristKey ^= Constants.sideToMoveZobrist[0];

			this.gameHistory.RemoveAt(this.gameHistory.Count - 1);
			this.historyHash[(this.zobristKey % (ulong)this.historyHash.Length)]--;
	    }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
		// ATTACK INFORMATION
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------

        public Bitboard getBitboardOfAttackers(int colourUnderAttack, int squareToCheck, Bitboard occupiedBitboard) {
            int shift = colourUnderAttack == Constants.WHITE ? 0 : 6;

            // Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
            ulong horizontalVerticalOccupancy = occupiedBitboard & Constants.rookOccupancyMask[squareToCheck];
            int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
            ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

            // Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
            ulong diagonalOccupancy = occupiedBitboard & Constants.bishopOccupancyMask[squareToCheck];
            int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
            ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

            // Looks up knight attack set from square, and intersects with opponent's knight bitboard
            ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

            // Looks up white pawn attack set from square, and intersects with opponent's pawn bitboard
            ulong pawnMoveFromSquare = Constants.capturesAndCapturePromotions[colourUnderAttack, squareToCheck];

            // Looks up king attack set from square, and intersects with opponent's king bitboard
            ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

            ulong bitboardOfAttackers = ((rookMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_ROOK - shift])
                                    | (rookMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_QUEEN - shift])
                                    | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_BISHOP - shift])
                                    | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_QUEEN - shift])
                                    | (knightMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_KNIGHT - shift])
                                    | (pawnMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_PAWN - shift])
                                    | (kingMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_KING - shift]));

            return bitboardOfAttackers;
        }

        // Method determines how many times a square is attacked
        // Takes in square and colour, and returns the number of pieces of the opposite colour attacking that square
        public int timesSquareIsAttacked(int colourUnderAttack, int squareToCheck) {

            // Gets the bitboard of attackers for that square and returns a popcount
            Bitboard bitboardOfAttackers = getBitboardOfAttackers(colourUnderAttack, squareToCheck, this.arrayOfAggregateBitboards[Constants.ALL]);
            
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

		//Checks if the king is in check and prints out the result
		public void kingInCheckTest(int colourOfKingToCheck) {

			int indexOfKing = 0;

			if (colourOfKingToCheck == Constants.WHITE) {
				indexOfKing = Constants.findFirstSet(this.arrayOfBitboards[Constants.WHITE_KING]);
			} else if (colourOfKingToCheck == Constants.BLACK) {
				indexOfKing = Constants.findFirstSet(this.arrayOfBitboards[Constants.BLACK_KING]);
			}
			int checkStatus = this.timesSquareIsAttacked(colourOfKingToCheck, indexOfKing);

			switch (checkStatus) {
				case Constants.NOT_IN_CHECK: Console.WriteLine("King not in check"); break;
				case Constants.CHECK: Console.WriteLine("King is in check"); break;
				case Constants.DOUBLE_CHECK: Console.WriteLine("King is in double check"); break;
				case Constants.MULTIPLE_CHECK: Console.WriteLine("King is in multiple check"); break;
			}
		}

		// Method that returns the least valuable piece attacking or defending a square
	    public Bitboard getLVP(Bitboard attackersAndDefenders, int side) {
		    
			// Loops through all attacker (or defender) bitboards from least valuable to most valuable
			// As soon as an intersection is found with the bitboard of attackers and defenders, returns that intersection
			for (int i = Constants.WHITE_PAWN + 6 * side; i <= Constants.WHITE_KING + 6*side; i ++) {
			    Bitboard LVP = attackersAndDefenders & arrayOfBitboards[i];
			    if (LVP != 0) {
				    Bitboard firstPiece = 0x1UL << (Constants.findFirstSet(LVP));
					Debug.Assert(Constants.popcount(firstPiece) == 1);
					return firstPiece;
			    }
		    }
		    return 0;
	    }

	    public Bitboard getXRay(int destinationSquare, int frontAttackerSquare, Bitboard oldOccupiedBitboard, Bitboard newOccupiedBitboard, int sideToCapture) {
		    
			if ((frontAttackerSquare % 8 == destinationSquare % 8) || (frontAttackerSquare/8 == destinationSquare/8)) {
				Bitboard rookAttacksFromDestionationSquareNew = generateRookMovesFromIndex(newOccupiedBitboard, destinationSquare);
				Bitboard rookAttacksFromFrontAttackerSquare = generateRookMovesFromIndex(newOccupiedBitboard, frontAttackerSquare);
				Bitboard rookAttacksFromDestinationSquareOld = generateRookMovesFromIndex(oldOccupiedBitboard, destinationSquare);
				Bitboard ray = rookAttacksFromDestionationSquareNew & rookAttacksFromFrontAttackerSquare & ~rookAttacksFromDestinationSquareOld;

				Bitboard potentialXRayAttackers = ray & this.arrayOfAggregateBitboards[Constants.ALL];
				int potentialXRayPiece = this.pieceArray[Constants.findFirstSet(potentialXRayAttackers)];
				if (potentialXRayPiece == Constants.WHITE_ROOK || potentialXRayPiece == Constants.WHITE_QUEEN || potentialXRayPiece == Constants.BLACK_ROOK || potentialXRayPiece == Constants.BLACK_QUEEN) {
					Debug.Assert(Constants.popcount(potentialXRayAttackers) <= 1);
					return potentialXRayAttackers;
				}
			} else {
				Bitboard bishopAttacksFromDestionationSquare = generateBishopMovesFromIndex(newOccupiedBitboard, destinationSquare);
				Bitboard bishopAttacksFromFrontAttackerSquare = generateBishopMovesFromIndex(newOccupiedBitboard, frontAttackerSquare);
				Bitboard bishopAttacksFromDestionationSquareOld = generateBishopMovesFromIndex(oldOccupiedBitboard, destinationSquare);
				Bitboard ray = bishopAttacksFromDestionationSquare & bishopAttacksFromFrontAttackerSquare & ~bishopAttacksFromDestionationSquareOld;

				Bitboard potentialXRayAttackers = ray & this.arrayOfAggregateBitboards[Constants.ALL];
				int potentialXRayPiece = this.pieceArray[Constants.findFirstSet(potentialXRayAttackers)];
				if (potentialXRayPiece == Constants.WHITE_BISHOP || potentialXRayPiece == Constants.WHITE_QUEEN || potentialXRayPiece == Constants.BLACK_BISHOP || potentialXRayPiece == Constants.BLACK_QUEEN) {
					Debug.Assert(Constants.popcount(potentialXRayAttackers) <= 1);
					return potentialXRayAttackers;
				}
			}
			return 0;
	    }

		// Method that returns the expected evaluation score gain or loss after a series of exchanges on a single square
	    public int staticExchangeEval(int startSquare, int destinationSquare, int sideToCapture) {

			// Gets the piece type on the start square
		    int piece = this.pieceArray[startSquare];

			// Gets the piece type on the destination square (if any)
			int captured = this.pieceArray[destinationSquare];

			// Whether or not the first move was a capture
		    bool firstMoveIsCapture = (captured == Constants.EMPTY) ? false : true;

			int[] gain = new int[32];
		    int depth = 0;

			Bitboard mayBeFrontAttacker = (this.arrayOfBitboards[Constants.WHITE_PAWN] | this.arrayOfBitboards[Constants.WHITE_BISHOP] |
		                                   this.arrayOfBitboards[Constants.WHITE_ROOK] | this.arrayOfBitboards[Constants.WHITE_QUEEN] |
		                                   this.arrayOfBitboards[Constants.BLACK_PAWN] | this.arrayOfBitboards[Constants.BLACK_BISHOP] |
		                                   this.arrayOfBitboards[Constants.BLACK_ROOK] | this.arrayOfBitboards[Constants.BLACK_QUEEN]);
			Bitboard startSquareBitboard = (0x1UL << startSquare);
			Bitboard occupiedBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
		    
			// Handles en passant
			if (piece == Constants.WHITE_PAWN && captured == Constants.EMPTY && (startSquare%8 != destinationSquare %8)) {
			    occupiedBitboard ^= (0x1UL << (destinationSquare - 8));
				captured = this.pieceArray[destinationSquare - 8];
			    firstMoveIsCapture = true;	
			} else if (piece == Constants.BLACK_PAWN && captured == Constants.EMPTY && (startSquare % 8 != destinationSquare % 8)) {
			    occupiedBitboard ^= (0x1UL << (destinationSquare + 8));
			    captured = this.pieceArray[destinationSquare + 8];
			    firstMoveIsCapture = true;
		    }

			Bitboard attackersAndDefenders = this.getBitboardOfAttackers(Constants.WHITE, destinationSquare, occupiedBitboard) | this.getBitboardOfAttackers(Constants.BLACK, destinationSquare, occupiedBitboard);
			
			// Material win for side capturing (e.g. white) if the target piece (e.g. pawn) is en-prise
			gain[depth] = Constants.arrayOfPieceValuesSEE[captured];
			
			// Handles promotions
			// Sets "captured" to the value of the piece sitting on the destination square (e.g. white rook)
		    if (piece == Constants.WHITE_PAWN && destinationSquare >= Constants.H8) {
			    gain[depth] += (Constants.arrayOfPieceValuesSEE[Constants.WHITE_QUEEN] - Constants.arrayOfPieceValuesSEE[Constants.WHITE_PAWN]);
				captured = Constants.WHITE_QUEEN;
		    } else if (piece == Constants.BLACK_PAWN && destinationSquare <= Constants.A1) {
				gain[depth] += (Constants.arrayOfPieceValuesSEE[Constants.BLACK_QUEEN] - Constants.arrayOfPieceValuesSEE[Constants.BLACK_PAWN]);
				captured = Constants.BLACK_QUEEN;
		    } else {
			    captured = this.pieceArray[startSquare];
		    }
			
			// If no piece from the other side is attcking the destination square, then return
			if ((attackersAndDefenders & this.arrayOfAggregateBitboards[sideToCapture ^ 1]) == 0) {
			    return gain[depth];
		    }

			do {
			    depth++;
				
				// Material win for other side (e.g. black) if the piece that just captured (e.g. rook) is en-prise
				gain[depth] = Constants.arrayOfPieceValuesSEE[captured] - gain[depth - 1];
				
				// Add any hidden attackers behind the piece that just captured
				if ((startSquareBitboard & mayBeFrontAttacker) != 0) {
					attackersAndDefenders |= getXRay(destinationSquare, startSquare, occupiedBitboard, occupiedBitboard ^ startSquareBitboard, sideToCapture);
				}
				
				// Remove the piece that just captured (e.g. rook) from the attackers/defenders and occupied bitboards
				// If the first move was a quiet move, still remove the piece that did the moving because it would still be attacking the square (unless it is a pawn, which can't attack forwards)
				occupiedBitboard ^= startSquareBitboard;
				if (firstMoveIsCapture == true || ((firstMoveIsCapture == false && piece != Constants.WHITE_PAWN && piece != Constants.BLACK_PAWN))) {
					attackersAndDefenders ^= startSquareBitboard;
				}
				
				firstMoveIsCapture = true;
				
				sideToCapture ^= 1;

				// Get the other side's (e.g. black) least valuable attacker
				startSquareBitboard = this.getLVP(attackersAndDefenders, sideToCapture);
				startSquare = Constants.findFirstSet(startSquareBitboard);
				piece = this.pieceArray[startSquare];

				// Handles promotions
				if (piece == Constants.WHITE_PAWN && destinationSquare >= Constants.H8) {
					gain[depth] += (Constants.arrayOfPieceValuesSEE[Constants.WHITE_QUEEN] - Constants.arrayOfPieceValuesSEE[Constants.WHITE_PAWN]);
					captured = Constants.WHITE_QUEEN;
				} else if (piece == Constants.BLACK_PAWN && destinationSquare <= Constants.A1) {
					gain[depth] += (Constants.arrayOfPieceValuesSEE[Constants.BLACK_QUEEN] - Constants.arrayOfPieceValuesSEE[Constants.BLACK_PAWN]);
					captured = Constants.BLACK_QUEEN;
				} else {
					captured = piece;
				}

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
		// Generates moves for both the PVS and the Quiescence search
		// Only called when the king (of the player whose turn it is) is not attacked
		// For pieces that are in an absolute pin, only generates moves along the pin ray (including capture of pinner)
		// For castling, only generates moves that don't involve king passing through attacked square 
		// When testing for legality, only have to check king moves and en passant
		// 
		// For PVS
		//		Flag: ALL_MOVES: all moves
		// For Quiescence:
		//		Flag: CAP_AND_QUEEN_PROMO: Captures, promotion captures, en passant captures, quiet queen promotions
		//		Flag: QUIET_CHECK: Quiet moves/Double pawn push/short castle/long castle/underpromotions that give check
		//		Flag: QUIET_NO_CHECK: Quie moves/Double pawn push/short castle/long caslte/underpromotions that don't give check (for perft testing purposes)
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public int[] moveGenerator(int flag) {

		    int shift = this.sideToMove == Constants.WHITE ? 0 : 6;
            int doublePawnMoveShift = this.sideToMove == Constants.WHITE ? 8 : -8;

            // Gets the bitboard of all the side to move's pieces, and the bitboard of all pieces
            Bitboard tempOwnPawnBitboard = this.arrayOfBitboards[Constants.WHITE_PAWN + shift];
            Bitboard tempOwnKnightBitboard = this.arrayOfBitboards[Constants.WHITE_KNIGHT + shift];
            Bitboard tempOwnBishopBitboard = this.arrayOfBitboards[Constants.WHITE_BISHOP + shift];
            Bitboard tempOwnRookBitboard = this.arrayOfBitboards[Constants.WHITE_ROOK + shift];
            Bitboard tempOwnQueenBitboard = this.arrayOfBitboards[Constants.WHITE_QUEEN + shift];
            Bitboard tempOwnKingBitboard = this.arrayOfBitboards[Constants.WHITE_KING + shift];
            Bitboard tempAllPieceBitboard = this.arrayOfAggregateBitboards[Constants.ALL];

            //Gets the bitboard of the opposing side's bishop, rook, queen, and king (for generating checks)
            Bitboard tempOppRookAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_ROOK - shift] | this.arrayOfBitboards[Constants.BLACK_QUEEN - shift]);
            Bitboard tempOppBishopAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_BISHOP - shift] | this.arrayOfBitboards[Constants.BLACK_QUEEN - shift]);
            Bitboard oppKingBitboard = this.arrayOfBitboards[Constants.BLACK_KING - shift];

            // Calculates the index of the own and opposing king
            int ownKingIndex = Constants.findFirstSet(tempOwnKingBitboard);
            int oppKingIndex = Constants.findFirstSet(oppKingBitboard);

            // declares an array to hold the almost legal moves
            int[] listOfAlmostLegalMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
            int index = 0;

            // CALCULATES SQUARES THAT WHITE PIECES CAN GIVE CHECK FROM

            // Calculates the squares that a white rook could stand on to check the black king
            ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL] & Constants.rookOccupancyMask[oppKingIndex];
            int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[oppKingIndex]) >> Constants.rookMagicShiftNumber[oppKingIndex]);
            ulong rookCheckSquares = Constants.rookMoves[oppKingIndex][rookMoveIndex];

            //  Calculates the squares that a white bishop could stand on to check the black king
            ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL] & Constants.bishopOccupancyMask[oppKingIndex];
            int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[oppKingIndex]) >> Constants.bishopMagicShiftNumber[oppKingIndex]);
            ulong bishopCheckSquares = Constants.bishopMoves[oppKingIndex][bishopMoveIndex];

            // Calculates the squares that a white queen could stand on to check the black king
            ulong queenCheckSquares = rookCheckSquares | bishopCheckSquares;

            // Calculates the squares that a white knight could stand on to check the black king
            ulong knightCheckSquares = Constants.knightMoves[oppKingIndex];

            // Calculates the squares that a white pawn could stand on to check the black king
            ulong pawnCheckSquares = Constants.capturesAndCapturePromotions[this.sideToMove ^ 1, oppKingIndex];
            
            // GENERATES MOVES FOR PINNED PIECES-----------------------------------------------------------------------------------------------------
            
            // Finds rook moves from the own side's king, and intersects with own side's pieces to get bitboard of potentially pinned pieces
            Bitboard potentiallyPinnedPiecesByRook = ((this.generateRookMovesFromIndex(tempAllPieceBitboard, ownKingIndex)) & this.arrayOfAggregateBitboards[this.sideToMove]);

            // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
            // Intersect with other side's rook and queen to get bitboard of potential pinners
            Bitboard tempAllPieceExceptPotentiallyPinnedByRookBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByRook);
            Bitboard rookMovesFromIndexWithoutPinned = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, ownKingIndex);
            Bitboard potentialPinners = (rookMovesFromIndexWithoutPinned & tempOppRookAndQueenBitboard);

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

                    // If the pinned piece is a pawn, then generate single and double pushes along the pin ray

                    if (pinnedPieceType == Constants.WHITE_PAWN + shift) {

                        //For pawns that are between rank 2-6 (white) or 3-7 (black), generate single pushes
                        if (indexOfPinnedPiece >= Constants.singlePushCaptureLower[this.sideToMove] && indexOfPinnedPiece <= Constants.singlePushCaptureUpper[this.sideToMove]) {
                            //Generates pawn single moves
                            Bitboard pawnMoveSquares = 0;

                            if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                                pawnMoveSquares = (Constants.singlePawnMovesAndPromotions[this.sideToMove, indexOfPinnedPiece] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & pinRay & (~pawnCheckSquares));
                            } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                                pawnMoveSquares = (Constants.singlePawnMovesAndPromotions[this.sideToMove, indexOfPinnedPiece] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & pinRay & (pawnCheckSquares));
                            } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {
                                pawnMoveSquares = (Constants.singlePawnMovesAndPromotions[this.sideToMove, indexOfPinnedPiece] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & pinRay);
                            }
                            this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
                        }
                        //For pawns that are on rank 2 (white) or rank 7 (black), generate double pawn pushes
                        if (indexOfPinnedPiece >= Constants.doublePushLower[this.sideToMove] && indexOfPinnedPiece <= Constants.doublePushUpper[this.sideToMove]) {
                            Bitboard singlePawnMovementFromIndex = Constants.singlePawnMovesAndPromotions[this.sideToMove, indexOfPinnedPiece];
                            Bitboard doublePawnMovementFromIndex = Constants.singlePawnMovesAndPromotions[this.sideToMove, indexOfPinnedPiece + doublePawnMoveShift];
                            Bitboard pawnMoveSquares = 0x0UL;

                            if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                                    pawnMoveSquares = (doublePawnMovementFromIndex & pinRay & (~pawnCheckSquares));
                                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                                    pawnMoveSquares = (doublePawnMovementFromIndex & pinRay & pawnCheckSquares);
                                } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {
                                    pawnMoveSquares = (doublePawnMovementFromIndex & pinRay);
                                }
                            }
                            this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.DOUBLE_PAWN_PUSH);
                        }
                        // Removes the own side's pawn from the list of pawns
                        tempOwnPawnBitboard &= (~pinnedPiece);
                    }
                    // If the pinned piece is a rook, then generate moves along the pin ray
                    else if (pinnedPieceType == Constants.WHITE_ROOK + shift) {
                        Bitboard rookMoveSquares = 0;

                        if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                            rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~rookCheckSquares));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                            rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ((rookCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                            rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                            rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.ALL_MOVES) {
                            rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay));
                        }
                        this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, rookMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);

                        // Removes the own side's rook from the list of rooks
                        tempOwnRookBitboard &= (~pinnedPiece);
                    }
                    // If the pinned piece is a queen, then generate moves along the pin ray (only rook moves)
                    else if (pinnedPieceType == Constants.WHITE_QUEEN + shift) {
                        Bitboard queenMoveSquares = 0;

                        if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                            queenMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~queenCheckSquares));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                            queenMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ((queenCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                            queenMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                            queenMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.ALL_MOVES) {
                            queenMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay));
                        }
                        this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, queenMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
                        // Removes the own side's queen from the list of queens
                        tempOwnQueenBitboard &= (~pinnedPiece);
                    }
                    // If pinned piece type is a knight, then it isn't allowed to move
                    else if (pinnedPieceType == Constants.WHITE_KNIGHT + shift) {
                        // Remove it from the knight list so that no night moves will be generated later on
                        tempOwnKnightBitboard &= (~pinnedPiece);
                    }
                    // If pinned piece type is a bishop, then it isn't allowed to move
                    else if (pinnedPieceType == Constants.WHITE_BISHOP + shift) {
                        // Remove it from the bishop list so that no bishop moves will be generated later on
                        tempOwnBishopBitboard &= (~pinnedPiece);
                    }
                    // Note that pawn captures, en-passant captures, promotions, promotion-captures, knight moves, and bishop moves will all be illegal
                }

            }

            // Finds bishop moves from the king, and intersects with own side's pieces to get bitboard of potentially pinned pieces
            Bitboard potentiallyPinnedPiecesByBishop = (this.generateBishopMovesFromIndex(tempAllPieceBitboard, ownKingIndex) & this.arrayOfAggregateBitboards[this.sideToMove]);

            // Removes potentially pinned pieces from the all pieces bitboard, and generates bishop moves from king again
            // Intersect with other side's bishop and queen to get bitboard of potential pinners
            Bitboard tempAllPieceExceptPotentiallyPinnedByBishopBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByBishop);
            Bitboard bishopMovesFromIndexWithoutPinned = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, ownKingIndex);
            potentialPinners = (bishopMovesFromIndexWithoutPinned & (tempOppBishopAndQueenBitboard));

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

                    // If the pinned piece is a pawn, then generate captures, en passant captures, and capture promotions
                    if (pinnedPieceType == Constants.WHITE_PAWN + shift) {

                        //For pawns that are between rank 2-6 (white) and 3-7 (black), generate captures
                        if (indexOfPinnedPiece >= Constants.singlePushCaptureLower[this.sideToMove] && indexOfPinnedPiece <= Constants.singlePushCaptureUpper[this.sideToMove]) {

                            Bitboard pawnCaptureSquares = 0;

                            if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK ||
                                flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO ||
                                flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO ||
                                flag == Constants.ALL_MOVES) {
                                //Generates pawn captures (will be a maximum of 1 along the pin ray)
                                pawnCaptureSquares = (Constants.capturesAndCapturePromotions[this.sideToMove, indexOfPinnedPiece] & this.arrayOfAggregateBitboards[this.sideToMove ^ 1] & pinRay);
                            }
                            this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnCaptureSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
                        }
                        //For pawns that are on rank 5 (white) or rank 4 (black), generate en passant captures
                        if ((this.enPassantSquare & Constants.enPassantRank[this.sideToMove]) != 0) {
                            if (indexOfPinnedPiece >= Constants.enPassantLower[this.sideToMove] && indexOfPinnedPiece <= Constants.enPassantUpper[this.sideToMove]) {

                                Bitboard pawnEPSquares = 0;

                                if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK ||
                                    flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO ||
                                    flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO ||
                                    flag == Constants.ALL_MOVES) {
                                    pawnEPSquares = (Constants.capturesAndCapturePromotions[this.sideToMove, indexOfPinnedPiece] & this.enPassantSquare & pinRay);
                                }
                                this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnEPSquares, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.EN_PASSANT_CAPTURE);
                            }
                        }
                        //For pawns on the rank 7 (white) or rank 2 (black), generate promotion captures
                        if (indexOfPinnedPiece >= Constants.promotionPromotionCaptureLower[this.sideToMove] && indexOfPinnedPiece <= Constants.promotionPromotionCaptureUpper[this.sideToMove]) {

                            Bitboard pawnPromoCapSquares = 0;

                            if (flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO || flag == Constants.ALL_MOVES) {
                                //Generates pawn capture promotions
                                pawnPromoCapSquares = (Constants.capturesAndCapturePromotions[this.sideToMove, indexOfPinnedPiece] & this.arrayOfAggregateBitboards[this.sideToMove ^ 1] & pinRay);
                                this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnPromoCapSquares, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.PROMOTION_CAPTURE);
                            }
                            else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK || flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO) {
                                pawnPromoCapSquares = (Constants.capturesAndCapturePromotions[this.sideToMove, indexOfPinnedPiece] & this.arrayOfAggregateBitboards[this.sideToMove ^ 1] & pinRay);
                                this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnPromoCapSquares, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.QUEEN_PROMOTION_CAPTURE);
                            } else if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                                pawnPromoCapSquares = (Constants.capturesAndCapturePromotions[this.sideToMove, indexOfPinnedPiece] & this.arrayOfAggregateBitboards[this.sideToMove ^ 1] & pinRay);
                                this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, pawnPromoCapSquares, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.UNDER_PROMOTION_CAPTURE);
                            }
                        }
                        // Removes the own side's pawn from the list of pawns
                        tempOwnPawnBitboard &= (~pinnedPiece);
                    }
                    // If the pinned piece is a bishop, then generate moves along the pin ray
                    else if (pinnedPieceType == Constants.WHITE_BISHOP + shift) {

                        Bitboard bishopMoveSquares = 0;
                        if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                            bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~bishopCheckSquares));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                            bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ((bishopCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                            bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                            bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.ALL_MOVES) {
                            bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay));
                        }
                        this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, bishopMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);

                        // Removes the own side's bishop from the list of bishops
                        tempOwnBishopBitboard &= (~pinnedPiece);
                    }
                    // If the pinned piece is a queen, then generate moves along the pin ray
                    else if (pinnedPieceType == Constants.WHITE_QUEEN + shift) {

                        Bitboard queenMoveSquares = 0;

                        if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                            queenMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~queenCheckSquares));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                            queenMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ((queenCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                            queenMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                            queenMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        } else if (flag == Constants.ALL_MOVES) {
                            queenMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], indexOfPinnedPiece) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (pinRay));
                        }

                        this.generatePawnKnightBishopRookQueenKingMoves(indexOfPinnedPiece, queenMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);

                        // Removes the own side's queen from the list of queens
                        tempOwnQueenBitboard &= (~pinnedPiece);
                    }
                    // If pinned piece type is a knight, then it isn't allowed to move
                    else if (pinnedPieceType == Constants.WHITE_KNIGHT + shift) {
                        // Remove it from the knight list so that no night moves will be generated later on
                        tempOwnKnightBitboard &= (~pinnedPiece);
                    }
                    // If pinned piece type is a rook, then it isn't allowed to move
                    else if (pinnedPieceType == Constants.WHITE_ROOK + shift) {
                        // Remove it from the rook list so that no bishop moves will be generated later on
                        tempOwnRookBitboard &= (~pinnedPiece);
                    }
                    // Note that single pawn pushes, double pawn pushes, promotions, promotion-captures, knight moves, and rook moves will all be illegal
                }

            }

            // GENERATES MOVES FOR NON-PINNED PIECES------------------------------------------------------------------------------------------------

            // Loops through own side's pawns and generates moves, captures, and promotions
            while (tempOwnPawnBitboard != 0) {

                // Finds the index of the first pawn, then removes it from the temporary pawn bitboard
                int pawnIndex = Constants.findFirstSet(tempOwnPawnBitboard);
                tempOwnPawnBitboard &= (tempOwnPawnBitboard - 1);

                //For pawns that are between ranks 2-6 (white) or 3-7 (black), generate single pushes and captures
                if (pawnIndex >= Constants.singlePushCaptureLower[this.sideToMove] && pawnIndex <= Constants.singlePushCaptureUpper[this.sideToMove]) {

                    // Passes a bitboard of possible pawn single moves to the generate move method (bitboard could be 0)
                    // Method reads bitboard of possible moves, encodes them, adds them to the list, and increments the index by 1
                    Bitboard pawnMoveSquares = 0;
                    if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                        pawnMoveSquares = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL] & (~pawnCheckSquares));
                    } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                        pawnMoveSquares = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL] & (pawnCheckSquares));
                    } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {
                        pawnMoveSquares = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                    }
                    this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);

                    // Passes a bitboard of possible pawn captures to the generate move method (bitboard could be 0)
                    Bitboard pawnCaptureSquares = Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                    if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK ||
                        flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO ||
                        flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO ||
                        flag == Constants.ALL_MOVES) {
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnCaptureSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
                    }
                }

                //For pawns that are on rank 2 (white) or rank 7 (black), generate double pawn pushes
                if (pawnIndex >= Constants.doublePushLower[this.sideToMove] && pawnIndex <= Constants.doublePushUpper[this.sideToMove]) {
                    Bitboard singlePawnMovementFromIndex = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex];
                    Bitboard doublePawnMovementFromIndex = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex + doublePawnMoveShift];
                    Bitboard pawnMoveSquares = 0x0UL;

                    if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                        if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                            pawnMoveSquares = doublePawnMovementFromIndex & (~pawnCheckSquares);
                        } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                            pawnMoveSquares = doublePawnMovementFromIndex & (pawnCheckSquares);
                        } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {
                            pawnMoveSquares = doublePawnMovementFromIndex;
                        }
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.DOUBLE_PAWN_PUSH);
                    }
                }
                //If en passant is possible, For pawns that are on rank 5 (white) or rank 4 (black), generate en passant captures
                if ((this.enPassantSquare & Constants.enPassantRank[this.sideToMove]) != 0) {
                    if (pawnIndex >= Constants.enPassantLower[this.sideToMove] && pawnIndex <= Constants.enPassantUpper[this.sideToMove]) {

                        Bitboard pawnEPSquare = 0;

                        if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK ||
                            flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO ||
                            flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO ||
                            flag == Constants.ALL_MOVES) {
                            pawnEPSquare = Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & this.enPassantSquare;
                        }
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnEPSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.EN_PASSANT_CAPTURE);
                    }
                }
                //For pawns on rank 7 (white) or rank 2 (black), generate promotions and promotion captures
                if (pawnIndex >= Constants.promotionPromotionCaptureLower[this.sideToMove] && pawnIndex <= Constants.promotionPromotionCaptureUpper[this.sideToMove]) {
                    Bitboard pawnPromotionSquare = 0;

                    if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                        pawnPromotionSquare = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnPromotionSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.UNDER_PROMOTION);

                        Bitboard pawnPromoCapSquare = Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnPromoCapSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.UNDER_PROMOTION_CAPTURE);
                    }
                    else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                        pawnPromotionSquare = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnPromotionSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.QUEEN_PROMOTION);

                        Bitboard pawnPromoCapSquare = Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnPromoCapSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.QUEEN_PROMOTION_CAPTURE);
                    } else if (flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO || flag == Constants.ALL_MOVES) {
                        pawnPromotionSquare = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnPromotionSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.PROMOTION);

                        Bitboard pawnPromoCapSquare = Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pawnPromoCapSquare, listOfAlmostLegalMoves, ref index, this.sideToMove, Constants.PROMOTION_CAPTURE);
                    }
                }
            }

            //generates own side's knight moves and captures
            while (tempOwnKnightBitboard != 0) {
                int knightIndex = Constants.findFirstSet(tempOwnKnightBitboard);
                tempOwnKnightBitboard &= (tempOwnKnightBitboard - 1);
                Bitboard knightMoveSquares = 0;

                if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                    knightMoveSquares = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove] & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~knightCheckSquares));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                    knightMoveSquares = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove] & ((knightCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                    knightMoveSquares = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]));
                } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                    knightMoveSquares = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove] & ~(this.arrayOfAggregateBitboards[this.sideToMove ^ 1]));
                } else if (flag == Constants.ALL_MOVES) {
                    knightMoveSquares = Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]);
                } 
                this.generatePawnKnightBishopRookQueenKingMoves(knightIndex, knightMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
            }

            //generates own side's bishop moves and captures
            while (tempOwnBishopBitboard != 0) {
                int bishopIndex = Constants.findFirstSet(tempOwnBishopBitboard);
                tempOwnBishopBitboard &= (tempOwnBishopBitboard - 1);
                Bitboard bishopMoveSquares = 0;

                if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                    bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~bishopCheckSquares));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                    bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & ((bishopCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                    bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                    bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                } else if (flag == Constants.ALL_MOVES) {
                    bishopMoveSquares = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]));
                }
                this.generatePawnKnightBishopRookQueenKingMoves(bishopIndex, bishopMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
            }

            //generates own side's rook moves and captures
            while (tempOwnRookBitboard != 0) {
                int rookIndex = Constants.findFirstSet(tempOwnRookBitboard);
                tempOwnRookBitboard &= (tempOwnRookBitboard - 1);
                Bitboard rookMoveSquares = 0;

                if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                    rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~rookCheckSquares));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                    rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & ((rookCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1])));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                    rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                    rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
                } else if (flag == Constants.ALL_MOVES) {
                    rookMoveSquares = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]));
                }
                this.generatePawnKnightBishopRookQueenKingMoves(rookIndex, rookMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
            }

            //generates own side's queen moves and captures
            while (tempOwnQueenBitboard != 0) {
                int queenIndex = Constants.findFirstSet(tempOwnQueenBitboard);
                tempOwnQueenBitboard &= (tempOwnQueenBitboard - 1);
                Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]));
                Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]));
                Bitboard queenMoveSquares = 0;

                if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                    queenMoveSquares = (pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & (~queenCheckSquares);
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK) {
                    queenMoveSquares = (pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & ((queenCheckSquares) | (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]));
                } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                    queenMoveSquares = (pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1];
                } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                    queenMoveSquares = (pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1];
                } else if (flag == Constants.ALL_MOVES) {
                    queenMoveSquares = (pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex);
                }
                this.generatePawnKnightBishopRookQueenKingMoves(queenIndex, queenMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);
            }

            //generates own side's king moves and captures
            Bitboard kingMoveSquares = 0;

            if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK) {
                kingMoveSquares = Constants.kingMoves[ownKingIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]) & (~this.arrayOfAggregateBitboards[this.sideToMove ^ 1]);
            } else if (flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO || flag == Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK || flag == Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO) {
                kingMoveSquares = Constants.kingMoves[ownKingIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]) & this.arrayOfAggregateBitboards[this.sideToMove ^ 1];
            } else if (flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS) {
                kingMoveSquares = Constants.kingMoves[ownKingIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]) & ~this.arrayOfAggregateBitboards[this.sideToMove ^ 1];
            } else if (flag == Constants.ALL_MOVES) {
                kingMoveSquares = Constants.kingMoves[ownKingIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]);
            }
            this.generatePawnKnightBishopRookQueenKingMoves(ownKingIndex, kingMoveSquares, listOfAlmostLegalMoves, ref index, this.sideToMove);


		    if (this.sideToMove == Constants.WHITE) {

                //Generates white king castling moves (if the king is not in check)
                if ((this.whiteShortCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {

                    if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK || flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {

                        int moveScore = Constants.GOOD_QUIET_SCORE;
                        int moveRepresentation = this.moveEncoder(Constants.E1, Constants.G1, Constants.SHORT_CASTLE, Constants.EMPTY, Constants.EMPTY, moveScore);

                        if (this.timesSquareIsAttacked(Constants.WHITE, Constants.F1) == 0) {
                            listOfAlmostLegalMoves[index++] = moveRepresentation;
                        }
                    }
                }
                if ((this.whiteLongCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {

                    if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK || flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {

                        int moveScore = Constants.GOOD_QUIET_SCORE;
                        int moveRepresentation = this.moveEncoder(Constants.E1, Constants.C1, Constants.LONG_CASTLE, Constants.EMPTY, Constants.EMPTY, moveScore);

                        if (this.timesSquareIsAttacked(Constants.WHITE, Constants.D1) == 0) {
                            listOfAlmostLegalMoves[index++] = moveRepresentation;
                        }
                    }
                }

		    } else if (this.sideToMove == Constants.BLACK) {

                //Generates black king castling moves (if the king is not in check)
                if ((this.blackShortCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {

                    if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK || flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {

                        int moveScore = Constants.GOOD_QUIET_SCORE;
                        int moveRepresentation = this.moveEncoder(Constants.E8, Constants.G8, Constants.SHORT_CASTLE, Constants.EMPTY, Constants.EMPTY, moveScore);

                        if (this.timesSquareIsAttacked(Constants.BLACK, Constants.F8) == 0) {
                            listOfAlmostLegalMoves[index++] = moveRepresentation;
                        }
                    }
                }
                if ((this.blackLongCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL] & Constants.BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {

                    if (flag == Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK || flag == Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS || flag == Constants.ALL_MOVES) {

                        int moveScore = Constants.GOOD_QUIET_SCORE;
                        int moveRepresentation = this.moveEncoder(Constants.E8, Constants.C8, Constants.LONG_CASTLE, Constants.EMPTY, Constants.EMPTY, moveScore);

                        if (this.timesSquareIsAttacked(Constants.BLACK, Constants.D8) == 0) {
                            listOfAlmostLegalMoves[index++] = moveRepresentation;
                        }
                    }
                }

		    }

            return listOfAlmostLegalMoves;
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

            int shift = this.sideToMove == Constants.WHITE ? 0 : 6;
            int doublePawnMoveShift = this.sideToMove == Constants.WHITE ? 8 : -8;

            // Gets temporary piece bitboards (they will be modified by removing the LSB until they equal 0, so can't use the actual piece bitboards)
            Bitboard tempOwnPawnBitboard = this.arrayOfBitboards[Constants.WHITE_PAWN + shift];
            Bitboard tempOwnKnightBitboard = this.arrayOfBitboards[Constants.WHITE_KNIGHT + shift];
            Bitboard tempOwnBishopBitboard = this.arrayOfBitboards[Constants.WHITE_BISHOP + shift];
            Bitboard tempOwnRookBitboard = this.arrayOfBitboards[Constants.WHITE_ROOK + shift];
            Bitboard tempOwnQueenBitboard = this.arrayOfBitboards[Constants.WHITE_QUEEN + shift];
            Bitboard tempOwnKingBitboard = this.arrayOfBitboards[Constants.WHITE_KING + shift];
            Bitboard tempAllPieceBitboard = this.arrayOfAggregateBitboards[Constants.ALL];
            Bitboard tempOppRookAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_ROOK - shift] | this.arrayOfBitboards[Constants.BLACK_QUEEN - shift]);
            Bitboard tempOppBishopAndQueenBitboard = (this.arrayOfBitboards[Constants.BLACK_BISHOP - shift] | this.arrayOfBitboards[Constants.BLACK_QUEEN - shift]);

            int kingIndex = Constants.findFirstSet(tempOwnKingBitboard);
            Bitboard bishopMovesFromKingPosition = this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex);
            Bitboard rookMovesFromKingPosition = this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex);

            // Initializes the list of pseudo legal moves (can only hold 220 moves), and initializes the array's index (so we know which element to add the move to)
            int[] listOfCheckEvasionMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
            int index = 0;

            // Checks to see whether the king is in check or double check in the current position
            int kingCheckStatus = this.timesSquareIsAttacked(this.sideToMove, kingIndex);

            // If the king is in check
            if (kingCheckStatus == Constants.CHECK) {

                // Finds rook moves from the king, and intersects with own side's pieces to get bitboard of potentially pinned pieces
                Bitboard potentiallyPinnedPiecesByRook = ((this.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex)) & this.arrayOfAggregateBitboards[this.sideToMove]);

                // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                // Intersect with black rook and queen to get bitboard of potential pinners
                Bitboard tempAllPieceExceptPotentiallyPinnedByRookBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByRook);
                Bitboard rookMovesFromIndexWithoutPinned = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, kingIndex);
                Bitboard potentialPinners = (rookMovesFromIndexWithoutPinned & tempOppRookAndQueenBitboard);

                // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                while (potentialPinners != 0) {
                    int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);

                    // Removes the potential pinner from the bitboard
                    potentialPinners &= (potentialPinners - 1);

                    Bitboard rookMovesFromPinnerIndex = this.generateRookMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByRookBitboard, indexOfPotentialPinner);

                    // If intersection with potentially pinned pieces is not zero, then piece is pinned
                    Bitboard pinnedPiece = (rookMovesFromPinnerIndex & potentiallyPinnedPiecesByRook);
                    if (pinnedPiece != 0) {

                        int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                        int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                        // Remove any pinned pieces (they will not be able to block or capture checking piece)
                        if (pinnedPieceType == Constants.WHITE_PAWN + shift) {
                            tempOwnPawnBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_KNIGHT + shift) {
                            tempOwnKnightBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_BISHOP + shift) {
                            tempOwnBishopBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_ROOK + shift) {
                            tempOwnRookBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_QUEEN + shift) {
                            tempOwnQueenBitboard &= (~pinnedPiece);
                        }
                    }
                }
                // Finds bishop moves from the king, and intersects with own side's pieces to get bitboard of potentially pinned pieces
                Bitboard potentiallyPinnedPiecesByBishop = (this.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex) & this.arrayOfAggregateBitboards[this.sideToMove]);

                // Removes potentially pinned pieces from the all pieces bitboard, and generates rook moves from king again
                // Intersect with other side's bishop and queen to get bitboard of potential pinners
                Bitboard tempAllPieceExceptPotentiallyPinnedByBishopBitboard = tempAllPieceBitboard & (~potentiallyPinnedPiecesByBishop);
                Bitboard bishopMovesFromIndexWithoutPinned = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, kingIndex);
                potentialPinners = (bishopMovesFromIndexWithoutPinned & (tempOppBishopAndQueenBitboard));

                // Loop through bitboard of potential pinners and intersect with bitboard of potentially pinned
                while (potentialPinners != 0) {
                    int indexOfPotentialPinner = Constants.findFirstSet(potentialPinners);

                    // Removes the potential pinner from the bitboard
                    potentialPinners &= (potentialPinners - 1);
                    Bitboard pinner = (0x1UL << indexOfPotentialPinner);
                    Bitboard bishopMovesFromPinnerIndex = this.generateBishopMovesFromIndex(tempAllPieceExceptPotentiallyPinnedByBishopBitboard, indexOfPotentialPinner);

                    // If intersection with potentially pinned pieces is not zero, then piece is pinned
                    Bitboard pinnedPiece = (bishopMovesFromPinnerIndex & potentiallyPinnedPiecesByBishop);
                    if (pinnedPiece != 0) {

                        int indexOfPinnedPiece = Constants.findFirstSet(pinnedPiece);
                        int pinnedPieceType = this.pieceArray[indexOfPinnedPiece];

                        // Remove any pinned pieces (they will not be able to block or capture checking piece)
                        if (pinnedPieceType == Constants.WHITE_PAWN + shift) {
                            tempOwnPawnBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_KNIGHT + shift) {
                            tempOwnKnightBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_BISHOP + shift) {
                            tempOwnBishopBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_ROOK + shift) {
                            tempOwnRookBitboard &= (~pinnedPiece);
                        } else if (pinnedPieceType == Constants.WHITE_QUEEN + shift) {
                            tempOwnQueenBitboard &= (~pinnedPiece);
                        }
                    }
                }

                //Calculates the squares that pieces can move to in order to capture or block the checking piece
                Bitboard checkingPieceBitboard = this.getBitboardOfAttackers(this.sideToMove, kingIndex, this.arrayOfAggregateBitboards[Constants.ALL]);
                int indexOfCheckingPiece = Constants.findFirstSet(checkingPieceBitboard);
                Bitboard blockOrCaptureSquares = 0x0UL;

                // If the checking piece is a pawn or knight, then can only capture (no interpositions)
                // If checking piece is a bishop, rook, or queen, then can capture or interpose
                if (pieceArray[indexOfCheckingPiece] == Constants.BLACK_PAWN - shift) {
                    blockOrCaptureSquares = checkingPieceBitboard;
                } else if (pieceArray[indexOfCheckingPiece] == Constants.BLACK_KNIGHT - shift) {
                    blockOrCaptureSquares = checkingPieceBitboard;
                } else if (pieceArray[indexOfCheckingPiece] == Constants.BLACK_BISHOP - shift) {
                    Bitboard bishopMovesFromChecker = this.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                    blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard));
                } else if (pieceArray[indexOfCheckingPiece] == Constants.BLACK_ROOK - shift) {
                    Bitboard rookMovesFromChecker = this.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                    blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard));
                } else if (pieceArray[indexOfCheckingPiece] == Constants.BLACK_QUEEN - shift) {
                    if ((bishopMovesFromKingPosition & checkingPieceBitboard) != 0) {
                        Bitboard bishopMovesFromChecker = this.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                        blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard));
                    } else if ((rookMovesFromKingPosition & checkingPieceBitboard) != 0) {
                        Bitboard rookMovesFromChecker = this.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                        blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard));
                    }
                }

                // Generates moves as normal, but the piece's move squares are intersected with the block or capture squares
                // Loops through all pawns and generates white pawn moves, captures, and promotions
                while (tempOwnPawnBitboard != 0) {

                    int pawnIndex = Constants.findFirstSet(tempOwnPawnBitboard);
                    tempOwnPawnBitboard &= (tempOwnPawnBitboard - 1);

                    if (pawnIndex >= Constants.singlePushCaptureLower[this.sideToMove] && pawnIndex <= Constants.singlePushCaptureUpper[this.sideToMove]) {

                        Bitboard possiblePawnSingleMoves = (Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, possiblePawnSingleMoves, listOfCheckEvasionMoves, ref index, this.sideToMove);

                        Bitboard possiblePawnCaptures = (Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & blockOrCaptureSquares);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, possiblePawnCaptures, listOfCheckEvasionMoves, ref index, this.sideToMove);
                    } if (pawnIndex >= Constants.doublePushLower[this.sideToMove] && pawnIndex <= Constants.doublePushUpper[this.sideToMove]) {
                        Bitboard singlePawnMovementFromIndex = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex];
                        Bitboard doublePawnMovementFromIndex = Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex + doublePawnMoveShift];
                        Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                        if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                            pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & blockOrCaptureSquares);
                        }

                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pseudoLegalDoubleMoveFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove, Constants.DOUBLE_PAWN_PUSH);
                    } if ((this.enPassantSquare & Constants.enPassantRank[this.sideToMove]) != 0) {
                        if (pawnIndex >= Constants.enPassantLower[this.sideToMove] && pawnIndex <= Constants.enPassantUpper[this.sideToMove]) {
                            Bitboard pseudoLegalEnPassantFromIndex = (Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & this.enPassantSquare);
                            this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pseudoLegalEnPassantFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove, Constants.EN_PASSANT_CAPTURE);
                        }
                    } if (pawnIndex >= Constants.promotionPromotionCaptureLower[this.sideToMove] && pawnIndex <= Constants.promotionPromotionCaptureUpper[this.sideToMove]) {
                        Bitboard pseudoLegalPromotionFromIndex = (Constants.singlePawnMovesAndPromotions[this.sideToMove, pawnIndex] & (~this.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pseudoLegalPromotionFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove, Constants.PROMOTION);

                        Bitboard pseudoLegalPromotionCaptureFromIndex = (Constants.capturesAndCapturePromotions[this.sideToMove, pawnIndex] & (this.arrayOfAggregateBitboards[this.sideToMove ^ 1]) & blockOrCaptureSquares);
                        this.generatePawnKnightBishopRookQueenKingMoves(pawnIndex, pseudoLegalPromotionCaptureFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove, Constants.PROMOTION_CAPTURE);
                    }
                }
                //generates knight moves and captures 
                while (tempOwnKnightBitboard != 0) {
                    int knightIndex = Constants.findFirstSet(tempOwnKnightBitboard);
                    tempOwnKnightBitboard &= (tempOwnKnightBitboard - 1);
                    Bitboard pseudoLegalKnightMovementFromIndex = (Constants.knightMoves[knightIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]) & blockOrCaptureSquares);
                    this.generatePawnKnightBishopRookQueenKingMoves(knightIndex, pseudoLegalKnightMovementFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove);
                }
                //generates bishop moves and captures
                while (tempOwnBishopBitboard != 0) {
                    int bishopIndex = Constants.findFirstSet(tempOwnBishopBitboard);
                    tempOwnBishopBitboard &= (tempOwnBishopBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & blockOrCaptureSquares);
                    this.generatePawnKnightBishopRookQueenKingMoves(bishopIndex, pseudoLegalBishopMovementFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove);
                }
                //generates rook moves and captures
                while (tempOwnRookBitboard != 0) {
                    int rookIndex = Constants.findFirstSet(tempOwnRookBitboard);
                    tempOwnRookBitboard &= (tempOwnRookBitboard - 1);
                    Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & blockOrCaptureSquares);
                    this.generatePawnKnightBishopRookQueenKingMoves(rookIndex, pseudoLegalRookMovementFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove);
                }
                //generates queen moves and captures
                while (tempOwnQueenBitboard != 0) {
                    int queenIndex = Constants.findFirstSet(tempOwnQueenBitboard);
                    tempOwnQueenBitboard &= (tempOwnQueenBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (this.generateBishopMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                    Bitboard pseudoLegalRookMovementFromIndex = (this.generateRookMovesFromIndex(this.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                    Bitboard pseudoLegalQueenMovementFromIndex = ((pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & (~this.arrayOfAggregateBitboards[this.sideToMove]) & blockOrCaptureSquares);
                    this.generatePawnKnightBishopRookQueenKingMoves(queenIndex, pseudoLegalQueenMovementFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove);
                }
                //generates king moves and captures
                Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]);
                this.generatePawnKnightBishopRookQueenKingMoves(kingIndex, pseudoLegalKingMovementFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove);
            }

            // If the king is in double check
            else if (kingCheckStatus == Constants.DOUBLE_CHECK) {

                // Only generates king moves
                Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~this.arrayOfAggregateBitboards[this.sideToMove]);
                this.generatePawnKnightBishopRookQueenKingMoves(kingIndex, pseudoLegalKingMovementFromIndex, listOfCheckEvasionMoves, ref index, this.sideToMove);
            }
            return listOfCheckEvasionMoves;
        }

	    public int[] phasedMoveGenerator(int flag) {
		    int[] move = new int[220];
		    int index = 0;

			if (flag == Constants.PERFT_ALL_MOVES) {
				int[] allMoves = this.moveGenerator(Constants.ALL_MOVES);
				
				for (int i = 0; i < allMoves.Length; i++) {
					if (allMoves[i] != 0) {
						move[index++] = allMoves[i];
					}
				}
		    }

			if (flag == Constants.PERFT_MAIN) {
				int[] mainCapture = this.moveGenerator(Constants.MAIN_CAP_EPCAP_CAPPROMO_PROMO);
				int[] mainQuiet = this.moveGenerator(Constants.MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS);
				
				for (int i = 0; i < mainCapture.Length; i++) {
					if (mainCapture[i] != 0) {
						move[index++] = mainCapture[i];
					}
				}
				for (int i = 0; i < mainQuiet.Length; i++) {
					if (mainQuiet[i] != 0) {
						move[index++] = mainQuiet[i];
					}
				}
		    }

			if (flag == Constants.PERFT_QUIESCENCE) {
				int[] quiescentCapQuietCheck = this.moveGenerator(Constants.QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK);
				int[] quiescentQuietNoCheck = this.moveGenerator(Constants.QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK);
				
				for (int i = 0; i < quiescentCapQuietCheck.Length; i++) {
					if (quiescentCapQuietCheck[i] != 0) {
						move[index++] = quiescentCapQuietCheck[i];
					}
				}
				for (int i = 0; i < quiescentQuietNoCheck.Length; i++) {
					if (quiescentQuietNoCheck[i] != 0) {
						move[index++] = quiescentQuietNoCheck[i];
					}
				}
		    }

		    return move;
	    }
		
		// Generates knight, bisohp, rook, queen, and king moves
		// Takes in the index of the piece, the bitboard of possible move squares
		// Loops through all of the move squares and generates a move int for each, and then adds it to the list of pseudo legal moves
	    private void generatePawnKnightBishopRookQueenKingMoves(int pieceIndex, Bitboard pseudoLegalPieceMovementFromIndex, int[] listOfPseudoLegalMoves, ref int index, int pieceColour, int flag = -1) {
			
			// Loops through all of the bits of the pseudo legal move squares
			while (pseudoLegalPieceMovementFromIndex != 0) {

				// Finds the first bit, and then removes it
				int pieceMoveIndex = Constants.findFirstSet(pseudoLegalPieceMovementFromIndex);
				pseudoLegalPieceMovementFromIndex &= (pseudoLegalPieceMovementFromIndex - 1);

				int moveRepresentation = 0x0;

				// If the first bit corresponds to an empty square, then it is a quiet move, double pawn push, en-passant capture, or promotion (short and long castles are generated above)
				if (this.pieceArray[pieceMoveIndex] == Constants.EMPTY) {
					// If the move is a double pawn push, then have to include that as a flag (so that the en-passant square can be created)
					if (flag == Constants.DOUBLE_PAWN_PUSH) {
						int moveScore = this.moveScoreQuiet(pieceColour, pieceIndex, pieceMoveIndex);
						moveRepresentation = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY, Constants.EMPTY, moveScore);
					} 
					// If move is an en passant, have to set the score as a good capture of a pawn x pawn
					// Also have to set the flag and the piece captured
					else if (flag == Constants.EN_PASSANT_CAPTURE) {
						int moveScore = Constants.GOOD_CAPTURE_SCORE + Constants.MvvLvaScore[Constants.PAWN, Constants.PAWN];
						moveRepresentation = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.EN_PASSANT_CAPTURE, (Constants.PAWN + 6 - 6 * pieceColour), Constants.EMPTY, moveScore);
					} else if (flag == Constants.PROMOTION) {
						int moveScore = this.moveScorePromotion(pieceColour, pieceIndex, pieceMoveIndex);

						int moveRepresentationQueenPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.QUEEN + 6 * pieceColour), moveScore);
						int moveRepresentationRookPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.ROOK + 6 * pieceColour), moveScore);
						int moveRepresentationBishopPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.BISHOP + 6 * pieceColour), moveScore);
						int moveRepresentationKnightPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.KNIGHT + 6 * pieceColour), moveScore);

						listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotion;
						listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotion;
						listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotion;
						listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotion;
					} else if (flag == Constants.QUEEN_PROMOTION) {
						int moveScore = this.moveScorePromotion(pieceColour, pieceIndex, pieceMoveIndex);
						int moveRepresentationQueenPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.QUEEN + 6 * pieceColour), moveScore);
						listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotion;
					} else if (flag == Constants.UNDER_PROMOTION) {
						int moveScore = this.moveScorePromotion(pieceColour, pieceIndex, pieceMoveIndex);

						int moveRepresentationKnightPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.KNIGHT + 6 * pieceColour), moveScore);
						int moveRepresentationBishopPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.BISHOP + 6 * pieceColour), moveScore);
						int moveRepresentationRookPromotion = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.ROOK + 6 * pieceColour), moveScore);

						listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotion;
						listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotion;
						listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotion;
					}else {
						int moveScore = this.moveScoreQuiet(pieceColour, pieceIndex, pieceMoveIndex);
						moveRepresentation = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY, Constants.EMPTY, moveScore);	
					}
				} 
				// If the first bit corresponds to an occupied square, then it is a capture or promotion-capture
				else if (this.pieceArray[pieceMoveIndex] != Constants.EMPTY) {
					if (flag == Constants.PROMOTION_CAPTURE) {
						int moveScore = this.moveScorePromotionCapture(pieceColour, pieceIndex, pieceMoveIndex);

						int moveRepresentationQueenPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.QUEEN + 6 * pieceColour), moveScore);
						int moveRepresentationRookPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.ROOK + 6 * pieceColour), moveScore);
						int moveRepresentationBishopPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.BISHOP + 6 * pieceColour), moveScore);
						int moveRepresentationKnightPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.KNIGHT + 6 * pieceColour), moveScore);
						
						listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotionCapture;
						listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotionCapture;
						listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotionCapture;
						listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotionCapture;
					} else if (flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						int moveScore = this.moveScorePromotionCapture(pieceColour, pieceIndex, pieceMoveIndex);
						int moveRepresentationQueenPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.QUEEN + 6 * pieceColour), moveScore);
						listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotionCapture;
					} else if (flag == Constants.UNDER_PROMOTION_CAPTURE) {
						int moveScore = this.moveScorePromotionCapture(pieceColour, pieceIndex, pieceMoveIndex);

						int moveRepresentationRookPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.ROOK + 6 * pieceColour), moveScore);
						int moveRepresentationBishopPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.BISHOP + 6 * pieceColour), moveScore);
						int moveRepresentationKnightPromotionCapture = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pieceMoveIndex], (Constants.KNIGHT + 6 * pieceColour), moveScore);						

						listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotionCapture;
						listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotionCapture;
						listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotionCapture;	
					} else {
						int moveScore = this.moveScoreCapture(pieceColour, pieceIndex, pieceMoveIndex);
						moveRepresentation = this.moveEncoder(pieceIndex, pieceMoveIndex, Constants.CAPTURE, pieceArray[pieceMoveIndex], Constants.EMPTY, moveScore);
					}	
				}
				// Only add if it is not a promotion or promotion capture
				// For promotions and promotion captures, the name of the move variable is different
				// Also it is added to the list within the conditional
				// If it is added here, would be adding a 0 to the list, which would cause the search and perft to think that there are no moves left
				if (flag != Constants.PROMOTION_CAPTURE && 
					flag != Constants.QUEEN_PROMOTION_CAPTURE &&
					flag != Constants.UNDER_PROMOTION_CAPTURE &&
					flag != Constants.PROMOTION &&
					flag != Constants.QUEEN_PROMOTION &&
					flag != Constants.UNDER_PROMOTION) {
					listOfPseudoLegalMoves[index++] = moveRepresentation;
				}
			}
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

		// Generates the move score for quiet moves
	    private int moveScoreQuiet(int pieceColour, int pieceIndex, int pieceMoveIndex) {
			
			int moveScore = 0;
			
			/*
			if (this.timesSquareIsAttacked(pieceColour, pieceMoveIndex) == 0 || this.staticExchangeEval(pieceIndex, pieceMoveIndex, pieceColour) >= 0) {
				moveScore += Constants.GOOD_QUIET_SCORE;
			} else {
				moveScore += Constants.BAD_QUIET_SCORE;
			}*/
		    return moveScore;
	    }

		// Generates the move score for captures
	    private int moveScoreCapture(int pieceColour, int pieceIndex, int pieceMoveIndex) {
			int moveScore = Constants.MvvLvaScore[pieceArray[pieceMoveIndex], pieceArray[pieceIndex]];
			if (this.staticExchangeEval(pieceIndex, pieceMoveIndex, pieceColour) >= 0) {
				moveScore += Constants.GOOD_CAPTURE_SCORE;
			} else {
				moveScore += Constants.BAD_CAPTURE_SCORE;
			}
		    return moveScore;
	    }

		// Generates the move score for promotion captures
	    private int moveScorePromotionCapture(int pieceColour, int pieceIndex, int pieceMoveIndex) {
			int moveScore = 0;
			if (this.staticExchangeEval(pieceIndex, pieceMoveIndex, pieceColour) >= 0) {
				moveScore += Constants.GOOD_PROMOTION_CAPTURE_SCORE;
			} else {
				moveScore += Constants.BAD_PROMOTION_CAPTURE_SCORE;
			}
		    return moveScore;
	    }

		// Generates the move score for promotions
	    private int moveScorePromotion(int pieceColour, int pieceIndex, int pieceMoveIndex) {
			int moveScore = 0;
			if (this.staticExchangeEval(pieceIndex, pieceMoveIndex, pieceColour) >= 0) {
				moveScore += Constants.GOOD_PROMOTION_SCORE;
			} else {
				moveScore += Constants.BAD_PROMOTION_SCORE;
			}
		    return moveScore;
	    }

        //OTHER METHODS----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        // initializes the piece Square tables (copy the values for white from the table in Utilities.cs, then generate the values for black by flipping and changing the sign)
        internal static void initialize() {
            
            for (int pType = 0; pType < 13; pType++) {
                pieceSquareTable[pType] = new Int32[64];
            }

            for (int pType = Constants.PAWN; pType <= Constants.KING; pType++) {
                Int32 matValue = Utilities.makeScore(pieceValueMidgame[pType], pieceValueEndgame[pType]);

                for (int sq = Constants.H1; sq <= Constants.A8; sq++) {
                    pieceSquareTable[Utilities.makePieceIndex(Constants.WHITE, pType)][sq] = (matValue + Utilities.PIECE_SQUARE_TABLE[pType][sq]);
                    pieceSquareTable[Utilities.makePieceIndex(Constants.BLACK, pType)][Utilities.flipSquare(sq)] = -(matValue + Utilities.PIECE_SQUARE_TABLE[pType][sq]);
                }
            }
        }

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

            this.material_PSQ_Value = 0;

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

        // Returns an integer specifying the material and piece square scores (midgame and endgame values encoded in one integer)
        // Called when a new position is set up
        internal void computeMatPSQScore() {
            Int32 score = 0;
            UInt64 occupied = this.arrayOfAggregateBitboards[Constants.ALL];

            for (UInt64 i = occupied; i != 0;) {
                Int32 square = Constants.findFirstSet(i);
                score += pieceSquareTable[this.pieceArray[square]][square];
                i &= (i - 1);
            }
            this.material_PSQ_Value += score;
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

        internal static Int32 updatePieceSquare(Int32 piece, Int32 fromSquare, Int32 toSquare) {
            return pieceSquareTable[piece][toSquare] - pieceSquareTable[piece][fromSquare];
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
		private int moveEncoder(int startSquare, int destinationSquare, int flag, int pieceCaptured, int piecePromoted, int score = 0x0) {
            int moveRepresentation = ((startSquare << Constants.START_SQUARE_SHIFT) | (destinationSquare << Constants.DESTINATION_SQUARE_SHIFT) | (flag << Constants.FLAG_SHIFT) | (pieceCaptured << Constants.PIECE_CAPTURED_SHIFT) | (piecePromoted << Constants.PIECE_PROMOTED_SHIFT) | (score << Constants.MOVE_SCORE_SHIFT));
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

	    internal bool isDraw() {
		    if (Search.board.fiftyMoveRule >= 100
		        || Search.board.getRepetitionNumber() > 1) {
			    return true;
		    }
		    return false;
	    }
	}
}
