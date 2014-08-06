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

            Constants.init();
            Constants.initEvalConstants();

            Console.WriteLine("Spark v0.343 by John");

            while (true) {
                if (!UCIInput.processGUIMessages(50)) {
                    break;
                }
            }
        }
    }
}
  
         