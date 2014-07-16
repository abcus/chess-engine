using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    class Move {

        //METHODS THAT ENCODE MOVES AND BOARD INSTANCE VARIABLES INTO U32-INT------------------------------------------

        //Takes information on piece moved, start square, destination square, type of move, and piece captured
        //Creates a 32-bit unsigned integer representing this information
        //bits 0-3 store the piece moved, 4-9 stores start square, 10-15 stores destination square, 16-19 stores move type, 20-23 stores piece captured
        public static uint moveEncoder(int pieceMoved, int startSquare, int destinationSquare, int flag, int pieceCaptured) {
            int moveRepresentation = 0x0;

            moveRepresentation |= pieceMoved;
            moveRepresentation |= startSquare << 4;
            moveRepresentation |= destinationSquare << 10;
            moveRepresentation |= flag << 16;
            
            //If no piece is captured, then we set the bits corresponding to that variable to 15 (the maximum value for 4 bits)
            if (pieceCaptured == 0) {
                moveRepresentation |= 15 << 20;
            } else if (pieceCaptured != 0) {
                moveRepresentation |= pieceCaptured << 20; 
            }
            return (uint)moveRepresentation;
        } 
    }
}
