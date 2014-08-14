using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {

    public static class UCI_IO {

		// Creates a board object and initializes to to the start position
        internal static Board position = new Board(Constants.FEN_START);

		// Creates a transposition table object (which is not cleared for the duration of the runtime of the program)
        internal static TTable hashTable = new TTable();

		// Creates a background worker object for the search
		internal static BackgroundWorker searchWorker;   

	    static UCI_IO  () {
			// Defining what method to call when the worker is started, what method to call when the worker completes
			// Enables the worker to support cancellation
			searchWorker = new BackgroundWorker();
			searchWorker.DoWork += new DoWorkEventHandler(searchWorker_StartSearch);
			searchWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(searchWorker_SearchCompleted);
			searchWorker.WorkerSupportsCancellation = true;
	    }

	    // Method that continuously accepts user input (it runs on the main thread)
        public static bool processGUIMessages(int waitTime) {

            string input = Console.ReadLine();

			// If the input is non-zero in length, it passes it to the input handler
			// Otherwise, it sleeps for a certain number of milliseconds (so that constant polling is not an issue) and returns true
            if (input.Length > 0) {
                return inputHandler(input);
            } else if (waitTime > 0) {
                Thread.Sleep(waitTime);
            }  
            return true;  
        }

		// Method that receives input and performs the appropriate action
        public static bool inputHandler(string input) {

            // Reads the input into an array, converts the array to a list, and gets the first element
            string[] stringArray = input.Split(' ');
            List<string> stringList = stringArray.ToList();
            string string0 = stringList[0];
            
			// Takes various actions depending on what the first element is
            
			// Prints the perft of the current board to the specified depth
			// Or prints the perft test suite
			if (string0 == "perft") {
                int depth = Convert.ToInt32(stringArray[1]);
                Test.printPerft(position, depth);
            } else if (string0 == "perftsuite") {
              Test.perftSuite1(position);
              Test.perftSuite2(position);
            } 
			// Prints the board along with other state variables (castling rights, etc.)
			else if (string0 == "print") {
                UCI_IO.drawBoard(position);
            } 
			// Quits the program
			else if (string0 == "quit") {
                return false;
            } 
			// Prints the author, program name
			else if (string0 == "uci") {
                UCIInitialize();
            } 
			// Doesn't do anything right now
			else if (string0 == "setoption") {
                stringList.RemoveAt(0);
                setOption(stringList);
            } 
			// Prints "readyok"
			else if (string0 == "isready") {
                isReady();
            } 
			// Doesn't do anything right now
			else if (string0 == "ucinewgame") {
                UCINewGame();
            } 
			// Sets the board to the position specified by the GUI
			else if (string0 == "position") {
                stringList.RemoveAt(0);
                setPosition(stringList);
            } 
			// Starts the search
			else if (string0 == "go") {
				searchWorker.RunWorkerAsync();
            } 
			// Stops the search
			else if (string0 == "stop") {
	            if (searchWorker.IsBusy) {
		            searchWorker.CancelAsync();
	            }
            }
            return true;
        }

		// Method that prints out the engine name, author
        public static void UCIInitialize() {
            Console.WriteLine("id name Spark v0.1");
            Console.WriteLine("id author John");
            Console.WriteLine("uciok");
        }

		// Doesn't do anything right now
        public static void setOption(List<string> inputStringList) {
            // set options
        }

		// Prints "readyok"
        public static void isReady() {
            Console.WriteLine("readyok");
        }

		// Doesn't do anything right now
        public static void UCINewGame() {
            //some shit
        }

		// Sets the board to the position specified by the GUI
        public static void setPosition(List<string> inputStringList) {
            
            // Sets the board to the starting position
            if (inputStringList[0] == "startpos") {
                inputStringList.RemoveAt(0);
                position = new Board(Constants.FEN_START);
            } 
			// Sets the board to the "kiwipete" position
			else if (inputStringList[0] == "kiwipete") {
	            inputStringList.RemoveAt(0);
	            position = new Board(Constants.FEN_KIWIPETE);
            }
            // Sets the baord to the position specified by the FEN string
            else if (inputStringList[0] == "fen") {
                inputStringList.RemoveAt(0);

                int index;
                if (inputStringList.Contains("moves")) {
                    index = (inputStringList.IndexOf("moves")) - 1;
                } else {
                    index = inputStringList.Count - 1;
                }
                
                string FEN = "";

                for (int i = 0; i <= index; i++) {
                    FEN += (inputStringList[0]);
                    inputStringList.RemoveAt(0);
                    if (i != index) { // fixed off by 1 issue that was causing crashes
                        FEN += " ";
                    }
                }
				position = new Board(FEN);
            }

            // Makes any moves
            if (inputStringList.Contains("moves")) {
                inputStringList.RemoveAt(0);

                // Loops through all the moves from the input
                for (int i = 0; i <= inputStringList.Count - 1; i++) {
                    string move = inputStringList[i];

                    // Generates all pseudo legal moves from the position
                    int[] pseudoLegalMoveList = null;
                    if (position.isInCheck() == false) {
                        pseudoLegalMoveList = position.generateAlmostLegalMoves(); 
                    } else {
                        pseudoLegalMoveList = position.checkEvasionGenerator();
                    }

                    // compares the move from the input to each pseudo legal move
                    // If they match, then makes the move
                    for (int j = 0; pseudoLegalMoveList[j] != 0; j++) {
                        String moveString = getMoveStringFromMoveRepresentation(pseudoLegalMoveList[j]);

                        if (move == moveString) {
                            position.makeMove(pseudoLegalMoveList[j]);
                            break;
                        }
                    }
                }
            } 
        }

		// Starts the search 
        public static void searchWorker_StartSearch(object sender, DoWorkEventArgs e) {
            
			// Creates a new search object
	        Search.initSearch(position, e);

        }

		// Prints out the results when the search has completed (or been stopped)
	    public static void searchWorker_SearchCompleted(object sender, RunWorkerCompletedEventArgs e) {
			Console.WriteLine("bestmove " + getMoveStringFromMoveRepresentation(Search.result.move));
			Console.WriteLine("");
			Console.WriteLine("Percentage of fail high first:\t\t" + Search.failHighFirst/(Search.failHigh + Search.failHighFirst) * 100);
			
		}

		// Prints out information during iterative deepening
	    public static void printInfo(List<string> PVLine, int depth) {
		    Console.Write("info");
			if (Constants.CHECKMATE - Search.result.evaluationScore < Constants.MAX_DEPTH) {
				Console.Write(" score mate " + (Constants.CHECKMATE - Search.result.evaluationScore + 1)/2);    
		    } else if (-Constants.CHECKMATE - Search.result.evaluationScore > -Constants.MAX_DEPTH) {
			    Console.Write(" score mate " + -(Constants.CHECKMATE - -Search.result.evaluationScore + 1)/2);
		    } else {
				Console.Write(" score cp " + (int)((Search.result.evaluationScore) / 2.28));    
		    }
			Console.Write(" depth " + depth);
		    Console.Write(" nodes " + Search.result.nodesVisited);
			Console.Write(" time " + Search.result.time + " pv ");
			foreach (string move in PVLine) {
				Console.Write(move + " ");
			}
			Console.Write("nps " + Search.result.nodesVisited/(ulong)(Search.result.time + 1) * 1000);
			Console.WriteLine("");
	    }

        // Extracts the start square from the integer that encodes the move
        private static int getStartSquare(int moveRepresentation) {
            int startSquare = ((moveRepresentation & Constants.START_SQUARE_MASK) >> 4);
            return startSquare;
        }
        // Extracts the destination square from the integer that encodes the move
        private static int getDestinationSquare(int moveRepresentation) {
            int destinationSquare = ((moveRepresentation & Constants.DESTINATION_SQUARE_MASK) >> 10);
            return destinationSquare;
        }
        // Extracts the piece promoted from the integer that encodes the move
        private static int getPiecePromoted(int moveRepresentation) {
            int piecePromoted = (moveRepresentation & Constants.PIECE_PROMOTED_MASK) >> 24;
            return piecePromoted;
        }
        
        // prints out a move string from a move representation uint
        public static string getMoveStringFromMoveRepresentation(int moveRepresentation) {
            int columnOfStartSquare = (getStartSquare(moveRepresentation) % 8);
            int rowOfStartSquare = (getStartSquare(moveRepresentation) / 8);
            char fileOfStartSquare = (char)('h' - columnOfStartSquare);
            string startSquare = (fileOfStartSquare + (1 + rowOfStartSquare).ToString());

            int columnOfDestinationSquare = (getDestinationSquare(moveRepresentation) % 8);
            int rowOfDestinationSquare = (getDestinationSquare(moveRepresentation) / 8);
            char fileOfDestinationSquare = (char)('h' - columnOfDestinationSquare);
            string destinationSquare = (fileOfDestinationSquare + (1 + rowOfDestinationSquare).ToString());

            string moveString = "";
            moveString += (startSquare + destinationSquare);

            int piecePromoted = getPiecePromoted(moveRepresentation);

            if (piecePromoted == Constants.WHITE_KNIGHT || piecePromoted == Constants.BLACK_KNIGHT) {
                moveString += "n";
            } else if (piecePromoted == Constants.WHITE_BISHOP || piecePromoted == Constants.BLACK_BISHOP) {
                moveString += "b";
            } else if (piecePromoted == Constants.WHITE_ROOK || piecePromoted == Constants.BLACK_ROOK) {
                moveString += "r";
            } else if (piecePromoted == Constants.WHITE_QUEEN || piecePromoted == Constants.BLACK_QUEEN) {
                moveString += "q";
            }
            return moveString;
        }



        //Draws the board on the console
        public static void drawBoard(Board inputBoard) {

            //gets the bitboards from the board object
            
            ulong wPawn = inputBoard.arrayOfBitboards[1];
			ulong wKnight = inputBoard.arrayOfBitboards[2];
			ulong wBishop = inputBoard.arrayOfBitboards[3];
			ulong wRook = inputBoard.arrayOfBitboards[4];
			ulong wQueen = inputBoard.arrayOfBitboards[5];
			ulong wKing = inputBoard.arrayOfBitboards[6];

			ulong bPawn = inputBoard.arrayOfBitboards[7];
			ulong bKnight = inputBoard.arrayOfBitboards[8];
			ulong bBishop = inputBoard.arrayOfBitboards[9];
			ulong bRook = inputBoard.arrayOfBitboards[10];
			ulong bQueen = inputBoard.arrayOfBitboards[11];
			ulong bKing = inputBoard.arrayOfBitboards[12];

            //creates a new 8x8 array of String and sets it all to spaces
            string[,] chessBoard = new string[8, 8];
            for (int i = 0; i < 64; i++) {
                chessBoard[i / 8, i % 8] = "   ";
            }

            //Goes through each of the bitboards; if they have a "1" then it sets the appropriate element in the array to the appropriate piece
            //Note that the array goes from A8 to H1
            for (int i = 0; i < 64; i++) {
                if (((wPawn >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " P ";
                } if (((wKnight >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " N ";
                } if (((wBishop >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " B ";
                } if (((wRook >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " R ";
                } if (((wQueen >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " Q ";
                } if (((wKing >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "(K)";
                } if (((bPawn >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " p ";
                } if (((bKnight >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " n ";
                } if (((bBishop >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " b ";
                } if (((bRook >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " r ";
                } if (((bQueen >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = " q ";
                } if (((bKing >> i) & 1L) == 1) {
                    chessBoard[7 - (i / 8), 7 - (i % 8)] = "(k)";
                }
            }

            //Goes through the 8x8 array and prints its contents
            for (int i = 0; i < 8; i++) {

                if (i == 0) {
                    Console.WriteLine("  ┌───┬───┬───┬───┬───┬───┬───┬───┐");
                } else if (i >= 1) {
                    Console.WriteLine("  ├───┼───┼───┼───┼───┼───┼───┼───┤");
                }

                Console.Write((8 - i) + " ");

                for (int j = 0; j < 8; j++) {
                    Console.Write("│" + chessBoard[i, j] + "");
                }
                Console.WriteLine("│");
            }
            Console.WriteLine("  └───┴───┴───┴───┴───┴───┴───┴───┘");
            Console.WriteLine("    A   B   C   D   E   F   G   H");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            //Prints out the side to move, castling rights, en-pessant square, halfmoves since capture/pawn advance, and fullmove number

            //side to move
            int sideToMove = inputBoard.sideToMove;
            String colour = (sideToMove == Constants.WHITE) ? "WHITE" : "BLACK";
            Console.WriteLine("Side to move: " + colour);

            //castle rights
	        int whiteShortCastleRights = inputBoard.whiteShortCastleRights;
			int whiteLongCastleRights = inputBoard.whiteLongCastleRights;
			int blackShortCastleRights = inputBoard.blackShortCastleRights;
			int blackLongCastleRights = inputBoard.blackLongCastleRights;

            Console.WriteLine("White Short Castle Rights: " + whiteShortCastleRights);
            Console.WriteLine("White Long Castle Rights: " + whiteLongCastleRights);
            Console.WriteLine("Black Short Castle Rights: " + blackShortCastleRights);
            Console.WriteLine("Black Long Castle Rights: " + blackLongCastleRights);

            //en passant square
            ulong enPassantSquareBitboard = inputBoard.enPassantSquare;


            if (enPassantSquareBitboard != 0) {
                int enPassantIndex = Constants.bitScan(enPassantSquareBitboard).ElementAt(0);
                string firstChar = char.ConvertFromUtf32((int)('H' - (enPassantIndex % 8))).ToString();
                string secondChar = ((enPassantIndex / 8) + 1).ToString();
                Console.WriteLine("En Passant Square: " + firstChar + secondChar);
            } else if (enPassantSquareBitboard == 0) {
                Console.WriteLine("En Passant Square: N/A");
            }


            //prints move data (fullmove number, half-moves since last pawn push/capture, repetitions of position)
            //If no move number data or halfmove clock data, then prints N/A
            int fullMoveNumber = inputBoard.fullMoveNumber;
            Console.WriteLine("Fullmove number: " + fullMoveNumber);

	        int fiftyMoveRule = inputBoard.fiftyMoveRule;
            Console.WriteLine("Half moves since last pawn push/capture: " + fiftyMoveRule);

	        int repetitionOfPosition = inputBoard.getRepetitionNumber();
			
			Console.WriteLine("Repetitions of this position: " + repetitionOfPosition);
	        if (repetitionOfPosition >= 3) {
		        Console.WriteLine("Draw by threefold repetition");
	        }

            Console.WriteLine("Zobrist key: " + inputBoard.zobristKey);
			Console.WriteLine("");

            Test.kingInCheckTest(inputBoard, inputBoard.sideToMove);
        }
    }
}
