using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zobrist = System.UInt64;
using Score = System.Int32;
using Move = System.Int32;

namespace Chess_Engine {
	public class TTable {

		internal TTEntry[] hashTable;
		internal TTEntry[] PVTable;
		internal int[] depthFrequency;

		// Constructor
		public TTable() {
			this.hashTable = new TTEntry[Constants.TT_SIZE + Constants.CLUSTER_SIZE];
			this.PVTable = new TTEntry[Constants.PV_TT_SIZE];
			this.depthFrequency = new int[2 * Constants.MAX_DEPTH];
		}

		// Method that stores an entry in the hash table
		public void storeTTable(Zobrist key, TTEntry entry) {
			int index = (int)(key % Constants.TT_SIZE);

			// If an entry in the cluster has the same hash key, then replace
			for (int i = index; i < index + Constants.CLUSTER_SIZE; i++) {
				if (this.hashTable[i].key == key) {
					this.updateDepthFrequency(i, entry);
					this.hashTable[i] = entry;
					return;
				}
			}
			// If there is an empty spot in the cluster, then store it there
			for (int i = index; i < index + Constants.CLUSTER_SIZE; i++) {
				if (this.hashTable[i].key == 0) {
					this.updateDepthFrequency(i, entry);
					this.hashTable[i] = entry;
					return;
				}
			}
			// If all entries full, then replace the entry with the lowest depth
			int shallowestDepth = Constants.INFINITE;
			int indexOfShallowestEntry = 0;
			
			for (int i = index; i < index + Constants.CLUSTER_SIZE; i++) {
				if (this.hashTable[i].depth < shallowestDepth) {
					shallowestDepth = this.hashTable[i].depth;
					indexOfShallowestEntry = i;
				}
			}
			this.updateDepthFrequency(indexOfShallowestEntry, entry);
			this.hashTable[indexOfShallowestEntry] = entry;
		}

		// Method that retrieves an entry from the hash table
		// Loops over the cluster and returns an entry if the key matches
		public TTEntry probeTTable(Zobrist key) {

			Debug.Assert(key != 0);
			int index = (int)(key % Constants.TT_SIZE);

			for (int i = index; i < index + Constants.CLUSTER_SIZE; i ++) {
				if (this.hashTable[i].key == key) {
					return this.hashTable[i];
				}	
			}
			return Constants.EMPTY_ENTRY;	
		}
		
		// Updates the depth frequency
		private void updateDepthFrequency(int index, TTEntry entry) {
			
			// If old entry is not empty, then decrement the frequency of the depth of the old entry
			TTEntry oldEntry = this.hashTable[index];
			if (oldEntry.key != 0) {
				this.depthFrequency[oldEntry.depth + Constants.MAX_DEPTH]--;
			}
			// Increment the frequency of the depth of the new entry
			if (entry.depth > 0) {
				this.depthFrequency[entry.depth + Constants.MAX_DEPTH]++;
			} else if (entry.depth <= 0) {
				this.depthFrequency[entry.depth + Constants.MAX_DEPTH]++;
			}
			
		}

		public void printDepthFrequency() {
			for (int i=0; i < this.depthFrequency.Count(); i++) {
				Console.WriteLine(i - Constants.MAX_DEPTH + ":\t" + this.depthFrequency[i]);
			}
		}


		// Method that stores an entry in the PV table
		public void storePVTTable(Zobrist key, TTEntry entry) {
			int index = (int) (key%Constants.PV_TT_SIZE);
			this.PVTable[index] = entry;
		}
		// Method that retrieves an entry from the PV table
		public TTEntry probePVTTable(Zobrist key) {
			int index = (int) (key%Constants.PV_TT_SIZE);
			return this.PVTable[index];
		}

		// Method that returns an array of integers containing the principal variation from the PV table
		public List<int> getPVLine(Board inputBoard, int maxDepth) {

			// Later: don't actually have to make the move, can just calculate the new key and look up the table entry


            Board cloneBoard = new Board(inputBoard);
			List<int> PVLine = new List<int>();
			int depth = 1;

			while (true) {
				TTEntry PVNode = this.probePVTTable(cloneBoard.zobristKey);
				int move = PVNode.move & ~Constants.MOVE_SCORE_MASK;
				
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
					pseudoLegalMoveList = cloneBoard.moveGenerator(Constants.ALL_MOVES);
				} else {
					pseudoLegalMoveList = cloneBoard.moveGenerator(Constants.ALL_MOVES);
				}
				for (int i = 0; i < pseudoLegalMoveList.Length; i++) {
					if (move == (pseudoLegalMoveList[i] & ~Constants.MOVE_SCORE_MASK)) {
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
				PVLine.Add(move);
			}
			return PVLine;
		}
	}


	// Entry that is stored in the transposition table
	public struct TTEntry {

		internal Zobrist key;
		internal int flag;
		internal int depth;
		internal Score evaluationScore;
		internal Move move;

		public TTEntry(Zobrist key, int flag, int depth, Score evaluationScore, Move move = 0) {
			this.key = key;
			this.flag = flag;
			this.depth = depth;
			this.evaluationScore = evaluationScore;
			this.move = move;
		}
	}
}
