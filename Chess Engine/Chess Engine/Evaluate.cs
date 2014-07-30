using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    public static class Evaluate {

        private const int PAWN_VALUE = 100;
        private const int KNIGHT_VALUE = 350;
        private const int BISHOP_VALUE = 350;
        private const int ROOK_VALUE = 525;
        private const int QUEEN_VALUE = 1000;
        private const int KING_VALUE = 20000;

        private static int evaluationValue;

        public static int evaluationFunction() {
            
            return evaluationValue;
        }

    }
}
