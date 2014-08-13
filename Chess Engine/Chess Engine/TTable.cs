using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zobrist = System.UInt64;
using Score = System.Int32;
using Move = System.Int32;

namespace Chess_Engine {
	public class TTable {

		internal TTEntry[] hashTable = new TTEntry[Constants.TT_SIZE];

		// Constructor
		public TTable() {
			
		}

		// Method that stores an entry in the table
		public void storeTTable(Zobrist key, TTEntry entry) {
			int index = (int)(key % Constants.TT_SIZE);
			hashTable[index] = entry;
		}

		// Method that retrieves an entry from the table
		public TTEntry probeTTable(Zobrist key) {
			int index = (int)(key % Constants.TT_SIZE);
			return hashTable[index];
		}

		// Method that returns an array of integers containing the principal variation
		public List<string> getPVLine(Board inputBoard, int maxDepth) {

			Board cloneBoard = new Board(inputBoard);
			List<string> PVLine = new List<string>();
			int depth = 1;

			while (true) {
				TTEntry PVNode = this.probeTTable(cloneBoard.zobristKey);
				int move = PVNode.move;
				
				// If we have reached max depth, then break out of the array
				if (depth++ > maxDepth) {
					break;
				}
				// If no move found, then break out of the array
				if (move == 0) {
					break;
				}
				// If move is not in the pseudo-legal move list, then break out of the array
				// This ensures that an impossible move is not printed
				bool inMoveList = false;
				int[] pseudoLegalMoveList;

				if (inputBoard.isInCheck() == false) {
					pseudoLegalMoveList = cloneBoard.generateListOfAlmostLegalMoves();
				} else {
					pseudoLegalMoveList = cloneBoard.generateListOfAlmostLegalMoves();
				}
				for (int i = 0; i < pseudoLegalMoveList.Length; i++) {
					if (move == pseudoLegalMoveList[i]) {
						inMoveList = true;
						break;
					}
				}
				if (inMoveList == false) {
					break;
				}

				cloneBoard.makeMove(move);
				if (cloneBoard.isMoveLegal(cloneBoard.sideToMove ^ 1) == false) {
					break;
				}
				PVLine.Add(UCI_IO.getMoveStringFromMoveRepresentation(move));
			}
			return PVLine;
		}
	}


	// Entry that is stored in the transposition table
	public struct TTEntry {

		public TTEntry(Zobrist key, int flag, int depth, Score evaluationScore, Move move = 0) {
			this.key = key;
			this.flag = flag;
			this.depth = depth;
			this.evaluationScore = evaluationScore;
			this.move = move;
		}

		internal Zobrist key;
		internal int flag;
		internal int depth;
		internal Score evaluationScore;
		internal Move move;
	}
}
