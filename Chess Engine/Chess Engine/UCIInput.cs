using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {

    public static class UCIInput {

        private static Board position = new Board(Constants.FEN_START);
        private static CancellationTokenSource stopSearchObject = new CancellationTokenSource();
        
        private static CancellationTokenSource stopPerft = new CancellationTokenSource();
        private static Task<int> search = null;


        // Method that continuously accepts user input
        public static bool processGUIMessages(int waitTime) {

            string input = Console.ReadLine();

            if (input.Length > 0) {
                return inputHandler(input);
            } else if (waitTime > 0) {
                Thread.Sleep(waitTime);
            }  
            return true;  
        }

        public static bool inputHandler(string input) {

            // Reads the input into an array, converts the array to a list, and gets the first element
            string[] stringArray = input.Split(' ');
            List<string> stringList = stringArray.ToList();
            string string0 = stringList[0];
            
            if (string0 == "perft") {
                int depth = Convert.ToInt32(stringArray[1]);
                Task perft = Task.Run(() => Test.printPerft(position, depth));
                return true;
            } else if (string0 == "print") {
                Output.drawBoard(position);
            } else if (string0 == "quit") {
                return false;
            } else if (string0 == "uci") {
                UCIInitialize();
            } else if (string0 == "setoption") {
                stringList.RemoveAt(0);
                setOption(stringList);
            } else if (string0 == "isready") {
                isReady();
            } else if (string0 == "ucinewgame") {
                UCINewGame();
            } else if (string0 == "position") {
                stringList.RemoveAt(0);
                setPosition(stringList);
            } else if (string0 == "go") {
                startSearch();
            } else if (string0 == "stop") {
               stopSearch();
            }
            
            return true;
        }

        public static void UCIInitialize() {
            Console.WriteLine("id name Spark v0.343");
            Console.WriteLine("id author John");
            Console.WriteLine("uciok");
        }

        public static void setOption(List<string> inputStringList) {
            // set options
        }

        public static void isReady() {
            Console.WriteLine("readyok");
        }

        public static void UCINewGame() {
            //some shit
        }

        public static void setPosition(List<string> inputStringList) {
            
            // Sets the board to the starting position
            if (inputStringList[0] == "startpos") {
                inputStringList.RemoveAt(0);
                position = new Board(Constants.FEN_START);
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
                    if (i != index - 1) {
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
                        pseudoLegalMoveList = position.generateListOfAlmostLegalMoves(); 
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

        public static void startSearch() {
            UCIInput.stopSearchObject = new CancellationTokenSource();
            search = Task.Run(() => Search.alphaBetaRoot(position, UCIInput.stopSearchObject.Token));
            Console.WriteLine("bestmove " + getMoveStringFromMoveRepresentation(search.Result));
        }

        public static void stopSearch() {
            UCIInput.stopSearchObject.Cancel();
            Console.WriteLine("bestmove " + getMoveStringFromMoveRepresentation(search.Result));
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
        //Extracts the piece promoted from the integer that encodes the move
        private static int getPiecePromoted(int moveRepresentation) {
            int piecePromoted = (moveRepresentation & Constants.PIECE_PROMOTED_MASK) >> 24;
            return piecePromoted;
        }
        
        //prints out a move string from a move representation uint
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

    }
}
