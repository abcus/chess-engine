using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
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

        //Checks if the king is in check and prints out the result
        public static void kingInCheckTest(Board inputBoard, int colourOfKingToCheck) {

            int indexOfKing = 0;

            if (colourOfKingToCheck == Constants.WHITE) {
                indexOfKing = Constants.findFirstSet(inputBoard.arrayOfBitboards[Constants.WHITE_KING]);
            } else if (colourOfKingToCheck == Constants.BLACK) {
                indexOfKing = Constants.findFirstSet(inputBoard.arrayOfBitboards[Constants.BLACK_KING]);
            }

            int checkStatus = inputBoard.timesSquareIsAttacked(colourOfKingToCheck, indexOfKing);

            switch (checkStatus) {
                case Constants.NOT_IN_CHECK: Console.WriteLine("King not in check"); break;
                case Constants.CHECK: Console.WriteLine("King is in check"); break;
                case Constants.DOUBLE_CHECK: Console.WriteLine("King is in double check"); break;
				case Constants.MULTIPLE_CHECK: Console.WriteLine("King is in multiple check"); break;
            }
        }
        //Prints out a list of legal moves
        // Have to test if the move is legal or not
        public static void printLegalMove(Board inputBoard)
        {
            int[] moveList = inputBoard.generateListOfAlmostLegalMoves();

            Console.WriteLine("Number of legal moves in this position: " + moveList.Length);
            int moveCount = 0;

            foreach (int moveRepresentation in moveList) {
                moveCount ++;

				Console.Write(moveCount + ". " + printMoveStringFromMoveRepresentation(moveRepresentation));
                


            }
        }

		//Extracts the piece moved from the integer that encodes the move
		private static int getPieceMoved(int moveRepresentation) {
            int pieceMoved = ((moveRepresentation & Constants.PIECE_MOVED_MASK) >> 0);
			return pieceMoved;
		}
		//Extracts the start square from the integer that encodes the move
		private static int getStartSquare(int moveRepresentation) {
			int startSquare = ((moveRepresentation & Constants.START_SQUARE_MASK) >> 4);
			return startSquare;
		}
		//Extracts the destination square from the integer that encodes the move
		private static int getDestinationSquare(int moveRepresentation) {
			int destinationSquare = ((moveRepresentation & Constants.DESTINATION_SQUARE_MASK) >> 10);
			return destinationSquare;
		}
		//Extracts the flag from the integer that encodes the move
		private static int getFlag(int moveRepresentation) {
			int flag = ((moveRepresentation & Constants.FLAG_MASK) >> 16);
			return flag;
		}
		//Extracts the piece captured from the integer that encodes the move
		private static int getPieceCaptured(int moveRepresentation) {
			int pieceCaptured = (moveRepresentation & Constants.PIECE_CAPTURED_MASK) >> 20;
            return pieceCaptured;
		}
        //Extracts the piece promoted from the integer that encodes the move
        private static int getPiecePromoted(int moveRepresentation) {
            int piecePromoted = (moveRepresentation & Constants.PIECE_PROMOTED_MASK) >> 24;
            return piecePromoted;
        }

		//prints out a move string from a move representation uint
        public static string printMoveStringFromMoveRepresentation(int moveRepresentation) {
            int columnOfStartSquare = (getStartSquare(moveRepresentation) % 8);
            int rowOfStartSquare = (getStartSquare(moveRepresentation) / 8);
            char fileOfStartSquare = (char)('h' - columnOfStartSquare);
            string startSquare = (fileOfStartSquare + (1 + rowOfStartSquare).ToString());

            int columnOfDestinationSquare = (getDestinationSquare(moveRepresentation) % 8);
            int rowOfDestinationSquare = (getDestinationSquare(moveRepresentation) / 8);
            char fileOfDestinationSquare = (char)('h' - columnOfDestinationSquare);
            string destinationSquare = (fileOfDestinationSquare + (1 + rowOfDestinationSquare).ToString() + " ");

            string moveString = "";
            moveString += (startSquare + destinationSquare);
            
            if (getPieceMoved(moveRepresentation) == Constants.WHITE_PAWN) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE || getFlag(moveRepresentation) == Constants.DOUBLE_PAWN_PUSH) {
                    moveString += startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE || getFlag(moveRepresentation) == Constants.EN_PASSANT_CAPTURE) {
                    moveString += startSquare + "x" + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.PROMOTION) {
                    switch (getPiecePromoted(moveRepresentation)) {
                        case Constants.WHITE_KNIGHT: moveString += startSquare + destinationSquare + "=N"; break;
                        case Constants.WHITE_BISHOP: moveString += startSquare + destinationSquare + "=B"; break;
                        case Constants.WHITE_ROOK: moveString += startSquare + destinationSquare + "=R"; break;
                        case Constants.WHITE_QUEEN: moveString += startSquare + destinationSquare + "=Q"; break;
                    }
                } else if (getFlag(moveRepresentation) == Constants.PROMOTION_CAPTURE) {
                    switch (getPiecePromoted(moveRepresentation)) {
                        case Constants.WHITE_KNIGHT: moveString += startSquare + "x" + destinationSquare + "=N"; break;
                        case Constants.WHITE_BISHOP: moveString += startSquare + "x" + destinationSquare + "=B"; break;
                        case Constants.WHITE_ROOK: moveString += startSquare + "x" + destinationSquare + "=R"; break;
                        case Constants.WHITE_QUEEN: moveString += startSquare + "x" + destinationSquare + "=Q"; break;
                    }
                }     
            } else if (getPieceMoved(moveRepresentation) == Constants.WHITE_KNIGHT) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "N" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "N" + startSquare + "x" + destinationSquare;
                } 
            } else if (getPieceMoved(moveRepresentation) == Constants.WHITE_BISHOP) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "B" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "B" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.WHITE_ROOK) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "R" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "R" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.WHITE_QUEEN) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "Q" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "Q" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.WHITE_KING) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "K" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "K" + startSquare + "x" + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.SHORT_CASTLE) {
                    moveString += "O-O";
                } else if (getFlag(moveRepresentation) == Constants.LONG_CASTLE) {
                    moveString += "O-O-O";
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.BLACK_PAWN) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE || getFlag(moveRepresentation) == Constants.DOUBLE_PAWN_PUSH) {
                    moveString += startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE || getFlag(moveRepresentation) == Constants.EN_PASSANT_CAPTURE) {
                    moveString += startSquare + "x" + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.PROMOTION) {
                    switch (getPiecePromoted(moveRepresentation)) {
                        case Constants.BLACK_KNIGHT: moveString += startSquare + destinationSquare + "=n"; break;
                        case Constants.BLACK_BISHOP: moveString += startSquare + destinationSquare + "=b"; break;
                        case Constants.BLACK_ROOK: moveString += startSquare + destinationSquare + "=r"; break;
                        case Constants.BLACK_QUEEN: moveString += startSquare + destinationSquare + "=q"; break;
                    }
                } else if (getFlag(moveRepresentation) == Constants.PROMOTION_CAPTURE) {
                    switch (getPiecePromoted(moveRepresentation)) {
                        case Constants.BLACK_KNIGHT: moveString += startSquare + "x" + destinationSquare + "=n"; break;
                        case Constants.BLACK_BISHOP: moveString += startSquare + "x" + destinationSquare + "=b"; break;
                        case Constants.BLACK_ROOK: moveString += startSquare + "x" + destinationSquare + "=r"; break;
                        case Constants.BLACK_QUEEN: moveString += startSquare + "x" + destinationSquare + "=q"; break;
                    }
                }  
            } else if (getPieceMoved(moveRepresentation) == Constants.BLACK_KNIGHT) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "n" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "n" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.BLACK_BISHOP) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "b" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "b" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.BLACK_ROOK) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "r" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "r" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.BLACK_QUEEN) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "q" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "q" + startSquare + "x" + destinationSquare;
                }
            } else if (getPieceMoved(moveRepresentation) == Constants.BLACK_KING) {
                if (getFlag(moveRepresentation) == Constants.QUIET_MOVE) {
                    moveString += "k" + startSquare + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.CAPTURE) {
                    moveString += "k" + startSquare + "x" + destinationSquare;
                } else if (getFlag(moveRepresentation) == Constants.SHORT_CASTLE) {
                    moveString += " O-O";
                } else if (getFlag(moveRepresentation) == Constants.LONG_CASTLE) {
                    moveString += "O-O-O";
                }
            }
            return moveString;
        }

		public static int perft(Board inputBoard, int depth) {
			int nodes = 0;

		    if (depth == 1) {
                
                int[] pseudoLegalMoveList;
                
		        if (inputBoard.isInCheck() == false) {
		            pseudoLegalMoveList = inputBoard.generateListOfAlmostLegalMoves();
		        } else {
                    pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
		        }

                int numberOfLegalMovesFromList = 0;
		        int index = 0;

                while (pseudoLegalMoveList[index] != 0) {
                    int move = pseudoLegalMoveList[index];
                    int pieceMoved = ((move & Constants.PIECE_MOVED_MASK) >> 0);
                    int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
                    int flag = ((move& Constants.FLAG_MASK) >> 16);

                    if (flag == Constants.EN_PASSANT_CAPTURE || pieceMoved == Constants.WHITE_KING || pieceMoved == Constants.BLACK_KING) {
                        inputBoard.makeMove(move);
                        if (inputBoard.isMoveLegal(sideToMove) == true) {
                            numberOfLegalMovesFromList++;
                        }
                        inputBoard.unmakeMove(move);
                        index++;
                    } else {
                        numberOfLegalMovesFromList++;
                        index ++;
                    }

                    
                }
                return numberOfLegalMovesFromList;
		    } else {
                
			    int[] pseudoLegalMoveList = null;
                
                if (inputBoard.isInCheck() == false) {
                    pseudoLegalMoveList = inputBoard.generateListOfAlmostLegalMoves(); 
                } else {
                    pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
                }
                
                int index = 0;

				while (pseudoLegalMoveList[index] != 0) {
				    int move = pseudoLegalMoveList[index];
                    int pieceMoved = ((move & Constants.PIECE_MOVED_MASK) >> 0);
                    int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
                    int flag = ((move & Constants.FLAG_MASK) >> 16);

                    inputBoard.makeMove(move);
				    
				    if (flag == Constants.EN_PASSANT_CAPTURE || pieceMoved == Constants.WHITE_KING || pieceMoved == Constants.BLACK_KING) {
                        if (inputBoard.isMoveLegal(sideToMove) == true) {
                            nodes += perft(inputBoard, depth - 1);
                        }    
				    } else {
				        nodes += perft(inputBoard, depth - 1);
				    }
                    inputBoard.unmakeMove(move);
				    index ++;
				}
				return nodes;
			}
		}
        /*
		public static void perftDivide(Board inputBoard, int depth) {

		    int[] pseudoLegalMoveList;
            if (inputBoard.isInCheck() == false) {
                pseudoLegalMoveList = inputBoard.generateListOfAlmostLegalMoves(); 
            } else {
                pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
            }
		    int index = 0;

			int count = 0;
            int boardRestoreData = inputBoard.encodeBoardRestoreData();

			while (pseudoLegalMoveList[index] != 0) {

			    int move = pseudoLegalMoveList[index];
				count++;
				int pieceMoved = (move & Constants.PIECE_MOVED_MASK) >> 0;
			    int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;

                inputBoard.makeMove(move);
			    if (inputBoard.isMoveLegal(sideToMove) == true) {
                    Console.WriteLine(printMoveStringFromMoveRepresentation(move) + "\t" + perft(inputBoard, depth - 1));
			    }
				inputBoard.unmakeMove(move, boardRestoreData);
			    index ++;
			}
		}
        */
	    public static void printPerft(Board inputBoard, int depth) {
            Stopwatch s = Stopwatch.StartNew();
            int numberOfNodes = Test.perft(inputBoard, depth);
            string numberOfNodesString = numberOfNodes.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";

            Console.WriteLine("Number Of Nodes: \t\t" + numberOfNodes.ToString(numberOfNodesString));
            Console.WriteLine("Time: \t\t\t\t" + s.Elapsed);
            long nodesPerSecond = (numberOfNodes) / (s.ElapsedMilliseconds) * 1000;
            string nodesPerSecondString = nodesPerSecond.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";

            Console.WriteLine("Nodes per second: \t\t" + nodesPerSecond.ToString(nodesPerSecondString));
	    }
         
        public static void perftSuite1(Board inputBoard) {
            inputBoard.FENToBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            int nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
            Console.WriteLine("119,060,324 (actual value)");
            Console.WriteLine("Difference: " + (119060324 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");
            nodes = Test.perft(inputBoard, 5);
            Console.WriteLine(nodes);
            Console.WriteLine("193,690,690 (actual value)");
            Console.WriteLine("Difference: " + (193690690 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -");
            nodes = Test.perft(inputBoard, 7);
            Console.WriteLine(nodes);
            Console.WriteLine("178,633,661 (actual value)");
            Console.WriteLine("Difference: " + (178633661 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
            nodes = Test.perft(inputBoard, 5);
            Console.WriteLine(nodes);
            Console.WriteLine("15,833,292 (actual value)");
            Console.WriteLine("Difference: " + (15833292 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1");
            nodes = Test.perft(inputBoard, 5);
            Console.WriteLine(nodes);
            Console.WriteLine("15,833,292 (actual value)");
            Console.WriteLine("Difference: " + (15833292 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6");
            nodes = Test.perft(inputBoard, 3);
            Console.WriteLine(nodes);
            Console.WriteLine("53,392(actual value)");
            Console.WriteLine("Difference: " + (53392 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
            nodes = Test.perft(inputBoard, 5);
            Console.WriteLine(nodes);
            Console.WriteLine("164,075,551(actual value)");
            Console.WriteLine("Difference: " + (164075551 - nodes));
            Console.WriteLine("");

            inputBoard.FENToBoard("3k4/3p4/8/K1P4r/8/8/8/8 b - - 0 1");
            nodes = Test.perft(inputBoard, 6);
			Console.WriteLine(nodes);
		    Console.WriteLine("1134888 (actual value)");
            Console.WriteLine("Difference: " + (1134888 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("8/8/4k3/8/2p5/8/B2P2K1/8 w - - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("1015133 (actual value)");
            Console.WriteLine("Difference: " + (1015133 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("1440467 (actual value)");
            Console.WriteLine("Difference: " + (1440467 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("5k2/8/8/8/8/8/8/4K2R w K - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("661072 (actual value)");
            Console.WriteLine("Difference: " + (661072 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("3k4/8/8/8/8/8/8/R3K3 w Q - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("803711 (actual value)");
            Console.WriteLine("Difference: " + (803711 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("r3k2r/1b4bq/8/8/8/8/7B/R3K2R w KQkq - 0 1");
            nodes = Test.perft(inputBoard, 4);
            Console.WriteLine(nodes);
			Console.WriteLine("1274206 (actual value)");
            Console.WriteLine("Difference: " + (1274206 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("r3k2r/8/3Q4/8/8/5q2/8/R3K2R b KQkq - 0 1");
            nodes = Test.perft(inputBoard, 4);
            Console.WriteLine(nodes);
			Console.WriteLine("1720476 (actual value)");
            Console.WriteLine("Difference: " + (1720476 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("2K2r2/4P3/8/8/8/8/8/3k4 w - - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("3821001 (actual value)");
            Console.WriteLine("Difference: " + (3821001 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("8/8/1P2K3/8/2n5/1q6/8/5k2 b - - 0 1");
            nodes = Test.perft(inputBoard, 5);
            Console.WriteLine(nodes);
			Console.WriteLine("1004658 (actual value)");
            Console.WriteLine("Difference: " + (1004658 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("4k3/1P6/8/8/8/8/K7/8 w - - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("217342 (actual value)");
            Console.WriteLine("Difference: " + (217342 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("8/P1k5/K7/8/8/8/8/8 w - - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("92683 (actual value)");
            Console.WriteLine("Difference: " + (92683 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("K1k5/8/P7/8/8/8/8/8 w - - 0 1");
            nodes = Test.perft(inputBoard, 6);
            Console.WriteLine(nodes);
			Console.WriteLine("2217 (actual value)");
            Console.WriteLine("Difference: " + (2217 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("8/k1P5/8/1K6/8/8/8/8 w - - 0 1");
            nodes = Test.perft(inputBoard, 7);
            Console.WriteLine(nodes);
			Console.WriteLine("567584 (actual value)");
            Console.WriteLine("Difference: " + (567584 - nodes));
			Console.WriteLine("");

            inputBoard.FENToBoard("8/8/2k5/5q2/5n2/8/5K2/8 b - - 0 1");
            nodes = Test.perft(inputBoard, 4);
            Console.WriteLine(nodes);
			Console.WriteLine("23527 (actual value)");
            Console.WriteLine("Difference: " + (23527 - nodes));
			Console.WriteLine("");
	    }

	    public static void perftSuite2(Board inputBoard) {
	        
            //Reads the perft suite into an array one line at a time
            string[] fileInput = System.IO.File.ReadAllLines(@"C:\Users\Kevin\Desktop\chess\perftsuite.txt");
	        
            //Splits each line into FEN, and depth 1-6
            string[][] delimitedFileInput = new string[fileInput.Length][];

	        for (int i=0; i<fileInput.Length; i++) {
	            string temp = fileInput[i];
                string[] position = temp.Split('\t');
	            delimitedFileInput[i] = position;
	        }
	        
            Console.WriteLine("Depth 6 Perft test:");
            Console.WriteLine("┌───────────────────────────────────────────────────────────┬──────────────┬──────────────┬───────────────┐");
            Console.WriteLine("{0,-60}{1,-15}{2,-15}{3,-30}", "│FEN STRING:", "│EXPECTED:", "│ACTUAL:", "│EXP - ACT:     │");

            Stopwatch s = Stopwatch.StartNew();
	        for (int j = 0; j < fileInput.Length; j++) {
                inputBoard.FENToBoard(delimitedFileInput[j][0]);
	            int perftDepth6ExpectedResult = Convert.ToInt32(delimitedFileInput[j][6]);
                int perftDepth6CalculatedResult = perft(inputBoard, 6);

                string perftDepth6ExpectedResultString = perftDepth6ExpectedResult.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";
                string perftDepth6CalculatedResultString = perftDepth6CalculatedResult.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";

                Console.WriteLine("├───────────────────────────────────────────────────────────┼──────────────┼──────────────┼───────────────┤");
                Console.WriteLine("{0,-60}{1,-15}{2,-15}{3,-30}", "│" + delimitedFileInput[j][0], "│" + perftDepth6ExpectedResult.ToString(perftDepth6ExpectedResultString), "│" + perftDepth6CalculatedResult.ToString(perftDepth6CalculatedResultString), "│" + (perftDepth6ExpectedResult - perftDepth6CalculatedResult) + "              │");
            }
            Console.WriteLine("└───────────────────────────────────────────────────────────┴──────────────┴──────────────┴───────────────┘");
            Console.WriteLine("Time:" + s.Elapsed);
            long nodesPerSecond = (4387232996) / (s.ElapsedMilliseconds / 1000);
            string nodesPerSecondString = nodesPerSecond.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";
            Console.WriteLine("Nodes per second: \t\t" + nodesPerSecond.ToString(nodesPerSecondString));
        }
    }
}
