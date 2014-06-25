using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class InputOutput {

        //CONSTRUCTOR----------------------------------------------------------------------------------------

        public InputOutput() {
            
        }

        //OTHER METHODS---------------------------------------------------------------------------------------

        //Accepts an FEN string
        public static string getFENString() {
            Console.WriteLine("Enter an FEN String:");
            string FENString = Console.ReadLine();
            return FENString;
        }

        //Draws the board on the console
        public static void drawBoard(Board inputBoard) {

            //gets the bitboards from the board object
            long wPawn = inputBoard.getWhitePawn();
            long wKnight = inputBoard.getWhiteKnight();
            long wBishop = inputBoard.getWhiteBishop();
            long wRook = inputBoard.getWhiteRook();
            long wQueen = inputBoard.getWhiteQueen();
            long wKing = inputBoard.getWhiteKing();

            long bPawn = inputBoard.getBlackPawn();
            long bKnight = inputBoard.getBlackKnight();
            long bBishop = inputBoard.getBlackBishop();
            long bRook = inputBoard.getBlackRook();
            long bQueen = inputBoard.getBlackQueen();
            long bKing = inputBoard.getBlackKing();

            //creates a new 8x8 array of String and sets it all to spaces
            string[,] chessBoard = new string[8, 8];
            for (int i = 0; i < 64; i++) {
                chessBoard[i / 8, i % 8] = " ";
            }

            //Goes through each of the bitboards; if they have a "1" then it sets the appropriate element in the array to the appropriate piece
            for (int i = 0; i < 64; i++) {
                if (((wPawn >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "P";
                } if (((wKnight >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "N";
                } if (((wBishop >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "B";
                } if (((wRook >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "R";
                } if (((wQueen >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "Q";
                } if (((wKing >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "K";
                } if (((bPawn >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "p";
                } if (((bKnight >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "n";
                } if (((bBishop >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "b";
                } if (((bRook >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "r";
                } if (((bQueen >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "q";
                } if (((bKing >> i) & 1L) == 1) {
                    chessBoard[i / 8, i % 8] = "k";
                }
            }

            //Goes through the 8x8 array and prints its contents
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    Console.Write(chessBoard[i, j] + ", ");
                }
                Console.WriteLine("");
            }
            Console.WriteLine("");
        }


    }
}
