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
	    
		// Keeps track of how many half-moves have been played out of book (for time management)
		internal static int plyOutOfBook = 0;

		// Stores the finish time
	    internal static SearchInfo info;

		// Creates a transposition table object (which is not cleared for the duration of the runtime of the program)
        // this object contains the actual transposition table, as well as a smaller PV table for storing and retrieving the principal variation (less overwrites)
		internal static TTable transpositionTable = new TTable();

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
                Perft.printPerft(position, depth);
            } else if (string0 == "perftsuite") {
              Perft.perftSuite1(position);
              Perft.perftSuite2(position);
            } else if (string0 == "divide") {
				Perft.perftDivide(position, Convert.ToInt32(stringList[1]));
            }

			// Prints the board along with other state variables (castling rights, etc.)
			else if (string0 == "print") {
                UCI_IO.drawBoard(position);
				UCI_IO.transpositionTable.printDepthFrequency();
            } else if (string0 == "moves") {
				Test.printLegalMove(position);
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
                parsePosition(stringList);
            } 
			// Starts the search
			else if (string0 == "go") {
				stringList.RemoveAt(0);
				info = parseGo(stringList);
				searchWorker.RunWorkerAsync();
            } 
			// Stops the search
			else if (string0 == "stop") {
	            if (searchWorker.IsBusy) {
		            searchWorker.CancelAsync();
	            }
            }
			// Sent by the GUI if the move made by the opponent was the move predicted by the engine
			// Set Search.info.timeControl to true
			// If the thinking time has been exceeded, then the search will exit immediately
			// Otherwise, the search will continue
			else if (string0 == "ponderhit") {
				Search.info.timeControl = true;
			}
            return true;
        }

		// Method that prints out the engine name, author
        public static void UCIInitialize() {
            Console.WriteLine("id name Spark v0.1");
            Console.WriteLine("id author Abcus");
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
        public static void parsePosition(List<string> inputStringList) {
            
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
                        pseudoLegalMoveList = position.moveGenerator(Constants.ALL_MOVES); 
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

		// Parses the go command
		public static SearchInfo parseGo(List<string> inputStringList) {

			bool timeControl = false;
			bool depthLimit = false;
			int depth = - 1;
			int movesToGo = 30;
			int moveTime = -1;

			int timeLeft = -1;
			int increment = 0;
			
			for (int i = 0; i < inputStringList.Count; i++) {
				if (inputStringList[i] == "infinite") {
				
				}
				if (inputStringList[i] == "depth") {
					depthLimit = true;
					depth = Convert.ToInt32(inputStringList[i + 1]);
				}
				if (inputStringList[i] == "movestogo") {
					movesToGo = Convert.ToInt32(inputStringList[i + 1]);
				}
				if (inputStringList[i] == "movetime") {
					timeControl = true;
					moveTime = Convert.ToInt32(inputStringList[i + 1]);
				}
				if (inputStringList[i] == "wtime" && position.sideToMove == Constants.WHITE) {
					timeControl = true;
					timeLeft = Convert.ToInt32(inputStringList[i + 1]);
				}
				if (inputStringList[i] == "winc" && position.sideToMove == Constants.WHITE) {
					timeControl = true;
					increment = Convert.ToInt32(inputStringList[i + 1]);
				}
				if (inputStringList[i] == "btime" && position.sideToMove == Constants.BLACK) {
					timeControl = true;
					timeLeft = Convert.ToInt32(inputStringList[i + 1]);
				}
				if (inputStringList[i] == "binc" && position.sideToMove == Constants.BLACK) {
					timeControl = true;
					increment = Convert.ToInt32(inputStringList[i + 1]);
				}
			}

			// Loop through the string again to see if the GUI sent a "ponderhit" command
			// If so, turn time control to false 
			// Need to keep track of time, but want to keep pondering until the "ponderhit" or "stop" command has been received (even if time has run out)
			// If "stop" command is received, then search will exit immediately
			// If "ponderhit" is received, then will set the Search.info.timeControl to true
			// If there is no more time, then the search will return immediately; if there is more time then the search will keep thinking
			for (int i = 0; i < inputStringList.Count; i++) {
				if (inputStringList[i] == "ponder") {
					timeControl = false;
				}
			}
			SearchInfo info = new SearchInfo(timeControl, depthLimit,depth, movesToGo, moveTime, timeLeft, increment);
			return info;
		}

		// Starts the search 
        public static void searchWorker_StartSearch(object sender, DoWorkEventArgs e) {
            
			// Starts the search
			// Passes the do work object "e" to the search
			// When the search is terminated, "e.Cancel" will be set to true
	        Search.initSearch(info, position, e);

        }

		// Prints out the results when the search has completed (or been stopped)
	    public static void searchWorker_SearchCompleted(object sender, RunWorkerCompletedEventArgs e) {
			
			// When the search thread has termined and "e.Cancel" is true, the best move will be printed
			// Also the opponent's expected reply will be printed
			Console.Write("bestmove " + getMoveStringFromMoveRepresentation(Search.result.move));
		    
			// Check to see if there is an expected reply move; if so then print it out so that the UCI will set up pondering
			if (Search.PVLine.Count > 1) {
				Console.WriteLine(" ponder " + Search.PVLine[1]);
		    }
			Console.WriteLine("");
		}

		// Prints out information during iterative deepening
	    public static void printInfo(List<string> PVLine, int depth) {
		    Console.WriteLine("");
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

			Console.WriteLine("fh1: " + Search.failHighFirst / (Search.failHighFirst + Search.failHigh) * 100 + "\t\t researches:" + Search.researches);
	    }

        // Extracts the start square from the integer that encodes the move
        private static int getStartSquare(int moveRepresentation) {
            int startSquare = ((moveRepresentation & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
            return startSquare;
        }
        // Extracts the destination square from the integer that encodes the move
        private static int getDestinationSquare(int moveRepresentation) {
            int destinationSquare = ((moveRepresentation & Constants.DESTINATION_SQUARE_MASK) >> Constants.DESTINATION_SQUARE_SHIFT);
            return destinationSquare;
        }
        // Extracts the piece promoted from the integer that encodes the move
        private static int getPiecePromoted(int moveRepresentation) {
            int piecePromoted = (moveRepresentation & Constants.PIECE_PROMOTED_MASK) >> Constants.PIECE_PROMOTED_SHIFT;
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

            //gets the piece array from the board object
	        int[] pieceArray = inputBoard.pieceArray;
			int sideToMove = inputBoard.sideToMove;
			int whiteShortCastleRights = inputBoard.whiteShortCastleRights;
			int whiteLongCastleRights = inputBoard.whiteLongCastleRights;
			int blackShortCastleRights = inputBoard.blackShortCastleRights;
			int blackLongCastleRights = inputBoard.blackLongCastleRights;
			ulong enPassantSquareBitboard = inputBoard.enPassantSquare;
			int fullMoveNumber = inputBoard.fullMoveNumber;
			int fiftyMoveRule = inputBoard.fiftyMoveRule;
			int repetitionOfPosition = inputBoard.getRepetitionNumber();
			string FEN_String = "";


            //creates a new 8x8 array of String and sets it all to spaces
            string[,] chessBoard = new string[8, 8];
            for (int i = 0; i < 64; i++) {
                chessBoard[i / 8, i % 8] = "   ";
            }

            //Goes through each element of the piece array and sets the corresponding element in the chess board array to the proper piece
            //Note that the chess board array goes from A8 to H1
	        for (int i = 0; i < 64; i+= 8) {
		        for (int j = 0; j < 8; j++) {
			        switch (pieceArray[i + j]) {
						case Constants.WHITE_PAWN: chessBoard[7 - (i / 8), (7 - j)] = " P "; break;
						case Constants.WHITE_KNIGHT: chessBoard[7 - (i / 8), (7 - j)] = " N "; break;
						case Constants.WHITE_BISHOP: chessBoard[7 - (i / 8), (7 - j)] = " B "; break;
						case Constants.WHITE_ROOK: chessBoard[7 - (i / 8), (7 - j)] = " R "; break;
						case Constants.WHITE_QUEEN: chessBoard[7 - (i / 8), (7 - j)] = " Q "; break;
						case Constants.WHITE_KING: chessBoard[7 - (i / 8), (7 - j)] = "(K)"; break;
						case Constants.BLACK_PAWN: chessBoard[7 - (i / 8), (7 - j)] = " p "; break;
						case Constants.BLACK_KNIGHT: chessBoard[7 - (i / 8), (7 - j)] = " n "; break;
						case Constants.BLACK_BISHOP: chessBoard[7 - (i / 8), (7 - j)] = " b "; break;
						case Constants.BLACK_ROOK: chessBoard[7 - (i / 8), (7 - j)] = " r "; break;
						case Constants.BLACK_QUEEN: chessBoard[7 - (i / 8), (7 - j)] = " q "; break;
						case Constants.BLACK_KING: chessBoard[7 - (i / 8), (7 - j)] = "(k)"; break;
			        }
		        }
	        }

			//Goes through each element of the chess board array to build the FEN string
	        for (int i = 56; i >= 0; i -= 8) {

				int emptyCounter = 0;
				
				for (int j = 7; j >= 0; j--) {

			        switch (pieceArray[i + j]) {
						case Constants.WHITE_PAWN:
					        FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
					        emptyCounter = 0;
							FEN_String += "P"; 
							break;
						case Constants.WHITE_KNIGHT:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "N"; 
							break;
						case Constants.WHITE_BISHOP:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "B";
							break;
						case Constants.WHITE_ROOK:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "R";
							break;
						case Constants.WHITE_QUEEN:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "Q";
							break;
						case Constants.WHITE_KING:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "K";
							break;
						case Constants.BLACK_PAWN:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "p"; 
							break;
						case Constants.BLACK_KNIGHT:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "n";
							break;
						case Constants.BLACK_BISHOP:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "b";
							break;
						case Constants.BLACK_ROOK:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "r"; 
							break;
						case Constants.BLACK_QUEEN:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "q"; 
							break;
						case Constants.BLACK_KING:
							FEN_String += emptyCounter != 0 ? Convert.ToString(emptyCounter) : "";
							emptyCounter = 0;
							FEN_String += "k"; 
							break;
						case Constants.EMPTY:
							emptyCounter++;
							break;
					}
					//If we have reached the H file, add on the number of empty spaces (if any), along with the slash
			        if (j == 0) {

				        FEN_String += (emptyCounter != 0) ? Convert.ToString(emptyCounter) : "";
				        FEN_String += (i != 0) ? "/" : "";
			        }
		        }     
	        }
			// adds the side to move to the FEN string
	        FEN_String += " ";
			FEN_String += (sideToMove == Constants.WHITE) ? "w" : "b";

			// adds the castling rights to the FEN string
	        if (whiteLongCastleRights == 1 || whiteLongCastleRights == 1 || blackShortCastleRights == 1 || blackLongCastleRights == 1) {
		        FEN_String += " ";
	        }
			FEN_String += (whiteShortCastleRights == 1) ? "K" : "";
	        FEN_String += (whiteLongCastleRights == 1) ? "Q" : "";
	        FEN_String += (blackShortCastleRights == 1) ? "k" : "";
	        FEN_String += (blackLongCastleRights == 1) ? "q" : "";

			// adds the en passant square to the FEN string
	        FEN_String += " ";
	        if (enPassantSquareBitboard != 0) {
		        int enPassantIndex = Constants.bitScan(enPassantSquareBitboard).ElementAt(0);
		        string firstChar = char.ConvertFromUtf32((int) ('h' - (enPassantIndex%8))).ToString();
		        string secondChar = ((enPassantIndex/8) + 1).ToString();
		        FEN_String += (firstChar + secondChar);
	        } else {
		        FEN_String += "-";
	        }

			// adds the fifty move rule clock to the FEN string
	        FEN_String += " " + fiftyMoveRule;

			// adds the fullmove number to the string
	        FEN_String += " " + fullMoveNumber;

            //Goes through the 8x8 chessboard array and prints its contents
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
            String colour = (sideToMove == Constants.WHITE) ? "WHITE" : "BLACK";
            Console.WriteLine("Side to move: " + colour);

            //castle rights
			Console.WriteLine("White Short Castle Rights: " + whiteShortCastleRights);
            Console.WriteLine("White Long Castle Rights: " + whiteLongCastleRights);
            Console.WriteLine("Black Short Castle Rights: " + blackShortCastleRights);
            Console.WriteLine("Black Long Castle Rights: " + blackLongCastleRights);

            //en passant square
            if (enPassantSquareBitboard != 0) {
                int enPassantIndex = Constants.bitScan(enPassantSquareBitboard).ElementAt(0);
                string firstChar = char.ConvertFromUtf32((int)('h' - (enPassantIndex % 8))).ToString();
                string secondChar = ((enPassantIndex / 8) + 1).ToString();
                Console.WriteLine("En Passant Square: " + firstChar + secondChar);
            } else if (enPassantSquareBitboard == 0) {
                Console.WriteLine("En Passant Square: N/A");
            }


            //prints move data (fullmove number, half-moves since last pawn push/capture, repetitions of position)
            //If no move number data or halfmove clock data, then prints N/A
            Console.WriteLine("Fullmove number: " + fullMoveNumber);
			Console.WriteLine("Half moves since last pawn push/capture: " + fiftyMoveRule);
			Console.WriteLine("Repetitions of this position: " + repetitionOfPosition);
	        if (repetitionOfPosition >= 3) {
		        Console.WriteLine("Draw by threefold repetition");
	        }

            Console.WriteLine("Zobrist key: " + inputBoard.zobristKey.ToString("x"));
			Console.WriteLine("Polyglot key: " + OpeningBook.calculatePolyglotKey(inputBoard).ToString("x"));
			Console.WriteLine("FEN String: " + FEN_String);
			Console.WriteLine("");

            inputBoard.kingInCheckTest(inputBoard.sideToMove);
        }
    }

	public struct SearchInfo {
		internal bool timeControl;
		internal bool depthLimit;
		internal int depth;
		internal int movesToGo;
		internal int moveTime;

		internal int timeLeft;
		internal int increment;

		public SearchInfo(bool timeControl, bool depthLimit, int depth, int movesToGo, int moveTime, int timeLeft, int increment) {
			this.timeControl = timeControl;
			this.depthLimit = depthLimit;
			this.depth = depth;
			this.movesToGo = movesToGo;
			this.moveTime = moveTime;
			this.timeLeft = timeLeft;
			this.increment = increment;
		}
	}
}
