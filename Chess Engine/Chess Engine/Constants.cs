using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    internal abstract class Constants {

        //File constants for masking
        public const long FILE_A = 72340172838076673L;
        public const long FILE_B = 144680345676153346L;
        public const long FILE_C = 289360691352306692L;
        public const long FILE_D = 578721382704613384L;
        public const long FILE_E = 1157442765409226768L;
        public const long FILE_F = 2314885530818453536L;
        public const long FILE_G = 4629771061636907072L;
        public const long FILE_H = -9187201950435737472L;

        //Rank constants for masking
        public const long RANK_1 = -72057594037927936;
        public const long RANK_2 = 71776119061217280L;
        public const long RANK_3 = 280375465082880L;
        public const long RANK_4 = 1095216660480L;
        public const long RANK_5 = 4278190080L;
        public const long RANK_6 = 16711680L;
        public const long RANK_7 = 65280L;
        public const long RANK_8 = 255L;


        //Board array for debugging purposes
        public static readonly string[] BOARD_ARRAY = {
                "r", "n", "b", "q", "k", "b", "n", "r",
                "p", "p", "p", "p", "p", "p", "p", "p",
                " ", " ", " ", " ", " ", " ", " ", " ",
                " ", " ", " ", " ", " ", " ", " ", " ",
                " ", " ", " ", " ", " ", " ", " ", " ",
                " ", " ", " ", " ", " ", " ", " ", " ",
                "P", "P", "P", "P", "P", "P", "P", "P",
                "R", "N", "B", "Q", "K", "B", "N", "R"
            };

        //FEN for starting position
        public const string FEN_START = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        
        //Enumerated type for types of moves
        public enum MoveType {
            QUIET_MOVE,
            DOUBLE_PAWN_PUSH,
            KINGSIDE_CASTLE,
            QUEENSIDE_CASTLE,
            CAPTURE,
            EN_PESSANT_CAPTURE,
            KNIGHT_PROMOTION,
            BISHOP_PROMITION,
            ROOK_PROMITION,
            QUEEN_PROMOTION,
            KNIGHT_PROMOTION_CAPTURE,
            BISHOP_PROMOTION_CAPTURE,
            ROOK_PROMOTION_CAPTURE,
            QUEEN_PROMOTION_CAPTURE
        }

        //Enumerated type for side to move
        public enum sideToMove {
            WHITE, 
            BLACK
        }

    }
}
