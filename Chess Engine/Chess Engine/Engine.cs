using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    public class Engine {

        // Constructor
        public Engine() {  
        }

        // Engine's run method
        public void run () {

            Constants.initConstants();
            Constants.initEvalConstants();
	        Constants.initZobrist();

			while (true) {
                if (!UCI_IO.processGUIMessages(50)) {
                    break;
                }
            }
        }
    }
}
  
         