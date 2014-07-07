using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            moveRepresentation |= pieceCaptured << 20;
            return (uint)moveRepresentation;
        }

        //Takes information on side to move, castling rights, en-passant square, en-passant color, move number, half-move clock
        //Creates a 32-bit unsigned integer representing this information
        public static uint boardRestoreDataEncoder(int sideToMove, int whiteShortCastle, int whiteLongCastle,
            int blackShortCastle, int blackLongCastle, int enPassantSquare, int enPassantColor, int halfMoveClock,
            int moveNumber) {
            int boardRestoreDataRepresentation = 0x0;

            boardRestoreDataRepresentation |= sideToMove << 0;
            boardRestoreDataRepresentation |= whiteShortCastle << 2;
            boardRestoreDataRepresentation |= whiteLongCastle << 3;
            boardRestoreDataRepresentation |= blackShortCastle << 4;
            boardRestoreDataRepresentation |= blackLongCastle << 5;
            boardRestoreDataRepresentation |= enPassantSquare << 6;
            boardRestoreDataRepresentation |= enPassantColor << 12;
            boardRestoreDataRepresentation |= halfMoveClock << 14;
            boardRestoreDataRepresentation |= moveNumber << 20;

            return (uint) boardRestoreDataRepresentation;
        }


        //METHODS THAT DECODE U32-INT REPRESENTING MOVES AND BOARD INSTANCE VARIABLES----------------------------

        //Extracts the piece moved from the integer that encodes the move
        private static int getPieceMoved(uint moveRepresentation) {
            int pieceMoved = (int)((moveRepresentation & 0xF) >> 0);
            return pieceMoved;
        }
        //Extracts the start square from the integer that encodes the move
        private static int getStartSquare(uint moveRepresentation) {
            int startSquare = (int)((moveRepresentation & 0x3F0) >> 4);
            return startSquare;
        }
        //Extracts the destination square from the integer that encodes the move
        private static int getDestinationSquare(uint moveRepresentation) {
            int destinationSquare = (int)((moveRepresentation & 0xFC00) >> 10);
            return destinationSquare;
        }
        //Extracts the flag from the integer that encodes the move
        private static int getFlag(uint moveRepresentation) {
            int flag = (int)((moveRepresentation & 0xF0000) >> 16);
            return flag;
        }
        //Extracts the piece captured from the integer that encodes the move
        private static int getPieceCaptured(uint moveRepresentation) {
            int pieceCaptured = (int)((moveRepresentation & 0xF00000) >> 20);

            if (pieceCaptured == 0) {
                return -1;
            } else {
                return pieceCaptured;
            }
        }



        //Extracts the side to move from the integer encoding the restore board data
        private static int getsideToMove(uint boardRestoreDataRepresentation) {
            int sideToMove = (int)((boardRestoreDataRepresentation & 0x3) >> 0);
            return sideToMove;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getWhiteShortCastleRights(uint boardRestoreDataRepresentation) {
            int whiteShortCastleRights = (int)((boardRestoreDataRepresentation & 0x4) >> 2);
            return whiteShortCastleRights;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getWhiteLongCastleRights(uint boardRestoreDataRepresentation) {
            int whiteLongCastleRights = (int)((boardRestoreDataRepresentation & 0x8) >> 3);
            return whiteLongCastleRights;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getBlackShortCastleRights(uint boardRestoreDataRepresentation) {
            int blackShortCastleRights = (int)((boardRestoreDataRepresentation & 0x10) >> 4);
            return blackShortCastleRights;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getBlackLongCastleRights(uint boardRestoreDataRepresentation) {
            int blackLongCastleRights = (int)((boardRestoreDataRepresentation & 0x20) >> 5);
            return blackLongCastleRights;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getEnPassantSquare(uint boardRestoreDataRepresentation) {
            int enPassantSquare = (int)((boardRestoreDataRepresentation & 0xFC0) >> 6);
            return enPassantSquare;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getEnPassantColour(uint boardRestoreDataRepresentation) {
            int sideToMove = (int)((boardRestoreDataRepresentation & 0x3000) >> 12);
            return sideToMove;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getHalfMoveClock(uint boardRestoreDataRepresentation) {
            int halfMoveClock = (int)((boardRestoreDataRepresentation & 0xFC000) >> 14);
            return halfMoveClock;
        }
        //Extracts the side to move from the integer encoding the restore board data
        private static int getMoveNumber(uint boardRestoreDataRepresentation) {
            int sideToMove = (int)((boardRestoreDataRepresentation & 0xFFF00000) >> 20);
            return sideToMove;
        }


        //Static method that makes a move by updating the board object's instance variables
        public static void makeMove(int moveRepresentation, Board inputBoard) {
            
        }

        //static method that unmakes a move by restoring the board object's instance variables
        public static void unmakeMove(int moveRepresentation, Board inputBoard) {
            
        }
    }
}
