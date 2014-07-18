﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    public sealed class Engine {
        
        public void run() {
            Constants.initializeConstants();

	        //Test.perftSuite();
			
			//string test = "8/8/1k6/2b5/2pP4/8/5K2/8 b - d3 0 1";
	        Board gameBoard = new Board(Constants.FEN_START);
	        InputOutput.drawBoard(gameBoard);
	        Test.kingInCheckTest(gameBoard, gameBoard.getSideToMove());

            //Test.perftDivide(6, gameBoard);

            Stopwatch s = Stopwatch.StartNew();
            int numberOfNodes = Test.perft(6, gameBoard);
            Console.WriteLine(numberOfNodes);
	        Console.WriteLine(s.Elapsed);
            Console.WriteLine("Nodes per second:" + (numberOfNodes)/(s.ElapsedMilliseconds/1000));

        }
    }
}
  
         