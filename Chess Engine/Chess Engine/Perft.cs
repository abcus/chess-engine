using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitboard = System.UInt64;
using Zobrist = System.UInt64;
using Score = System.UInt32;
using Move = System.UInt32;

namespace Chess_Engine {

	public class Perft {

		private static PerftTTEntry[] perftTT = new PerftTTEntry[Constants.TT_SIZE];

		// Calls the perft method to determine the number of nodes at a given depth
		public static int perftInit(Board inputBoard, int depth) {
			
			// Creates a new transposition table before perft is called to eliminate collisions
			perftTT = new PerftTTEntry[Constants.TT_SIZE];
			return perft(inputBoard, depth);
		}

		// Returns the number of nodes at a given depth
		public static int perft(Board inputBoard, int depth) {

			int nodes = 0;

			// If the depth is 1
			if (depth == 1) {

				// Looks up the result in the transposition table
				PerftTTEntry entry = perftTT[inputBoard.zobristKey % Constants.TT_SIZE];
				if (entry.key == inputBoard.zobristKey && entry.depth == 1) {
					return entry.nodeCount;
				}

				// If the node is not found in the TTable, then get a list of almost legal moves
				int[] pseudoLegalMoveList = null;
				if (inputBoard.isInCheck() == false) {
					pseudoLegalMoveList = inputBoard.phasedMoveGenerator(Constants.PERFT_ALL_MOVES);
				} else {
					pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
				}

				int numberOfLegalMovesFromList = 0;
				int index = 0;

				stateVariables restoreData = new stateVariables(inputBoard);

				// Loop through all the almost legal moves (testing for legality if it is a king move/en passant)
				// If the move is legal, then increment the move count by 1
				while (pseudoLegalMoveList[index] != 0) {
					int move = pseudoLegalMoveList[index];
					int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
					int pieceMoved = (inputBoard.pieceArray[startSquare]);
					int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
					int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

					if (flag == Constants.EN_PASSANT_CAPTURE || pieceMoved == Constants.WHITE_KING || pieceMoved == Constants.BLACK_KING) {
						inputBoard.makeMove(move);
						if (inputBoard.isMoveLegal(sideToMove) == true) {
							numberOfLegalMovesFromList++;
						}
						inputBoard.unmakeMove(move, restoreData);
						index++;
					} else {
						numberOfLegalMovesFromList++;
						index++;
					}
				}

				// Stores the result in the transposition table and returns the result
				PerftTTEntry newEntry = new PerftTTEntry(inputBoard.zobristKey, 1, numberOfLegalMovesFromList);
				perftTT[inputBoard.zobristKey % Constants.TT_SIZE] = newEntry;
				return numberOfLegalMovesFromList;
			} 
			// If depth is greater than 1
			else {

				// Looks up the result in the transposition table
				PerftTTEntry entry = perftTT[inputBoard.zobristKey % Constants.TT_SIZE];
				if (entry.key == inputBoard.zobristKey && entry.depth == depth) {
					return entry.nodeCount;
				}

				// If the result is not found in the transposition table, then get a list of almost legal moves
				int[] pseudoLegalMoveList = null;
				if (inputBoard.isInCheck() == false) {
					pseudoLegalMoveList = inputBoard.phasedMoveGenerator(Constants.PERFT_ALL_MOVES);
				} else {
					pseudoLegalMoveList = inputBoard.checkEvasionGenerator();
				}

				int index = 0;
				stateVariables restoreData = new stateVariables(inputBoard);

				// Loop through and make all the almost legal moves (testing for legality if it is a king move/en passant)
				// If the move is legal, then call perft at depth - 1
				while (pseudoLegalMoveList[index] != 0) {
					int move = pseudoLegalMoveList[index];
					int startSquare = ((move & Constants.START_SQUARE_MASK) >> Constants.START_SQUARE_SHIFT);
					int pieceMoved = (inputBoard.pieceArray[startSquare]);
					int sideToMove = (pieceMoved <= Constants.WHITE_KING) ? Constants.WHITE : Constants.BLACK;
					int flag = ((move & Constants.FLAG_MASK) >> Constants.FLAG_SHIFT);

					inputBoard.makeMove(move);

					if (flag == Constants.EN_PASSANT_CAPTURE || pieceMoved == Constants.WHITE_KING || pieceMoved == Constants.BLACK_KING) {
						if (inputBoard.isMoveLegal(sideToMove) == true) {
							nodes += perft(inputBoard, depth - 1);
						}
					} else {
						nodes += perft(inputBoard, depth - 1);
					}
					inputBoard.unmakeMove(move, restoreData);
					index++;
				}
				// Stores the result in the transposition table and return the result
				PerftTTEntry newEntry = new PerftTTEntry(inputBoard.zobristKey, depth, nodes);
				perftTT[inputBoard.zobristKey % Constants.TT_SIZE] = newEntry;
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
			int numberOfNodes = Perft.perftInit(inputBoard, depth);
			string numberOfNodesString = numberOfNodes.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";

			Console.WriteLine("Number Of Nodes: \t\t" + numberOfNodes.ToString(numberOfNodesString));
			Console.WriteLine("Time: \t\t\t\t" + s.Elapsed);
			long nodesPerSecond = (numberOfNodes) / (s.ElapsedMilliseconds) * 1000;
			string nodesPerSecondString = nodesPerSecond.ToString().IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator) >= 0 ? "#,##0.00" : "#,##0";

			Console.WriteLine("Nodes per second: \t\t" + nodesPerSecond.ToString(nodesPerSecondString));
		}

		public static void perftSuite1(Board inputBoard) {
			inputBoard.FENToBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
			int nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("119,060,324 (actual value)");
			Console.WriteLine("Difference: " + (119060324 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");
			nodes = Perft.perftInit(inputBoard, 5);
			Console.WriteLine(nodes);
			Console.WriteLine("193,690,690 (actual value)");
			Console.WriteLine("Difference: " + (193690690 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -");
			nodes = Perft.perftInit(inputBoard, 7);
			Console.WriteLine(nodes);
			Console.WriteLine("178,633,661 (actual value)");
			Console.WriteLine("Difference: " + (178633661 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
			nodes = Perft.perftInit(inputBoard, 5);
			Console.WriteLine(nodes);
			Console.WriteLine("15,833,292 (actual value)");
			Console.WriteLine("Difference: " + (15833292 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1");
			nodes = Perft.perftInit(inputBoard, 5);
			Console.WriteLine(nodes);
			Console.WriteLine("15,833,292 (actual value)");
			Console.WriteLine("Difference: " + (15833292 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6");
			nodes = Perft.perftInit(inputBoard, 3);
			Console.WriteLine(nodes);
			Console.WriteLine("53,392(actual value)");
			Console.WriteLine("Difference: " + (53392 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10");
			nodes = Perft.perftInit(inputBoard, 5);
			Console.WriteLine(nodes);
			Console.WriteLine("164,075,551(actual value)");
			Console.WriteLine("Difference: " + (164075551 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("3k4/3p4/8/K1P4r/8/8/8/8 b - - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("1134888 (actual value)");
			Console.WriteLine("Difference: " + (1134888 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/8/4k3/8/2p5/8/B2P2K1/8 w - - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("1015133 (actual value)");
			Console.WriteLine("Difference: " + (1015133 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("1440467 (actual value)");
			Console.WriteLine("Difference: " + (1440467 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("5k2/8/8/8/8/8/8/4K2R w K - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("661072 (actual value)");
			Console.WriteLine("Difference: " + (661072 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("3k4/8/8/8/8/8/8/R3K3 w Q - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("803711 (actual value)");
			Console.WriteLine("Difference: " + (803711 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("r3k2r/1b4bq/8/8/8/8/7B/R3K2R w KQkq - 0 1");
			nodes = Perft.perftInit(inputBoard, 4);
			Console.WriteLine(nodes);
			Console.WriteLine("1274206 (actual value)");
			Console.WriteLine("Difference: " + (1274206 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("r3k2r/8/3Q4/8/8/5q2/8/R3K2R b KQkq - 0 1");
			nodes = Perft.perftInit(inputBoard, 4);
			Console.WriteLine(nodes);
			Console.WriteLine("1720476 (actual value)");
			Console.WriteLine("Difference: " + (1720476 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("2K2r2/4P3/8/8/8/8/8/3k4 w - - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("3821001 (actual value)");
			Console.WriteLine("Difference: " + (3821001 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/8/1P2K3/8/2n5/1q6/8/5k2 b - - 0 1");
			nodes = Perft.perftInit(inputBoard, 5);
			Console.WriteLine(nodes);
			Console.WriteLine("1004658 (actual value)");
			Console.WriteLine("Difference: " + (1004658 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("4k3/1P6/8/8/8/8/K7/8 w - - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("217342 (actual value)");
			Console.WriteLine("Difference: " + (217342 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/P1k5/K7/8/8/8/8/8 w - - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("92683 (actual value)");
			Console.WriteLine("Difference: " + (92683 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("K1k5/8/P7/8/8/8/8/8 w - - 0 1");
			nodes = Perft.perftInit(inputBoard, 6);
			Console.WriteLine(nodes);
			Console.WriteLine("2217 (actual value)");
			Console.WriteLine("Difference: " + (2217 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/k1P5/8/1K6/8/8/8/8 w - - 0 1");
			nodes = Perft.perftInit(inputBoard, 7);
			Console.WriteLine(nodes);
			Console.WriteLine("567584 (actual value)");
			Console.WriteLine("Difference: " + (567584 - nodes));
			Console.WriteLine("");

			inputBoard.FENToBoard("8/8/2k5/5q2/5n2/8/5K2/8 b - - 0 1");
			nodes = Perft.perftInit(inputBoard, 4);
			Console.WriteLine(nodes);
			Console.WriteLine("23527 (actual value)");
			Console.WriteLine("Difference: " + (23527 - nodes));
			Console.WriteLine("");
		}

		public static void perftSuite2(Board inputBoard) {

			//Reads the perft suite into an array one line at a time
			string[] fileInput = System.IO.File.ReadAllLines(@"perftsuite.txt");

			//Splits each line into FEN, and depth 1-6
			string[][] delimitedFileInput = new string[fileInput.Length][];

			for (int i = 0; i < fileInput.Length; i++) {
				string temp = fileInput[i];
				string[] position = temp.Split('\t');
				delimitedFileInput[i] = position;
			}

			Console.WriteLine("Depth 6 Perft Perft:");
			Console.WriteLine("┌───────────────────────────────────────────────────────────┬──────────────┬──────────────┬───────────────┐");
			Console.WriteLine("{0,-60}{1,-15}{2,-15}{3,-30}", "│FEN STRING:", "│EXPECTED:", "│ACTUAL:", "│EXP - ACT:     │");

			Stopwatch s = Stopwatch.StartNew();
			for (int j = 0; j < fileInput.Length; j++) {
				inputBoard.FENToBoard(delimitedFileInput[j][0]);
				int perftDepth6ExpectedResult = Convert.ToInt32(delimitedFileInput[j][6]);
				int perftDepth6CalculatedResult = perftInit(inputBoard, 6);

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

	// Entry that is stored in the transposition table for use during perft
	public struct PerftTTEntry {

		internal Zobrist key;
		internal int depth;
		internal int nodeCount;

		public PerftTTEntry(Zobrist key, int depth, int nodeCount) {
			this.key = key;
			this.depth = depth;
			this.nodeCount = nodeCount;
		}
	}
}
