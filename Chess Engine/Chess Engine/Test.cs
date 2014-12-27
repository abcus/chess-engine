using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Bitboard = System.UInt64;

namespace Chess_Engine {
    
	public sealed class Test {

		//Generates king moves from square H1 to A8
        public static void generateKingMoves() {
            ulong[] array = new ulong[10];
            for (int i = 0; i <= 9; i++) {
                ulong kingG2Moves = 0x0000000000070507UL;
                ulong temp = kingG2Moves >> i;
                if (i % 8 == 2) {
                    temp &= (~Constants.FILE_H);
                } else if (i % 8 == 1) {
                    temp &= (~Constants.FILE_A);
                }
                array[i] = temp;
            }
            for (int i = 9; i >= 0; i--) {
                Console.Write("0x{0:X16}", array[i]);
                Console.WriteLine("UL,");
            }
            for (int i = 1; i <= 54; i++) {
                ulong kingG2Moves = 0x0000000000070507UL;
                ulong temp = kingG2Moves << i;
                if (i % 8 == 6) {
                    temp &= (~Constants.FILE_H);
                } else if (i % 8 == 7) {
                    temp &= (~Constants.FILE_A);
                }
                Console.Write("0x{0:X16}", temp);
                Console.WriteLine("UL,");
            }
        }

        //Generates knight moves from square H1 to A8
        public static void generateKnightMoves() {
            ulong[] array = new ulong[19];
            for (int i = 0; i <= 18; i++) {
                ulong knightF3Moves = 0x0000000A1100110AUL;
                ulong temp = knightF3Moves >> i;
                if (i % 8 == 1) {
                    temp &= (~Constants.FILE_A);
                } else if (i%8 == 2) {
                    temp &= (~(Constants.FILE_A|Constants.FILE_B));
                } else if (i % 8 == 3) {
                    temp &= (~(Constants.FILE_G|Constants.FILE_H));
                } else if (i % 8 == 4) {
                    temp &= (~Constants.FILE_H);
                }
                array[i] = temp;
            }
            for (int i = 18; i >= 0; i--) {
                Console.Write("0x{0:X16}", array[i]);
                Console.WriteLine("UL,");
            }
            for (int i = 1; i <= 45; i++) {
                ulong knightF3Moves = 0x0000000A1100110AUL;
                ulong temp = knightF3Moves << i;
                if (i % 8 == 7) {
                    temp &= (~Constants.FILE_A);
                } else if (i % 8 == 6) {
                    temp &= (~(Constants.FILE_A | Constants.FILE_B));
                } else if (i % 8 == 5) {
                    temp &= (~(Constants.FILE_G | Constants.FILE_H));
                } else if (i % 8 == 4) {
                    temp &= (~Constants.FILE_H);
                }
                Console.Write("0x{0:X16}", temp);
                Console.WriteLine("UL,");
            }
        }
        //Generates white pawn single moves from square H1 to A8
        public static void generateWhitePawnSingleMovesAndPromotions() {
            
            for (int i = 0; i <= 63; i++) {
                ulong whitePawnH2Moves = 0x0000000000010000UL;
                ulong temp = 0x0UL;
                
                if (i >= 0 && i <= 7 || i >= 56) {
            
                }
                else {
                    temp = whitePawnH2Moves << (i - 8);
                }

                Console.Write("0x{0:X16}", temp);
                Console.WriteLine("UL,");
            }
        }

        //Generates black pawn single moves from square H1 to A8
        public static void generateBlackPawnSingleMovesAndPromotions() {

            for (int i = 0; i <= 63; i++) {
                ulong blackPawnH2Moves = 0x0000000000000001UL;
                ulong temp = 0x0UL;

                if (i >= 0 && i <= 7 || i >= 56) {

                } else {
                    temp = blackPawnH2Moves << (i - 8);
                }

                Console.Write("0x{0:X16}", temp);
                Console.WriteLine("UL,");
            }
        }

        //Generates white pawn captures from square H1 to A8
        public static void generateWhitePawnCaptures() {

            for (int i = 0; i <= 63; i++) {
                ulong whitePawnG1Captures = 0x0000000000000500UL;
                ulong temp = 0x0UL;

                if (i >= 56) {

                } else if (i == 0) {
                    temp = ((whitePawnG1Captures >> 1) & (~Constants.FILE_A));
                } else if (i >= 1 && i < 56 ) {
                    temp = whitePawnG1Captures << (i - 1);
                    if ((i-1) % 8 == 6) {
                        temp &= ~Constants.FILE_H;
                    } else if ((i-1) % 8 == 7) {
                        temp &= ~Constants.FILE_A;
                    }
                }

                Console.Write("0x{0:X16}", temp);
                Console.WriteLine("UL,");
            }
        }

        //Generates white pawn captures from square H1 to A8
        public static void generateBlackPawnCaptures() {

            for (int i = 0; i <= 63; i++) {
                ulong blackPawnG2Captures = 0x0000000000000005UL;
                ulong temp = 0x0UL;

                if (i >= 0 && i <= 7) {

                } else if (i == 8) {
                    temp = ((blackPawnG2Captures >> 1) & (~Constants.FILE_A));
                } else if (i >= 9 && i < 64) {
                    temp = blackPawnG2Captures << (i - 9);
                    if ((i - 9) % 8 == 6) {
                        temp &= ~Constants.FILE_H;
                    } else if ((i - 9) % 8 == 7) {
                        temp &= ~Constants.FILE_A;
                    }
                }

                Console.Write("0x{0:X16}", temp);
                Console.WriteLine("UL,");
            }
        }

        //Generates the rook occupancy mask from square H1 to A8
        public static void generateRookOccupancyMask() {
            for (int i = 0; i <= 63; i++) {
                int upShift = 0;
                int downShift = 0;
                int rightShift = 0;
                int leftShift = 0;

                ulong rookOccupancyMask = 0x0UL;
                ulong square = 0x1UL << i;

                //If (index/8 <= 6), shift up 7-1-(index)/8 times
                if (i/8 <= 6) {
                    for (int j = 0; j <= 6 - i/8; j++) {
                        if (j > 0) {upShift ++;}
                        rookOccupancyMask |= square << (8*j);
                    }
                }
                //If (index/8 >= 1) shift down 0 + (index/8) -1 times
                if (i/8 >= 1) {
                    for (int j = 0; j <= (i/8) - 1; j++) {
                        if (j > 0) {downShift++;}
                        rookOccupancyMask |= square >> (8 * j);
                    }
                }
                //If (index % 8 <= 6) shift left 7 - 1 - (index % 8) times
                if (i%8 <= 6) {
                    for (int j = 0; j <= 6 - (i%8); j++) {
                        if (j > 0) {leftShift++;}
                        rookOccupancyMask |= square << (j);
                    }
                }
                //If (index % 8 >= 1) shift right (index % 8) -1 times
                if (i%8 >= 1) {
                    for (int j = 0; j <= (i%8) - 1; j++) {
                        if (j > 0) {rightShift++;}
                        rookOccupancyMask |= square >> (j);
                    }
                }

                rookOccupancyMask &= ~square;

                //Console.Write("0x{0:X16}", rookOccupancyMask);
                //Console.WriteLine("UL,");

                Console.WriteLine("Upshift: " + upShift + "   Downshift:" + downShift + "   Leftshift:" + leftShift + "   Rightshift:" + rightShift);
                if ((i + 1)%8 == 0) {
                    Console.WriteLine("");
                }
            }
        }

        //Generates the bishop occupancy mask from square H1 to A8
        public static void generateBishopOccupancyMask() {
            for (int i = 0; i <= 63; i++) {
                int upLeftShift = 0;
                int upRightShift= 0;
                int downLeftShift = 0;
                int downRightShift = 0;

                ulong bishopOccupancyMask = 0x0UL;
                ulong square = 0x1UL << i;

                //If (index/8 <= 6) && (index % 8 <= 6), shift up-left 7-1-(index)/8 && 7 - 1 - (index % 8) times (min of the two)
                if (i / 8 <= 6 && i % 8 <= 6) {
                    for (int j = 0; (j <= 6 - i / 8 && j <= 6 - (i % 8)); j++) {
                        if (j > 0) { upLeftShift++; }
                        bishopOccupancyMask |= square << (9 * j);
                    }
                }
                //If (index/8 >= 1) && (index % 8 <= 6), shift down-right 0 + (index/8) -1 &&  (index % 8) -1 times (min of the two)
                if (i / 8 >= 1 && i % 8 >= 1) {
                    for (int j = 0; (j <= (i / 8) - 1 && j <= (i % 8) - 1); j++) {
                        if (j > 0) { downRightShift++; }
                        bishopOccupancyMask |= square >> (9 * j);
                    }
                }
                //If (index % 8 <= 6) and (index / 8 >= 1). shift down-left 7 - 1 - (index % 8) && (index/8) -1 times (min of the two)
                if (i / 8 >= 1 && i % 8 <= 6) {
                    for (int j = 0; (j <= (i / 8) - 1 && j <= 6 - (i % 8)); j++) {
                        if (j > 0) { downLeftShift++; }
                        bishopOccupancyMask |= square >> (7 * j);
                    }
                }
                //If (index % 8 >= 1) and (index / 8 <= 6), shift up-right (index % 8) -1 && (7-1-(index)/8) times (min ov the two)
                if (i / 8 <= 6 && i % 8 >= 1) {
                    for (int j = 0; (j <= 6 - (i / 8) && j <= (i % 8) - 1); j++) {
                        if (j > 0) { upRightShift++; }
                       bishopOccupancyMask|= square << (7 * j);
                    }
                }

                bishopOccupancyMask &= ~square;

                Console.Write("0x{0:X16}", bishopOccupancyMask);
                Console.WriteLine("UL,");

                /*Console.WriteLine("Upleftshift: " + upLeftShift + "   Downrightshift:" + downRightShift + "   Downleftshift:" + downLeftShift + "   Uprightshift:" + upRightShift);
                if ((i + 1) % 8 == 0) {
                    Console.WriteLine("");
                }*/
            }
        }

        //Generates the rook shift right number from square H1 to A8
        public static void generateRookRightShiftNumber() {
            for (int i = 0; i <= 63; i++) {
                int shiftNumber = 64 - Constants.popcount(Constants.rookOccupancyMask[i]);
                Console.WriteLine(shiftNumber+ ",");
            }
        }

        //Generates the rook shift right number from square H1 to A8
        public static void generateBishopRightShiftNumber() {
            for (int i = 0; i <= 63; i++) {
                int shiftNumber = 64 - Constants.popcount(Constants.bishopOccupancyMask[i]);
                Console.WriteLine(shiftNumber + ",");
            }
        }

        //Prints out the piece array
        public static void printPieceArray(int [] pieceArray) {

            //creates a new 8x8 array of String and sets it all to spaces
            string[,] chessBoard = new string[8, 8];
            for (int i = 0; i < 64; i++) {
                chessBoard[i / 8, i % 8] = " ";
            }

            //Goes through the piece array and sees if there is a piece (int != 0)
            //If so, it sets the corresponding element chessBoard array to the appropriate letter
            for (int i = 0; i <= 63; i++) {
                switch (pieceArray[i]) {
                    case Constants.WHITE_PAWN: chessBoard[7 - (i/8), 7 - (i%8)] = "P"; break;
                    case Constants.WHITE_KNIGHT: chessBoard[7 - (i/8), 7 - (i%8)] = "N"; break;
                    case Constants.WHITE_BISHOP: chessBoard[7 - (i/8), 7 - (i%8)] = "B"; break;
                    case Constants.WHITE_ROOK: chessBoard[7 - (i/8), 7 - (i%8)] = "R"; break;
                    case Constants.WHITE_QUEEN: chessBoard[7 - (i/8), 7 - (i%8)] = "Q"; break;
                    case Constants.WHITE_KING: chessBoard[7 - (i/8), 7 - (i%8)] = "K"; break;
                    case Constants.BLACK_PAWN: chessBoard[7 - (i/8), 7 - (i%8)] = "p"; break;
                    case Constants.BLACK_KNIGHT: chessBoard[7 - (i/8), 7 - (i%8)] = "n"; break;
                    case Constants.BLACK_BISHOP: chessBoard[7 - (i/8), 7 - (i%8)] = "b"; break;
                    case Constants.BLACK_ROOK: chessBoard[7 - (i/8), 7 - (i%8)] = "r"; break;
                    case Constants.BLACK_QUEEN: chessBoard[7 - (i/8), 7 - (i%8)] = "q"; break;
                    case Constants.BLACK_KING: chessBoard[7 - (i/8), 7 - (i%8)] = "k"; break;
                    case 0:break;
                }
            }


            for (int i = 0; i < 8; i++) {

                if (i == 0) {
                    Console.WriteLine("  ┌───┬───┬───┬───┬───┬───┬───┬───┐");
                } else if (i >= 1) {
                    Console.WriteLine("  ├───┼───┼───┼───┼───┼───┼───┼───┤");
                }

                Console.Write((i - i) + " ");

                for (int j = 0; j < 8; j++) {
                    Console.Write("| " + chessBoard[i,j] + " ");
                }
                Console.WriteLine("|");
            }
            Console.WriteLine("  └───┴───┴───┴───┴───┴───┴───┴───┘");
            Console.WriteLine("    A   B   C   D   E   F   G   H");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
        }
        
        //Prints out board showing moves from start square to end square for king and knight
        public static void printBitboard(Bitboard inputBoard) {
        
            //Note that the array goes from A8 to H1
                string[,] chessBoard = new string[8, 8];
                for (int i = 0; i < 64; i++) {
                    chessBoard[i / 8, i % 8] = " ";
                }
                for (int i = 0; i < 64; i++) {
                    if (((inputBoard >> i) & 1L) == 1) {
                        chessBoard[7 - (i / 8), 7 - (i % 8)] = "X";
                    }
                }
                for (int i = 0; i < 8; i++) {

                    if (i == 0) {
                        Console.WriteLine("  ┌───┬───┬───┬───┬───┬───┬───┬───┐");
                    } else if (i >= 1) {
                        Console.WriteLine("  ├───┼───┼───┼───┼───┼───┼───┼───┤");
                    }

                    Console.Write((8 - i) + " ");
                    
                    for (int j = 0; j < 8; j++) {
                        Console.Write("| " + chessBoard[i, j] + " ");
                    }
                    Console.WriteLine("|");
                }
                Console.WriteLine("  └───┴───┴───┴───┴───┴───┴───┴───┘");
                Console.WriteLine("    A   B   C   D   E   F   G   H");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine(""); 
        }

        // prints king moves, white pawn moves, black pawn moves, white pawn captures, black pawn captures
        // rook occupancy masks, bishop occupancy masks
	    public static void printMoveAndMask() {

            Console.WriteLine("King moves");
	        for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.kingMoves[i]); 
	        }
            Console.WriteLine("Knight moves");
	        for (int i = 0; i <= 63; i++) {
	            Test.printBitboard(Constants.knightMoves[i]);
	        }
            Console.WriteLine("White pawn moves");
            for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.whiteSinglePawnMovesAndPromotionMoves[i]);
            }
            Console.WriteLine("Black pawn moves");
            for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.blackSinglePawnMovesAndPromotionMoves[i]);
            }
            Console.WriteLine("White pawn captures");
            for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.whiteCapturesAndCapturePromotions[i]);
            }
            Console.WriteLine("Black pawn captures");
            for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.blackCapturesAndCapturePromotions[i]);
            }
            Console.WriteLine("Rook occupancy mask");
            for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.rookOccupancyMask[i]);
            }
            Console.WriteLine("Bishop occupancy mask");
            for (int i = 0; i <= 63; i++) {
                Test.printBitboard(Constants.bishopOccupancyMask[i]);
            }
	    }
       

        //Prints out all occupancy variations and associated moves for a particular square
        public static void printOccupancyVariationAndMove(int square, String range, String piece) {

            int startPoint = 0;
            int endPoint = 0;

            if (range == "First20") {
                startPoint = 0;
                endPoint = 20;
            } else if (range == "Last20") {
                if (piece == "Bishop") {
                    startPoint = Constants.bishopOccupancyVariations[square].Length - 20;
                    endPoint = Constants.bishopOccupancyVariations[square].Length;
                } else if (piece == "Rook") {
                    startPoint = Constants.rookOccupancyVariations[square].Length - 20;
                    endPoint = Constants.rookOccupancyVariations[square].Length;
                }
            }

            for (int i = startPoint; i < endPoint; i++) {

                //array to hold the occupancy variation
                //Note that the array goes from A8 to H1
                string[,] chessBoard = new string[8, 8];
                for (int j = 0; j < 64; j++) {
                    chessBoard[j / 8, j % 8] = " ";
                }
                chessBoard[7 - square / 8, 7 - square % 8] = "*";

                //array to hold the move
                //Note that the array goes from A8 to H1
                string[,] moves = new string[8, 8];
                for (int j = 0; j < 64; j++) {
                    moves[j/8, j%8] = " ";
                }
                moves[7 - square/8, 7 - square%8] = "*";

                if (piece == "Bishop") {

                    int index = (int)((Constants.bishopOccupancyVariations[square][i]* Constants.bishopMagicNumbers[square]) >> Constants.bishopMagicShiftNumber[square]);
                    
                    for (int j = 0; j < 64; j++) {
                        if (((Constants.bishopOccupancyVariations[square][i] >> j) & 1L) == 1) {
                            chessBoard[7 - (j / 8), 7 - (j % 8)] = "X";
                        }
                        if (((Constants.bishopMoves[square][index] >> j) & 1L) == 1) {
                            moves[7 - (j/8), 7 - (j%8)] = "X";
                        }
                    }
                } else if (piece == "Rook") {

                    int index = (int)((Constants.rookOccupancyVariations[square][i] * Constants.rookMagicNumbers[square]) >> Constants.rookMagicShiftNumber[square]);

                    for (int j = 0; j < 64; j++) {
                        if (((Constants.rookOccupancyVariations[square][i] >> j) & 1L) == 1) {
                            chessBoard[7 - (j / 8), 7 - (j % 8)] = "X";
                        }
                        if (((Constants.rookMoves[square][index] >> j) & 1L) == 1) {
                            moves[7 - (j / 8), 7 - (j % 8)] = "X";
                        }
                    }
                }


                Console.WriteLine("  Occupancy Variation:\t\t\t  Moveset:");

                for (int j = 0; j < 8; j++) {

                    if (i == 0) {
                        Console.WriteLine("  ┌───┬───┬───┬───┬───┬───┬───┬───┐\t  ┌───┬───┬───┬───┬───┬───┬───┬───┐");
                    } else if (i >= 1) {
                        Console.WriteLine("  ├───┼───┼───┼───┼───┼───┼───┼───┤\t  ├───┼───┼───┼───┼───┼───┼───┼───┤");
                    }

                    Console.Write((8 - j) + " ");

                    for (int k = 0; k < 8; k++) {
                        Console.Write("| " + chessBoard[j, k] + " ");
                    }
                    Console.Write("| \t" + (8-j) + " ");

                    for (int k = 0; k < 8; k++) {
                        Console.Write("| " + moves[j, k] + " ");
                    }
                    Console.WriteLine("|");
                }
                Console.WriteLine("  └───┴───┴───┴───┴───┴───┴───┴───┘\t  └───┴───┴───┴───┴───┴───┴───┴───┘");
                Console.WriteLine("    A   B   C   D   E   F   G   H\t    A   B   C   D   E   F   G   H");
                Console.WriteLine("");
            }
        }

		/*

        //creates and prints a move object
        public static void createAndPrintMove() {
            uint move = Board.moveEncoder(Constants.BLACK_ROOK, Constants.A1, Constants.A8, Constants.QUIET_MOVE, 0);
            Console.WriteLine(Convert.ToString(move, 2).PadLeft(24, '0'));
        }

        //makes a move using the move method and prints out the resulting board
        public static uint makeMoveTest(Board inputBoard, int sideToMove, int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured) {
            uint moveRepresentation = Board.moveEncoder(pieceMoved, startSquare, destinationSquare, flag, pieceCaptured);
            uint boardRestoreData = inputBoard.makeMove(moveRepresentation);
            InputOutput.drawBoard(inputBoard);
            return boardRestoreData;
        }

        //Unmakes a move using the unmake move method and prints out the resulting board
        public static void unmakeMoveTest(Board inputBoard, int sideToMove, int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured, uint boardRestoreData) {
            uint moveRepresentation = Board.moveEncoder(pieceMoved, startSquare, destinationSquare, flag, pieceCaptured);
            inputBoard.unmakeMove(moveRepresentation, boardRestoreData);
            InputOutput.drawBoard(inputBoard);
        }

		 */

        //Prints out a list of legal moves
        // Have to test if the move is legal or not
        public static void printLegalMove(Board inputBoard)
        {
            int[] moveList = inputBoard.moveGenerator(Constants.ALL_MOVES);
			
            int moveCount = 1;

            foreach (int moveRepresentation in moveList) {

				int startSquare = ((moveRepresentation & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
				int destinationSquare = ((moveRepresentation & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
				int pieceMoved = (inputBoard.pieceArray[startSquare]);
				int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
				stateVariables restoreData = new stateVariables(inputBoard);
	            bool moveLegal = false;

	            if (moveRepresentation == 0) {
		            break;
	            } else {
					
					inputBoard.makeMove(moveRepresentation);
		            if (inputBoard.isMoveLegal(sideToMove) == true) {
						moveLegal = true;
					}
					inputBoard.unmakeMove(moveRepresentation, restoreData);

		            if (moveLegal == true) {
						Console.Write(moveCount + ". " + Test.printMoveStringFromMoveRepresentation(moveRepresentation, inputBoard) + "\t" + ((moveRepresentation & Constants.MOVE_SCORE_MASK) >> Constants.MOVE_SCORE_SHIFT) + "\t");
						Console.WriteLine(inputBoard.staticExchangeEval(startSquare, destinationSquare, sideToMove));
						moveCount++;
		            }
				}
            }
        }

		// Prints out the MVV/LVA array scores
		public static void printMvvLva() {
			for (int i = 1; i < 13; i++) {
				for (int j = 1; j < 13; j++) {
					Console.WriteLine(Constants.pieceCharacter[j] + " x " + Constants.pieceCharacter[i] + " " + Constants.MvvLvaScore[i, j]);
				}
			}
		}

		
		//prints out a move string from a move representation uint
        public static string printMoveStringFromMoveRepresentation(int moveRepresentation, Board inputBoard) {

			int startSquareInt = ((moveRepresentation & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
			int destinationSquareInt = ((moveRepresentation & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
			int flag = ((moveRepresentation & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);
			int pieceMoved = (inputBoard.pieceArray[startSquareInt]);
			int piecePromoted = (moveRepresentation & Constants.PIECE_PROMOTED_MASK) >> Constants.PIECE_PROMOTED_SHIFT;
			string moveString = "";

			// Converts the start square integer to a rank and file
			int columnOfStartSquare = (startSquareInt % 8);
            int rowOfStartSquare = (startSquareInt / 8);
            char fileOfStartSquare = (char)('h' - columnOfStartSquare);
            string startSquare = (fileOfStartSquare + (1 + rowOfStartSquare).ToString());

			// Converts the destination square integer to a rank and file
            int columnOfDestinationSquare = (destinationSquareInt % 8);
            int rowOfDestinationSquare = (destinationSquareInt / 8);
            char fileOfDestinationSquare = (char)('h' - columnOfDestinationSquare);
            string destinationSquare = (fileOfDestinationSquare + (1 + rowOfDestinationSquare).ToString() + " ");

			if (pieceMoved == Constants.WHITE_PAWN) {
                if (flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH) {
                    moveString += (startSquare + destinationSquare);
				} else if (flag == Constants.CAPTURE || flag == Constants.EN_PASSANT_CAPTURE) {
                    moveString += (startSquare + "x" + destinationSquare);
                } else if (flag == Constants.PROMOTION) {
                    switch (piecePromoted) {
                        case Constants.WHITE_KNIGHT: moveString += (startSquare + destinationSquare + "=N"); break;
                        case Constants.WHITE_BISHOP: moveString += (startSquare + destinationSquare + "=B"); break;
                        case Constants.WHITE_ROOK: moveString += (startSquare + destinationSquare + "=R"); break;
                        case Constants.WHITE_QUEEN: moveString += (startSquare + destinationSquare + "=Q"); break;
                    }
                } else if (flag == Constants.PROMOTION_CAPTURE) {
                    switch (piecePromoted) {
                        case Constants.WHITE_KNIGHT: moveString += (startSquare + "x" + destinationSquare + "=N"); break;
                        case Constants.WHITE_BISHOP: moveString += (startSquare + "x" + destinationSquare + "=B"); break;
                        case Constants.WHITE_ROOK: moveString += (startSquare + "x" + destinationSquare + "=R"); break;
                        case Constants.WHITE_QUEEN: moveString += (startSquare + "x" + destinationSquare + "=Q"); break;
                    }
                }

			

			} else if (pieceMoved == Constants.WHITE_KNIGHT) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "N" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "N" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.WHITE_BISHOP) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "B" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "B" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.WHITE_ROOK) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "R" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "R" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.WHITE_QUEEN) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "Q" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "Q" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.WHITE_KING) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "K" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "K" + startSquare + "x" + destinationSquare;
                } else if (flag == Constants.SHORT_CASTLE) {
                    moveString += "O-O ";
                } else if (flag == Constants.LONG_CASTLE) {
                    moveString += "O-O-O ";
                }
			} else if (pieceMoved == Constants.BLACK_PAWN) {
                if (flag == Constants.QUIET_MOVE || flag == Constants.DOUBLE_PAWN_PUSH) {
                    moveString += startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE || flag == Constants.EN_PASSANT_CAPTURE) {
                    moveString += startSquare + "x" + destinationSquare;
                } else if (flag == Constants.PROMOTION) {
                    switch (piecePromoted) {
                        case Constants.BLACK_KNIGHT: moveString += startSquare + destinationSquare + "=n"; break;
                        case Constants.BLACK_BISHOP: moveString += startSquare + destinationSquare + "=b"; break;
                        case Constants.BLACK_ROOK: moveString += startSquare + destinationSquare + "=r"; break;
                        case Constants.BLACK_QUEEN: moveString += startSquare + destinationSquare + "=q"; break;
                    }
                } else if (flag == Constants.PROMOTION_CAPTURE) {
                    switch (piecePromoted) {
                        case Constants.BLACK_KNIGHT: moveString += startSquare + "x" + destinationSquare + "=n"; break;
                        case Constants.BLACK_BISHOP: moveString += startSquare + "x" + destinationSquare + "=b"; break;
                        case Constants.BLACK_ROOK: moveString += startSquare + "x" + destinationSquare + "=r"; break;
                        case Constants.BLACK_QUEEN: moveString += startSquare + "x" + destinationSquare + "=q"; break;
                    }
                }
			} else if (pieceMoved == Constants.BLACK_KNIGHT) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "n" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "n" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.BLACK_BISHOP) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "b" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "b" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.BLACK_ROOK) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "r" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "r" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.BLACK_QUEEN) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "q" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "q" + startSquare + "x" + destinationSquare;
                }
			} else if (pieceMoved == Constants.BLACK_KING) {
                if (flag == Constants.QUIET_MOVE) {
                    moveString += "k" + startSquare + destinationSquare;
                } else if (flag == Constants.CAPTURE) {
                    moveString += "k" + startSquare + "x" + destinationSquare;
                } else if (flag == Constants.SHORT_CASTLE) {
                    moveString += " O-O ";
                } else if (flag == Constants.LONG_CASTLE) {
                    moveString += "O-O-O ";
                }
            }
			return moveString;
        }
    }
}
