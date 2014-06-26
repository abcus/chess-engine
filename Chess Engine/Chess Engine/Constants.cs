using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    
    internal abstract class Constants {

        //File constants for masking
        public const ulong FILE_A = FILE_H << 7;
        public const ulong FILE_B = FILE_H << 6;
        public const ulong FILE_C = FILE_H << 5;
        public const ulong FILE_D = FILE_H << 4;
        public const ulong FILE_E = FILE_H << 3;
        public const ulong FILE_F = FILE_H << 2;
        public const ulong FILE_G = FILE_H << 1;
        public const ulong FILE_H = 0x8080808080808080UL;

        //Rank constants for masking
        public const ulong RANK_1 = 0xffUL;
        public const ulong RANK_2 = RANK_1 << (8 * 1);
        public const ulong RANK_3 = RANK_1 << (8 * 2);
        public const ulong RANK_4 = RANK_1 << (8 * 3);
        public const ulong RANK_5 = RANK_1 << (8 * 4);
        public const ulong RANK_6 = RANK_1 << (8 * 5);
        public const ulong RANK_7 = RANK_1 << (8 * 6);
        public const ulong RANK_8 = RANK_1 << (8 * 7);

        //Light and dark squares
        public const ulong LIGHT_SQUARES = 0xAA55AA55AA55AA55UL;
        public const ulong DARK_SQUARES = 0x55AA55AA55AA55AAUL;

        //To convert from unsigned long to signed, subtract 18446744073709551616 if the unsigned long is bigger than 9223372036854775807

        //FEN for starting position
        public const string FEN_START = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -";
        
        //Enumerated type for types of moves
        public const int 
            QUIET_MOVE = 1,
            DOUBLE_PAWN_PUSH = 2,
            KINGSIDE_CASTLE = 3,
            QUEENSIDE_CASTLE = 4,
            CAPTURE = 5,
            EN_PASSANT_CAPTURE = 6,
            KNIGHT_PROMOTION = 7,
            BISHOP_PROMITION = 8,
            ROOK_PROMITION = 9,
            QUEEN_PROMOTION = 10,
            KNIGHT_PROMOTION_CAPTURE = 11,
            BISHOP_PROMOTION_CAPTURE = 12,
            ROOK_PROMOTION_CAPTURE = 13,
            QUEEN_PROMOTION_CAPTURE = 14;
        

        //Enumerated type for side to move
        public const int WHITE = 1, BLACK = -1;

        //Enumerated type for squares
        public const int 
            H1 = 0, G1 = 1, F1 = 2, E1 = 3, D1 = 4, C1 = 5, B1 = 6, A1 = 7, 
            H2 = 8, G2 = 9, F2 = 10, E2 = 11, D2 = 12, C2 = 13, B2 = 14, A2 = 15, 
            H3 = 16,G3 = 17, F3 = 18, E3 = 19, D3 = 20, C3 = 21, B3 = 22, A3 = 23, 
            H4 = 24, G4 = 25, F4 = 26, E4 = 27, D4 = 28, C4 = 29, B4 = 30, A4 = 31, 
            H5 = 32, G5 = 33, F5 = 34, E5 = 35, D5 = 36, C5 = 37, B5 = 38, A5 = 39, 
            H6 = 40, G6 = 41, F6 = 42, E6 = 43, D6 = 44, C6 = 45, B6 = 46, A6 = 47, 
            H7 = 48, G7 = 49, F7 = 50, E7 = 51, D7 = 52, C7 = 53, B7 = 54, A7 = 55, 
            H8 = 56, G8 = 57, F8 = 58, E8 = 59, D8 = 60, C8 = 61, B8 = 62, A8 = 63;

        //De Brujin tables
        public static readonly int[] index64 = {
             0, 47,  1, 56, 48, 27,  2, 60,
             57, 49, 41, 37, 28, 16,  3, 61,
             54, 58, 35, 52, 50, 42, 21, 44,
             38, 32, 29, 23, 17, 11,  4, 62,
             46, 55, 26, 59, 40, 36, 15, 53,
             34, 51, 20, 43, 31, 22, 10, 45,
             25, 39, 14, 33, 19, 30,  9, 24,
             13, 18,  8, 12,  7,  6,  5, 63
        };

        //Find first set (index of least significant bit)
        public static int findFirstSet(ulong bitboard) {
            const ulong deBruijn64 = 0x03f79d71b4cb0a89UL;
            if (bitboard == 0) {
                return 0;
            }
            else {
                return index64[((bitboard ^ (bitboard - 1)) * deBruijn64) >> 58];
            }
        }
    }

}
