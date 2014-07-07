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
            ulong[] pieceBitboards = inputBoard.getPieceBitboards();

            ulong wPawn = pieceBitboards[0];
            ulong wKnight = pieceBitboards[1];
            ulong wBishop = pieceBitboards[2];
            ulong wRook = pieceBitboards[3];
            ulong wQueen = pieceBitboards[4];
            ulong wKing = pieceBitboards[5];

            ulong bPawn = pieceBitboards[6];
            ulong bKnight = pieceBitboards[7];
            ulong bBishop = pieceBitboards[8];
            ulong bRook = pieceBitboards[9];
            ulong bQueen = pieceBitboards[10];
            ulong bKing = pieceBitboards[11];

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
                Console.Write((i - i) + " ");
                
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
            ulong[] enPassantData = inputBoard.getEnPassant();
            if (enPassantData[0] != 0) {
                colour = (enPassantData[0] == 1) ? "WHITE" : "BLACK";
                Console.WriteLine("En Passant Colour: " + colour);
            } else if (enPassantData[0] == 0) {
                Console.WriteLine("En Passant Color: N/A");
            }
            
            if (enPassantData[1] != 0) {
                string firstChar = char.ConvertFromUtf32((int) ('h' - (enPassantData[1] % 8))).ToString();
                string secondChar = ((enPassantData[1] / 8) + 1).ToString();
                Console.WriteLine("En Passant Square: " + firstChar + secondChar);
            } else if (enPassantData[1] == 0) {
                Console.WriteLine("En Passant Square: N/A");
            }
            

            //move number
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
            
        }


    }
}
