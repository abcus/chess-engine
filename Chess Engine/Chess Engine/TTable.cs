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
