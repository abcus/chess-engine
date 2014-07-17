using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
   
    public sealed class Board {

        //INSTANCE VARIABLES-----------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Variables that uniquely describe a board state

        //bitboards and array to hold bitboards
        internal ulong[] arrayOfBitboards = new ulong[12];
        internal ulong wPawn = 0x0UL;
        internal ulong wKnight = 0x0UL;
        internal ulong wBishop = 0x0UL;
        internal ulong wRook = 0x0UL;
        internal ulong wQueen = 0x0UL;
        internal ulong wKing = 0x0UL;
        internal ulong bPawn = 0x0UL;
        internal ulong bKnight = 0x0UL;
        internal ulong bBishop = 0x0UL;
        internal ulong bRook = 0x0UL;
        internal ulong bQueen = 0x0UL;
        internal ulong bKing = 0x0UL;

        //aggregate bitboards and array to hold aggregate bitboards
        internal ulong[] arrayOfAggregateBitboards = new ulong[3];
        internal ulong whitePieces = 0x0UL;
        internal ulong blackPieces = 0x0UL;
        internal ulong allPieces = 0x0UL;

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
        internal int moveNumber = 0;
        internal int HalfMovesSincePawnMoveOrCapture = 0;
        internal int repetionOfPosition = 0;

        //Variables that can be calculated
        internal int blackInCheck = 0;
        internal int blackInCheckmate = 0;
        internal int whiteInCheck = 0;
        internal int whiteInCheckmate = 0;
        internal int stalemate = 0;

        internal Boolean endGame = false;

        internal uint lastMove = 0x0;

        internal int evaluationFunctionValue = 0;

        internal ulong zobristKey = 0x0UL;

        //CONSTRUCTOR------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Constructor that sets up the board to the default starting position
        public Board() {
        }

        //Constructor that takes in a FEN string and generates the appropriate board position
        public Board(string FEN) {
            FENToBoard(FEN);
        }

		//MAKE MOVE METHODS-------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

		//Static method that makes a move by updating the board object's instance variables
		public int makeMove(int moveRepresentationInput) {

			//stores the board restore data in a 32-bit unsigned integer
			int boardRestoreData = 0x0;
			
			//Encodes en passant square
			//Calculates the en passant square number (if any) from the en passant bitboard
			//If there is no en-passant square, then we set the bits corresponding to that variable to 63 (the maximum value for 6 bits)
			if (Constants.bitScan(enPassantSquare).Count == 0) {
				boardRestoreData |= 63 << 6;
			} else {
				boardRestoreData |= (Constants.bitScan(enPassantSquare).ElementAt(0) << 6);
			}

			//encodes castling rights
			boardRestoreData |= sideToMove << 0;
			boardRestoreData |= whiteShortCastleRights << 2;
			boardRestoreData |= whiteLongCastleRights << 3;
			boardRestoreData |= blackShortCastleRights << 4;
			boardRestoreData |= blackLongCastleRights << 5;

			//encodes repetition number
			boardRestoreData |= repetionOfPosition << 12;

			//encodes half-move clock
			//If there is no half-move clock, then we set the bits corresponding to that variable to 63 (the maximum value for 6 bits)
			if (HalfMovesSincePawnMoveOrCapture == -1) {
				boardRestoreData |= 63 << 14;
			} else {
				boardRestoreData |= HalfMovesSincePawnMoveOrCapture << 14;
			}

			//encodes move number
			//If there is no move number, then we set the bits corresponding to that variable to 2047 (the maximum value for 11 bits)
			if (moveNumber == -1) {
				boardRestoreData |= 2047 << 20;
			} else {
				boardRestoreData |= moveNumber << 20;
			}

			//Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
			int pieceMoved = ((moveRepresentationInput & 0xF) >> 0);
			int startSquare = ((moveRepresentationInput & 0x3F0) >> 4);
			int destinationSquare = ((moveRepresentationInput & 0xFC00) >> 10);
			int flag = ((moveRepresentationInput & 0xF0000) >> 16);
			int pieceCaptured;
			if (((moveRepresentationInput & 0xF00000) >> 20) == 15) {
				pieceCaptured = 0;
			} else {
				pieceCaptured = ((moveRepresentationInput & 0xF00000) >> 20);
			}

			//Calculates bitboards for removing piece from start square and adding piece to destionation square
			//"and" with startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
			ulong startSquareBitboard = (0x1UL << startSquare);
			ulong destinationSquareBitboard = (0x1UL << destinationSquare);

			//If quiet move, updates the piece's bitboard and piece array
			//If capture, also updates the captured piece's bitboard
			//If en-passant capture, also updates the captured pawn's bitboard and array
			//Also updates the castling rights instance variable for rook/king moves, sets en passant square to 0
			if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.EN_PASSANT_CAPTURE) {

				//Updates the castling rights instance variables if either the king or rook were moved
				if (pieceMoved == Constants.WHITE_KING) {
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceMoved == Constants.BLACK_KING) {
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceMoved == Constants.WHITE_ROOK && startSquare == Constants.A1) {
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceMoved == Constants.WHITE_ROOK && startSquare == Constants.H1) {
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceMoved == Constants.BLACK_ROOK && startSquare == Constants.A8) {
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceMoved == Constants.BLACK_ROOK && startSquare == Constants.H8) {
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
				}

				//Updates the castling rights instance variables if the rook got captured
				if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.A1) {
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.H1) {
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.A8) {
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.H8) {
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
				}

				//sets the en Passant square to 0x0UL;
				this.enPassantSquare = 0x0UL;

				//Removes the bit corresponding to the start square, and adds a bit corresponding with the destination square
				//updates the bitboard 
				this.arrayOfBitboards[pieceMoved - 1] &= (~startSquareBitboard);
				this.arrayOfBitboards[pieceMoved - 1] |= destinationSquareBitboard;
				//Removes the int representing the piece from the start square of the piece array, and adds an int representing the piece to the destination square of the piece array
				this.pieceArray[startSquare] = Constants.EMPTY;
				this.pieceArray[destinationSquare] = pieceMoved;

				//If there was a capture, remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
				//Don't have to update the array because it was already overridden with the capturing piece
				if (flag == Constants.CAPTURE) {
					this.arrayOfBitboards[pieceCaptured - 1] &= (~destinationSquareBitboard);
					}

				//If there was an en-passant capture, remove the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
				//Update the array because the pawn destination square and captured pawn are on different squares
				if (flag == Constants.EN_PASSANT_CAPTURE) {
					if (pieceMoved == Constants.WHITE_PAWN) {
						this.arrayOfBitboards[Constants.BLACK_PAWN - 1] &= (~(destinationSquareBitboard >> 8));
						this.pieceArray[destinationSquare - 8] = Constants.EMPTY;
					} else if (pieceMoved == Constants.BLACK_PAWN) {
						this.arrayOfBitboards[Constants.WHITE_PAWN - 1] &= (~(destinationSquareBitboard << 8));
						this.pieceArray[destinationSquare + 8] = Constants.EMPTY;
					}
				}
			}
				//Updates the pawn bitboard and piece array
				//Also updates the en passant instance variables
			else if (flag == Constants.DOUBLE_PAWN_PUSH) {

				//Updates the en passant square instance variable
				if (pieceMoved == Constants.WHITE_PAWN) {
					enPassantSquare = (0x1UL << (destinationSquare - 8));
				} else if (pieceMoved == Constants.BLACK_PAWN) {
					enPassantSquare = (0x1UL << (destinationSquare + 8));
				}

				//Removes the bit corresponding to the start square, and adds a bit corresponding with the destination square
				this.arrayOfBitboards[pieceMoved - 1] &= ~startSquareBitboard;
				this.arrayOfBitboards[pieceMoved - 1] |= destinationSquareBitboard;
				//Removes the int representing the pawn from the start square of the piece array, and adds an int representing the pawn to the destination square of the piece array
				this.pieceArray[startSquare] = Constants.EMPTY;
				this.pieceArray[destinationSquare] = pieceMoved;
				//updates the bitboard and the piece array
			}

			//Updates the king and rook bitboard and piece array
				//Also updates the castling instance variable, sets en passant square to 0
			else if (flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {

				//Updates the castling rights instance variables (sets them to false)
				if (pieceMoved == Constants.WHITE_KING) {
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceMoved == Constants.BLACK_KING) {
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
				}
				//sets the en Passant square to 0x0UL;
				this.enPassantSquare = 0x0UL;

				//Removes the bit corresponding to the start square, and adds a bit corresponding with the destination square
				this.arrayOfBitboards[pieceMoved - 1] &= ~startSquareBitboard;
				this.arrayOfBitboards[pieceMoved - 1] |= destinationSquareBitboard;
				//Removes the int representing the king from the start square of the piece array, and adds an int representing the king to the destination square of the piece array
				this.pieceArray[startSquare] = Constants.EMPTY;
				this.pieceArray[destinationSquare] = pieceMoved;
				//updates the bitboard and the piece array

				if (pieceMoved == Constants.WHITE_KING) {

					//If short castle, then move the rook from H1 to F1
					if (flag == Constants.SHORT_CASTLE) {
						ulong rookStartSquareBitboard = (0x1UL << Constants.H1);
						ulong rookDestinationSquareBitboard = 0x1UL << Constants.F1;

						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~rookStartSquareBitboard);
						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= rookDestinationSquareBitboard;
						
						this.pieceArray[Constants.H1] = Constants.EMPTY;
						this.pieceArray[Constants.F1] = Constants.WHITE_ROOK;

					}
						//If long castle, then move the rook from A1 to D1
					else if (flag == Constants.LONG_CASTLE) {
						ulong rookStartSquareBitboard = (0x1UL << Constants.A1);
						ulong rookDestinationSquareBitboard = 0x1UL << Constants.D1;

						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~rookStartSquareBitboard);
						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= rookDestinationSquareBitboard;
						
						this.pieceArray[Constants.A1] = Constants.EMPTY;
						this.pieceArray[Constants.D1] = Constants.WHITE_ROOK;
					}

				} else if (pieceMoved == Constants.BLACK_KING) {

					//If short castle, then move the rook from H8 to F8
					if (flag == Constants.SHORT_CASTLE) {
						ulong rookStartSquareBitboard = (0x1UL << Constants.H8);
						ulong rookDestinationSquareBitboard = 0x1UL << Constants.F8;

						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(rookStartSquareBitboard);
						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= rookDestinationSquareBitboard;
						
						this.pieceArray[Constants.H8] = Constants.EMPTY;
						this.pieceArray[Constants.F8] = Constants.BLACK_ROOK;
					}
						//If long castle, then move the rook from A8 to D8
					else if (flag == Constants.LONG_CASTLE) {
						ulong rookStartSquareBitboard = (0x1UL << Constants.A8);
						ulong rookDestinationSquareBitboard = 0x1UL << Constants.D8;

						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(rookStartSquareBitboard);
						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= rookDestinationSquareBitboard;
						
						this.pieceArray[Constants.A8] = Constants.EMPTY;
						this.pieceArray[Constants.D8] = Constants.BLACK_ROOK;
					}
				}
			}

			//If regular promotion, updates the pawn's bitboard, the promoted piece bitboard, and the piece array
				//If capture-promotion, also updates the captured piece's bitboard
				//sets en passant square to 0
			else if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.BISHOP_PROMOTION || flag == Constants.ROOK_PROMOTION
				|| flag == Constants.QUEEN_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE
				|| flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {

				//Updates the castling rights instance variables if the rook got captured
				if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.A1) {
					this.whiteLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.H1) {
					this.whiteShortCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.A8) {
					this.blackLongCastleRights = Constants.CANNOT_CASTLE;
				} else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.H8) {
					this.blackShortCastleRights = Constants.CANNOT_CASTLE;
				}

				//sets the en Passant square to 0x0UL;
				this.enPassantSquare = 0x0UL;

				if (pieceMoved == Constants.WHITE_PAWN) {
					//Removes the bit corresponding to the start square 
					this.arrayOfBitboards[Constants.WHITE_PAWN - 1] &= (~startSquareBitboard);
					this.pieceArray[startSquare] = Constants.EMPTY;

					//Adds a bit corresponding to the destination square in the promoted piece bitboard
					if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_KNIGHT - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.WHITE_KNIGHT;
					} else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_BISHOP - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.WHITE_BISHOP;
					} else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.WHITE_ROOK;
					} else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_QUEEN - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.WHITE_QUEEN;
					}

					//If there was a capture, remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
					//Don't have to update the array because it was already overridden with the capturing piece
					if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[pieceCaptured - 1] &= (~destinationSquareBitboard);
						}

				} else if (pieceMoved == Constants.BLACK_PAWN) {
					//Removes the bit corresponding to the start square 
					this.arrayOfBitboards[Constants.BLACK_PAWN - 1] &= (~startSquareBitboard);
					this.pieceArray[startSquare] = Constants.EMPTY;

					//Adds a bit corresponding to the destination square in the promoted piece bitboard
					if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_KNIGHT - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.BLACK_KNIGHT;
					} else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_BISHOP - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.BLACK_BISHOP;
					} else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.BLACK_ROOK;
					} else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_QUEEN - 1] |= destinationSquareBitboard;
						this.pieceArray[destinationSquare] = Constants.BLACK_QUEEN;
					}

					//If there was a capture, remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
					//Don't have to update the array because it was already overridden with the capturing piece
					if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[pieceCaptured - 1] &= (~destinationSquareBitboard);
						}
				}
			}

			//sets the side to move to the other player
			if (sideToMove == Constants.WHITE) {
				sideToMove = Constants.BLACK;
			} else if (sideToMove == Constants.BLACK) {
				sideToMove = Constants.WHITE;
			}

			//updates the aggregate bitboards
			this.arrayOfAggregateBitboards[Constants.WHITE_PIECES] = (this.arrayOfBitboards[Constants.WHITE_PAWN - 1] | this.arrayOfBitboards[Constants.WHITE_KNIGHT - 1] | this.arrayOfBitboards[Constants.WHITE_BISHOP - 1] | this.arrayOfBitboards[Constants.WHITE_ROOK - 1] | this.arrayOfBitboards[Constants.WHITE_QUEEN - 1] | this.arrayOfBitboards[Constants.WHITE_KING - 1]);
			this.arrayOfAggregateBitboards[Constants.BLACK_PIECES] = (this.arrayOfBitboards[Constants.BLACK_PAWN - 1] | this.arrayOfBitboards[Constants.BLACK_KNIGHT - 1] | this.arrayOfBitboards[Constants.BLACK_BISHOP - 1] | this.arrayOfBitboards[Constants.BLACK_ROOK - 1] | this.arrayOfBitboards[Constants.BLACK_QUEEN - 1] | this.arrayOfBitboards[Constants.BLACK_KING - 1]);
			this.arrayOfAggregateBitboards[Constants.ALL_PIECES] = (this.arrayOfAggregateBitboards[Constants.WHITE_PIECES] | this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);

			//increments the full-move number (implement later)
			//Implement the half-move clock later (in the moves)
			//Also implement the repetitions later

			//returns the board restore data
			return boardRestoreData;
		}

		//UNMAKE MOVE METHODS--------------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Method that unmakes a move by restoring the board object's instance variables
	    public void unmakeMove(int unmoveRepresentationInput, int boardRestoreDataRepresentation) {

		    //Sets the side to move, white short/long castle rights, black short/long castle rights from the integer encoding the restore board data
		    this.sideToMove = ((boardRestoreDataRepresentation & 0x3) >> 0);
			this.whiteShortCastleRights = ((boardRestoreDataRepresentation & 0x4) >> 2);
			this.whiteLongCastleRights = ((boardRestoreDataRepresentation & 0x8) >> 3);
			this.blackShortCastleRights = ((boardRestoreDataRepresentation & 0x10) >> 4);
			this.blackLongCastleRights = ((boardRestoreDataRepresentation & 0x20) >> 5);

		    //Sets the en passant square from the integer encoding the restore board data
		    //If we extract 63, then we know that there was no en passant square and set the instance variable for EQ square to 0x0UL
		    if (((boardRestoreDataRepresentation & 0xFC0) >> 6) == 63) {
				this.enPassantSquare = 0x0UL;
		    } else {
			    this.enPassantSquare = 0x1UL << ((boardRestoreDataRepresentation & 0xFC0) >> 6);
		    }
			
		    //Sets the repetition number from the integer encoding the restore board data
		    this.repetionOfPosition = ((boardRestoreDataRepresentation & 0x3000) >> 12);

		    //Sets the half move clock (since last pawn push/capture) from the integer encoding the restore board data
		    //If we extract 63, then we know that there was no half-move clock and return -1 (original value of instance variable)
		    if (((boardRestoreDataRepresentation & 0xFC000) >> 14) == 63) {
			    this.HalfMovesSincePawnMoveOrCapture = -1;
		    } else {
			    this.HalfMovesSincePawnMoveOrCapture = ((boardRestoreDataRepresentation & 0xFC000) >> 14);
		    }

		    //Sets the move number from the integer encoding the restore board data
		    //If we extract 4095, then we know that there was no move number and return -1 (original value of instance variable)
		    if (((boardRestoreDataRepresentation & 0x7FF00000) >> 20) == 2047) {
			    this.moveNumber = -1;
		    } else {
			    this.moveNumber = ((boardRestoreDataRepresentation & 0x7FF00000) >> 20);
		    }

		    //Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
		    int pieceMoved = ((unmoveRepresentationInput & 0xF) >> 0);
		    int startSquare = ((unmoveRepresentationInput & 0x3F0) >> 4);
		    int destinationSquare = ((unmoveRepresentationInput & 0xFC00) >> 10);
		    int flag = ((unmoveRepresentationInput & 0xF0000) >> 16);
		    int pieceCaptured;
		    if (((unmoveRepresentationInput & 0xF00000) >> 20) == 15) {
			    pieceCaptured = 0;
		    } else {
			    pieceCaptured = ((unmoveRepresentationInput & 0xF00000) >> 20);
		    }

		    //Calculates bitboards for removing piece from start square and adding piece to destionation square
		    //"and" with startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
		    ulong startSquareBitboard = (0x1UL << startSquare);
		    ulong destinationSquareBitboard = (0x1UL << destinationSquare);

		    if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.EN_PASSANT_CAPTURE
		        || flag == Constants.DOUBLE_PAWN_PUSH) {

			    //Removes the bit corresponding to the destination square, and adds a bit corresponding with the start square (to unmake move)
				this.arrayOfBitboards[pieceMoved - 1] &= (~destinationSquareBitboard);
				this.arrayOfBitboards[pieceMoved - 1] |= (startSquareBitboard);
			    
			    //Removes the int representing the piece from the destination square of the piece array, and adds an int representing the piece to the start square of the piece array (to unmake move)
			    this.pieceArray[destinationSquare] = Constants.EMPTY;
			    this.pieceArray[startSquare] = pieceMoved;
			    
				//If there was a capture, add to the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
			    //Also re-add the captured piece to the array
			    if (flag == Constants.CAPTURE) {
					this.arrayOfBitboards[pieceCaptured - 1] |= (destinationSquareBitboard);
					this.pieceArray[destinationSquare] = pieceCaptured;
			    }
				    //If there was an en-passant capture, add the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
				    //Also re-add teh captured pawn to the array 
			    else if (flag == Constants.EN_PASSANT_CAPTURE) {
				    if (pieceMoved == Constants.WHITE_PAWN) {
						this.arrayOfBitboards[Constants.BLACK_PAWN-1] |= (destinationSquareBitboard >> 8);
						this.pieceArray[destinationSquare - 8] = Constants.BLACK_PAWN;
					}
				    else if (pieceMoved == Constants.BLACK_PAWN) {
					    this.arrayOfBitboards[Constants.WHITE_PAWN - 1] |= (destinationSquareBitboard << 8);
						this.pieceArray[destinationSquare + 8] = Constants.WHITE_PAWN;
					}
			    }
		    }
		    else if (flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {

			    //Removes the bit corresponding to the DESTINATION square, and adds a bit corresponding with the start square
			    this.arrayOfBitboards[pieceMoved - 1] &= (~destinationSquareBitboard);
				this.arrayOfBitboards[pieceMoved - 1] |= startSquareBitboard;
				
			    //Removes the int representing the king from the start square of the piece array, and adds an int representing the king to the destination square of the piece array
			    this.pieceArray[destinationSquare] = Constants.EMPTY;
			    this.pieceArray[startSquare] = pieceMoved;

			    if (pieceMoved == Constants.WHITE_KING) {
				    
				    //If short castle, then move the rook from F1 to H1
				    if (flag == Constants.SHORT_CASTLE) {
					    ulong rookStartSquareBitboard = (0x1UL << Constants.H1);
					    ulong rookDestinationSquareBitboard = (0x1UL << Constants.F1);

						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~rookDestinationSquareBitboard);
						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= rookStartSquareBitboard;
						
					    this.pieceArray[Constants.F1] = Constants.EMPTY;
					    this.pieceArray[Constants.H1] = Constants.WHITE_ROOK;

				    }
					    //If long castle, then move the rook from D1 to A1
				    else if (flag == Constants.LONG_CASTLE) {
					    ulong rookStartSquareBitboard = (0x1UL << Constants.A1);
					    ulong rookDestinationSquareBitboard = 0x1UL << Constants.D1;

						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~rookDestinationSquareBitboard);
						this.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= rookStartSquareBitboard;
						
					    this.pieceArray[Constants.D1] = Constants.EMPTY;
					    this.pieceArray[Constants.A1] = Constants.WHITE_ROOK;
				    }
			    }
			    else if (pieceMoved == Constants.BLACK_KING) {
				    
				    //If short castle, then move the rook from F8 to H8
				    if (flag == Constants.SHORT_CASTLE) {
					    ulong rookStartSquareBitboard = (0x1UL << Constants.H8);
					    ulong rookDestinationSquareBitboard = 0x1UL << Constants.F8;

						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(rookDestinationSquareBitboard);
						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= rookStartSquareBitboard;
						
					    pieceArray[Constants.F8] = Constants.EMPTY;
					    pieceArray[Constants.H8] = Constants.BLACK_ROOK;
				    }
					    //If long castle, then move the rook from D8 to A8
				    else if (flag == Constants.LONG_CASTLE) {
					    ulong rookStartSquareBitboard = (0x1UL << Constants.A8);
					    ulong rookDestinationSquareBitboard = 0x1UL << Constants.D8;

						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(rookDestinationSquareBitboard);
						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= rookStartSquareBitboard;
						
					    pieceArray[Constants.D8] = Constants.EMPTY;
					    pieceArray[Constants.A8] = Constants.BLACK_ROOK;
				    }
			    }
		    }
		    else if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.BISHOP_PROMOTION ||
		             flag == Constants.ROOK_PROMOTION
		             || flag == Constants.QUEEN_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE ||
		             flag == Constants.BISHOP_PROMOTION_CAPTURE
		             || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {

			    if (pieceMoved == Constants.WHITE_PAWN) {
				    //Adds the bit corresponding to the start square 
					this.arrayOfBitboards[pieceMoved - 1] |= startSquareBitboard;
					this.pieceArray[startSquare] = pieceMoved;

				    //removes a bit corresponding to the destination square in the promoted piece bitboard
				    if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_KNIGHT - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    } else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_BISHOP - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    } else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
					    this.arrayOfBitboards[Constants.WHITE_ROOK - 1]&= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    } else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.WHITE_QUEEN - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    }

				    //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
				    //Also adds the captured piece back to the array
				    if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE ||
				        flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[pieceCaptured - 1] |= (destinationSquareBitboard);
						this.pieceArray[destinationSquare] = pieceCaptured;
				    }
			    }
			    else if (pieceMoved == Constants.BLACK_PAWN) {
				    //Adds the bit corresponding to the start square 
					this.arrayOfBitboards[pieceMoved - 1] |= (startSquareBitboard);
					this.pieceArray[startSquare] = pieceMoved;

				    //removes the bit corresponding to the destination square in the promoted piece bitboard
				    if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_KNIGHT - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    } else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_BISHOP - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    } else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    } else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[Constants.BLACK_QUEEN - 1] &= (~destinationSquareBitboard);
						this.pieceArray[destinationSquare] = Constants.EMPTY;
				    }

				    //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
				    //Also adds the captured piece back to the array
				    if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE ||
				        flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
						this.arrayOfBitboards[pieceCaptured - 1] |= destinationSquareBitboard;
					    this.pieceArray[destinationSquare] = pieceCaptured;
				    }
			    }
		    }
			//updates the aggregate bitboards
			this.arrayOfAggregateBitboards[Constants.WHITE_PIECES] = (this.arrayOfBitboards[Constants.WHITE_PAWN - 1] | this.arrayOfBitboards[Constants.WHITE_KNIGHT - 1] | this.arrayOfBitboards[Constants.WHITE_BISHOP - 1] | this.arrayOfBitboards[Constants.WHITE_ROOK - 1] | this.arrayOfBitboards[Constants.WHITE_QUEEN - 1] | this.arrayOfBitboards[Constants.WHITE_KING - 1]);
			this.arrayOfAggregateBitboards[Constants.BLACK_PIECES] = (this.arrayOfBitboards[Constants.BLACK_PAWN - 1] | this.arrayOfBitboards[Constants.BLACK_KNIGHT - 1] | this.arrayOfBitboards[Constants.BLACK_BISHOP - 1] | this.arrayOfBitboards[Constants.BLACK_ROOK - 1] | this.arrayOfBitboards[Constants.BLACK_QUEEN - 1] | this.arrayOfBitboards[Constants.BLACK_KING - 1]);
			this.arrayOfAggregateBitboards[Constants.ALL_PIECES] = (this.arrayOfAggregateBitboards[Constants.WHITE_PIECES] | this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
	    }

		//METHOD THAT RETURNS HOW MANY TIMES A CERTAIN SQUARE IS ATTACKED--------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

		public int timesSquareIsAttacked(int colourUnderAttack, int squareToCheck) {

			int numberOfTimesAttacked = 0;
			ulong bitboardOfAttackers = 0x0UL;

			if (colourUnderAttack == Constants.WHITE) {

				//Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
				ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.rookOccupancyMask[squareToCheck];
				int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
				ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

				//Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
				ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.bishopOccupancyMask[squareToCheck];
				int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
				ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

				//Looks up knight attack set from square, and intersects with opponent's knight bitboard
				ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

				//Looks up white pawn attack set from square, and intersects with opponent's pawn bitboard
				ulong whitePawnMoveFromSquare = Constants.whiteCapturesAndCapturePromotions[squareToCheck];

				//Looks up king attack set from square, and intersects with opponent's king bitboard
				ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

				bitboardOfAttackers |= ((rookMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_ROOK - 1])
									| (rookMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_QUEEN - 1])
									| (bishopMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_BISHOP - 1])
									| (bishopMovesFromSquare & this.arrayOfBitboards[Constants.BLACK_QUEEN - 1])
									| (knightMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_KNIGHT - 1])
									| (whitePawnMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_PAWN - 1])
									| (kingMoveFromSquare & this.arrayOfBitboards[Constants.BLACK_KING - 1]));

				if (bitboardOfAttackers == 0) {
					return 0;
				} else {
					numberOfTimesAttacked = Constants.popcount(bitboardOfAttackers);
					return numberOfTimesAttacked;
				}

			} else if (colourUnderAttack == Constants.BLACK) {

				//Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
				ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.rookOccupancyMask[squareToCheck];
				int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
				ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

				//Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
				ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.bishopOccupancyMask[squareToCheck];
				int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
				ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

				//Looks up knight attack set from square, and intersects with opponent's knight bitboard
				ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

				//Looks up black pawn attack set from square, and intersects with opponent's pawn bitboard
				ulong blackPawnMoveFromSquare = Constants.blackCapturesAndCapturePromotions[squareToCheck];

				//Looks up king attack set from square, and intersects with opponent's king bitboard
				ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

				bitboardOfAttackers = ((rookMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_ROOK - 1])
				                       | (rookMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_QUEEN - 1])
				                       | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_BISHOP - 1])
				                       | (bishopMovesFromSquare & this.arrayOfBitboards[Constants.WHITE_QUEEN - 1])
				                       | (knightMoveFromSquare & this.arrayOfBitboards[Constants.WHITE_KNIGHT - 1])
				                       | (blackPawnMoveFromSquare & this.arrayOfBitboards[Constants.WHITE_PAWN - 1])
				                       | (kingMoveFromSquare & this.arrayOfBitboards[Constants.WHITE_KING - 1]));

				if (bitboardOfAttackers == 0) {
					return 0;
				} else {
					numberOfTimesAttacked = Constants.popcount(bitboardOfAttackers);
					return numberOfTimesAttacked;
				}
			}
			return 0;
		}
		
		//METHOD THAT RETURNS WHETHER THE KING IS IN CHECK----------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

		public int kingInCheck(int colourOfKingUnderAttack) {

			int numberOfChecks = 0;

			if (colourOfKingUnderAttack == Constants.WHITE) {

				//determines the index of the white king
				int indexOfWhiteKing = Constants.bitScan(this.arrayOfBitboards[Constants.WHITE_KING - 1]).ElementAt(0);

				//Passes this to the times square attacked method
				numberOfChecks = this.timesSquareIsAttacked(Constants.WHITE, indexOfWhiteKing);

				//If number of checks is 0, then the king is not in check
				//If number of checks is 1, then king is in check
				//If number of checks is 2, then king is in double-check
				if (numberOfChecks == 0) {
					return Constants.NOT_IN_CHECK;
				} else if (numberOfChecks == 1) {
					return Constants.CHECK;
				} else if (numberOfChecks == 2) {
					return Constants.DOUBLE_CHECK;
				} else {
					return Constants.MULTIPLE_CHECK;
				}
			} else if (colourOfKingUnderAttack == Constants.BLACK) {

				//determines the index of the black king
				int indexOfBlackKing = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_KING - 1]).ElementAt(0);

				//Passes this to the times square attacked method
				numberOfChecks = this.timesSquareIsAttacked(Constants.BLACK, indexOfBlackKing);

				//If number of checks is 0, then the king is not in check
				//If number of checks is 1, then king is in check
				//If number of checks is 2, then king is in double-check
				if (numberOfChecks == 0) {
					return Constants.NOT_IN_CHECK;
				} else if (numberOfChecks == 1) {
					return Constants.CHECK;
				} else if (numberOfChecks == 2) {
					return Constants.DOUBLE_CHECK;
				} else if (numberOfChecks > 2) {
					return Constants.MULTIPLE_CHECK;
				}
			}
			return Constants.NOT_IN_CHECK;
		}

		//METHOD THAT GENERATES A LIST OF LEGAL MOVES FROM THE CURRENT BOARD POSITION------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		
		public List<int> generateListOfPsdueoLegalMoves() {
			
			//Generates all of the white moves
			if (sideToMove == Constants.WHITE) {

				List<int> listOfPseudoLegalMoves = new List<int>(50);
				List<int> listOfLegalMoves = new List<int>(50);

				//Checks to see if the king is in check in the current position
				int kingCheckStatus = this.kingInCheck(sideToMove);

				//Gets the indices of all of the pieces
				List<int> indicesOfWhitePawn = Constants.bitScan(this.arrayOfBitboards[Constants.WHITE_PAWN - 1]);
				List<int> indicesOfWhiteKnight = Constants.bitScan(this.arrayOfBitboards[Constants.WHITE_KNIGHT - 1]);
				List<int> indicesOfWhiteBishop = Constants.bitScan(this.arrayOfBitboards[Constants.WHITE_BISHOP - 1]);
				List<int> indicesOfWhiteRook = Constants.bitScan(this.arrayOfBitboards[Constants.WHITE_ROOK - 1]);
				List<int> indicesOfWhiteQueen = Constants.bitScan(this.arrayOfBitboards[Constants.WHITE_QUEEN - 1]);

				//Generates white pawn moves and captures
				foreach (int pawnIndex in indicesOfWhitePawn) {

					//For pawns that are between the 2nd and 6th ranks, generate single pushes and captures
					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A6) {
						//Generates white pawn single moves
						ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];

						if ((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0) {
							int indexOfWhitePawnSingleMoveFromIndex = Constants.bitScan(singlePawnMovementFromIndex).ElementAt(0);
							int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.QUIET_MOVE, Constants.EMPTY);
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}

						//Generates white pawn captures
						ulong pawnCapturesFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
						List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnCapturesFromIndex);
						foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex) {

							int moveRepresentation = 0x0;

							moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArray[pawnMoveIndex]);
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
					}

					//For pawns that are on the 2nd rank, generate double pawn pushes
					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
						ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
						ulong doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];

						if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0)) {
							int indexOfWhitePawnDoubleMoveFromIndex = Constants.bitScan(doublePawnMovementFromIndex).ElementAt(0);
							int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnDoubleMoveFromIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
					}

					//If en passant is possible, For pawns that are on the 5th rank, generate en passant captures
					if ((this.enPassantSquare & Constants.RANK_6 ) != 0) {
						if (pawnIndex >= Constants.H5 && pawnIndex <= Constants.A5) {
							ulong pawnCapturesFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];

							if ((pawnCapturesFromIndex & this.enPassantSquare) != 0) {
								int indexOfWhiteEnPassantCaptureFromIndex = Constants.bitScan(pawnCapturesFromIndex & this.enPassantSquare).ElementAt(0);
								int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhiteEnPassantCaptureFromIndex, Constants.EN_PASSANT_CAPTURE, Constants.BLACK_PAWN);
								listOfPseudoLegalMoves.Add(moveRepresentation);
							}
						}
					}

					//For pawns on the 7th rank, generate promotions and promotion captures
					if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
						ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
						
						//Generates white pawn promotions
						if ((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0) {
							int indexOfWhitePawnSingleMoveFromIndex = Constants.bitScan(singlePawnMovementFromIndex).ElementAt(0);
							int moveRepresentationKnightPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.KNIGHT_PROMOTION, Constants.EMPTY);
							int moveRepresentationBishopPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.BISHOP_PROMOTION, Constants.EMPTY);
							int moveRepresentationRookPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.ROOK_PROMOTION, Constants.EMPTY);
							int moveRepresentationQueenPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.QUEEN_PROMOTION, Constants.EMPTY);

							listOfPseudoLegalMoves.Add(moveRepresentationKnightPromotion);
							listOfPseudoLegalMoves.Add(moveRepresentationBishopPromotion);
							listOfPseudoLegalMoves.Add(moveRepresentationRookPromotion);
							listOfPseudoLegalMoves.Add(moveRepresentationQueenPromotion);
						}


						//Generates white pawn capture promotions
						ulong pawnCapturesFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
						List<int> indicesOfWhitePawnCapturesFromIndex = Constants.bitScan(pseudoLegalPawnCapturesFromIndex);
						foreach (int pawnMoveIndex in indicesOfWhitePawnCapturesFromIndex) {

							int moveRepresentationKnightPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.KNIGHT_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);
							int moveRepresentationBishopPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.BISHOP_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);
							int moveRepresentationRookPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.ROOK_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);
							int moveRepresentationQueenPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.QUEEN_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);

							listOfPseudoLegalMoves.Add(moveRepresentationKnightPromotionCapture);
							listOfPseudoLegalMoves.Add(moveRepresentationBishopPromotionCapture);
							listOfPseudoLegalMoves.Add(moveRepresentationRookPromotionCapture);
							listOfPseudoLegalMoves.Add(moveRepresentationQueenPromotionCapture);
						}
					}
				}

				//generates white knight moves and captures
				foreach (int knightIndex in indicesOfWhiteKnight) {
					ulong knightMovementFromIndex = Constants.knightMoves[knightIndex];
					ulong pseudoLegalKnightMovementFromIndex = knightMovementFromIndex &= (~this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);
					List<int> indicesOfWhiteKnightMovesFromIndex = Constants.bitScan(pseudoLegalKnightMovementFromIndex);
					foreach (int knightMoveIndex in indicesOfWhiteKnightMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[knightMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_KNIGHT, knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						} else if (pieceArray[knightMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_KNIGHT, knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArray[knightMoveIndex]);
						}
						listOfPseudoLegalMoves.Add(moveRepresentation);
					}

				}


				//generates white bishop moves and captures
				foreach (int bishopIndex in indicesOfWhiteBishop) {
					ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.bishopOccupancyMask[bishopIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[bishopIndex]) >> Constants.bishopMagicShiftNumber[bishopIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[bishopIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);
					List<int> indicesOfWhiteBishopMovesFromIndex = Constants.bitScan(pseudoLegalBishopMovementFromIndex);
					foreach (int bishopMoveIndex in indicesOfWhiteBishopMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[bishopMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_BISHOP, bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a black piece at that location, so generate a capture
						else if (pieceArray[bishopMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_BISHOP, bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArray[bishopMoveIndex]);
						}
						listOfPseudoLegalMoves.Add(moveRepresentation);
					}

				}

				//generates white rook moves and captures
				foreach (int rookIndex in indicesOfWhiteRook) {
					ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.rookOccupancyMask[rookIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[rookIndex]) >> Constants.rookMagicShiftNumber[rookIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[rookIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);
					List<int> indicesOfWhiteRookMovesFromIndex = Constants.bitScan(pseudoLegalRookMovementFromIndex);
					foreach (int rookMoveIndex in indicesOfWhiteRookMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[rookMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_ROOK, rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a black piece at that location, so generate a capture
						else if (pieceArray[rookMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_ROOK, rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArray[rookMoveIndex]);
						}
						listOfPseudoLegalMoves.Add(moveRepresentation);
					}

				}

				//generates white queen moves and captures
				foreach (int queenIndex in indicesOfWhiteQueen) {
					ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.bishopOccupancyMask[queenIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[queenIndex]) >> Constants.bishopMagicShiftNumber[queenIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[queenIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);

					ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.rookOccupancyMask[queenIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[queenIndex]) >> Constants.rookMagicShiftNumber[queenIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[queenIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);

					ulong pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
					List<int> indicesOfWhiteQueenMovesFromIndex = Constants.bitScan(pseudoLegalQueenMovementFromIndex);

					foreach (int queenMoveIndex in indicesOfWhiteQueenMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[queenMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_QUEEN, queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a black piece at that location, so generate a capture
						else if (pieceArray[queenMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_QUEEN, queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArray[queenMoveIndex]);
						}
						listOfPseudoLegalMoves.Add(moveRepresentation);
					}
				}

				//generates white king moves and captures
				ulong whiteKingBitboard = this.arrayOfBitboards[Constants.WHITE_KING - 1];
				List<int> indicesOfWhiteKing = Constants.bitScan(whiteKingBitboard);

				foreach (int kingIndex in indicesOfWhiteKing) {
					ulong kingMovementFromIndex = Constants.kingMoves[kingIndex];
					ulong pseudoLegalKingMovementFromIndex = kingMovementFromIndex &= (~this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);
					List<int> indicesOfWhiteKingMovesFromIndex = Constants.bitScan(pseudoLegalKingMovementFromIndex);
					foreach (int kingMoveIndex in indicesOfWhiteKingMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[kingMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						} else if (pieceArray[kingMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArray[kingMoveIndex]);
						}
						listOfPseudoLegalMoves.Add(moveRepresentation);
					}
				}

				//Generates white king castling moves (if the king is not in check)
				if (kingCheckStatus == Constants.NOT_IN_CHECK) {
					if ((this.whiteShortCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.G1, Constants.SHORT_CASTLE, Constants.EMPTY);

						int boardRestoreData = this.makeMove(moveRepresentation);
						if (this.timesSquareIsAttacked(Constants.WHITE, Constants.F1) == 0) {
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
						this.unmakeMove(moveRepresentation, boardRestoreData);
					}

					if ((this.whiteLongCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.C1, Constants.LONG_CASTLE, Constants.EMPTY);

						int boardRestoreData = this.makeMove(moveRepresentation);
						if (this.timesSquareIsAttacked(Constants.WHITE, Constants.D1) == 0) {
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
						this.unmakeMove(moveRepresentation, boardRestoreData);
					}
				}
				
				//Checks all pseudo legal moves and adds them to list of legal moves if king is not in check
				foreach (int moveRepresentation in listOfPseudoLegalMoves) {
					int boardRestoreData = this.makeMove(moveRepresentation);
					if (this.kingInCheck(Constants.WHITE) == Constants.NOT_IN_CHECK) {
						listOfLegalMoves.Add(moveRepresentation);
					}
					this.unmakeMove(moveRepresentation, boardRestoreData);
				}

				//returns the list of legal moves
				return listOfLegalMoves;

			} else if (sideToMove == Constants.BLACK) {

				List<int> listOfPseudoLegalMoves = new List<int>(50);
				List<int> listOfLegalMoves = new List<int>(50);

				//Checks to see if the king is in check in the current position
				int kingCheckStatus = this.kingInCheck(sideToMove);

				//Gets the en passant square
				ulong enPassantBitboard = this.getEnPassant();

				//Inces of all the pieces
				List<int> indicesOfBlackPawn = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_PAWN - 1]);
				List<int> indicesOfBlackKnight = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_KNIGHT - 1]);
				List<int> indicesOfBlackBishop = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_BISHOP - 1]);
				List<int> indicesOfBlackRook = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_ROOK - 1]);
				List<int> indicesOfBlackQueen = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_QUEEN - 1]);
				List<int> indicesOfBlackKing = Constants.bitScan(this.arrayOfBitboards[Constants.BLACK_KING - 1]);
				
				//Generates black pawn moves
				foreach (int pawnIndex in indicesOfBlackPawn) {

					if (pawnIndex >= Constants.H3 && pawnIndex <= Constants.A7) {

						//Generates black pawn single moves
						ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];

						if ((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0) {
							int indexOfBlackPawnSingleMoveFromIndex = Constants.bitScan(singlePawnMovementFromIndex).ElementAt(0);
							int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.QUIET_MOVE, Constants.EMPTY);
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}

						//Generates pawn captures
						ulong pawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);
						List<int> indicesOfBlackPawnCapturesFromIndex = Constants.bitScan(pseudoLegalPawnCapturesFromIndex);
						foreach (int pawnMoveIndex in indicesOfBlackPawnCapturesFromIndex) {

							int moveRepresentation = 0x0;

							moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArray[pawnMoveIndex]);
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
					}

					//Generates black pawn double moves
					if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
						ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
						ulong doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];

						if (((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0) && ((doublePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0)) {
							int indexOfBlackPawnDoubleMoveFromIndex = Constants.bitScan(doublePawnMovementFromIndex).ElementAt(0);
							int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnDoubleMoveFromIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
					}

					//Generates black pawn en passant captures
					if ((this.enPassantSquare & Constants.RANK_3) != 0) {
						if (pawnIndex >= Constants.H4 && pawnIndex <= Constants.A4) {
							ulong pawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];

							if ((pawnCapturesFromIndex & this.enPassantSquare) != 0) {
								int indexOfBlackEnPassantCaptureFromIndex = Constants.bitScan(pawnCapturesFromIndex & this.enPassantSquare).ElementAt(0);
								int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackEnPassantCaptureFromIndex, Constants.EN_PASSANT_CAPTURE, Constants.WHITE_PAWN);
								listOfPseudoLegalMoves.Add(moveRepresentation);
							}
						}
					}


					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
						ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];

						if ((singlePawnMovementFromIndex & this.arrayOfAggregateBitboards[Constants.ALL_PIECES]) == 0) {
							int indexOfBlackPawnSingleMoveFromIndex = Constants.bitScan(singlePawnMovementFromIndex).ElementAt(0);
							int moveRepresentationKnightPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.KNIGHT_PROMOTION, Constants.EMPTY);
							int moveRepresentationBishopPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.BISHOP_PROMOTION, Constants.EMPTY);
							int moveRepresentationRookPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.ROOK_PROMOTION, Constants.EMPTY);
							int moveRepresentationQueenPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.QUEEN_PROMOTION, Constants.EMPTY);

							listOfPseudoLegalMoves.Add(moveRepresentationKnightPromotion);
							listOfPseudoLegalMoves.Add(moveRepresentationBishopPromotion);
							listOfPseudoLegalMoves.Add(moveRepresentationRookPromotion);
							listOfPseudoLegalMoves.Add(moveRepresentationQueenPromotion);
						}

						ulong pawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (this.arrayOfAggregateBitboards[Constants.WHITE_PIECES]);
						List<int> indicesOfBlackPawnCapturesFromIndex = Constants.bitScan(pseudoLegalPawnCapturesFromIndex);
						foreach (int pawnMoveIndex in indicesOfBlackPawnCapturesFromIndex) {

							int moveRepresentationKnightPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.KNIGHT_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);
							int moveRepresentationBishopPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.BISHOP_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);
							int moveRepresentationRookPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.ROOK_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);
							int moveRepresentationQueenPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.QUEEN_PROMOTION_CAPTURE, pieceArray[pawnMoveIndex]);

							listOfPseudoLegalMoves.Add(moveRepresentationKnightPromotionCapture);
							listOfPseudoLegalMoves.Add(moveRepresentationBishopPromotionCapture);
							listOfPseudoLegalMoves.Add(moveRepresentationRookPromotionCapture);
							listOfPseudoLegalMoves.Add(moveRepresentationQueenPromotionCapture);
						}
					}

				}

				

				//Generates black knight moves and captures
				foreach (int knightIndex in indicesOfBlackKnight) {
					ulong knightMovementFromIndex = Constants.knightMoves[knightIndex];
					ulong pseudoLegalKnightMovementFromIndex = knightMovementFromIndex &= (~this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
					List<int> indicesOfBlackKnightMovesFromIndex = Constants.bitScan(pseudoLegalKnightMovementFromIndex);
					foreach (int knightMoveIndex in indicesOfBlackKnightMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[knightMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_KNIGHT, knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						} else if (pieceArray[knightMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_KNIGHT, knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArray[knightMoveIndex]);
						}

						listOfPseudoLegalMoves.Add(moveRepresentation);
					}
				}

				//generates black bishop moves and captures
				foreach (int bishopIndex in indicesOfBlackBishop) {
					ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.bishopOccupancyMask[bishopIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[bishopIndex]) >> Constants.bishopMagicShiftNumber[bishopIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[bishopIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
					List<int> indicesOfBlackBishopMovesFromIndex = Constants.bitScan(pseudoLegalBishopMovementFromIndex);
					foreach (int bishopMoveIndex in indicesOfBlackBishopMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[bishopMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_BISHOP, bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a white piece at that location, so generate a capture
						else if (pieceArray[bishopMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_BISHOP, bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArray[bishopMoveIndex]);
						}

						listOfPseudoLegalMoves.Add(moveRepresentation);
					}
				}
				//generates black rook moves and captures
				foreach (int rookIndex in indicesOfBlackRook) {
					ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.rookOccupancyMask[rookIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[rookIndex]) >> Constants.rookMagicShiftNumber[rookIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[rookIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
					List<int> indicesOfBlackRookMovesFromIndex = Constants.bitScan(pseudoLegalRookMovementFromIndex);
					foreach (int rookMoveIndex in indicesOfBlackRookMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[rookMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_ROOK, rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a white piece at that location, so generate a capture
						else if (pieceArray[rookMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_ROOK, rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArray[rookMoveIndex]);
						}

						listOfPseudoLegalMoves.Add(moveRepresentation);
					}

				}
				//generates black queen moves and captures
				foreach (int queenIndex in indicesOfBlackQueen) {
					ulong diagonalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.bishopOccupancyMask[queenIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[queenIndex]) >> Constants.bishopMagicShiftNumber[queenIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[queenIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);

					ulong horizontalVerticalOccupancy = this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.rookOccupancyMask[queenIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[queenIndex]) >> Constants.rookMagicShiftNumber[queenIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[queenIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);

					ulong pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
					List<int> indicesOfBlackQueenMovesFromIndex = Constants.bitScan(pseudoLegalQueenMovementFromIndex);

					foreach (int queenMoveIndex in indicesOfBlackQueenMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[queenMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_QUEEN, queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a white piece at that location, so generate a capture
						else if (pieceArray[queenMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_QUEEN, queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArray[queenMoveIndex]);
						}

						listOfPseudoLegalMoves.Add(moveRepresentation);
					}
				}

				//generates black king moves and captures
				foreach (int kingIndex in indicesOfBlackKing) {
					ulong kingMovementFromIndex = Constants.kingMoves[kingIndex];
					ulong pseudoLegalKingMovementFromIndex = kingMovementFromIndex &= (~this.arrayOfAggregateBitboards[Constants.BLACK_PIECES]);
					List<int> indicesOfBlackKingMovesFromIndex = Constants.bitScan(pseudoLegalKingMovementFromIndex);
					foreach (int kingMoveIndex in indicesOfBlackKingMovesFromIndex) {

						int moveRepresentation = 0x0;

						if (pieceArray[kingMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						} else if (pieceArray[kingMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArray[kingMoveIndex]);
						}

						listOfPseudoLegalMoves.Add(moveRepresentation);
					}
				}

				//Generates black king castling moves (if the king is not in check)
				if (kingCheckStatus == Constants.NOT_IN_CHECK) {
					if ((this.blackShortCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.G8, Constants.SHORT_CASTLE, Constants.EMPTY);

						int boardRestoreData = this.makeMove(moveRepresentation);
						if (this.timesSquareIsAttacked(Constants.BLACK, Constants.F8) == 0) {
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
						this.unmakeMove(moveRepresentation, boardRestoreData);
					}

					if ((this.blackLongCastleRights == Constants.CAN_CASTLE) && ((this.arrayOfAggregateBitboards[Constants.ALL_PIECES] & Constants.BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.C8, Constants.LONG_CASTLE, Constants.EMPTY);

						int boardRestoreData = this.makeMove(moveRepresentation);
						if (this.timesSquareIsAttacked(Constants.BLACK, Constants.D8) == 0) {
							listOfPseudoLegalMoves.Add(moveRepresentation);
						}
						this.unmakeMove(moveRepresentation, boardRestoreData);
					}
				}
				//Checks all pseudo legal moves and adds them to list of legal moves if king is not in check
				foreach (int moveRepresentation in listOfPseudoLegalMoves) {
					int boardRestoreData = this.makeMove(moveRepresentation);
					if (this.kingInCheck(Constants.BLACK) == Constants.NOT_IN_CHECK) {
						listOfLegalMoves.Add(moveRepresentation);
					}
					this.unmakeMove(moveRepresentation, boardRestoreData);
				}

				//returns the list of legal moves
				return listOfLegalMoves;
			}

			return null;

		}

	    //OTHER METHODS----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //takes in a FEN string and sets all the instance variables based on it
        public void FENToBoard(string FEN) {
           
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
                        case 'P': wPawn |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'N': wKnight |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'B': wBishop |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'R': wRook |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'Q': wQueen |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'K': wKing |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'p': bPawn |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'n': bKnight |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'b': bBishop |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'r': bRook |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'q': bQueen |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
                        case 'k': bKing |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
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

            //stores the bitboards in the array
            arrayOfBitboards[0] = wPawn;
            arrayOfBitboards[1] = wKnight;
            arrayOfBitboards[2] = wBishop;
            arrayOfBitboards[3] = wRook;
            arrayOfBitboards[4] = wQueen;
            arrayOfBitboards[5] = wKing;
            arrayOfBitboards[6] = bPawn;
            arrayOfBitboards[7] = bKnight;
            arrayOfBitboards[8] = bBishop;
            arrayOfBitboards[9] = bRook;
            arrayOfBitboards[10] = bQueen;
            arrayOfBitboards[11] = bKing;

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
                        case 'P': pieceArray[7 + 8 * i - index] = Constants.WHITE_PAWN; index++; break;
                        case 'N': pieceArray[7 + 8 * i - index] = Constants.WHITE_KNIGHT; index++; break;
                        case 'B': pieceArray[7 + 8 * i - index] = Constants.WHITE_BISHOP; index++; break;
                        case 'R': pieceArray[7 + 8 * i - index] = Constants.WHITE_ROOK; index++; break;
                        case 'Q': pieceArray[7 + 8 * i - index] = Constants.WHITE_QUEEN; index++; break;
                        case 'K': pieceArray[7 + 8 * i - index] = Constants.WHITE_KING; index++; break;
                        case 'p': pieceArray[7 + 8 * i - index] = Constants.BLACK_PAWN; index++; break;
                        case 'n': pieceArray[7 + 8 * i - index] = Constants.BLACK_KNIGHT; index++; break;
                        case 'b': pieceArray[7 + 8 * i - index] = Constants.BLACK_BISHOP; index++; break;
                        case 'r': pieceArray[7 + 8 * i - index] = Constants.BLACK_ROOK; index++; break;
                        case 'q': pieceArray[7 + 8 * i - index] = Constants.BLACK_QUEEN; index++; break;
                        case 'k': pieceArray[7 + 8 * i - index] = Constants.BLACK_KING; index++; break;
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
                    sideToMove = Constants.WHITE;
                } else if (c == 'b') {
                    sideToMove = Constants.BLACK;
                }
            }
            
            //Sets the castling availability variables
            if (FENfields[2] == "-") {
                whiteShortCastleRights = 0;
                whiteLongCastleRights = 0;
                blackShortCastleRights = 0;
                blackLongCastleRights = 0;
            } else if (FENfields[2] != "-") {
                foreach (char c in FENfields[2]) {
                    if (c == 'K') {
                        whiteShortCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'Q') {
                        whiteLongCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'k') {
                        blackShortCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'q') {
                        blackLongCastleRights = Constants.CAN_CASTLE;
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
                enPassantSquare = 0x1UL << (baseOfEPSquare + factorOfEPSquare * 8);
            }
            
            //Checks to see if there is a halfmove clock or move number in the FEN string
            //If there isn't, then it sets the halfmove number clock and move number to 999;
            if (FENfields.Length >= 5) {
                //sets the halfmove clock since last capture or pawn move
                foreach (char c in FENfields[4]) {
                    HalfMovesSincePawnMoveOrCapture = (int) Char.GetNumericValue(c);
                }

                //sets the move number
                foreach (char c in FENfields[5]) {
                    moveNumber = (int) Char.GetNumericValue(c);
                }
            } else {
                HalfMovesSincePawnMoveOrCapture = -1;
                moveNumber = -1;
            }

            //Sets the repetition number variable
            repetionOfPosition = 0;

            //Computes the white pieces, black pieces, and occupied bitboard by using "or" on all the individual pieces
            whitePieces = wPawn | wKnight | wBishop | wRook | wQueen | wKing;
            blackPieces = bPawn | bKnight | bBishop | bRook | bQueen | bKing;
            allPieces = whitePieces | blackPieces;

            arrayOfAggregateBitboards[0] = whitePieces;
            arrayOfAggregateBitboards[1] = blackPieces;
            arrayOfAggregateBitboards[2] = allPieces;

        }

	//Takes information on piece moved, start square, destination square, type of move, and piece captured
		//Creates a 32-bit unsigned integer representing this information
		//bits 0-3 store the piece moved, 4-9 stores start square, 10-15 stores destination square, 16-19 stores move type, 20-23 stores piece captured
		private static int moveEncoder(int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured) {
			int moveRepresentation = (pieceMoved | (startSquare << 4) | (destinationSquare << 10) | (flag << 16));

			//If no piece is captured, then we set the bits corresponding to that variable to 15 (the maximum value for 4 bits)
			if (pieceCaptured == 0) {
				moveRepresentation |= 15 << 20;
			} else {
				moveRepresentation |= pieceCaptured << 20;
			}
			return moveRepresentation;
		} 


        //GET METHODS (FOR I/O)----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //gets the array of piece bitboards (returns an array of 12 bitboards)
        //element 0 = white pawn, 1 = white knight, 2 = white bishop, 3 = white rook, 4 = white queen, 5 = white king
        //element 6 = black pawn, 7 = black knight, 8 = black bishop, 9 = black rook, 10 = black queen, 11 = black king
        public ulong[] getArrayOfPieceBitboards() {
            return arrayOfBitboards;
        }

        //gets the array of aggregate piece bitboards (returns an array of 3 bitboards)
        //element 0 = white pieces, element 1 = black pieces, element 2 = all pieces
        public ulong[] getArrayOfAggregatePieceBitboards() {
            return arrayOfAggregateBitboards;
        }

        //gets the array of pieces
        public int[] getPieceArray() {
            return pieceArray;
        }

        //gets the side to move
        public int getSideToMove() {
            return sideToMove;
        }

        //gets the castling rights (returns an array of 4 bools)
        //element 0 = white short castle rights, 1 = white long castle rights
        //2 = black short castle rights, 3 = black long castle rights
        public int[] getCastleRights() {
            int[] castleRights = new int[4];

            castleRights[0] = whiteShortCastleRights;
            castleRights[1] = whiteLongCastleRights;
            castleRights[2] = blackShortCastleRights;
            castleRights[3] = blackLongCastleRights;

            return castleRights;
        }

        //gets the En Passant colour and square
        //element 0 = en passant colour, and element 1 = en passant square
        public ulong getEnPassant() {
            return enPassantSquare;
        }

        //gets the move data
        //element 0 = move number, 1 = half moves since pawn move or capture, 2 = repetition of position
        public int[] getMoveData() {
            int[] moveData = new int[3];

            moveData[0] = moveNumber;
            moveData[1] = HalfMovesSincePawnMoveOrCapture;
            moveData[2] = repetionOfPosition;

            return moveData;
        }
    }
}
