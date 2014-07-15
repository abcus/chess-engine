﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
   
    public sealed class Board {

        //INSTANCE VARIABLES-----------------------------------------------------------------------------

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
        
        //Constructor that sets up the board to the default starting position
        public Board() {
            
        }

        //Constructor that takes in a FEN string and generates the appropriate board position
        public Board(string FEN) {
            FENToBoard(FEN);
        }

        //OTHER METHODS----------------------------------------------------------------------------------------

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

        //GET METHODS----------------------------------------------------------------------------------------

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


        //SET METHODS-----------------------------------------------------------------------------------------


        //sets the appropriate bitboard and updates the piecearray
        //Updates the white pieces bitboard (index 0), black pieces bitboard (index 1), and occupied bitboard (index 2)
        public void setPieceBitboard(int pieceTypeInput, ulong bitboardInput, int[] pieceArrayInput) {

            ulong oldPieceBitboard = arrayOfBitboards[(pieceTypeInput - 1)];
            
            if (pieceTypeInput == Constants.WHITE_PAWN || pieceTypeInput == Constants.WHITE_KNIGHT ||
                pieceTypeInput == Constants.WHITE_BISHOP || pieceTypeInput == Constants.WHITE_ROOK ||
                pieceTypeInput == Constants.WHITE_QUEEN || pieceTypeInput == Constants.WHITE_KING) {
                arrayOfAggregateBitboards[0] &= (~oldPieceBitboard);
                arrayOfBitboards[(pieceTypeInput - 1)] = bitboardInput;
                arrayOfAggregateBitboards[0] |= bitboardInput;
            } else if (pieceTypeInput == Constants.BLACK_PAWN || pieceTypeInput == Constants.BLACK_KNIGHT ||
                pieceTypeInput == Constants.BLACK_BISHOP || pieceTypeInput == Constants.BLACK_ROOK ||
                pieceTypeInput == Constants.BLACK_QUEEN || pieceTypeInput == Constants.BLACK_KING) {
                arrayOfAggregateBitboards[1] &= (~oldPieceBitboard);
                arrayOfBitboards[(pieceTypeInput - 1)] = bitboardInput;
                arrayOfAggregateBitboards[1] |= bitboardInput;
            }
            arrayOfAggregateBitboards[2] = arrayOfAggregateBitboards[0] | arrayOfAggregateBitboards[1];
            
            //sets the piece array to the input
            pieceArray = pieceArrayInput;

            
        }

        //Sets the appropriate bitboard (overridden method)
        //Updates the white pieces bitboard, black pieces bitboard, and occupied bitboard
        public void setPieceBitboard(int pieceTypeInput, ulong bitboardInput) {
            ulong oldPieceBitboard = arrayOfBitboards[(pieceTypeInput - 1)];

            if (pieceTypeInput == Constants.WHITE_PAWN || pieceTypeInput == Constants.WHITE_KNIGHT ||
                pieceTypeInput == Constants.WHITE_BISHOP || pieceTypeInput == Constants.WHITE_ROOK ||
                pieceTypeInput == Constants.WHITE_QUEEN || pieceTypeInput == Constants.WHITE_KING) {
                arrayOfAggregateBitboards[0] &= (~oldPieceBitboard);
                arrayOfBitboards[(pieceTypeInput - 1)] = bitboardInput;
                arrayOfAggregateBitboards[0] |= bitboardInput;
            } else if (pieceTypeInput == Constants.BLACK_PAWN || pieceTypeInput == Constants.BLACK_KNIGHT ||
                pieceTypeInput == Constants.BLACK_BISHOP || pieceTypeInput == Constants.BLACK_ROOK ||
                pieceTypeInput == Constants.BLACK_QUEEN || pieceTypeInput == Constants.BLACK_KING) {
                arrayOfAggregateBitboards[1] &= (~oldPieceBitboard);
                arrayOfBitboards[(pieceTypeInput - 1)] = bitboardInput;
                arrayOfAggregateBitboards[1] |= bitboardInput;
            }
            arrayOfAggregateBitboards[2] = arrayOfAggregateBitboards[0] | arrayOfAggregateBitboards[1];
        }

        //sets the side to move
        public void setSideToMove(int sideToMoveInput) {
            sideToMove = sideToMoveInput;
        }

        //sets white short castling rights
        public void setWhiteShortCastle(int whiteShortCastleRightsInput) {
            whiteShortCastleRights = whiteShortCastleRightsInput;
        }
        //sets white short castling rights
        public void setWhiteLongCastle(int whiteLongCastleRightsInput) {
            whiteLongCastleRights = whiteLongCastleRightsInput;
        }
        //sets white short castling rights
        public void setBlackShortCastle(int blackShortCastleRightsInput) {
            blackShortCastleRights = blackShortCastleRightsInput;
        }
        //sets white short castling rights
        public void setBlackLongCastle(int blackLongCastleRightsInput) {
            blackLongCastleRights = blackLongCastleRightsInput;
        }

        //sets en passant square and color
        public void setEnPassantSquare(ulong enPassantSquareInput) {
            enPassantSquare = enPassantSquareInput;
        }

        //sets the move number
        public void setMoveNumber(int moveNumberInput) {
            moveNumber = moveNumberInput;
        }

        //sets the halfmove clock (since pawn pushes or captures)
        public void setHalfMoveClock(int halfMoveClockInput) {
            HalfMovesSincePawnMoveOrCapture = halfMoveClockInput;
        }

        //sets the number of repetitions
        public void setRepetitionNumber(int repetitionNumberInput) {
            repetionOfPosition = repetitionNumberInput;
        }
    }
}
