using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zobrist = System.UInt64;
using Score = System.UInt32;
using Move = System.UInt32;

namespace Chess_Engine {
	class TTable {

		internal TTEntry[] hashTable = new TTEntry[Constants.TT_SIZE];

		// Constructor
		public TTable() {
			
		}

		// Method that stores an entry in the table
		public void storeTTable(Zobrist key, TTEntry entry) {
			int index = keyToIndex(key);
			hashTable[index] = entry;
		}

		// Method that retrieves an entry from the table
		public TTEntry probeTTable(Zobrist key) {
			int index = keyToIndex(key);
			return hashTable[index];
		}

		// Method that converts the zobrist key into a table index
		private int keyToIndex(Zobrist key) {
			return (int) (key%Constants.TT_SIZE);
		}
		

	}


	// Entry that is stored in the transposition table
	public struct TTEntry {
		internal Zobrist key;
		internal int flag;
		internal int depth;
		internal Score evaluationScore;
		internal Move move;
	}
}
