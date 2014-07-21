﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
   
    public static class Board {

        //INSTANCE VARIABLES-----------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Variables that uniquely describe a board state

        //bitboards and array to hold bitboards
        internal static ulong[] arrayOfBitboards = new ulong[12];
        internal static ulong wPawn = 0x0UL;
        internal static ulong wKnight = 0x0UL;
        internal static ulong wBishop = 0x0UL;
        internal static ulong wRook = 0x0UL;
        internal static ulong wQueen = 0x0UL;
        internal static ulong wKing = 0x0UL;
        internal static ulong bPawn = 0x0UL;
        internal static ulong bKnight = 0x0UL;
        internal static ulong bBishop = 0x0UL;
        internal static ulong bRook = 0x0UL;
        internal static ulong bQueen = 0x0UL;
        internal static ulong bKing = 0x0UL;

        //aggregate bitboards and array to hold aggregate bitboards
        internal static ulong whitePieces = 0x0UL;
        internal static ulong blackPieces = 0x0UL;
        internal static ulong allPieces = 0x0UL;

        //piece array that stores the pieces as integers in a 64-element array
        //Index 0 is H1, and element 63 is A8
        internal static int[] pieceArray = new int[64];

        //side to move
        internal static int sideToMove = 0;

        //castling rights
        internal static int whiteShortCastleRights = 0;
        internal static int whiteLongCastleRights = 0;
        internal static int blackShortCastleRights = 0;
        internal static int blackLongCastleRights = 0;

        //en passant state
        internal static ulong enPassantSquare = 0x0UL;
        
        //move data
        internal static int halfmoveNumber = 0;
        internal static int HalfMovesSincePawnMoveOrCapture = 0;
        internal static int repetionOfPosition = 0;

        //Variables that can be calculated
        internal static int blackInCheck = 0;
        internal static int blackInCheckmate = 0;
        internal static int whiteInCheck = 0;
        internal static int whiteInCheckmate = 0;
        internal static int stalemate = 0;

        internal static bool endGame = false;

        internal static uint lastMove = 0x0;

        internal static int evaluationFunctionValue = 0;

        internal static ulong zobristKey = 0x0UL;

		//MAKE MOVE METHODS-------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

		//Static method that makes a move by updating the board object's instance variables
        public static void makeMove(int moveRepresentationInput) {

            //Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
			int pieceMoved = ((moveRepresentationInput & Constants.PIECE_MOVED_MASK) >> 0);
			int startSquare = ((moveRepresentationInput & Constants.START_SQUARE_MASK) >> 4);
			int destinationSquare = ((moveRepresentationInput & Constants.DESTINATION_SQUARE_MASK) >> 10);
			int flag = ((moveRepresentationInput & Constants.FLAG_MASK) >> 16);
			int pieceCaptured = ((moveRepresentationInput & Constants.PIECE_CAPTURED_MASK) >> 20);
            int piecePromoted = ((moveRepresentationInput & Constants.PIECE_PROMOTED_MASK) >> 24);
			
            //Calculates bitboards for removing piece from start square and adding piece to destionation square
			//"and" with startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
			ulong startSquareBitboard = (0x1UL << startSquare);
			ulong destinationSquareBitboard = (0x1UL << destinationSquare);

            //Sets the board's instance variables-----------------------------------------------------------------------------
            //sets the side to move to the other player (white = 0 and black = 1, so side ^ 1 = other side)
            sideToMove ^= 1;

            //If the move is anything but a double pawn push, sets the en passant square bitboard to 0x0UL;
		    if (flag != Constants.DOUBLE_PAWN_PUSH) {
                Board.enPassantSquare = 0x0UL;
		    }
            //Updates the en passant square instance variable if there was a double pawn push
		    if (flag == Constants.DOUBLE_PAWN_PUSH) {
		        if (pieceMoved == Constants.WHITE_PAWN) {
		            enPassantSquare = (0x1UL << (destinationSquare - 8));
		        } else if (pieceMoved == Constants.BLACK_PAWN) {
		            enPassantSquare = (0x1UL << (destinationSquare + 8));
		        }
		    }
            //If the king moved, then set the castling rights to false
            if (pieceMoved == Constants.WHITE_KING) {
                Board.whiteShortCastleRights = Constants.CANNOT_CASTLE;
                Board.whiteLongCastleRights = Constants.CANNOT_CASTLE;
            } else if (pieceMoved == Constants.BLACK_KING) {
                Board.blackShortCastleRights = Constants.CANNOT_CASTLE;
                Board.blackLongCastleRights = Constants.CANNOT_CASTLE;
            }
		    //If the start square or destination square is A1, H1, A8, or H8, then updates the castling rights
		    if (startSquare == Constants.A1 || destinationSquare == Constants.A1) {
                Board.whiteLongCastleRights = Constants.CANNOT_CASTLE;
            } if (startSquare == Constants.H1 || destinationSquare == Constants.H1) {
                Board.whiteShortCastleRights = Constants.CANNOT_CASTLE;
            } if (startSquare == Constants.A8 || destinationSquare == Constants.A8) {
                Board.blackLongCastleRights = Constants.CANNOT_CASTLE;
            } if (startSquare == Constants.H8 || destinationSquare == Constants.H8) {
                Board.blackShortCastleRights = Constants.CANNOT_CASTLE;
            }


            //Updates the bitboards and arrays---------------------------------------------------------------------------------------

            //If is any move except a promotion, then add a piece to the destination square
            //If it is a promotion don't add piece (pawn) to the destination square
            //Only remove it from the start square
            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                //updates the bitboard and removes the int representing the piece from the start square of the piece array, and adds an int representing the piece to the destination square of the piece array
                Board.arrayOfBitboards[pieceMoved - 1] &= (~startSquareBitboard);
                Board.arrayOfBitboards[pieceMoved - 1] |= destinationSquareBitboard;
                Board.pieceArray[startSquare] = Constants.EMPTY;
                Board.pieceArray[destinationSquare] = pieceMoved;   
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
               Board.arrayOfBitboards[pieceMoved - 1] &= (~startSquareBitboard);
               Board.pieceArray[startSquare] = Constants.EMPTY;
            }

            //If there was a capture, also remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
			//Don't have to update the array because it was already overridden with the capturing piece
			if (flag == Constants.CAPTURE) {
				Board.arrayOfBitboards[pieceCaptured - 1] &= (~destinationSquareBitboard);
			}
            //If there was an en-passant capture, remove the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
			//Update the array because the pawn destination square and captured pawn are on different squares
            else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    Board.arrayOfBitboards[Constants.BLACK_PAWN - 1] &= (~(destinationSquareBitboard >> 8));
                    Board.pieceArray[destinationSquare - 8] = Constants.EMPTY;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    Board.arrayOfBitboards[Constants.WHITE_PAWN - 1] &= (~(destinationSquareBitboard << 8));
                    Board.pieceArray[destinationSquare + 8] = Constants.EMPTY;
                }	
			}
            //If short castle, then move the rook from H1 to F1
            //If short castle, then move the rook from H8 to F8
            else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~Constants.H1_BITBOARD);
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= Constants.F1_BITBOARD;
                    Board.pieceArray[Constants.H1] = Constants.EMPTY;
                    Board.pieceArray[Constants.F1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(Constants.H8_BITBOARD);
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= Constants.F8_BITBOARD;
                    Board.pieceArray[Constants.H8] = Constants.EMPTY;
                    Board.pieceArray[Constants.F8] = Constants.BLACK_ROOK;
                }
            }
            //If long castle, then move the rook from A1 to D1     
            //If long castle, then move the rook from A8 to D8
            else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~Constants.A1_BITBOARD);
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= Constants.D1_BITBOARD;
                    Board.pieceArray[Constants.A1] = Constants.EMPTY;
                    Board.pieceArray[Constants.D1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(Constants.A8_BITBOARD);
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= Constants.D8_BITBOARD;
                    Board.pieceArray[Constants.A8] = Constants.EMPTY;
                    Board.pieceArray[Constants.D8] = Constants.BLACK_ROOK;
                }       
            }
                //If regular promotion, updates the pawn's bitboard, the promoted piece bitboard, and the piece array
            else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    Board.arrayOfBitboards[piecePromoted - 1] |= destinationSquareBitboard;
                    Board.pieceArray[destinationSquare] = piecePromoted;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    Board.arrayOfBitboards[piecePromoted - 1] |= destinationSquareBitboard;
                    Board.pieceArray[destinationSquare] = piecePromoted;
                }
            } 

            //If there was a capture, removes the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
		    if (flag == Constants.PROMOTION_CAPTURE) {
                    Board.arrayOfBitboards[pieceCaptured - 1] &= (~destinationSquareBitboard);
		    }
            
			//updates the aggregate bitboards
		    updateAggregateBitboards();
            
            //increments the full-move number (implement later)
			//Implement the half-move clock later (in the moves)
			//Also implement the repetitions later

		}

		//UNMAKE MOVE METHODS--------------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Method that unmakes a move by restoring the board object's instance variables
        public static void unmakeMove(int unmoveRepresentationInput, int boardRestoreDataRepresentation) {

            //restores the board instance variables
	        restoreBoardState(boardRestoreDataRepresentation);

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

            //Removes the bit corresponding to the destination square, and adds a bit corresponding with the start square (to unmake move)
            //Removes the int representing the piece from the destination square of the piece array, and adds an int representing the piece to the start square of the piece array (to unmake move)
            //If it was a promotion, then don't have to remove the pawn from the destination square
            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                Board.arrayOfBitboards[pieceMoved - 1] &= (~destinationSquareBitboard);
                Board.arrayOfBitboards[pieceMoved - 1] |= (startSquareBitboard);
                Board.pieceArray[destinationSquare] = Constants.EMPTY;
                Board.pieceArray[startSquare] = pieceMoved;      
            } else {
                Board.arrayOfBitboards[pieceMoved - 1] |= (startSquareBitboard);
                Board.pieceArray[startSquare] = pieceMoved;      
            }

            //If there was a capture, add to the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
            //Also re-add the captured piece to the array
            if (flag == Constants.CAPTURE) {
                Board.arrayOfBitboards[pieceCaptured - 1] |= (destinationSquareBitboard);
                Board.pieceArray[destinationSquare] = pieceCaptured;
            }
            //If there was an en-passant capture, add the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
            //Also re-add teh captured pawn to the array 
            else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    Board.arrayOfBitboards[Constants.BLACK_PAWN - 1] |= (destinationSquareBitboard >> 8);
                    Board.pieceArray[destinationSquare - 8] = Constants.BLACK_PAWN;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    Board.arrayOfBitboards[Constants.WHITE_PAWN - 1] |= (destinationSquareBitboard << 8);
                    Board.pieceArray[destinationSquare + 8] = Constants.WHITE_PAWN; 
                }  
            } 
            //If white king short castle, then move the rook from F1 to H1
            //If black king short castle, then move the rook from F8 to H8
            else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~Constants.F1_BITBOARD);
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= Constants.H1_BITBOARD;
                    Board.pieceArray[Constants.F1] = Constants.EMPTY;
                    Board.pieceArray[Constants.H1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(Constants.F8_BITBOARD);
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= Constants.H8_BITBOARD;
                    Board.pieceArray[Constants.F8] = Constants.EMPTY;
                    Board.pieceArray[Constants.H8] = Constants.BLACK_ROOK;
                }
            }
           //If king long castle, then move the rook from D1 to A1
            //If black king long castle, then move the rook from D8 to A8
            else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] &= (~Constants.D1_BITBOARD);
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] |= Constants.A1_BITBOARD;
                    Board.pieceArray[Constants.D1] = Constants.EMPTY;
                    Board.pieceArray[Constants.A1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] &= ~(Constants.D8_BITBOARD);
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] |= Constants.A8_BITBOARD;
                    Board.pieceArray[Constants.D8] = Constants.EMPTY;
                    Board.pieceArray[Constants.A8] = Constants.BLACK_ROOK;
                }
            }
            //If there were promotions, update the promoted piece bitboard
            else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    Board.arrayOfBitboards[piecePromoted - 1] &= (~destinationSquareBitboard);
                    Board.pieceArray[destinationSquare] = Constants.EMPTY;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    Board.arrayOfBitboards[piecePromoted - 1] &= (~destinationSquareBitboard);
                    Board.pieceArray[destinationSquare] = Constants.EMPTY;
                }
            } 


            //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
            //Also adds the captured piece back to the array
	        if (flag == Constants.PROMOTION_CAPTURE) {
                Board.arrayOfBitboards[pieceCaptured - 1] |= destinationSquareBitboard;
                Board.pieceArray[destinationSquare] = pieceCaptured;
	        }

	        //updates the aggregate bitboards
			updateAggregateBitboards();
	    }

		//METHOD THAT RETURNS HOW MANY TIMES A CERTAIN SQUARE IS ATTACKED--------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        public static int timesSquareIsAttacked(int colourUnderAttack, int squareToCheck) {

			int numberOfTimesAttacked = 0;
			ulong bitboardOfAttackers = 0x0UL;

			if (colourUnderAttack == Constants.WHITE) {

				//Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
				ulong horizontalVerticalOccupancy = Board.allPieces & Constants.rookOccupancyMask[squareToCheck];
				int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
				ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

				//Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
				ulong diagonalOccupancy = Board.allPieces & Constants.bishopOccupancyMask[squareToCheck];
				int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
				ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

				//Looks up knight attack set from square, and intersects with opponent's knight bitboard
				ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

				//Looks up white pawn attack set from square, and intersects with opponent's pawn bitboard
				ulong whitePawnMoveFromSquare = Constants.whiteCapturesAndCapturePromotions[squareToCheck];

				//Looks up king attack set from square, and intersects with opponent's king bitboard
				ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

				bitboardOfAttackers |= ((rookMovesFromSquare & Board.arrayOfBitboards[Constants.BLACK_ROOK - 1])
									| (rookMovesFromSquare & Board.arrayOfBitboards[Constants.BLACK_QUEEN - 1])
									| (bishopMovesFromSquare & Board.arrayOfBitboards[Constants.BLACK_BISHOP - 1])
									| (bishopMovesFromSquare & Board.arrayOfBitboards[Constants.BLACK_QUEEN - 1])
									| (knightMoveFromSquare & Board.arrayOfBitboards[Constants.BLACK_KNIGHT - 1])
									| (whitePawnMoveFromSquare & Board.arrayOfBitboards[Constants.BLACK_PAWN - 1])
									| (kingMoveFromSquare & Board.arrayOfBitboards[Constants.BLACK_KING - 1]));

				if (bitboardOfAttackers == 0) {
					return 0;
				} else {
					numberOfTimesAttacked = Constants.popcount(bitboardOfAttackers);
					return numberOfTimesAttacked;
				}

			} else if (colourUnderAttack == Constants.BLACK) {

				//Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
				ulong horizontalVerticalOccupancy = Board.allPieces & Constants.rookOccupancyMask[squareToCheck];
				int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
				ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

				//Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
				ulong diagonalOccupancy = Board.allPieces & Constants.bishopOccupancyMask[squareToCheck];
				int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
				ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

				//Looks up knight attack set from square, and intersects with opponent's knight bitboard
				ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

				//Looks up black pawn attack set from square, and intersects with opponent's pawn bitboard
				ulong blackPawnMoveFromSquare = Constants.blackCapturesAndCapturePromotions[squareToCheck];

				//Looks up king attack set from square, and intersects with opponent's king bitboard
				ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

				bitboardOfAttackers = ((rookMovesFromSquare & Board.arrayOfBitboards[Constants.WHITE_ROOK - 1])
				                       | (rookMovesFromSquare & Board.arrayOfBitboards[Constants.WHITE_QUEEN - 1])
				                       | (bishopMovesFromSquare & Board.arrayOfBitboards[Constants.WHITE_BISHOP - 1])
				                       | (bishopMovesFromSquare & Board.arrayOfBitboards[Constants.WHITE_QUEEN - 1])
				                       | (knightMoveFromSquare & Board.arrayOfBitboards[Constants.WHITE_KNIGHT - 1])
				                       | (blackPawnMoveFromSquare & Board.arrayOfBitboards[Constants.WHITE_PAWN - 1])
				                       | (kingMoveFromSquare & Board.arrayOfBitboards[Constants.WHITE_KING - 1]));

				if (bitboardOfAttackers == 0) {
					return 0;
				} else {
					numberOfTimesAttacked = Constants.popcount(bitboardOfAttackers);
					return numberOfTimesAttacked;
				}
			}
			return 0;
		}

		//METHOD THAT GENERATES A LIST OF PSDUEO LEGAL MOVES FROM THE CURRENT BOARD POSITION------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        public static int[] generateListOfPsdueoLegalMoves() {
			
			//Generates all of the white moves
			if (sideToMove == Constants.WHITE) {

                //Gets the indices of all of the pieces
			    ulong tempWhitePawnBitboard = Board.arrayOfBitboards[Constants.WHITE_PAWN - 1];
			    ulong tempWhiteKnightBitboard = Board.arrayOfBitboards[Constants.WHITE_KNIGHT - 1];
                ulong tempWhiteBishopBitboard = Board.arrayOfBitboards[Constants.WHITE_BISHOP - 1];
                ulong tehpWhiteRookBitboard = Board.arrayOfBitboards[Constants.WHITE_ROOK - 1];
                ulong tempWhiteQueenBitboard = Board.arrayOfBitboards[Constants.WHITE_QUEEN - 1];
                ulong tempWhiteKingBitboard = Board.arrayOfBitboards[Constants.WHITE_KING - 1];

				int[] listOfPseudoLegalMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
			    int index = 0;

				//Checks to see if the king is in check in the current position
				int kingCheckStatus = Board.timesSquareIsAttacked(Constants.WHITE, Constants.findFirstSet(tempWhiteKingBitboard));

				//Generates white pawn moves and captures
				while (tempWhitePawnBitboard != 0) {

                    int pawnIndex = Constants.findFirstSet(tempWhitePawnBitboard);
                    tempWhitePawnBitboard &= (tempWhitePawnBitboard - 1);

					//For pawns that are between the 2nd and 6th ranks, generate single pushes and captures
					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A6) {
						//Generates white pawn single moves
						ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];

						if ((singlePawnMovementFromIndex & Board.allPieces) == 0) {
							int indexOfWhitePawnSingleMoveFromIndex = Constants.findFirstSet(singlePawnMovementFromIndex);
							int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.QUIET_MOVE, Constants.EMPTY);
							listOfPseudoLegalMoves[index++] = moveRepresentation;
						}

						//Generates white pawn captures
						ulong pawnCapturesFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (Board.blackPieces);
						while (pseudoLegalPawnCapturesFromIndex != 0) {

                            int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPawnCapturesFromIndex);
                            pseudoLegalPawnCapturesFromIndex &= (pseudoLegalPawnCapturesFromIndex - 1);

							int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArray[pawnMoveIndex]);
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
						}
					}

					//For pawns that are on the 2nd rank, generate double pawn pushes
					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
						ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
						ulong doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];

						if (((singlePawnMovementFromIndex & Board.allPieces) == 0) && ((doublePawnMovementFromIndex & Board.allPieces) == 0)) {
							int indexOfWhitePawnDoubleMoveFromIndex = Constants.findFirstSet(doublePawnMovementFromIndex);
							int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnDoubleMoveFromIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
						}
					}

					//If en passant is possible, For pawns that are on the 5th rank, generate en passant captures
					if ((Board.enPassantSquare & Constants.RANK_6 ) != 0) {
						if (pawnIndex >= Constants.H5 && pawnIndex <= Constants.A5) {
							ulong pawnCapturesFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];

							if ((pawnCapturesFromIndex & Board.enPassantSquare) != 0) {
								int indexOfWhiteEnPassantCaptureFromIndex = Constants.findFirstSet(pawnCapturesFromIndex & Board.enPassantSquare);
								int moveRepresentation = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhiteEnPassantCaptureFromIndex, Constants.EN_PASSANT_CAPTURE, Constants.BLACK_PAWN);
                                listOfPseudoLegalMoves[index++] = moveRepresentation;
							}
						}
					}

					//For pawns on the 7th rank, generate promotions and promotion captures
					if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
						ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
						
						//Generates white pawn promotions
						if ((singlePawnMovementFromIndex & Board.allPieces) == 0) {
							int indexOfWhitePawnSingleMoveFromIndex = Constants.findFirstSet(singlePawnMovementFromIndex);
                            int moveRepresentationKnightPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.WHITE_KNIGHT);
                            int moveRepresentationBishopPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.WHITE_BISHOP);
                            int moveRepresentationRookPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.WHITE_ROOK);
                            int moveRepresentationQueenPromotion = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.WHITE_QUEEN);

							listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotion;
                            listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotion;
                            listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotion;
                            listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotion;
						}


						//Generates white pawn capture promotions
						ulong pawnCapturesFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (Board.blackPieces);
						
                        while (pseudoLegalPawnCapturesFromIndex != 0) {

                            int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPawnCapturesFromIndex);
                            pseudoLegalPawnCapturesFromIndex &= (pseudoLegalPawnCapturesFromIndex - 1);

							int moveRepresentationKnightPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.WHITE_KNIGHT);
                            int moveRepresentationBishopPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.WHITE_BISHOP);
                            int moveRepresentationRookPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.WHITE_ROOK);
                            int moveRepresentationQueenPromotionCapture = Board.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.WHITE_QUEEN);

							listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotionCapture;
							listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotionCapture;
							listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotionCapture;
							listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotionCapture;
						}
					}
				}

				//generates white knight moves and captures
				while (tempWhiteKnightBitboard != 0) {

                    int knightIndex = Constants.findFirstSet(tempWhiteKnightBitboard);
                    tempWhiteKnightBitboard &= (tempWhiteKnightBitboard - 1);

					ulong knightMovementFromIndex = Constants.knightMoves[knightIndex];
					ulong pseudoLegalKnightMovementFromIndex = knightMovementFromIndex &= (~Board.whitePieces);
					while (pseudoLegalKnightMovementFromIndex != 0) {

                        int knightMoveIndex = Constants.findFirstSet(pseudoLegalKnightMovementFromIndex);
                        pseudoLegalKnightMovementFromIndex &= (pseudoLegalKnightMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[knightMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_KNIGHT, knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						} else if (pieceArray[knightMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_KNIGHT, knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArray[knightMoveIndex]);
						}
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}

				}

                //generates white bishop moves and captures
				while (tempWhiteBishopBitboard != 0) {

                    int bishopIndex = Constants.findFirstSet(tempWhiteBishopBitboard);
                    tempWhiteBishopBitboard &= (tempWhiteBishopBitboard - 1);

					ulong diagonalOccupancy = Board.allPieces & Constants.bishopOccupancyMask[bishopIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[bishopIndex]) >> Constants.bishopMagicShiftNumber[bishopIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[bishopIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~Board.whitePieces);
					
                    while (pseudoLegalBishopMovementFromIndex != 0) {

                        int bishopMoveIndex = Constants.findFirstSet(pseudoLegalBishopMovementFromIndex);
                        pseudoLegalBishopMovementFromIndex &= (pseudoLegalBishopMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[bishopMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_BISHOP, bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a black piece at that location, so generate a capture
						else if (pieceArray[bishopMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_BISHOP, bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArray[bishopMoveIndex]);
						}
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}

				}

				//generates white rook moves and captures
				while (tehpWhiteRookBitboard != 0) {
                    
                    int rookIndex = Constants.findFirstSet(tehpWhiteRookBitboard);
                    tehpWhiteRookBitboard &= (tehpWhiteRookBitboard - 1);

					ulong horizontalVerticalOccupancy = Board.allPieces & Constants.rookOccupancyMask[rookIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[rookIndex]) >> Constants.rookMagicShiftNumber[rookIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[rookIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~Board.whitePieces);
					
					while (pseudoLegalRookMovementFromIndex != 0) {

                        int rookMoveIndex = Constants.findFirstSet(pseudoLegalRookMovementFromIndex);
                        pseudoLegalRookMovementFromIndex &= (pseudoLegalRookMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[rookMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_ROOK, rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a black piece at that location, so generate a capture
						else if (pieceArray[rookMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_ROOK, rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArray[rookMoveIndex]);
						}
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}

				}

				//generates white queen moves and captures
				while (tempWhiteQueenBitboard != 0) {

                    int queenIndex = Constants.findFirstSet(tempWhiteQueenBitboard);
                    tempWhiteQueenBitboard &= (tempWhiteQueenBitboard - 1);
                    
                    ulong diagonalOccupancy = Board.allPieces & Constants.bishopOccupancyMask[queenIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[queenIndex]) >> Constants.bishopMagicShiftNumber[queenIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[queenIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~Board.whitePieces);

					ulong horizontalVerticalOccupancy = Board.allPieces & Constants.rookOccupancyMask[queenIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[queenIndex]) >> Constants.rookMagicShiftNumber[queenIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[queenIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~Board.whitePieces);

					ulong pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
					
					while (pseudoLegalQueenMovementFromIndex != 0) {

                        int queenMoveIndex = Constants.findFirstSet(pseudoLegalQueenMovementFromIndex);
                        pseudoLegalQueenMovementFromIndex &= (pseudoLegalQueenMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[queenMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_QUEEN, queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a black piece at that location, so generate a capture
						else if (pieceArray[queenMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.WHITE_QUEEN, queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArray[queenMoveIndex]);
						}
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}
				}

				//generates white king moves and captures
                int kingIndex = Constants.findFirstSet(tempWhiteKingBitboard);
                ulong kingMovementFromIndex = Constants.kingMoves[kingIndex];
                ulong pseudoLegalKingMovementFromIndex = kingMovementFromIndex &= (~Board.whitePieces);
                
                while (pseudoLegalKingMovementFromIndex != 0) {

                    int kingMoveIndex = Constants.findFirstSet(pseudoLegalKingMovementFromIndex);
                    pseudoLegalKingMovementFromIndex &= (pseudoLegalKingMovementFromIndex - 1);

                    int moveRepresentation = 0x0;

                    if (pieceArray[kingMoveIndex] == Constants.EMPTY) {
                        moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                    } else if (pieceArray[kingMoveIndex] != Constants.EMPTY) {
                        moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArray[kingMoveIndex]);
                    }
                    listOfPseudoLegalMoves[index++] = moveRepresentation;
                }	
				

				//Generates white king castling moves (if the king is not in check)
				if (kingCheckStatus == Constants.NOT_IN_CHECK) {
					if ((Board.whiteShortCastleRights == Constants.CAN_CASTLE) && ((Board.allPieces & Constants.WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.G1, Constants.SHORT_CASTLE, Constants.EMPTY);
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}

					if ((Board.whiteLongCastleRights == Constants.CAN_CASTLE) && ((Board.allPieces & Constants.WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.C1, Constants.LONG_CASTLE, Constants.EMPTY);
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
						
					}
				}
				//returns the list of legal moves
				return listOfPseudoLegalMoves;

			} else if (sideToMove == Constants.BLACK) {

                //Inces of all the pieces
                ulong tempBlackPawnBitboard = Board.arrayOfBitboards[Constants.BLACK_PAWN - 1];
                ulong tempBlackKnightBitboard = Board.arrayOfBitboards[Constants.BLACK_KNIGHT- 1];
                ulong tempBlackBishopBitboard = Board.arrayOfBitboards[Constants.BLACK_BISHOP - 1];
                ulong tempBlackRookBitboard = Board.arrayOfBitboards[Constants.BLACK_ROOK - 1];
                ulong tempBlackQueenBitboard = Board.arrayOfBitboards[Constants.BLACK_QUEEN - 1];
                ulong tempBlackKingBitboard = Board.arrayOfBitboards[Constants.BLACK_KING - 1];

                int[] listOfPseudoLegalMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;
				
				//Checks to see if the king is in check in the current position
				int kingCheckStatus = Board.timesSquareIsAttacked(Constants.BLACK, Constants.findFirstSet(tempBlackKingBitboard));

				//Generates black pawn moves
				while (tempBlackPawnBitboard != 0) {

                    int pawnIndex = Constants.findFirstSet(tempBlackPawnBitboard);
                    tempBlackPawnBitboard &= (tempBlackPawnBitboard - 1);

					if (pawnIndex >= Constants.H3 && pawnIndex <= Constants.A7) {

                         //Generates black pawn single moves
						ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];

						if ((singlePawnMovementFromIndex & Board.allPieces) == 0) {
							int indexOfBlackPawnSingleMoveFromIndex = Constants.findFirstSet(singlePawnMovementFromIndex);
							int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
						}

						//Generates pawn captures
						ulong pawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (Board.whitePieces);
						
						while (pseudoLegalPawnCapturesFromIndex != 0) {

                            int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPawnCapturesFromIndex);
                            pseudoLegalPawnCapturesFromIndex &= (pseudoLegalPawnCapturesFromIndex - 1);

							int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArray[pawnMoveIndex]);
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
						}
					}

					//Generates black pawn double moves
					if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
						ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
						ulong doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];

						if (((singlePawnMovementFromIndex & Board.allPieces) == 0) && ((doublePawnMovementFromIndex & Board.allPieces) == 0)) {
							int indexOfBlackPawnDoubleMoveFromIndex = Constants.findFirstSet(doublePawnMovementFromIndex);
							int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnDoubleMoveFromIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
						}
					}

					//Generates black pawn en passant captures
					if ((Board.enPassantSquare & Constants.RANK_3) != 0) {
						if (pawnIndex >= Constants.H4 && pawnIndex <= Constants.A4) {
							ulong pawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];

							if ((pawnCapturesFromIndex & Board.enPassantSquare) != 0) {
								int indexOfBlackEnPassantCaptureFromIndex = Constants.findFirstSet(pawnCapturesFromIndex & Board.enPassantSquare);
								int moveRepresentation = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackEnPassantCaptureFromIndex, Constants.EN_PASSANT_CAPTURE, Constants.WHITE_PAWN);
                                listOfPseudoLegalMoves[index++] = moveRepresentation;
							}
						}
					}


					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
						ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];

						if ((singlePawnMovementFromIndex & Board.allPieces) == 0) {
							int indexOfBlackPawnSingleMoveFromIndex = Constants.findFirstSet(singlePawnMovementFromIndex);
							int moveRepresentationKnightPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.BLACK_KNIGHT);
                            int moveRepresentationBishopPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.BLACK_BISHOP);
                            int moveRepresentationRookPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.BLACK_ROOK);
                            int moveRepresentationQueenPromotion = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, indexOfBlackPawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, Constants.BLACK_QUEEN);

                            listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotion;
                            listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotion;
                            listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotion;
                            listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotion;
						}

						ulong pawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
						ulong pseudoLegalPawnCapturesFromIndex = pawnCapturesFromIndex &= (Board.whitePieces);
						
						while (pseudoLegalPawnCapturesFromIndex != 0) {

                            int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPawnCapturesFromIndex);
                            pseudoLegalPawnCapturesFromIndex &= (pseudoLegalPawnCapturesFromIndex - 1);

                            int moveRepresentationKnightPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.BLACK_KNIGHT);
                            int moveRepresentationBishopPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.BLACK_BISHOP);
                            int moveRepresentationRookPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.BLACK_ROOK);
                            int moveRepresentationQueenPromotionCapture = Board.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], Constants.BLACK_QUEEN);

                            listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotionCapture;
                            listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotionCapture;
                            listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotionCapture;
                            listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotionCapture;
						}
					}

				}

				

				//Generates black knight moves and captures
				while (tempBlackKnightBitboard != 0) {

                    int knightIndex = Constants.findFirstSet(tempBlackKnightBitboard);
                    tempBlackKnightBitboard &= (tempBlackKnightBitboard - 1);

					ulong knightMovementFromIndex = Constants.knightMoves[knightIndex];
					ulong pseudoLegalKnightMovementFromIndex = knightMovementFromIndex &= (~Board.blackPieces);
					
                    while (pseudoLegalKnightMovementFromIndex != 0) {

                        int knightMoveIndex = Constants.findFirstSet(pseudoLegalKnightMovementFromIndex);
                        pseudoLegalKnightMovementFromIndex &= (pseudoLegalKnightMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[knightMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_KNIGHT, knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						} else if (pieceArray[knightMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_KNIGHT, knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArray[knightMoveIndex]);
						}

                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}
				}

				//generates black bishop moves and captures
				while (tempBlackBishopBitboard != 0) {

                    int bishopIndex = Constants.findFirstSet(tempBlackBishopBitboard);
                    tempBlackBishopBitboard &= (tempBlackBishopBitboard - 1);

					ulong diagonalOccupancy = Board.allPieces & Constants.bishopOccupancyMask[bishopIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[bishopIndex]) >> Constants.bishopMagicShiftNumber[bishopIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[bishopIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~Board.blackPieces);
					
					while (pseudoLegalBishopMovementFromIndex != 0) {

                        int bishopMoveIndex = Constants.findFirstSet(pseudoLegalBishopMovementFromIndex);
                        pseudoLegalBishopMovementFromIndex &= (pseudoLegalBishopMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[bishopMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_BISHOP, bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a white piece at that location, so generate a capture
						else if (pieceArray[bishopMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_BISHOP, bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArray[bishopMoveIndex]);
						}

                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}
				}
				//generates black rook moves and captures
				while (tempBlackRookBitboard != 0) {

                    int rookIndex = Constants.findFirstSet(tempBlackRookBitboard);
                    tempBlackRookBitboard &= (tempBlackRookBitboard - 1);

					ulong horizontalVerticalOccupancy = Board.allPieces & Constants.rookOccupancyMask[rookIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[rookIndex]) >> Constants.rookMagicShiftNumber[rookIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[rookIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~Board.blackPieces);
					
					while (pseudoLegalRookMovementFromIndex != 0) {

                        int rookMoveIndex = Constants.findFirstSet(pseudoLegalRookMovementFromIndex);
                        pseudoLegalRookMovementFromIndex &= (pseudoLegalRookMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[rookMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_ROOK, rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a white piece at that location, so generate a capture
						else if (pieceArray[rookMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_ROOK, rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArray[rookMoveIndex]);
						}

                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}

				}
				//generates black queen moves and captures
				while (tempBlackQueenBitboard != 0) {

                    int queenIndex= Constants.findFirstSet(tempBlackQueenBitboard);
                    tempBlackQueenBitboard &= (tempBlackQueenBitboard - 1);

					ulong diagonalOccupancy = Board.allPieces & Constants.bishopOccupancyMask[queenIndex];
					int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[queenIndex]) >> Constants.bishopMagicShiftNumber[queenIndex]);
					ulong bishopMovesFromIndex = Constants.bishopMoves[queenIndex][indexOfBishopMoveBitboard];

					ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~Board.blackPieces);

					ulong horizontalVerticalOccupancy = Board.allPieces & Constants.rookOccupancyMask[queenIndex];
					int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[queenIndex]) >> Constants.rookMagicShiftNumber[queenIndex]);
					ulong rookMovesFromIndex = Constants.rookMoves[queenIndex][indexOfRookMoveBitboard];

					ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~Board.blackPieces);

					ulong pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
					
					while (pseudoLegalQueenMovementFromIndex != 0) {

                        int queenMoveIndex = Constants.findFirstSet(pseudoLegalQueenMovementFromIndex);
                        pseudoLegalQueenMovementFromIndex &= (pseudoLegalQueenMovementFromIndex - 1);

						int moveRepresentation = 0x0;

						if (pieceArray[queenMoveIndex] == Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_QUEEN, queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
						}
							//If not empty, then must be a white piece at that location, so generate a capture
						else if (pieceArray[queenMoveIndex] != Constants.EMPTY) {
							moveRepresentation = Board.moveEncoder(Constants.BLACK_QUEEN, queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArray[queenMoveIndex]);
						}

                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}
				}

				//generates black king moves and captures
			    int kingIndex = Constants.findFirstSet(tempBlackKingBitboard);
                ulong kingMovementFromIndex = Constants.kingMoves[kingIndex];
                ulong pseudoLegalKingMovementFromIndex = kingMovementFromIndex &= (~Board.blackPieces);
                
                while (pseudoLegalKingMovementFromIndex != 0) {

                    int kingMoveIndex = Constants.findFirstSet(pseudoLegalKingMovementFromIndex);
                    pseudoLegalKingMovementFromIndex &= (pseudoLegalKingMovementFromIndex - 1);

                    int moveRepresentation = 0x0;

                    if (pieceArray[kingMoveIndex] == Constants.EMPTY) {
                        moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                    } else if (pieceArray[kingMoveIndex] != Constants.EMPTY) {
                        moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArray[kingMoveIndex]);
                    }

                    listOfPseudoLegalMoves[index++] = moveRepresentation;
                }	
				

				//Generates black king castling moves (if the king is not in check)
				if (kingCheckStatus == Constants.NOT_IN_CHECK) {
					if ((Board.blackShortCastleRights == Constants.CAN_CASTLE) && ((Board.allPieces & Constants.BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.G8, Constants.SHORT_CASTLE, Constants.EMPTY);
                        listOfPseudoLegalMoves[index++] = moveRepresentation;
					}

					if ((Board.blackLongCastleRights == Constants.CAN_CASTLE) && ((Board.allPieces & Constants.BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.C8, Constants.LONG_CASTLE, Constants.EMPTY);
                        listOfPseudoLegalMoves[index++] = moveRepresentation;

					}
				}
				//returns the list of legal moves
				return listOfPseudoLegalMoves;
			}
			return null;
		}

	    //OTHER METHODS----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //takes in a FEN string, resets the board, and then sets all the instance variables based on it  
        public static void FENToBoard(string FEN) {

            Board.arrayOfBitboards = new ulong[12];
            Board.wPawn = 0x0UL;
            Board.wKnight = 0x0UL;
            Board.wBishop = 0x0UL;
            Board.wRook = 0x0UL;
            Board.wQueen = 0x0UL;
            Board.wKing = 0x0UL;
            Board.bPawn = 0x0UL;
            Board.bKnight = 0x0UL;
            Board.bBishop = 0x0UL;
            Board.bRook = 0x0UL;
            Board.bQueen = 0x0UL;
            Board.bKing = 0x0UL;
            Board.whitePieces = 0x0UL;
            Board.blackPieces = 0x0UL;
            Board.allPieces = 0x0UL;
            Board.pieceArray = new int[64];
            Board.sideToMove = 0;
            Board.whiteShortCastleRights = 0;
            Board.whiteLongCastleRights = 0;
            Board.blackShortCastleRights = 0;
            Board.blackLongCastleRights = 0;
            Board.enPassantSquare = 0x0UL;
            Board.halfmoveNumber = 0;
            Board.HalfMovesSincePawnMoveOrCapture = 0;
            Board.repetionOfPosition = 0;
            Board.blackInCheck = 0;
            Board.blackInCheckmate = 0;
            Board.whiteInCheck = 0;
            Board.whiteInCheckmate = 0;
            Board.stalemate = 0;
            Board.endGame = false;
            Board.lastMove = 0x0;
            Board.evaluationFunctionValue = 0;
            Board.zobristKey = 0x0UL;

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
                        case 'P': Board.wPawn |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'N': Board.wKnight |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'B': Board.wBishop |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'R': Board.wRook |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'Q': Board.wQueen |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'K': Board.wKing |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'p': Board.bPawn |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'n': Board.bKnight |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'b': Board.bBishop |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'r': Board.bRook |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'q': Board.bQueen |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
						case 'k': Board.bKing |= (Convert.ToUInt64(binary, 2) << (i * 8)); index++; break;
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
			Board.arrayOfBitboards[0] = wPawn;
			Board.arrayOfBitboards[1] = wKnight;
			Board.arrayOfBitboards[2] = wBishop;
			Board.arrayOfBitboards[3] = wRook;
			Board.arrayOfBitboards[4] = wQueen;
			Board.arrayOfBitboards[5] = wKing;
			Board.arrayOfBitboards[6] = bPawn;
			Board.arrayOfBitboards[7] = bKnight;
			Board.arrayOfBitboards[8] = bBishop;
			Board.arrayOfBitboards[9] = bRook;
			Board.arrayOfBitboards[10] = bQueen;
			Board.arrayOfBitboards[11] = bKing;

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
						case 'P': Board.pieceArray[7 + 8 * i - index] = Constants.WHITE_PAWN; index++; break;
						case 'N': Board.pieceArray[7 + 8 * i - index] = Constants.WHITE_KNIGHT; index++; break;
						case 'B': Board.pieceArray[7 + 8 * i - index] = Constants.WHITE_BISHOP; index++; break;
						case 'R': Board.pieceArray[7 + 8 * i - index] = Constants.WHITE_ROOK; index++; break;
						case 'Q': Board.pieceArray[7 + 8 * i - index] = Constants.WHITE_QUEEN; index++; break;
						case 'K': Board.pieceArray[7 + 8 * i - index] = Constants.WHITE_KING; index++; break;
						case 'p': Board.pieceArray[7 + 8 * i - index] = Constants.BLACK_PAWN; index++; break;
						case 'n': Board.pieceArray[7 + 8 * i - index] = Constants.BLACK_KNIGHT; index++; break;
						case 'b': Board.pieceArray[7 + 8 * i - index] = Constants.BLACK_BISHOP; index++; break;
						case 'r': Board.pieceArray[7 + 8 * i - index] = Constants.BLACK_ROOK; index++; break;
						case 'q': Board.pieceArray[7 + 8 * i - index] = Constants.BLACK_QUEEN; index++; break;
						case 'k': Board.pieceArray[7 + 8 * i - index] = Constants.BLACK_KING; index++; break;
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
					Board.sideToMove = Constants.WHITE;
                } else if (c == 'b') {
					Board.sideToMove = Constants.BLACK;
                }
            }
            
            //Sets the castling availability variables
            if (FENfields[2] == "-") {
				Board.whiteShortCastleRights = 0;
				Board.whiteLongCastleRights = 0;
				Board.blackShortCastleRights = 0;
				Board.blackLongCastleRights = 0;
            } else if (FENfields[2] != "-") {
                foreach (char c in FENfields[2]) {
                    if (c == 'K') {
						Board.whiteShortCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'Q') {
						Board.whiteLongCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'k') {
						Board.blackShortCastleRights = Constants.CAN_CASTLE;
                    } else if (c == 'q') {
						Board.blackLongCastleRights = Constants.CAN_CASTLE;
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
				Board.enPassantSquare = 0x1UL << (baseOfEPSquare + factorOfEPSquare * 8);
            }
            
            //Checks to see if there is a halfmove clock or move number in the FEN string
            //If there isn't, then it sets the halfmove number clock and move number to 999;
            if (FENfields.Length >= 5) {
                //sets the halfmove clock since last capture or pawn move
                foreach (char c in FENfields[4]) {
					Board.HalfMovesSincePawnMoveOrCapture = (int)Char.GetNumericValue(c);
                }
                //sets the move number
                foreach (char c in FENfields[5]) {
					Board.halfmoveNumber = (int)Char.GetNumericValue(c);
                }
            } else {
				Board.HalfMovesSincePawnMoveOrCapture = 0;
				Board.halfmoveNumber = 0;
            }

            //Sets the repetition number variable
			Board.repetionOfPosition = 0;

            //Computes the white pieces, black pieces, and occupied bitboard by using "or" on all the individual pieces
			Board.whitePieces = wPawn | wKnight | wBishop | wRook | wQueen | wKing;
			Board.blackPieces = bPawn | bKnight | bBishop | bRook | bQueen | bKing;
			Board.allPieces = whitePieces | blackPieces;
        }

        //Encodes the 32-bit int board restore data from the board's current instance variables
        public static int encodeBoardRestoreData() {
            //stores the board restore data in a 32-bit unsigned integer
            //encodes side to move, castling rights, Encodes en passant square, repetition number, half-move clock, and move number
            int boardRestoreData = ((Board.sideToMove << 0) | (Board.whiteShortCastleRights << 2) | (Board.whiteLongCastleRights << 3) | (Board.blackShortCastleRights << 4) 
                | (Board.blackLongCastleRights << 5) | (repetionOfPosition << 12) | (HalfMovesSincePawnMoveOrCapture << 14) | (halfmoveNumber << 20));

            //Calculates the en passant square number (if any) from the en passant bitboard
            //If there is no en-passant square, then we set the bits corresponding to that variable to 0 
            if (enPassantSquare != 0) {
                boardRestoreData |= (Constants.findFirstSet(enPassantSquare) << 6);
            }

            return boardRestoreData;
        }

        //Updates the aggregate bitboards
        private static void updateAggregateBitboards() {
            
            Board.whitePieces = (Board.arrayOfBitboards[Constants.WHITE_PAWN - 1] | Board.arrayOfBitboards[Constants.WHITE_KNIGHT - 1] | Board.arrayOfBitboards[Constants.WHITE_BISHOP - 1] | Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] | Board.arrayOfBitboards[Constants.WHITE_QUEEN - 1] | Board.arrayOfBitboards[Constants.WHITE_KING - 1]);
            Board.blackPieces = (Board.arrayOfBitboards[Constants.BLACK_PAWN - 1] | Board.arrayOfBitboards[Constants.BLACK_KNIGHT - 1] | Board.arrayOfBitboards[Constants.BLACK_BISHOP - 1] | Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] | Board.arrayOfBitboards[Constants.BLACK_QUEEN - 1] | Board.arrayOfBitboards[Constants.BLACK_KING - 1]);
            Board.allPieces = (Board.whitePieces | Board.blackPieces);
        }

	    //Takes information on piece moved, start square, destination square, type of move, and piece captured
		//Creates a 32-bit unsigned integer representing this information
		//bits 0-3 store the piece moved, 4-9 stores start square, 10-15 stores destination square, 16-19 stores move type, 20-23 stores piece captured
		private static int moveEncoder(int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured) {
			int moveRepresentation = (pieceMoved | (startSquare << 4) | (destinationSquare << 10) | (flag << 16) | (pieceCaptured << 20));
            return moveRepresentation;
		}
        private static int moveEncoder(int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured, int piecePromoted) {
            int moveRepresentation = (pieceMoved | (startSquare << 4) | (destinationSquare << 10) | (flag << 16) | (pieceCaptured << 20) | (piecePromoted << 24));
            return moveRepresentation;
        } 

        //Takes in board restore data and restores the board's instance variables (in the unmake move method)
        private static void restoreBoardState(int boardRestoreDataRepresentation) {
            //Sets the side to move, white short/long castle rights, black short/long castle rights from the integer encoding the restore board data
            //Sets the repetition number, half-live clock (since last pawn push/capture), and move number from the integer encoding the restore board data
            Board.sideToMove = ((boardRestoreDataRepresentation & 0x3) >> 0);
            Board.whiteShortCastleRights = ((boardRestoreDataRepresentation & 0x4) >> 2);
            Board.whiteLongCastleRights = ((boardRestoreDataRepresentation & 0x8) >> 3);
            Board.blackShortCastleRights = ((boardRestoreDataRepresentation & 0x10) >> 4);
            Board.blackLongCastleRights = ((boardRestoreDataRepresentation & 0x20) >> 5);
            Board.repetionOfPosition = ((boardRestoreDataRepresentation & 0x3000) >> 12);
            Board.HalfMovesSincePawnMoveOrCapture = ((boardRestoreDataRepresentation & 0xFC000) >> 14);
            Board.halfmoveNumber = ((boardRestoreDataRepresentation & 0x7FF00000) >> 20);

            //Sets the en passant square bitboard from the integer encoding the restore board data (have to convert an int index to a ulong bitboard)
            if (((boardRestoreDataRepresentation & 0xFC0) >> 6) == 0) {
                Board.enPassantSquare = 0x0UL;
            } else {
                Board.enPassantSquare = 0x1UL << ((boardRestoreDataRepresentation & 0xFC0) >> 6);
            }
        }

        //GET METHODS (FOR I/O)----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //gets the array of piece bitboards (returns an array of 12 bitboards)
        //element 0 = white pawn, 1 = white knight, 2 = white bishop, 3 = white rook, 4 = white queen, 5 = white king
        //element 6 = black pawn, 7 = black knight, 8 = black bishop, 9 = black rook, 10 = black queen, 11 = black king
        public static ulong[] getArrayOfPieceBitboards() {
			return Board.arrayOfBitboards;
        }
        //gets the array of aggregate piece bitboards (returns an array of 3 bitboards)
        //element 0 = white pieces, element 1 = black pieces, element 2 = all pieces
        public static ulong[] getArrayOfAggregatePieceBitboards() {
            ulong[] arrayOfAggregateBitboards = {whitePieces, blackPieces, allPieces};
            return arrayOfAggregateBitboards;
        }
        //gets the array of pieces
        public static int[] getPieceArray() {
			return Board.pieceArray;
        }
        //gets the side to move
        public static int getSideToMove() {
			return Board.sideToMove;
        }
        //gets the castling rights (returns an array of 4 bools)
        //element 0 = white short castle rights, 1 = white long castle rights
        //2 = black short castle rights, 3 = black long castle rights
        public static int[] getCastleRights() {
           int[] castleRights = new int[4];

			castleRights[0] = Board.whiteShortCastleRights;
			castleRights[1] = Board.whiteLongCastleRights;
			castleRights[2] = Board.blackShortCastleRights;
			castleRights[3] = Board.blackLongCastleRights;

            return castleRights;
        }
        //gets the En Passant colour and square
        //element 0 = en passant colour, and element 1 = en passant square
        public static ulong getEnPassant() {
			return Board.enPassantSquare;
        }
        //gets the move data
        //element 0 = move number, 1 = half moves since pawn move or capture, 2 = repetition of position
        public static int[] getMoveData() {
           int[] moveData = new int[3];

			moveData[0] = Board.halfmoveNumber;
			moveData[1] = Board.HalfMovesSincePawnMoveOrCapture;
			moveData[2] = Board.repetionOfPosition;

            return moveData;
        }
    }
}
