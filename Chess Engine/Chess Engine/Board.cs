using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Bitboard = System.UInt64;

namespace Chess_Engine {
   
    public static class Board {

        //INSTANCE VARIABLES-----------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Variables that uniquely describe a board state

        //bitboards and array to hold bitboards
        internal static Bitboard[] arrayOfBitboards = new Bitboard[12];
        internal static Bitboard wPawn = 0x0UL;
        internal static Bitboard wKnight = 0x0UL;
        internal static Bitboard wBishop = 0x0UL;
        internal static Bitboard wRook = 0x0UL;
        internal static Bitboard wQueen = 0x0UL;
        internal static Bitboard wKing = 0x0UL;
        internal static Bitboard bPawn = 0x0UL;
        internal static Bitboard bKnight = 0x0UL;
        internal static Bitboard bBishop = 0x0UL;
        internal static Bitboard bRook = 0x0UL;
        internal static Bitboard bQueen = 0x0UL;
        internal static Bitboard bKing = 0x0UL;

        //aggregate bitboards and array to hold aggregate bitboards
        internal static Bitboard[] arrayOfAggregateBitboards = new Bitboard[3];
        internal static Bitboard whitePieces = 0x0UL;
        internal static Bitboard blackPieces = 0x0UL;
        internal static Bitboard allPieces = 0x0UL;

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
        internal static int blackInCheckmate = 0;
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

            //Extracts information (piece moved, start square, destination square,  flag, piece captured, piece promoted)--------
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
            int colourOfPiece = 0;
            
            if (pieceMoved <= Constants.WHITE_KING) {
                colourOfPiece = Constants.WHITE;
            } else if (pieceMoved >= Constants.BLACK_PAWN) {
                colourOfPiece = Constants.BLACK;
            }

            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                
                //updates the bitboard and removes the int representing the piece from the start square of the piece array, and adds an int representing the piece to the destination square of the piece array
                Board.arrayOfBitboards[pieceMoved - 1] ^= (startSquareBitboard | destinationSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard | destinationSquareBitboard);

                Board.pieceArray[startSquare] = Constants.EMPTY;
                Board.pieceArray[destinationSquare] = pieceMoved;
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                Board.arrayOfBitboards[pieceMoved - 1] ^= (startSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard);

                Board.pieceArray[startSquare] = Constants.EMPTY;     
            }

            //If there was a capture, also remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
			//Don't have to update the array because it was already overridden with the capturing piece
			if (flag == Constants.CAPTURE) {

                Board.arrayOfBitboards[pieceCaptured - 1] ^= (destinationSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);
			}
            //If there was an en-passant capture, remove the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
			//Update the array because the pawn destination square and captured pawn are on different squares
            else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    Board.arrayOfBitboards[pieceCaptured - 1] ^= (destinationSquareBitboard >> 8);
                    Board.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard >> 8);

                    Board.pieceArray[destinationSquare - 8] = Constants.EMPTY;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    Board.arrayOfBitboards[pieceCaptured - 1] ^= (destinationSquareBitboard << 8);
                    Board.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard << 8);

                    Board.pieceArray[destinationSquare + 8] = Constants.EMPTY;
                }	
			}
            //If short castle, then move the rook from H1 to F1
            //If short castle, then move the rook from H8 to F8
            else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] ^= (Constants.H1_BITBOARD | Constants.F1_BITBOARD);
                     Board.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.H1_BITBOARD | Constants.F1_BITBOARD);

                    Board.pieceArray[Constants.H1] = Constants.EMPTY;
                    Board.pieceArray[Constants.F1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] ^= (Constants.H8_BITBOARD | Constants.F8_BITBOARD);
                    Board.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.H8_BITBOARD | Constants.F8_BITBOARD);

                    Board.pieceArray[Constants.H8] = Constants.EMPTY;
                    Board.pieceArray[Constants.F8] = Constants.BLACK_ROOK;
                }
            }
            //If long castle, then move the rook from A1 to D1     
            //If long castle, then move the rook from A8 to D8
            else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] ^= (Constants.A1_BITBOARD | Constants.D1_BITBOARD);
                     Board.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.A1_BITBOARD | Constants.D1_BITBOARD);

                    Board.pieceArray[Constants.A1] = Constants.EMPTY;
                    Board.pieceArray[Constants.D1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] ^= (Constants.A8_BITBOARD | Constants.D8_BITBOARD);
                    Board.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.A8_BITBOARD | Constants.D8_BITBOARD);

                    Board.pieceArray[Constants.A8] = Constants.EMPTY;
                    Board.pieceArray[Constants.D8] = Constants.BLACK_ROOK;
                }       
            }
                //If regular promotion, updates the pawn's bitboard, the promoted piece bitboard, and the piece array
            else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                Board.arrayOfBitboards[piecePromoted - 1] ^= destinationSquareBitboard;
                Board.arrayOfAggregateBitboards[colourOfPiece] ^= (destinationSquareBitboard);

                Board.pieceArray[destinationSquare] = piecePromoted;

                //If there was a capture, removes the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                if (flag == Constants.PROMOTION_CAPTURE) {
                    Board.arrayOfBitboards[pieceCaptured - 1] ^= (destinationSquareBitboard);
                    Board.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);
                }
            } 
            
			//updates the aggregate bitboards
            Board.arrayOfAggregateBitboards[Constants.ALL] = ( Board.arrayOfAggregateBitboards[Constants.WHITE] | Board.arrayOfAggregateBitboards[Constants.BLACK]);

            //increments the full-move number (implement later)
            //Implement the half-move clock later (in the moves)
            //Also implement the repetitions later

        }

		//UNMAKE MOVE METHODS--------------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //Method that unmakes a move by restoring the board object's instance variables
        public static void unmakeMove(int unmoveRepresentationInput, int boardRestoreDataRepresentation) {

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
            int colourOfPiece = 0;

            if (pieceMoved <= Constants.WHITE_KING) {
                colourOfPiece = Constants.WHITE;
            } else if (pieceMoved >= Constants.BLACK_PAWN) {
                colourOfPiece = Constants.BLACK;
            }
            
            
            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.DOUBLE_PAWN_PUSH || flag == Constants.EN_PASSANT_CAPTURE || flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {
                Board.arrayOfBitboards[pieceMoved - 1] ^= (startSquareBitboard | destinationSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard | destinationSquareBitboard);

                Board.pieceArray[destinationSquare] = Constants.EMPTY;
                Board.pieceArray[startSquare] = pieceMoved; 
            } else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                Board.arrayOfBitboards[pieceMoved - 1] ^= (startSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece] ^= (startSquareBitboard);

                Board.pieceArray[startSquare] = pieceMoved;    
            }

            //If there was a capture, add to the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
            //Also re-add the captured piece to the array
            if (flag == Constants.CAPTURE) {
                Board.arrayOfBitboards[pieceCaptured - 1] ^= (destinationSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);

                Board.pieceArray[destinationSquare] = pieceCaptured;  
            }
            //If there was an en-passant capture, add the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
            //Also re-add teh captured pawn to the array 
            else if (flag == Constants.EN_PASSANT_CAPTURE) {
                if (pieceMoved == Constants.WHITE_PAWN) {
                    Board.arrayOfBitboards[Constants.BLACK_PAWN - 1] ^= (destinationSquareBitboard >> 8);
                    Board.arrayOfAggregateBitboards[Constants.BLACK] ^= (destinationSquareBitboard >> 8);

                    Board.pieceArray[destinationSquare - 8] = Constants.BLACK_PAWN;
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    Board.arrayOfBitboards[Constants.WHITE_PAWN - 1] ^= (destinationSquareBitboard << 8);
                     Board.arrayOfAggregateBitboards[Constants.WHITE] ^= (destinationSquareBitboard << 8);

                    Board.pieceArray[destinationSquare + 8] = Constants.WHITE_PAWN; 
                }  
            } 
            //If white king short castle, then move the rook from F1 to H1
            //If black king short castle, then move the rook from F8 to H8
            else if (flag == Constants.SHORT_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] ^= (Constants.F1_BITBOARD | Constants.H1_BITBOARD);
                     Board.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.F1_BITBOARD | Constants.H1_BITBOARD);

                    Board.pieceArray[Constants.F1] = Constants.EMPTY;
                    Board.pieceArray[Constants.H1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] ^= (Constants.F8_BITBOARD | Constants.H8_BITBOARD);
                    Board.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.F8_BITBOARD | Constants.H8_BITBOARD);

                    Board.pieceArray[Constants.F8] = Constants.EMPTY;
                    Board.pieceArray[Constants.H8] = Constants.BLACK_ROOK;
                }
            }
           //If king long castle, then move the rook from D1 to A1
            //If black king long castle, then move the rook from D8 to A8
            else if (flag == Constants.LONG_CASTLE) {
                if (pieceMoved == Constants.WHITE_KING) {
                    Board.arrayOfBitboards[Constants.WHITE_ROOK - 1] ^= (Constants.D1_BITBOARD | Constants.A1_BITBOARD);
                     Board.arrayOfAggregateBitboards[Constants.WHITE] ^= (Constants.D1_BITBOARD | Constants.A1_BITBOARD);

                    Board.pieceArray[Constants.D1] = Constants.EMPTY;
                    Board.pieceArray[Constants.A1] = Constants.WHITE_ROOK;
                } else if (pieceMoved == Constants.BLACK_KING) {
                    Board.arrayOfBitboards[Constants.BLACK_ROOK - 1] ^= (Constants.D8_BITBOARD | Constants.A8_BITBOARD);
                    Board.arrayOfAggregateBitboards[Constants.BLACK] ^= (Constants.D8_BITBOARD | Constants.A8_BITBOARD);

                    Board.pieceArray[Constants.D8] = Constants.EMPTY;
                    Board.pieceArray[Constants.A8] = Constants.BLACK_ROOK;
                }
            }
            //If there were promotions, update the promoted piece bitboard
            else if (flag == Constants.PROMOTION || flag == Constants.PROMOTION_CAPTURE) {
                Board.arrayOfBitboards[piecePromoted - 1] ^= (destinationSquareBitboard);
                Board.arrayOfAggregateBitboards[colourOfPiece] ^= (destinationSquareBitboard);

                Board.pieceArray[destinationSquare] = Constants.EMPTY;

                //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                //Also adds the captured piece back to the array
                if (flag == Constants.PROMOTION_CAPTURE) {
                    Board.arrayOfBitboards[pieceCaptured - 1] ^= (destinationSquareBitboard);
                    Board.arrayOfAggregateBitboards[colourOfPiece ^ 1] ^= (destinationSquareBitboard);

                    Board.pieceArray[destinationSquare] = pieceCaptured;
                }
            } 

	        //updates the aggregate bitboards
            Board.arrayOfAggregateBitboards[Constants.ALL] = ( Board.arrayOfAggregateBitboards[Constants.WHITE] | Board.arrayOfAggregateBitboards[Constants.BLACK]);
        }

		//METHOD THAT RETURNS HOW MANY TIMES A CERTAIN SQUARE IS ATTACKED--------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        public static Bitboard getBitboardOfAttackers(int colourUnderAttack, int squareToCheck) {
            ulong bitboardOfAttackers = 0x0UL;

            if (colourUnderAttack == Constants.WHITE) {

                //Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.rookOccupancyMask[squareToCheck];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
                ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

                //Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.bishopOccupancyMask[squareToCheck];
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

                return bitboardOfAttackers;

            } else {

                //Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.rookOccupancyMask[squareToCheck];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
                ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

                //Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.bishopOccupancyMask[squareToCheck];
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

                return bitboardOfAttackers;
            }
        }

        public static int timesSquareIsAttacked(int colourUnderAttack, int squareToCheck) {

            Bitboard bitboardOfAttackers = getBitboardOfAttackers(colourUnderAttack, squareToCheck);
            int numberOfTimesAttacked;

			if (bitboardOfAttackers == 0) {
                return 0;
            } else {
                numberOfTimesAttacked = Constants.popcount(bitboardOfAttackers);
                return numberOfTimesAttacked;
            }
		}

        // Takes in side to move, and returns boolean that gives legality of moves
        // Called after you make a move
        public static bool isMoveLegal(int sideToMove) {

            bool isMoveLegal = false;

            // Finds the corresponding king, and checks to see if it is attacked
            int indexOfKing = Constants.findFirstSet(Board.arrayOfBitboards[Constants.KING + 6 * sideToMove - 1]);
            if (Board.timesSquareIsAttacked(sideToMove, indexOfKing) == Constants.NOT_IN_CHECK) {
                isMoveLegal = true;
            }

            // If it is attacked, will return true
            // If not attacked, will return false
            return isMoveLegal;
        }

        // called before you make a move
        public static bool isInCheck() {

            bool isInCheck = false;

            // Finds the corresponding king, and checks to see if it is attacked
            int indexOfKing = Constants.findFirstSet(Board.arrayOfBitboards[Constants.KING + 6 * Board.sideToMove - 1]);
            if (Board.timesSquareIsAttacked(Board.sideToMove, indexOfKing) != Constants.NOT_IN_CHECK) {
                isInCheck = true;
            }

            // If it is attacked, will return true
            // If not attacked, will return false
            return isInCheck;
        }

        
		//METHOD THAT GENERATES A LIST OF PSDUEO LEGAL MOVES FROM THE CURRENT BOARD POSITION------------------------------------------------------------------------------------
		//Only generates castling moves that don't involve king passing through attacked square (but king might be attacked at destination square)
		//--------------------------------------------------------------------------------------------------------

        public static int[] generateListOfPsdueoLegalMoves() {
			
			// Generates all of the white moves
			if (sideToMove == Constants.WHITE) {

                // Gets temporary piece bitboards (they will be modified by removing the LSB until they equal 0, so can't use the actual piece bitboards)
			    ulong tempWhitePawnBitboard = Board.arrayOfBitboards[Constants.WHITE_PAWN - 1];
			    ulong tempWhiteKnightBitboard = Board.arrayOfBitboards[Constants.WHITE_KNIGHT - 1];
                ulong tempWhiteBishopBitboard = Board.arrayOfBitboards[Constants.WHITE_BISHOP - 1];
                ulong tempWhiteRookBitboard = Board.arrayOfBitboards[Constants.WHITE_ROOK - 1];
                ulong tempWhiteQueenBitboard = Board.arrayOfBitboards[Constants.WHITE_QUEEN - 1];
                ulong tempWhiteKingBitboard = Board.arrayOfBitboards[Constants.WHITE_KING - 1];

                // Initializes the list of pseudo legal moves (can only hold 220 moves), and initializes the array's index (so we know which element to add the move to)
				int[] listOfPseudoLegalMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
			    int index = 0;

				// Checks to see if the king is in check in the current position
				int kingCheckStatus = Board.timesSquareIsAttacked(Constants.WHITE, Constants.findFirstSet(tempWhiteKingBitboard));

				// Loops through all pawns and generates white pawn moves, captures, and promotions
				while (tempWhitePawnBitboard != 0) {

                    // Finds the index of the first white pawn, then removes it from the temporary pawn bitboard
                    int pawnIndex = Constants.findFirstSet(tempWhitePawnBitboard);
                    tempWhitePawnBitboard &= (tempWhitePawnBitboard - 1);

					//For pawns that are between the 2nd and 6th ranks, generate single pushes and captures
					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A6) {
                       
                        // Passes a bitboard of possible pawn single moves to the generate move method (bitboard could be 0)
                        // Method reads bitboard of possible moves, encodes them, adds them to the list, and increments the index by 1
                        Bitboard possiblePawnSingleMoves = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]);
                        index = Board.generatePawnMove(pawnIndex, possiblePawnSingleMoves, listOfPseudoLegalMoves, index, Constants.WHITE);

                        // Passes a bitboard of possible pawn captures to the generate move method (bitboard could be 0)
                        Bitboard possiblePawnCaptures = Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (Board.arrayOfAggregateBitboards[Constants.BLACK]);
                        index = Board.generatePawnCaptures(pawnIndex, possiblePawnCaptures, listOfPseudoLegalMoves, index, Constants.WHITE);
					}
                    //For pawns that are on the 2nd rank, generate double pawn pushes
					if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                        Bitboard singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                        Bitboard doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];
                        Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

					    if (((singlePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
					        pseudoLegalDoubleMoveFromIndex = doublePawnMovementFromIndex;
					    }
					    
                        index = Board.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);
					}
                    //If en passant is possible, For pawns that are on the 5th rank, generate en passant captures
					if ((Board.enPassantSquare & Constants.RANK_6 ) != 0) {
						if (pawnIndex >= Constants.H5 && pawnIndex <= Constants.A5) {
                            Bitboard pseudoLegalEnPassantFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex] & Board.enPassantSquare;
                            index = Board.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);
						}
					}
                    //For pawns on the 7th rank, generate promotions and promotion captures
					if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                        Bitboard pseudoLegalPromotionFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]);
                        index = Board.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);

                        Bitboard pseudoLegalPromotionCaptureFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (Board.arrayOfAggregateBitboards[Constants.BLACK]);
                        index = Board.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);
					}
				}

				//generates white knight moves and captures
				while (tempWhiteKnightBitboard != 0) {
                    int knightIndex = Constants.findFirstSet(tempWhiteKnightBitboard);
                    tempWhiteKnightBitboard &= (tempWhiteKnightBitboard - 1);
                    Bitboard pseudoLegalKnightMovementFromIndex = Constants.knightMoves[knightIndex] & (~ Board.arrayOfAggregateBitboards[Constants.WHITE]);
                    index = Board.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);
				}

                //generates white bishop moves and captures
				while (tempWhiteBishopBitboard != 0) {
                    int bishopIndex = Constants.findFirstSet(tempWhiteBishopBitboard);
                    tempWhiteBishopBitboard &= (tempWhiteBishopBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~ Board.arrayOfAggregateBitboards[Constants.WHITE]));
                    index = Board.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);
				}

				//generates white rook moves and captures
				while (tempWhiteRookBitboard != 0) {
                    int rookIndex = Constants.findFirstSet(tempWhiteRookBitboard);
                    tempWhiteRookBitboard &= (tempWhiteRookBitboard - 1);
                    Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~ Board.arrayOfAggregateBitboards[Constants.WHITE]));
                    index = Board.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE);
				}

				//generates white queen moves and captures
				while (tempWhiteQueenBitboard != 0) {
                    int queenIndex = Constants.findFirstSet(tempWhiteQueenBitboard);
                    tempWhiteQueenBitboard &= (tempWhiteQueenBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~ Board.arrayOfAggregateBitboards[Constants.WHITE]));
                    Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~ Board.arrayOfAggregateBitboards[Constants.WHITE]));
                    Bitboard pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
                    index = Board.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE); 
				}

				//generates white king moves and captures
                int kingIndex = Constants.findFirstSet(tempWhiteKingBitboard);
                Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~ Board.arrayOfAggregateBitboards[Constants.WHITE]);
                index = Board.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex, listOfPseudoLegalMoves, index, Constants.WHITE); 
               
                //Generates white king castling moves (if the king is not in check)
				if (kingCheckStatus == Constants.NOT_IN_CHECK) {
					if ((Board.whiteShortCastleRights == Constants.CAN_CASTLE) && ((Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.G1, Constants.SHORT_CASTLE, Constants.EMPTY);

					    if (Board.timesSquareIsAttacked(Constants.WHITE, Constants.F1) == 0) {
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
					    }
                    }
                    if ((Board.whiteLongCastleRights == Constants.CAN_CASTLE) && ((Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.C1, Constants.LONG_CASTLE, Constants.EMPTY);

                        if (Board.timesSquareIsAttacked(Constants.WHITE, Constants.D1) == 0) {
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
                        }
					}
				}
				//returns the list of legal moves
				return listOfPseudoLegalMoves;

			} 
            
            else if (sideToMove == Constants.BLACK) {

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
                        Bitboard pseudoLegalSinglePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]);
                        index = Board.generatePawnMove(pawnIndex, pseudoLegalSinglePawnMovementFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);

                        Bitboard pseudoLegalPawnCapturesFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex] & ( Board.arrayOfAggregateBitboards[Constants.WHITE]);
                        index = Board.generatePawnCaptures(pawnIndex, pseudoLegalPawnCapturesFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
					}

					//Generates black pawn double moves
					if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                        Bitboard singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                        Bitboard doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];
                        Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                        if (((singlePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                            pseudoLegalDoubleMoveFromIndex = doublePawnMovementFromIndex;
                        }
                        index = Board.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
					}

					//Generates black pawn en passant captures
					if ((Board.enPassantSquare & Constants.RANK_3) != 0) {
						if (pawnIndex >= Constants.H4 && pawnIndex <= Constants.A4) {
                            Bitboard pseudoLegalEnPassantFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex] & Board.enPassantSquare;
                            index = Board.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
						}
					}
                    if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                        Bitboard pseudoLegalPromotionFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]);
                        index = Board.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);

                        Bitboard pseudoLegalPromotionCaptureFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex] & ( Board.arrayOfAggregateBitboards[Constants.WHITE]);
                        index = Board.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
					}
				}

				//Generates black knight moves and captures
				while (tempBlackKnightBitboard != 0) {

                    int knightIndex = Constants.findFirstSet(tempBlackKnightBitboard);
                    tempBlackKnightBitboard &= (tempBlackKnightBitboard - 1);
                    Bitboard pseudoLegalKnightMovementFromIndex = Constants.knightMoves[knightIndex] & (~Board.arrayOfAggregateBitboards[Constants.BLACK]);
                    index = Board.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
				}

				//generates black bishop moves and captures
				while (tempBlackBishopBitboard != 0) {

                    int bishopIndex = Constants.findFirstSet(tempBlackBishopBitboard);
                    tempBlackBishopBitboard &= (tempBlackBishopBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]));
                    index = Board.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);

				}
				//generates black rook moves and captures
				while (tempBlackRookBitboard != 0) {

                    int rookIndex = Constants.findFirstSet(tempBlackRookBitboard);
                    tempBlackRookBitboard &= (tempBlackRookBitboard - 1);
                    Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]));
				    index = Board.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
				}
				//generates black queen moves and captures
				while (tempBlackQueenBitboard != 0) {

                    int queenIndex = Constants.findFirstSet(tempBlackQueenBitboard);
                    tempBlackQueenBitboard &= (tempBlackQueenBitboard - 1);
                    Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]));
                    Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]));
                    Bitboard pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
                    index = Board.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK);
				}

				//generates black king moves and captures
			    int kingIndex = Constants.findFirstSet(tempBlackKingBitboard);
			    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~Board.arrayOfAggregateBitboards[Constants.BLACK]);
                index = Board.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex, listOfPseudoLegalMoves, index, Constants.BLACK); 
                
                //Generates black king castling moves (if the king is not in check)
				if (kingCheckStatus == Constants.NOT_IN_CHECK) {
					if ((Board.blackShortCastleRights == Constants.CAN_CASTLE) && ((Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.G8, Constants.SHORT_CASTLE, Constants.EMPTY);

                        if (Board.timesSquareIsAttacked(Constants.BLACK, Constants.F8) == 0) {
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
                        }
					}

					if ((Board.blackLongCastleRights == Constants.CAN_CASTLE) && ((Board.arrayOfAggregateBitboards[Constants.ALL] & Constants.BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
						int moveRepresentation = Board.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.C8, Constants.LONG_CASTLE, Constants.EMPTY);

                        if (Board.timesSquareIsAttacked(Constants.BLACK, Constants.D8) == 0) {
                            listOfPseudoLegalMoves[index++] = moveRepresentation;
                        }
					}
				}
				//returns the list of legal moves
				return listOfPseudoLegalMoves;
			}
			return null;
		}

        // Takes in the index of the pawn and the bitboard of all pieces, and generates single pawn pushes
        private static int generatePawnMove(int pawnIndex, Bitboard pseudoLegalSinglePawnMoveFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            if (pseudoLegalSinglePawnMoveFromIndex != 0) {
                int indexOfWhitePawnSingleMoveFromIndex = Constants.findFirstSet(pseudoLegalSinglePawnMoveFromIndex);
                int moveRepresentation = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        
        private static int generatePawnCaptures(int pawnIndex, Bitboard pseudoLegalPawnCapturesFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalPawnCapturesFromIndex != 0) {

                int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPawnCapturesFromIndex);
                pseudoLegalPawnCapturesFromIndex &= (pseudoLegalPawnCapturesFromIndex - 1);

                int moveRepresentation = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArray[pawnMoveIndex]);
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generatePawnDoubleMove(int pawnIndex, Bitboard pseudoLegalDoubleMoveFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            if (pseudoLegalDoubleMoveFromIndex != 0) {
                int indexOfWhitePawnDoubleMoveFromIndex = Constants.findFirstSet(pseudoLegalDoubleMoveFromIndex);
                int moveRepresentation = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnDoubleMoveFromIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generatePawnEnPassant(int pawnIndex, Bitboard pseudoLegalEnPassantFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            if (pseudoLegalEnPassantFromIndex != 0) {
                int indexOfWhiteEnPassantCaptureFromIndex = Constants.findFirstSet(pseudoLegalEnPassantFromIndex);
                int moveRepresentation = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhiteEnPassantCaptureFromIndex, Constants.EN_PASSANT_CAPTURE, (Constants.PAWN + 6 - 6 * pieceColour));
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generatePawnPromotion(int pawnIndex, Bitboard pseudoLegalPromotionFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            //Generates white pawn promotions
            if (pseudoLegalPromotionFromIndex != 0) {
                int indexOfWhitePawnSingleMoveFromIndex = Constants.findFirstSet(pseudoLegalPromotionFromIndex);
                int moveRepresentationKnightPromotion = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.KNIGHT + 6 * pieceColour));
                int moveRepresentationBishopPromotion = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.BISHOP + 6 * pieceColour));
                int moveRepresentationRookPromotion = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.ROOK + 6 * pieceColour));
                int moveRepresentationQueenPromotion = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, indexOfWhitePawnSingleMoveFromIndex, Constants.PROMOTION, Constants.EMPTY, (Constants.QUEEN + 6 * pieceColour));

                listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotion;
                listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotion;
                listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotion;
                listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotion;
            }
            return index;
        }

        private static int generatePawnPromotionCapture(int pawnIndex, Bitboard pseudoLegalPromotionCaptureFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalPromotionCaptureFromIndex != 0) {

                int pawnMoveIndex = Constants.findFirstSet(pseudoLegalPromotionCaptureFromIndex);
                pseudoLegalPromotionCaptureFromIndex &= (pseudoLegalPromotionCaptureFromIndex - 1);

                int moveRepresentationKnightPromotionCapture = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.KNIGHT + 6 * pieceColour));
                int moveRepresentationBishopPromotionCapture = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.BISHOP + 6 * pieceColour));
                int moveRepresentationRookPromotionCapture = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.ROOK + 6 * pieceColour));
                int moveRepresentationQueenPromotionCapture = Board.moveEncoder((Constants.PAWN + 6 * pieceColour), pawnIndex, pawnMoveIndex, Constants.PROMOTION_CAPTURE, pieceArray[pawnMoveIndex], (Constants.QUEEN + 6 * pieceColour));

                listOfPseudoLegalMoves[index++] = moveRepresentationKnightPromotionCapture;
                listOfPseudoLegalMoves[index++] = moveRepresentationBishopPromotionCapture;
                listOfPseudoLegalMoves[index++] = moveRepresentationRookPromotionCapture;
                listOfPseudoLegalMoves[index++] = moveRepresentationQueenPromotionCapture;
            }
            return index;
        }

        private static int generateKnightMoves(int knightIndex, Bitboard pseudoLegalKnightMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColor) {
            
            while (pseudoLegalKnightMovementFromIndex != 0) {

                int knightMoveIndex = Constants.findFirstSet(pseudoLegalKnightMovementFromIndex);
                pseudoLegalKnightMovementFromIndex &= (pseudoLegalKnightMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (pieceArray[knightMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.KNIGHT + 6 * pieceColor), knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                } else if (pieceArray[knightMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.KNIGHT + 6 * pieceColor), knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArray[knightMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generateBishopMoves(int bishopIndex, ulong pseudoLegalBishopMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalBishopMovementFromIndex != 0) {

                int bishopMoveIndex = Constants.findFirstSet(pseudoLegalBishopMovementFromIndex);
                pseudoLegalBishopMovementFromIndex &= (pseudoLegalBishopMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (pieceArray[bishopMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.BISHOP + 6 * pieceColour), bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                }
                    //If not empty, then must be a black piece at that location, so generate a capture
                else if (pieceArray[bishopMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.BISHOP + 6 * pieceColour), bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArray[bishopMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generateRookMoves(int rookIndex, ulong pseudoLegalRookMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalRookMovementFromIndex != 0) {

                int rookMoveIndex = Constants.findFirstSet(pseudoLegalRookMovementFromIndex);
                pseudoLegalRookMovementFromIndex &= (pseudoLegalRookMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (pieceArray[rookMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.ROOK + 6 * pieceColour), rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                }
                    //If not empty, then must be a black piece at that location, so generate a capture
                else if (pieceArray[rookMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.ROOK + 6 * pieceColour), rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArray[rookMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generateQueenMoves(int queenIndex, ulong pseudoLegalQueenMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            
            while (pseudoLegalQueenMovementFromIndex != 0) {

                int queenMoveIndex = Constants.findFirstSet(pseudoLegalQueenMovementFromIndex);
                pseudoLegalQueenMovementFromIndex &= (pseudoLegalQueenMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (pieceArray[queenMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.QUEEN + 6 * pieceColour), queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                }
                    //If not empty, then must be a black piece at that location, so generate a capture
                else if (pieceArray[queenMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.QUEEN + 6 * pieceColour), queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArray[queenMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

        private static int generateKingMoves(int kingIndex, Bitboard pseudoLegalKingMovementFromIndex, int[] listOfPseudoLegalMoves, int index, int pieceColour) {
            while (pseudoLegalKingMovementFromIndex != 0) {

                int kingMoveIndex = Constants.findFirstSet(pseudoLegalKingMovementFromIndex);
                pseudoLegalKingMovementFromIndex &= (pseudoLegalKingMovementFromIndex - 1);

                int moveRepresentation = 0x0;

                if (pieceArray[kingMoveIndex] == Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.KING + 6 * pieceColour), kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                } else if (pieceArray[kingMoveIndex] != Constants.EMPTY) {
                    moveRepresentation = Board.moveEncoder((Constants.KING + 6 * pieceColour), kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArray[kingMoveIndex]);
                }
                listOfPseudoLegalMoves[index++] = moveRepresentation;
            }
            return index;
        }

       //Generate rook moves from index
        private static Bitboard generateRookMovesFromIndex(Bitboard allPieces, int index) {
            ulong horizontalVerticalOccupancy = allPieces & Constants.rookOccupancyMask[index];
            int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[index]) >> Constants.rookMagicShiftNumber[index]);
            return Constants.rookMoves[index][indexOfRookMoveBitboard];
        }

        //Generate bishop moves from index
        private static Bitboard generateBishopMovesFromIndex(Bitboard allPieces, int index) {
            ulong diagonalOccupancy = allPieces & Constants.bishopOccupancyMask[index];
            int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[index]) >> Constants.bishopMagicShiftNumber[index]);
            return Constants.bishopMoves[index][indexOfBishopMoveBitboard];
        }



        //CHECK EVASION------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
        public static int[] checkEvasionGenerator() {

            if (Board.sideToMove == Constants.WHITE) {
                // Gets temporary piece bitboards (they will be modified by removing the LSB until they equal 0, so can't use the actual piece bitboards)
                Bitboard tempWhitePawnBitboard = Board.arrayOfBitboards[Constants.WHITE_PAWN - 1];
                Bitboard tempWhiteKnightBitboard = Board.arrayOfBitboards[Constants.WHITE_KNIGHT - 1];
                Bitboard tempWhiteBishopBitboard = Board.arrayOfBitboards[Constants.WHITE_BISHOP - 1];
                Bitboard tempWhiteRookBitboard = Board.arrayOfBitboards[Constants.WHITE_ROOK - 1];
                Bitboard tempWhiteQueenBitboard = Board.arrayOfBitboards[Constants.WHITE_QUEEN - 1];
                Bitboard tempWhiteKingBitboard = Board.arrayOfBitboards[Constants.WHITE_KING - 1];
                Bitboard tempAllPieceBitboard = Board.arrayOfAggregateBitboards[Constants.ALL];

                int kingIndex = Constants.findFirstSet(tempWhiteKingBitboard);
                Bitboard bishopMovesFromKingPosition = Board.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex);
                Bitboard rookMovesFromKingPosition = Board.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex);

                // Initializes the list of pseudo legal moves (can only hold 220 moves), and initializes the array's index (so we know which element to add the move to)
                int[] listOfCheckEvasionMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;
                

                // Checks to see whether the king is in check or double check in the current position
                int kingCheckStatus = Board.timesSquareIsAttacked(Constants.WHITE, kingIndex);

                // If the king is in check
                if (kingCheckStatus == Constants.CHECK) {

                    //Calculates the squares that pieces can move to in order to capture or block the checking piece
                    Bitboard checkingPieceBitboard = Board.getBitboardOfAttackers(Constants.WHITE, kingIndex);
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
                            Bitboard bishopMovesFromChecker = Board.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard)); break;
                        // If the checking piece is a black rook, then can capture or interpose
                        case Constants.BLACK_ROOK:
                            Bitboard rookMovesFromChecker = Board.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard)); break;
                        // If the checking piece is a black queen, then can capture or interpose
                        case Constants.BLACK_QUEEN:
                            if ((bishopMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                bishopMovesFromChecker = Board.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                                blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard));
                            } else if ((rookMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                rookMovesFromChecker = Board.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
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

                            Bitboard possiblePawnSingleMoves = (Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = Board.generatePawnMove(pawnIndex, possiblePawnSingleMoves,listOfCheckEvasionMoves, index, Constants.WHITE);

                            Bitboard possiblePawnCaptures = (Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (Board.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                            index = Board.generatePawnCaptures(pawnIndex, possiblePawnCaptures,listOfCheckEvasionMoves, index, Constants.WHITE);
                        }
                        if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                            Bitboard singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                            Bitboard doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];
                            Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                            if (((singlePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & blockOrCaptureSquares);
                            }

                            index = Board.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                        }
                        if ((Board.enPassantSquare & Constants.RANK_6) != 0) {
                            if (pawnIndex >= Constants.H5 && pawnIndex <= Constants.A5) {
                                Bitboard pseudoLegalEnPassantFromIndex = (Constants.whiteCapturesAndCapturePromotions[pawnIndex] & Board.enPassantSquare);
                                index = Board.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                            }
                        }
                        if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                            Bitboard pseudoLegalPromotionFromIndex = (Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = Board.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);

                            Bitboard pseudoLegalPromotionCaptureFromIndex = (Constants.whiteCapturesAndCapturePromotions[pawnIndex] & (Board.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                            index = Board.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                        }
                    }

                    //generates white knight moves and captures 
                    while (tempWhiteKnightBitboard != 0) {
                        int knightIndex = Constants.findFirstSet(tempWhiteKnightBitboard);
                        tempWhiteKnightBitboard &= (tempWhiteKnightBitboard - 1);
                        Bitboard pseudoLegalKnightMovementFromIndex = (Constants.knightMoves[knightIndex] & (~Board.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = Board.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white bishop moves and captures
                    while (tempWhiteBishopBitboard != 0) {
                        int bishopIndex = Constants.findFirstSet(tempWhiteBishopBitboard);
                        tempWhiteBishopBitboard &= (tempWhiteBishopBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~Board.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = Board.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white rook moves and captures
                    while (tempWhiteRookBitboard != 0) {
                        int rookIndex = Constants.findFirstSet(tempWhiteRookBitboard);
                        tempWhiteRookBitboard &= (tempWhiteRookBitboard - 1);
                        Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~Board.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = Board.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white queen moves and captures
                    while (tempWhiteQueenBitboard != 0) {
                        int queenIndex = Constants.findFirstSet(tempWhiteQueenBitboard);
                        tempWhiteQueenBitboard &= (tempWhiteQueenBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalQueenMovementFromIndex = ((pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & (~Board.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                        index = Board.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);
                    }

                    //generates white king moves and captures
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~Board.arrayOfAggregateBitboards[Constants.WHITE]);
                    index = Board.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);

                    //returns the list of legal moves
                    return listOfCheckEvasionMoves;

                }

                // If the king is in double check
                else if (kingCheckStatus == Constants.DOUBLE_CHECK) {

                    // Only generates king moves
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~Board.arrayOfAggregateBitboards[Constants.WHITE]);
                    index = Board.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.WHITE);

                    return listOfCheckEvasionMoves;
                }
            } 
            // If side to move is black
            else if (Board.sideToMove == Constants.BLACK) {
                
                // Gets temporary piece bitboards (they will be modified by removing the LSB until they equal 0, so can't use the actual piece bitboards)
                Bitboard tempBlackPawnBitboard = Board.arrayOfBitboards[Constants.BLACK_PAWN - 1];
                Bitboard tempBlackKnightBitboard = Board.arrayOfBitboards[Constants.BLACK_KNIGHT - 1];
                Bitboard tempBlackBishopBitboard = Board.arrayOfBitboards[Constants.BLACK_BISHOP - 1];
                Bitboard tempBlackRookBitboard = Board.arrayOfBitboards[Constants.BLACK_ROOK - 1];
                Bitboard tempBlackQueenBitboard = Board.arrayOfBitboards[Constants.BLACK_QUEEN - 1];
                Bitboard tempBlackKingBitboard = Board.arrayOfBitboards[Constants.BLACK_KING - 1];
                Bitboard tempAllPieceBitboard = Board.arrayOfAggregateBitboards[Constants.ALL];

                int kingIndex = Constants.findFirstSet(tempBlackKingBitboard);
                Bitboard bishopMovesFromKingPosition = Board.generateBishopMovesFromIndex(tempAllPieceBitboard, kingIndex);
                Bitboard rookMovesFromKingPosition = Board.generateRookMovesFromIndex(tempAllPieceBitboard, kingIndex);

                // Initializes the list of pseudo legal moves (can only hold 220 moves), and initializes the array's index (so we know which element to add the move to)
                int[]listOfCheckEvasionMoves = new int[Constants.MAX_MOVES_FROM_POSITION];
                int index = 0;

                // Checks to see whether the king is in check or double check in the current position
                int kingCheckStatus = Board.timesSquareIsAttacked(Constants.BLACK, kingIndex);

                // If the king is in check
                if (kingCheckStatus == Constants.CHECK) {

                    //Calculates the squares that pieces can move to in order to capture or block the checking piece
                    Bitboard checkingPieceBitboard = Board.getBitboardOfAttackers(Constants.BLACK, kingIndex);
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
                            Bitboard bishopMovesFromChecker = Board.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard)); break;
                        // If the checking piece is a white rook, then can capture or interpose
                        case Constants.WHITE_ROOK:
                            Bitboard rookMovesFromChecker = Board.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                            blockOrCaptureSquares = (rookMovesFromKingPosition & (rookMovesFromChecker | checkingPieceBitboard)); break;
                        // If the checking piece is a white queen, then can capture or interpose
                        case Constants.WHITE_QUEEN:
                            if ((bishopMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                bishopMovesFromChecker = Board.generateBishopMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
                                blockOrCaptureSquares = (bishopMovesFromKingPosition & (bishopMovesFromChecker | checkingPieceBitboard));
                            } else if ((rookMovesFromKingPosition & checkingPieceBitboard) != 0) {
                                rookMovesFromChecker = Board.generateRookMovesFromIndex(tempAllPieceBitboard, indexOfCheckingPiece);
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

                            Bitboard possiblePawnSingleMoves = (Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = Board.generatePawnMove(pawnIndex, possiblePawnSingleMoves,listOfCheckEvasionMoves, index, Constants.BLACK);

                            Bitboard possiblePawnCaptures = (Constants.blackCapturesAndCapturePromotions[pawnIndex] & (Board.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                            index = Board.generatePawnCaptures(pawnIndex, possiblePawnCaptures,listOfCheckEvasionMoves, index, Constants.BLACK);
                        }
                        if (pawnIndex >= Constants.H7 && pawnIndex <= Constants.A7) {
                            Bitboard singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                            Bitboard doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];
                            Bitboard pseudoLegalDoubleMoveFromIndex = 0x0UL;

                            if (((singlePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0) && ((doublePawnMovementFromIndex & Board.arrayOfAggregateBitboards[Constants.ALL]) == 0)) {
                                pseudoLegalDoubleMoveFromIndex = (doublePawnMovementFromIndex & blockOrCaptureSquares);
                            }

                            index = Board.generatePawnDoubleMove(pawnIndex, pseudoLegalDoubleMoveFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                        }
                        if ((Board.enPassantSquare & Constants.RANK_3) != 0) {
                            if (pawnIndex >= Constants.H4 && pawnIndex <= Constants.A4) {
                                Bitboard pseudoLegalEnPassantFromIndex = (Constants.blackCapturesAndCapturePromotions[pawnIndex] & Board.enPassantSquare);
                                index = Board.generatePawnEnPassant(pawnIndex, pseudoLegalEnPassantFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                            }
                        }
                        if (pawnIndex >= Constants.H2 && pawnIndex <= Constants.A2) {
                            Bitboard pseudoLegalPromotionFromIndex = (Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex] & (~Board.arrayOfAggregateBitboards[Constants.ALL]) & blockOrCaptureSquares);
                            index = Board.generatePawnPromotion(pawnIndex, pseudoLegalPromotionFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);

                            Bitboard pseudoLegalPromotionCaptureFromIndex = (Constants.blackCapturesAndCapturePromotions[pawnIndex] & (Board.arrayOfAggregateBitboards[Constants.WHITE]) & blockOrCaptureSquares);
                            index = Board.generatePawnPromotionCapture(pawnIndex, pseudoLegalPromotionCaptureFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                        }
                    }

                    //generates black knight moves and captures
                    while (tempBlackKnightBitboard != 0) {
                        int knightIndex = Constants.findFirstSet(tempBlackKnightBitboard);
                        tempBlackKnightBitboard &= (tempBlackKnightBitboard - 1);
                        Bitboard pseudoLegalKnightMovementFromIndex = (Constants.knightMoves[knightIndex] & (~Board.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = Board.generateKnightMoves(knightIndex, pseudoLegalKnightMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black bishop moves and captures
                    while (tempBlackBishopBitboard != 0) {
                        int bishopIndex = Constants.findFirstSet(tempBlackBishopBitboard);
                        tempBlackBishopBitboard &= (tempBlackBishopBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], bishopIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = Board.generateBishopMoves(bishopIndex, pseudoLegalBishopMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black rook moves and captures
                    while (tempBlackRookBitboard != 0) {
                        int rookIndex = Constants.findFirstSet(tempBlackRookBitboard);
                        tempBlackRookBitboard &= (tempBlackRookBitboard - 1);
                        Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], rookIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = Board.generateRookMoves(rookIndex, pseudoLegalRookMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black queen moves and captures
                    while (tempBlackQueenBitboard != 0) {
                        int queenIndex = Constants.findFirstSet(tempBlackQueenBitboard);
                        tempBlackQueenBitboard &= (tempBlackQueenBitboard - 1);
                        Bitboard pseudoLegalBishopMovementFromIndex = (Board.generateBishopMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalRookMovementFromIndex = (Board.generateRookMovesFromIndex(Board.arrayOfAggregateBitboards[Constants.ALL], queenIndex));
                        Bitboard pseudoLegalQueenMovementFromIndex = ((pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex) & (~Board.arrayOfAggregateBitboards[Constants.BLACK]) & blockOrCaptureSquares);
                        index = Board.generateQueenMoves(queenIndex, pseudoLegalQueenMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);
                    }

                    //generates black king moves and captures
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~Board.arrayOfAggregateBitboards[Constants.BLACK]);
                    index = Board.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);

                    //returns the list of legal moves
                    return listOfCheckEvasionMoves;

                }

                // If the king is in double check
                else if (kingCheckStatus == Constants.DOUBLE_CHECK) {

                    // Only generates king moves
                    Bitboard pseudoLegalKingMovementFromIndex = Constants.kingMoves[kingIndex] & (~Board.arrayOfAggregateBitboards[Constants.BLACK]);
                    index = Board.generateKingMoves(kingIndex, pseudoLegalKingMovementFromIndex,listOfCheckEvasionMoves, index, Constants.BLACK);

                    return listOfCheckEvasionMoves;
                }
            }
            return null;
        }
        
        
	    //OTHER METHODS----------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------

        //takes in a FEN string, resets the board, and then sets all the instance variables based on it  
        public static void FENToBoard(string FEN) {

            Board.arrayOfBitboards = new ulong[12];
            Board.wPawn = Board.wKnight = Board.wBishop = Board.wRook = Board.wQueen = Board.wKing = 0x0UL;
            Board.bPawn = Board.bKnight = Board.bBishop = Board.bRook = Board.bQueen = Board.bKing = 0x0UL;
            Board.whitePieces = Board.blackPieces = Board.allPieces = 0x0UL;
            Board.arrayOfAggregateBitboards[0] = Board.arrayOfAggregateBitboards[1] = Board.arrayOfAggregateBitboards[2] = 0;
            Board.pieceArray = new int[64];
            Board.sideToMove = 0;
            Board.whiteShortCastleRights = Board.whiteLongCastleRights = Board.blackShortCastleRights = Board.blackLongCastleRights = 0;
            Board.enPassantSquare = 0x0UL;
            Board.halfmoveNumber = 0;
            Board.HalfMovesSincePawnMoveOrCapture = 0;
            Board.repetionOfPosition = 0;
            Board.blackInCheckmate = 0;
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

            Board.arrayOfAggregateBitboards[0] = Board.whitePieces;
            Board.arrayOfAggregateBitboards[1] = Board.blackPieces;
            Board.arrayOfAggregateBitboards[2] = Board.allPieces;

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
            return Board.arrayOfAggregateBitboards;
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
