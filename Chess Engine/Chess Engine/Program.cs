using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Program {
        
        static void Main(string[] args) {
  
            //Creates a new engine object and calls its run method
            Engine chessEngine = new Engine();
            chessEngine.run();
        }
    }
}
