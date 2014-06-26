using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Program {
        
        static void Main(string[] args) {





            Console.WriteLine(Constants.findFirstSet(585748335649226752));


            //Creates a new engine object and calls its run method
            Engine chessEngine = new Engine();
            chessEngine.run();
        }
    }
}
