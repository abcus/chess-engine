using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Program {
        
        static void Main(string[] args) {

            Console.BufferHeight = 2000 ;
            Constants.initializeConstants();


            Test.printOccupancyVariationAndMove(Constants.D4, "Last50", "Bishop");
            //Test.printOccupancyVariation(Constants.D4, "Last50", "Bishop");
            //Test.printMoves(Constants.D4, "Last50", "Bishop");


            //Creates a new engine object and calls its run method

            //Engine chessEngine = new Engine();
            //chessEngine.run(); 
        }

    }

}

