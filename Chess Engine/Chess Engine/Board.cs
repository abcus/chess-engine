using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
   
    class Board {

        //INSTANCE VARIABLES-----------------------------------------------------------------------------

        //12 bitboards (one for each piece type)
        internal long wPawn;
        internal long wKnight;
        internal long wBishop;
        internal long wRook;
        internal long wQueen;
        internal long wKing;
        internal long bPawn;
        internal long bKnight;
        internal long bBishop;
        internal long bRook;
        internal long bQueen;
        internal long bKing;

        internal int sideToMove;

        internal Move lastMove;

        internal Boolean blackInCheck;
        internal Boolean blackInCheckmate;
        internal Boolean whiteInCheck;
        internal Boolean whiteInCheckmate;
        internal Boolean stalemate;

        internal Boolean whiteShortCastleRights;
        internal Boolean whiteLongCastleRights;
        internal Boolean blackShortCastleRights;
        internal Boolean blackLongCastleRights;

        internal int enPessantColour;
        internal int enPessantPosition;

        internal int movesSincePawnMoveOrCapture;
        internal int repitionOfPosition;

        internal Boolean endGame;

        internal int evaluationFunctionValue;

        internal long zobristKey;

        //CONSTRUCTOR------------------------------------------------------------------------------------
        
        //Constructor that sets up the board to the default starting position
        public Board() {
            
        }

        //Constructor that takes in a FEN string and generates the appropriate board position
        public Board(string FEN) {

        }

        //Constructor that takes in an array and generates the appropriate bitboards (for testing purposes)
        public Board(string[] inputBoardArray) {
            arrayToBitboards(inputBoardArray);
        }

        //OTHER METHODS----------------------------------------------------------------------------------------

        //takes in array and generates 12 bitboards

        //gets the white pawn
        private void arrayToBitboards(string[] inputBoardArray) {

            String binary;

            for (int i = 0; i < 64; i++) {
                binary = "0000000000000000000000000000000000000000000000000000000000000000";
                binary = binary.Substring(i+1) + "1" + binary.Substring(0, i);
                switch (inputBoardArray[i]) {
                    case "P": wPawn |= Convert.ToInt64(binary, 2); break;
                    case "N": wKnight |= Convert.ToInt64(binary, 2); break;
                    case "B": wBishop |= Convert.ToInt64(binary, 2); break;
                    case "R": wRook |= Convert.ToInt64(binary, 2); break;
                    case "Q": wQueen |= Convert.ToInt64(binary, 2); break;
                    case "K": wKing |= Convert.ToInt64(binary, 2); break;
                    case "p": bPawn |= Convert.ToInt64(binary, 2); break;
                    case "n": bKnight |= Convert.ToInt64(binary, 2); break;
                    case "b": bBishop |= Convert.ToInt64(binary, 2); break;
                    case "r": bRook |= Convert.ToInt64(binary, 2); break;
                    case "q": bQueen |= Convert.ToInt64(binary, 2); break;
                    case "k": bKing |= Convert.ToInt64(binary, 2); break;
                }
            }
        }

        //GET METHODS----------------------------------------------------------------------------------------

        public long getWhitePawn() {
            return wPawn;
        }
        //gets the white knight
        public long getWhiteKnight() {
            return wKnight;
        }
        //gets the white bishop
        public long getWhiteBishop() {
            return wBishop;
        }
        //gets the white rook
        public long getWhiteRook() {
            return wRook;
        }
        //gets the white queen
        public long getWhiteQueen() {
            return wQueen;
        }
        //gets the white king
        public long getWhiteKing() {
            return wKing;
        }
        //gets the black pawn
        public long getBlackPawn() {
            return bPawn;
        }
        //gets the black knight
        public long getBlackKnight() {
            return bKnight;
        }
        //gets the black bishop
        public long getBlackBishop() {
            return bBishop;
        }
        //gets the black rook
        public long getBlackRook() {
            return bRook;
        }
        //gets the black queen
        public long getBlackQueen() {
            return bQueen;
        }
        //gets the black king
        public long getBlackKing() {
            return bKing;
        }

        //SET METHODS-----------------------------------------------------------------------------------------


    }
}
