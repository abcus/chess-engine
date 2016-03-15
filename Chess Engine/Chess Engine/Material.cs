using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    internal sealed class MaterialEntry {

        internal UInt64 key;
        internal Int16 value;

    }

    internal sealed class MaterialTable {
        
        internal const Int32 midgameLimit = 15581;
        internal const Int32 endgameLimit = 3998;

        internal const Int32 redundantQueenPenalty = 320;
        internal const Int32 redundantRookPenalty = 554;

        internal static readonly int[] noPawnScaleFactor = {6, 12, 32, 0};

        internal static readonly int[] linearCoefficients = {1617, -162, -1172, -190, 105, 26};

        internal static readonly int[,] quadraticCoefficientsSameColor = {
            {7, 7, 7, 7, 7, 7}, 
            {39, 2, 7, 7, 7, 7},
            {35, 271, -4, 7, 7, 7}, 
            {7, 25, 4, 7, 7, 7}, 
            {-27, -2, 46, 100, 56, 7}, 
            {58, 29, 83, 148, -3, -25}
        };

        internal static readonly int[,] quadraticCoefficientsOppositeColor = {
            {41, 41, 41, 41, 41, 41},
            {37, 41, 41, 41, 41, 41},
            {10, 62, 41, 41, 41, 41},
            {57, 64, 39, 41, 41, 41},
            {50, 40, 23, -22, 41, 41},
            {106, 101, 3, 151, 171, 41}
        };

        private readonly MaterialEntry[] entries = new MaterialEntry[Constants.MaterialTableSize];
        private readonly int[][] pieceCount = new int[2][];

        // Constructor
        internal MaterialTable() {
            pieceCount[0] = new int[6];
            pieceCount[1] = new int[6];

            for (int i = 0; i < Constants.MaterialTableSize; i++) {
                entries[i] = new MaterialEntry();
            }
        }
    }
}
