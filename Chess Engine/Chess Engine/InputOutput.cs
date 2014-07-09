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

        //INPUT METHODS--------------------------------------------------------------------------------------

        //Accepts an FEN string
        public static string getFENString() {
            Console.WriteLine("Enter an FEN String:");
            string FENString = Console.ReadLine();
            return FENString;
        }

        //OUTPUT METHODS---------------------------------------------------------------------------------------

        //Draws the board on the console
        public static void drawBoard(Board inputBoard) {

            //gets the bitboards from the board object
            ulong[] arrayOfBitboards = inputBoard.getPieceBitboards();

            ulong wPawn = arrayOfBitboards[0];
            ulong wKnight = arrayOfBitboards[1];
            ulong wBishop = arrayOfBitboards[2];
            ulong wRook = arrayOfBitboards[3];
            ulong wQueen = arrayOfBitboards[4];
            ulong wKing = arrayOfBitboards[5];

            ulong bPawn = arrayOfBitboards[6];
            ulong bKnight = arrayOfBitboards[7];
            ulong bBishop = arrayOfBitboards[8];
            ulong bRook = arrayOfBitboards[9];
            ulong bQueen = arrayOfBitboards[10];
            ulong bKing = arrayOfBitboards[11];

            //creates a new 8x8 array of String and sets it all to spaces
            string[,] chessBoard = new string[8, 8];
            for (int i = 0; i < 64; i++) {
                chessBoard[i / 8, i % 8] = " ";
            }

            //Goes through each of the bitboards; if they have a "1" then it sets the appropriate element in the array to the appropriate piece
            //Note that the array goes from A8 to H1
            for (int i = 0; i < 64; i++) {
                if (((wPawn >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "P";
                } if (((wKnight >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "N";
                } if (((wBishop >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "B";
                } if (((wRook >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "R";
                } if (((wQueen >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "Q";
                } if (((wKing >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "K";
                } if (((bPawn >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "p";
                } if (((bKnight >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "n";
                } if (((bBishop >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "b";
                } if (((bRook >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "r";
                } if (((bQueen >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "q";
                } if (((bKing >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "k";
                }
            }

            //Goes through the 8x8 array and prints its contents
            for (int i = 0; i < 8; i++) {

                Console.WriteLine("  +---+---+---+---+---+---+---+---+");
                Console.Write((8 - i) + " ");
                
                for (int j = 0; j < 8; j++) {
                    Console.Write("| " + chessBoard[i, j] + " ");
                }
                Console.WriteLine("|"); 
            }
            Console.WriteLine("  +---+---+---+---+---+---+---+---+");
            Console.WriteLine("    A   B   C   D   E   F   G   H");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            //Prints out the side to move, castling rights, en-pessant square, halfmoves since capture/pawn advance, and fullmove number
            
            //side to move
            int sideToMove = inputBoard.getSideToMove();
            String colour = (sideToMove == 1) ? "WHITE" : "BLACK";
            Console.WriteLine("Side to move: " + colour);
            
            //castle rights
            int[] castleRights = inputBoard.getCastleRights();
            Console.WriteLine("White Short Castle Rights: " + castleRights[0]);
            Console.WriteLine("White Long Castle Rights: " + castleRights[1]);
            Console.WriteLine("Black Short Castle Rights: " + castleRights[2]);
            Console.WriteLine("Black Long Castle Rights: " + castleRights[3]);

            //en passant square
            ulong enPassantSquareBitboard = inputBoard.getEnPassant();
            

            if (enPassantSquareBitboard != 0) {
                int enPassantIndex = Constants.bitScan(enPassantSquareBitboard).ElementAt(0);
                string firstChar = char.ConvertFromUtf32((int) ('H' - (enPassantIndex % 8))).ToString();
                string secondChar = ((enPassantIndex/ 8) + 1).ToString();
                Console.WriteLine("En Passant Square: " + firstChar + secondChar);
            } else if (enPassantSquareBitboard == 0) {
                Console.WriteLine("En Passant Square: N/A");
            }
            

            //prints move data (fullmove number, half-moves since last pawn push/capture, repetitions of position)
            //If no move number data or halfmove clock data, then prints N/A
            int[] moveData = inputBoard.getMoveData();
            if (moveData[0] != -1) {
                Console.WriteLine("Fullmove number: " + moveData[0]);
            } else {
                Console.WriteLine("Fullmove number: N/A");
            }

            if (moveData[1] != -1) {
                Console.WriteLine("Half moves since last pawn push/capture: " + moveData[1]);
            } else {
              Console.WriteLine("Half moves since last pawn push/capture: N/A");  
            }

            Console.WriteLine("Repetitions of this position: " + moveData[2]);
            
        }


    }
}
