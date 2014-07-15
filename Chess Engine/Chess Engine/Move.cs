using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Move {

        //METHODS THAT ENCODE MOVES AND BOARD INSTANCE VARIABLES INTO U32-INT------------------------------------------

        //Takes information on piece moved, start square, destination square, type of move, and piece captured
        //Creates a 32-bit unsigned integer representing this information
        //bits 0-3 store the piece moved, 4-9 stores start square, 10-15 stores destination square, 16-19 stores move type, 20-23 stores piece captured
        public static uint moveEncoder(int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured) {
            int moveRepresentation = 0x0;

            moveRepresentation |= pieceMoved;
            moveRepresentation |= startSquare << 4;
            moveRepresentation |= destinationSquare << 10;
            moveRepresentation |= flag << 16;
            
            //If no piece is captured, then we set the bits corresponding to that variable to 15 (the maximum value for 4 bits)
            if (pieceCaptured == 0) {
                moveRepresentation |= 15 << 20;
            } else if (pieceCaptured != 0) {
                moveRepresentation |= pieceCaptured << 20; 
            }
            return (uint)moveRepresentation;
        }

        //Takes information on side to move, castling rights, en-passant square, en-passant color, move number, half-move clock
        //Creates a 32-bit unsigned integer representing this information
        //Have to convert the bitboard for en-passant into an index
        public static uint boardRestoreDataEncoder(int sideToMove, int whiteShortCastle, int whiteLongCastle,
            int blackShortCastle, int blackLongCastle, int enPassantSquare, int halfMoveClock,
            int moveNumber, int repetitionNumber) {
            int boardRestoreDataRepresentation = 0x0;

            //encodes castling rights
            boardRestoreDataRepresentation |= sideToMove << 0;
            boardRestoreDataRepresentation |= whiteShortCastle << 2;
            boardRestoreDataRepresentation |= whiteLongCastle << 3;
            boardRestoreDataRepresentation |= blackShortCastle << 4;
            boardRestoreDataRepresentation |= blackLongCastle << 5;
            
            //Encodes en passant square
            //If there is no en-passant square, then we set the bits corresponding to that variable to 63 (the maximum value for 6 bits)
            if (enPassantSquare == 0) {
                boardRestoreDataRepresentation |= 63 << 6;
            } else if (enPassantSquare != 0) {
                boardRestoreDataRepresentation |= enPassantSquare << 6;
            }

            //encodes repetition number
            boardRestoreDataRepresentation |= repetitionNumber << 12;
            
            //encodes half-move clock
            //If there is no half-move clock, then we set the bits corresponding to that variable to 63 (the maximum value for 6 bits)
            if (halfMoveClock == -1) {
                boardRestoreDataRepresentation |= 63 << 14;
            } else if (halfMoveClock != -1) {
                boardRestoreDataRepresentation |= halfMoveClock << 14;
            }
            
            //encodes move number
            //If there is no move number, then we set the bits corresponding to that variable to 4095 (the maximum value for 12 bits)
            if (moveNumber == -1) {
                boardRestoreDataRepresentation |= 4095 << 20;
            } else if (moveNumber != -1) {
                boardRestoreDataRepresentation |= moveNumber << 20;
            }

            //returns the encoded board restore data
            return (uint) boardRestoreDataRepresentation;
        }


        //METHODS THAT DECODE U32-INT REPRESENTING MOVES AND BOARD INSTANCE VARIABLES----------------------------

        //Extracts the piece moved from the integer that encodes the move
        public static int getPieceMoved(uint moveRepresentation) {
            int pieceMoved = (int)((moveRepresentation & 0xF) >> 0);
            return pieceMoved;
        }
        //Extracts the start square from the integer that encodes the move
        public static int getStartSquare(uint moveRepresentation) {
            int startSquare = (int)((moveRepresentation & 0x3F0) >> 4);
            return startSquare;
        }
        //Extracts the destination square from the integer that encodes the move
        public static int getDestinationSquare(uint moveRepresentation) {
            int destinationSquare = (int)((moveRepresentation & 0xFC00) >> 10);
            return destinationSquare;
        }
        //Extracts the flag from the integer that encodes the move
        public static int getFlag(uint moveRepresentation) {
            int flag = (int)((moveRepresentation & 0xF0000) >> 16);
            return flag;
        }
        //Extracts the piece captured from the integer that encodes the move
        //If we extract 15, then we know that there was no piece captured and return 0
        public static int getPieceCaptured(uint moveRepresentation) {
            int pieceCaptured = (int)((moveRepresentation & 0xF00000) >> 20);

            if (pieceCaptured == 15) {
                return 0;
            } else {
                return pieceCaptured;
            }
        }


        //Extracts the side to move from the integer encoding the restore board data
        private static int getsideToMove(uint boardRestoreDataRepresentation) {
            int sideToMove = (int)((boardRestoreDataRepresentation & 0x3) >> 0);
            return sideToMove;
        }
        //Extracts the white short castle rights from the integer encoding the restore board data
        private static int getWhiteShortCastleRights(uint boardRestoreDataRepresentation) {
            int whiteShortCastleRights = (int)((boardRestoreDataRepresentation & 0x4) >> 2);
            return whiteShortCastleRights;
        }
        //Extracts the white long castle rights from the integer encoding the restore board data
        private static int getWhiteLongCastleRights(uint boardRestoreDataRepresentation) {
            int whiteLongCastleRights = (int)((boardRestoreDataRepresentation & 0x8) >> 3);
            return whiteLongCastleRights;
        }
        //Extracts the black short castle rights from the integer encoding the restore board data
        private static int getBlackShortCastleRights(uint boardRestoreDataRepresentation) {
            int blackShortCastleRights = (int)((boardRestoreDataRepresentation & 0x10) >> 4);
            return blackShortCastleRights;
        }
        //Extracts the black long castle rights from the integer encoding the restore board data
        private static int getBlackLongCastleRights(uint boardRestoreDataRepresentation) {
            int blackLongCastleRights = (int)((boardRestoreDataRepresentation & 0x20) >> 5);
            return blackLongCastleRights;
        }
        //Extracts the en passant square from the integer encoding the restore board data
        //If we extract 63, then we know that there was no en passant square and return 0 (so that the instance variable for EQ square can be set to 0x0UL)
        private static int getEnPassantSquare(uint boardRestoreDataRepresentation) {
            int enPassantSquare = (int)((boardRestoreDataRepresentation & 0xFC0) >> 6);

            if (enPassantSquare == 63) {
                return 0;
            } else {
                return enPassantSquare;
            }
        }
        //Extracts the repetition number from the integer encoding the restore board data
        private static int getRepetitionNumber(uint boardRestoreDataRepresentation) {
            int repetitionNumber = (int)((boardRestoreDataRepresentation & 0x3000) >> 12);
            return repetitionNumber;
        }
        
        //Extracts the half move clock (since last pawn push/capture) from the integer encoding the restore board data
        //If we extract 63, then we know that there was no half-move clock and return -1 (original value of instance variable)
        private static int getHalfMoveClock(uint boardRestoreDataRepresentation) {
            int halfMoveClock = (int)((boardRestoreDataRepresentation & 0xFC000) >> 14);

            if (halfMoveClock == 63) {
                return -1;
            } else {
                return halfMoveClock;
            } 
        }
        //Extracts the move number from the integer encoding the restore board data
        //If we extract 4095, then we know that there was no move number and return -1 (original value of instance variable)
        private static int getMoveNumber(uint boardRestoreDataRepresentation) {
            int moveNumber = (int)((boardRestoreDataRepresentation & 0xFFF00000) >> 20);

            if (moveNumber == 4095) {
                return -1;
            } else {
                return moveNumber;  
            }
        }
        


        //MAKE AND UNMAKE MOVE METHODS-----------------------------------------------------------------------

        //Static method that makes a move by updating the board object's instance variables
        public static uint makeMove(uint moveRepresentationInput, Board inputBoard) {

            //stores the board restore data in a 32-bit unsigned integer
            uint boardRestoreData = 0;

            int sideToMove = inputBoard.getSideToMove();
            int whiteShortCastleRights = inputBoard.getCastleRights()[0];
            int whiteLongCastleRights = inputBoard.getCastleRights()[1];
            int blackShortCastleRights = inputBoard.getCastleRights()[2];
            int blackLongCastleRights = inputBoard.getCastleRights()[3];
            ulong enPassantSquare = inputBoard.getEnPassant();

            //Calculates the en passant square number (if any) from the en passant bitboard
            int enPassantSquareNumber = 0;
            List<int> enPassantIndices = Constants.bitScan(enPassantSquare);
            if (enPassantIndices.Count == 0) {
                enPassantSquareNumber = 0;
            } else if (enPassantIndices.Count == 1) {
                enPassantSquareNumber = enPassantIndices.ElementAt(0);
            }
            int moveNumber = inputBoard.getMoveData()[0];
            int halfMoveClock = inputBoard.getMoveData()[1];
            int repetitionNumber = inputBoard.getMoveData()[2];

            boardRestoreData = boardRestoreDataEncoder(sideToMove, whiteShortCastleRights, whiteLongCastleRights,
                blackShortCastleRights, blackLongCastleRights, enPassantSquareNumber, halfMoveClock, moveNumber,
                repetitionNumber);

            //Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
            int pieceMoved = getPieceMoved(moveRepresentationInput);
            int startSquare = getStartSquare(moveRepresentationInput);
            int destinationSquare = getDestinationSquare(moveRepresentationInput);
            int flag = getFlag(moveRepresentationInput);
            int pieceCaptured = getPieceCaptured(moveRepresentationInput);

            //Gets the array of bitboards and piece array
            ulong[] arrayOfBitboards = inputBoard.getArrayOfPieceBitboards();
            int[] pieceArray = inputBoard.getPieceArray();

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
                    inputBoard.setWhiteShortCastle(0);
                    inputBoard.setWhiteLongCastle(0);
                } else if (pieceMoved == Constants.BLACK_KING) {
                    inputBoard.setBlackShortCastle(0);
                    inputBoard.setBlackLongCastle(0);
                } else if (pieceMoved == Constants.WHITE_ROOK && startSquare == Constants.A1) {
                    inputBoard.setWhiteLongCastle(0);
                } else if (pieceMoved == Constants.WHITE_ROOK && startSquare == Constants.H1) {
                    inputBoard.setWhiteShortCastle(0);
                } else if (pieceMoved == Constants.BLACK_ROOK && startSquare == Constants.A8) {
                    inputBoard.setBlackLongCastle(0);
                } else if (pieceMoved == Constants.BLACK_ROOK && startSquare == Constants.H8) {
                    inputBoard.setBlackShortCastle(0);
                }

                //Updates the castling rights instance variables if the rook got captured
                if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.A1) {
                    inputBoard.setWhiteLongCastle(0);
                } else if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.H1) {
                    inputBoard.setWhiteShortCastle(0);
                } else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.A8) {
                    inputBoard.setBlackLongCastle(0);
                } else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.H8) {
                    inputBoard.setBlackShortCastle(0);
                }

                //sets the en Passant square to 0x0UL;
                inputBoard.setEnPassantSquare(0x0UL);

                //Gets the corresponding piece bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong pieceBitboard = arrayOfBitboards[pieceMoved - 1];
                //Removes the bit corresponding to the start square, and adds a bit corresponding with the destination square
                pieceBitboard &= (~startSquareBitboard);
                pieceBitboard |= destinationSquareBitboard;
                //Removes the int representing the piece from the start square of the piece array, and adds an int representing the piece to the destination square of the piece array
                pieceArray[startSquare] = 0;
                pieceArray[destinationSquare] = pieceMoved;
                //updates the bitboard and the piece array
                inputBoard.setPieceBitboard(pieceMoved, pieceBitboard, pieceArray);

                //If there was a capture, remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                //Don't have to update the array because it was already overridden with the capturing piece
                if (flag == Constants.CAPTURE) {
                    ulong capturedPieceBitboard = arrayOfBitboards[pieceCaptured - 1];
                    capturedPieceBitboard &= (~destinationSquareBitboard);
                    inputBoard.setPieceBitboard(pieceCaptured, capturedPieceBitboard);
                }

                //If there was an en-passant capture, remove the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
                //Update the array because the pawn destination square and captured pawn are on different squares
                if (flag == Constants.EN_PASSANT_CAPTURE) {
                    if (pieceMoved == Constants.WHITE_PAWN) {
                        ulong capturedPawnBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPawnBitboard &= (~(destinationSquareBitboard >> 8));
                        pieceArray[destinationSquare - 8] = 0;
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPawnBitboard, pieceArray);
                    } else if (pieceMoved == Constants.BLACK_PAWN) {
                        ulong capturedPawnBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPawnBitboard &= (~(destinationSquareBitboard << 8));
                        pieceArray[destinationSquare + 8] = 0;
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPawnBitboard, pieceArray);
                    }
                }
            }

            //Updates the pawn bitboard and piece array
            //Also updates the en passant instance variables
            else if (flag == Constants.DOUBLE_PAWN_PUSH) {

                //Updates the en passant square instance variable
                if (pieceMoved == Constants.WHITE_PAWN) {
                    int indexOfEnPassant = destinationSquare - 8;
                    ulong enPassantBitboard = 0x1UL << indexOfEnPassant;
                    inputBoard.setEnPassantSquare(enPassantBitboard);
                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    int indexOfEnPassant = destinationSquare + 8;
                    ulong enPassantBitboard = 0x1UL << indexOfEnPassant;
                    inputBoard.setEnPassantSquare(enPassantBitboard);
                }

                //Gets the white/black pawn bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong pawnBitboard = arrayOfBitboards[pieceMoved - 1];
                //Removes the bit corresponding to the start square, and adds a bit corresponding with the destination square
                pawnBitboard &= ~startSquareBitboard;
                pawnBitboard |= destinationSquareBitboard;
                //Removes the int representing the pawn from the start square of the piece array, and adds an int representing the pawn to the destination square of the piece array
                pieceArray[startSquare] = 0;
                pieceArray[destinationSquare] = pieceMoved;
                //updates the bitboard and the piece array
                inputBoard.setPieceBitboard(pieceMoved, pawnBitboard, pieceArray);
            }

            //Updates the king and rook bitboard and piece array
            //Also updates the castling instance variable, sets en passant square to 0
            else if (flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {

                //Updates the castling rights instance variables (sets them to false)
                if (pieceMoved == Constants.WHITE_KING) {
                    inputBoard.setWhiteShortCastle(0);
                    inputBoard.setWhiteLongCastle(0);
                } else if (pieceMoved == Constants.BLACK_KING) {
                    inputBoard.setBlackShortCastle(0);
                    inputBoard.setBlackLongCastle(0);
                }
                //sets the en Passant square to 0x0UL;
                inputBoard.setEnPassantSquare(0x0UL);
               
                //Gets the white/black king bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong kingBitboard = arrayOfBitboards[pieceMoved - 1];
                //Removes the bit corresponding to the start square, and adds a bit corresponding with the destination square
                kingBitboard &= ~startSquareBitboard;
                kingBitboard |= destinationSquareBitboard;
                //Removes the int representing the king from the start square of the piece array, and adds an int representing the king to the destination square of the piece array
                pieceArray[startSquare] = 0;
                pieceArray[destinationSquare] = pieceMoved;
                //updates the bitboard and the piece array
                inputBoard.setPieceBitboard(pieceMoved, kingBitboard, pieceArray);

                if (pieceMoved == Constants.WHITE_KING) {
                    ulong rookBitboard = arrayOfBitboards[Constants.WHITE_ROOK - 1];

                    //If short castle, then move the rook from H1 to F1
                    if (flag == Constants.SHORT_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.H1);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.F1;

                        rookBitboard &= (~rookStartSquareBitboard);
                        rookBitboard |= rookDestinationSquareBitboard;

                        pieceArray[Constants.H1] = 0;
                        pieceArray[Constants.F1] = Constants.WHITE_ROOK;

                    } 
                    //If long castle, then move the rook from A1 to D1
                    else if (flag == Constants.LONG_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.A1);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.D1;

                        rookBitboard &= (~rookStartSquareBitboard);
                        rookBitboard |= rookDestinationSquareBitboard;

                        pieceArray[Constants.A1] = 0;
                        pieceArray[Constants.D1] = Constants.WHITE_ROOK;
                    }
                    inputBoard.setPieceBitboard(Constants.WHITE_ROOK, rookBitboard, pieceArray);

                } else if (pieceMoved == Constants.BLACK_KING) {
                    ulong rookBitboard = arrayOfBitboards[Constants.BLACK_ROOK - 1];

                    //If short castle, then move the rook from H8 to F8
                    if (flag == Constants.SHORT_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.H8);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.F8;

                        rookBitboard &= ~(rookStartSquareBitboard);
                        rookBitboard |= rookDestinationSquareBitboard;

                        pieceArray[Constants.H8] = 0;
                        pieceArray[Constants.F8] = Constants.BLACK_ROOK;
                    } 
                    //If long castle, then move the rook from A8 to D8
                    else if (flag == Constants.LONG_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.A8);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.D8;

                        rookBitboard &= ~(rookStartSquareBitboard);
                        rookBitboard |= rookDestinationSquareBitboard;

                        pieceArray[Constants.A8] = 0;
                        pieceArray[Constants.D8] = Constants.BLACK_ROOK;
                    }
                    inputBoard.setPieceBitboard(Constants.BLACK_ROOK, rookBitboard, pieceArray);
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
                    inputBoard.setWhiteLongCastle(0);
                } else if (pieceCaptured == Constants.WHITE_ROOK && destinationSquare == Constants.H1) {
                    inputBoard.setWhiteShortCastle(0);
                } else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.A8) {
                    inputBoard.setBlackLongCastle(0);
                } else if (pieceCaptured == Constants.BLACK_ROOK && destinationSquare == Constants.H8) {
                    inputBoard.setBlackShortCastle(0);
                }

                //sets the en Passant square to 0x0UL;
                inputBoard.setEnPassantSquare(0x0UL);

                //Gets the corresponding pawn bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong pawnBitboard = arrayOfBitboards[pieceMoved - 1];

                if (pieceMoved == Constants.WHITE_PAWN) {
                    //Removes the bit corresponding to the start square 
                    pawnBitboard &= (~startSquareBitboard);
                    pieceArray[startSquare] = 0;
                    inputBoard.setPieceBitboard(pieceMoved, pawnBitboard, pieceArray);

                    //Adds a bit corresponding to the destination square in the promoted piece bitboard
                    if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_KNIGHT - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                        pieceArray[destinationSquare] = Constants.WHITE_KNIGHT;
                        inputBoard.setPieceBitboard(Constants.WHITE_KNIGHT, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_BISHOP - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                        pieceArray[destinationSquare] = Constants.WHITE_BISHOP;
                        inputBoard.setPieceBitboard(Constants.WHITE_BISHOP, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_ROOK - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                         pieceArray[destinationSquare] = Constants.WHITE_ROOK;
                        inputBoard.setPieceBitboard(Constants.WHITE_ROOK, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_QUEEN - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                         pieceArray[destinationSquare] = Constants.WHITE_QUEEN;
                        inputBoard.setPieceBitboard(Constants.WHITE_QUEEN, promotedPieceBitboard, pieceArray);
                    }

                    //If there was a capture, remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                    //Don't have to update the array because it was already overridden with the capturing piece
                    if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong capturedPieceBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPieceBitboard &= (~destinationSquareBitboard);
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPieceBitboard);
                    }

                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    //Removes the bit corresponding to the start square 
                    pawnBitboard &= (~startSquareBitboard);
                    pieceArray[startSquare] = 0;
                    inputBoard.setPieceBitboard(pieceMoved, pawnBitboard, pieceArray);

                    //Adds a bit corresponding to the destination square in the promoted piece bitboard
                    if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_KNIGHT - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                        pieceArray[destinationSquare] = Constants.BLACK_KNIGHT;
                        inputBoard.setPieceBitboard(Constants.BLACK_KNIGHT, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_BISHOP - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                        pieceArray[destinationSquare] = Constants.BLACK_BISHOP;
                        inputBoard.setPieceBitboard(Constants.BLACK_BISHOP, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_ROOK - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                         pieceArray[destinationSquare] = Constants.BLACK_ROOK;
                        inputBoard.setPieceBitboard(Constants.BLACK_ROOK, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_QUEEN - 1];
                        promotedPieceBitboard |= destinationSquareBitboard;
                         pieceArray[destinationSquare] = Constants.BLACK_QUEEN;
                        inputBoard.setPieceBitboard(Constants.BLACK_QUEEN, promotedPieceBitboard, pieceArray);
                    }

                    //If there was a capture, remove the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                    //Don't have to update the array because it was already overridden with the capturing piece
                    if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong capturedPieceBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPieceBitboard &= (~destinationSquareBitboard);
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPieceBitboard);
                    }
                }
            }


            //sets the side to move to the other player
            if (sideToMove == Constants.WHITE) {
                inputBoard.setSideToMove(Constants.BLACK);
            } else if (sideToMove == Constants.BLACK) {
                inputBoard.setSideToMove(Constants.WHITE);
            }

            //increments the full-move number (implement later)
            //Implement the half-move clock later (in the moves)
            //Also implement the repetitions later

            //returns the board restore data
            return boardRestoreData;
        }

        //static method that unmakes a move by restoring the board object's instance variables
        public static void unmakeMove(uint unmoveRepresentationInput, Board inputBoard, uint boardRestoreDataRepresentation) {

            //extracts the individual fields of the board restore data from the 32-bit unsigned integer
            int sideToMove = getsideToMove(boardRestoreDataRepresentation);
            int whiteShortCastleRights = getWhiteShortCastleRights(boardRestoreDataRepresentation);
            int whiteLongCastleRights = getWhiteLongCastleRights(boardRestoreDataRepresentation);
            int blackShortCastleRights = getBlackShortCastleRights(boardRestoreDataRepresentation);
            int blackLongCastleRights = getBlackLongCastleRights(boardRestoreDataRepresentation);
            int enPassantSquare = getEnPassantSquare(boardRestoreDataRepresentation);

            //Creates the en passant bitboard from the en passant square (if any)
            ulong enPassantBitboard = 0x0UL;

            if (enPassantSquare == 0) {
                enPassantBitboard = 0x0UL;
            } else if (enPassantSquare != 0) {
                enPassantBitboard = 0x1UL << enPassantSquare;
            }

            int moveNumber = getMoveNumber(boardRestoreDataRepresentation);
            int halfMoveClock = getHalfMoveClock(boardRestoreDataRepresentation);
            int repetitionNumber = getRepetitionNumber(boardRestoreDataRepresentation);

            //Restores the instance variable of the board
            inputBoard.setSideToMove(sideToMove);
            inputBoard.setWhiteShortCastle(whiteShortCastleRights);
            inputBoard.setWhiteLongCastle(whiteLongCastleRights);
            inputBoard.setBlackShortCastle(blackShortCastleRights);
            inputBoard.setBlackLongCastle(blackLongCastleRights);
            inputBoard.setEnPassantSquare(enPassantBitboard);
            inputBoard.setMoveNumber(moveNumber);
            inputBoard.setHalfMoveClock(halfMoveClock);
            inputBoard.setRepetitionNumber(repetitionNumber);

            //Gets the piece moved, start square, destination square,  flag, and piece captured from the int encoding the move
            int pieceMoved = getPieceMoved(unmoveRepresentationInput);
            int startSquare = getStartSquare(unmoveRepresentationInput);
            int destinationSquare = getDestinationSquare(unmoveRepresentationInput);
            int flag = getFlag(unmoveRepresentationInput);
            int pieceCaptured = getPieceCaptured(unmoveRepresentationInput);

            //Gets the array of bitboards and piece array
            ulong[] arrayOfBitboards = inputBoard.getArrayOfPieceBitboards();
            int[] pieceArray = inputBoard.getPieceArray();

            //Calculates bitboards for removing piece from start square and adding piece to destionation square
            //"and" with startMask will remove piece from start square, and "or" with destinationMask will add piece to destination square
            ulong startSquareBitboard = (0x1UL << startSquare);
            ulong destinationSquareBitboard = (0x1UL << destinationSquare);

            if (flag == Constants.QUIET_MOVE || flag == Constants.CAPTURE || flag == Constants.EN_PASSANT_CAPTURE
                || flag == Constants.DOUBLE_PAWN_PUSH) {

                //Gets the corresponding piece bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong pieceBitboard = arrayOfBitboards[pieceMoved - 1];

                //Removes the bit corresponding to the destination square, and adds a bit corresponding with the start square (to unmake move)
                pieceBitboard &= (~destinationSquareBitboard);
                pieceBitboard |= (startSquareBitboard);

                //Removes the int representing the piece from the destination square of the piece array, and adds an int representing the piece to the start square of the piece array (to unmake move)
                pieceArray[destinationSquare] = 0;
                pieceArray[startSquare] = pieceMoved;
                //updates the bitboard and the piece array
                inputBoard.setPieceBitboard(pieceMoved, pieceBitboard, pieceArray);

                //If there was a capture, add to the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                //Also re-add the captured piece to the array
                if (flag == Constants.CAPTURE ) {
                    ulong capturedPieceBitboard = arrayOfBitboards[pieceCaptured - 1];
                    capturedPieceBitboard |= (destinationSquareBitboard);
                    pieceArray[destinationSquare] = pieceCaptured;
                    inputBoard.setPieceBitboard(pieceCaptured,capturedPieceBitboard,pieceArray);
                }
                //If there was an en-passant capture, add the bit corresponding to the square of the captured pawn (one below destination square for white pawn capturing, and one above destination square for black pawn capturing) from the bitboard
                //Also re-add teh captured pawn to the array 
                else if (flag == Constants.EN_PASSANT_CAPTURE) {
                    if (pieceMoved == Constants.WHITE_PAWN) {
                        ulong capturedPawnBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPawnBitboard |= (destinationSquareBitboard >> 8);
                        pieceArray[destinationSquare - 8] = Constants.BLACK_PAWN;
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPawnBitboard, pieceArray);
                    } else if (pieceMoved == Constants.BLACK_PAWN) {
                        ulong capturedPawnBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPawnBitboard |= (destinationSquareBitboard << 8);
                        pieceArray[destinationSquare + 8] = Constants.WHITE_PAWN;
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPawnBitboard, pieceArray);
                    }
                }

            } 
            else if (flag == Constants.SHORT_CASTLE || flag == Constants.LONG_CASTLE) {

                //Gets the white/black king bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong kingBitboard = arrayOfBitboards[pieceMoved - 1];

                //Removes the bit corresponding to the DESTINATION square, and adds a bit corresponding with the start square
                kingBitboard &= (~destinationSquareBitboard);
                kingBitboard |= startSquareBitboard;

                //Removes the int representing the king from the start square of the piece array, and adds an int representing the king to the destination square of the piece array
                pieceArray[destinationSquare] = 0;
                pieceArray[startSquare] = pieceMoved;

                //updates the bitboard and the piece array
                inputBoard.setPieceBitboard(pieceMoved, kingBitboard, pieceArray);

                if (pieceMoved == Constants.WHITE_KING) {
                    ulong rookBitboard = arrayOfBitboards[Constants.WHITE_ROOK - 1];

                    //If short castle, then move the rook from F1 to H1
                    if (flag == Constants.SHORT_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.H1);
                        ulong rookDestinationSquareBitboard = (0x1UL << Constants.F1);

                        rookBitboard &= (~rookDestinationSquareBitboard);
                        rookBitboard |= rookStartSquareBitboard;

                        pieceArray[Constants.F1] = 0;
                        pieceArray[Constants.H1] = Constants.WHITE_ROOK;

                    }
                        //If long castle, then move the rook from D1 to A1
                    else if (flag == Constants.LONG_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.A1);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.D1;

                        rookBitboard &= (~rookDestinationSquareBitboard);
                        rookBitboard |= rookStartSquareBitboard;

                        pieceArray[Constants.D1] = 0;
                        pieceArray[Constants.A1] = Constants.WHITE_ROOK;
                    }
                    inputBoard.setPieceBitboard(Constants.WHITE_ROOK, rookBitboard, pieceArray);

                } else if (pieceMoved == Constants.BLACK_KING) {
                    ulong rookBitboard = arrayOfBitboards[Constants.BLACK_ROOK - 1];

                    //If short castle, then move the rook from F8 to H8
                    if (flag == Constants.SHORT_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.H8);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.F8;

                        rookBitboard &= ~(rookDestinationSquareBitboard);
                        rookBitboard |= rookStartSquareBitboard;

                        pieceArray[Constants.F8] = 0;
                        pieceArray[Constants.H8] = Constants.BLACK_ROOK;
                    }
                        //If long castle, then move the rook from D8 to A8
                    else if (flag == Constants.LONG_CASTLE) {
                        ulong rookStartSquareBitboard = (0x1UL << Constants.A8);
                        ulong rookDestinationSquareBitboard = 0x1UL << Constants.D8;

                        rookBitboard &= ~(rookDestinationSquareBitboard);
                        rookBitboard |= rookStartSquareBitboard;

                        pieceArray[Constants.D8] = 0;
                        pieceArray[Constants.A8] = Constants.BLACK_ROOK;
                    }
                    inputBoard.setPieceBitboard(Constants.BLACK_ROOK, rookBitboard, pieceArray);
                }

            }
            else if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.BISHOP_PROMOTION ||
                flag == Constants.ROOK_PROMOTION
                || flag == Constants.QUEEN_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE ||
                flag == Constants.BISHOP_PROMOTION_CAPTURE
                || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {

                //Gets the corresponding pawn bitboard from the array of bitboards (int value of piece type - 1 is equal to the index that the corresponding bitboard is stored in)
                ulong pawnBitboard = arrayOfBitboards[pieceMoved - 1];

                if (pieceMoved == Constants.WHITE_PAWN) {
                    //Adds the bit corresponding to the start square 
                    pawnBitboard |= startSquareBitboard;
                    pieceArray[startSquare] = pieceMoved;
                    inputBoard.setPieceBitboard(pieceMoved, pawnBitboard, pieceArray);

                    //removes a bit corresponding to the destination square in the promoted piece bitboard
                    if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_KNIGHT - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                        pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.WHITE_KNIGHT, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_BISHOP - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                        pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.WHITE_BISHOP, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_ROOK - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                         pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.WHITE_ROOK, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.WHITE_QUEEN - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                         pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.WHITE_QUEEN, promotedPieceBitboard, pieceArray);
                    }

                    //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                    //Also adds the captured piece back to the array
                    if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong capturedPieceBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPieceBitboard |= (destinationSquareBitboard);
                        pieceArray[destinationSquare] = pieceCaptured;
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPieceBitboard, pieceArray);
                    }

                } else if (pieceMoved == Constants.BLACK_PAWN) {
                    //Adds the bit corresponding to the start square 
                    pawnBitboard |= (startSquareBitboard);
                    pieceArray[startSquare] = pieceMoved;
                    inputBoard.setPieceBitboard(pieceMoved, pawnBitboard, pieceArray);

                    //removes the bit corresponding to the destination square in the promoted piece bitboard
                    if (flag == Constants.KNIGHT_PROMOTION || flag == Constants.KNIGHT_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_KNIGHT - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                        pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.BLACK_KNIGHT, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.BISHOP_PROMOTION || flag == Constants.BISHOP_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_BISHOP - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                        pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.BLACK_BISHOP, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.ROOK_PROMOTION || flag == Constants.ROOK_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_ROOK - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                         pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.BLACK_ROOK, promotedPieceBitboard, pieceArray);
                    } else if (flag == Constants.QUEEN_PROMOTION || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong promotedPieceBitboard = arrayOfBitboards[Constants.BLACK_QUEEN - 1];
                        promotedPieceBitboard &= (~destinationSquareBitboard);
                         pieceArray[destinationSquare] = Constants.EMPTY;
                        inputBoard.setPieceBitboard(Constants.BLACK_QUEEN, promotedPieceBitboard, pieceArray);
                    }

                    //If there was a capture, add the bit corresponding to the square of the captured piece (destination square) from the appropriate bitboard
                    //Also adds the captured piece back to the array
                    if (flag == Constants.KNIGHT_PROMOTION_CAPTURE || flag == Constants.BISHOP_PROMOTION_CAPTURE || flag == Constants.ROOK_PROMOTION_CAPTURE || flag == Constants.QUEEN_PROMOTION_CAPTURE) {
                        ulong capturedPieceBitboard = arrayOfBitboards[pieceCaptured - 1];
                        capturedPieceBitboard |= destinationSquareBitboard;
                        pieceArray[destinationSquare] = pieceCaptured;
                        inputBoard.setPieceBitboard(pieceCaptured, capturedPieceBitboard, pieceArray);
                    }
                }
            }
            
        }
    }
}
