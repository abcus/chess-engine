using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    internal static class EvaluationWeights {
        internal const Int32 MOBILITY = 0, PASSED_PAWNS = 1, SPACE = 2, KING_DANGER_US = 3, KING_DANGER_THEM = 4;
    }
}
