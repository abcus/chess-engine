using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {

    class LegalMoveGenerator {

        private static int ffs(long input) {

            if (input == 0) {
                return 0;
            }
            long mask = 1;
            int index = 0;

            while ((input & mask) == 0) {
                mask = mask << 1;
                index = index + 1;
            }
            return index;
        }

    }
}
