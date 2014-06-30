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
        public const ulong FILE_H = 0x0101010101010101UL;

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
            QUIET_MOVE = 0,
            DOUBLE_PAWN_PUSH = 1,
            KINGSIDE_CASTLE = 2,
            QUEENSIDE_CASTLE = 3,
            CAPTURE = 4,
            EN_PASSANT_CAPTURE = 5,
            KNIGHT_PROMOTION = 6,
            BISHOP_PROMITION = 7,
            ROOK_PROMITION = 8,
            QUEEN_PROMOTION = 9,
            KNIGHT_PROMOTION_CAPTURE = 10,
            BISHOP_PROMOTION_CAPTURE = 11,
            ROOK_PROMOTION_CAPTURE = 12,
            QUEEN_PROMOTION_CAPTURE = 13;
        
        //Enumerated types for pieces
           public const int
               WHITE_PAWN   =  2,
               BLACK_PAWN   =  3,
               WHITE_KNIGHT =  4,
               BLACK_KNIGHT =  5,
               WHITE_ROOK   =  6,
               BLACK_ROOK   =  7,
               WHITE_BISHOP =  8,
               BLACK_BISHOP =  9,
               WHITE_QUEEN  = 10,
               BLACK_QUEEN  = 11,
               WHITE_KING   = 12,
               BLACK_KING = 13;

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

        //King moves from any position
        //Starts at H1 and goes to A8
        public static readonly ulong[] kingMoves = {
            0x0000000000000302UL, 0x0000000000000705UL, 0x0000000000000E0AUL, 0x0000000000001C14UL,
            0x0000000000003828UL, 0x0000000000007050UL, 0x000000000000E0A0UL, 0x000000000000C040UL,
            0x0000000000030203UL, 0x0000000000070507UL, 0x00000000000E0A0EUL, 0x00000000001C141CUL,
            0x0000000000382838UL, 0x0000000000705070UL, 0x0000000000E0A0E0UL, 0x0000000000C040C0UL,
            0x0000000003020300UL, 0x0000000007050700UL, 0x000000000E0A0E00UL, 0x000000001C141C00UL,
            0x0000000038283800UL, 0x0000000070507000UL, 0x00000000E0A0E000UL, 0x00000000C040C000UL,
            0x0000000302030000UL, 0x0000000705070000UL, 0x0000000E0A0E0000UL, 0x0000001C141C0000UL,
            0x0000003828380000UL, 0x0000007050700000UL, 0x000000E0A0E00000UL, 0x000000C040C00000UL,
            0x0000030203000000UL, 0x0000070507000000UL, 0x00000E0A0E000000UL, 0x00001C141C000000UL,
            0x0000382838000000UL, 0x0000705070000000UL, 0x0000E0A0E0000000UL, 0x0000C040C0000000UL,
            0x0003020300000000UL, 0x0007050700000000UL, 0x000E0A0E00000000UL, 0x001C141C00000000UL,
            0x0038283800000000UL, 0x0070507000000000UL, 0x00E0A0E000000000UL, 0x00C040C000000000UL,
            0x0302030000000000UL, 0x0705070000000000UL, 0x0E0A0E0000000000UL, 0x1C141C0000000000UL,
            0x3828380000000000UL, 0x7050700000000000UL, 0xE0A0E00000000000UL, 0xC040C00000000000UL,
            0x0203000000000000UL, 0x0507000000000000UL, 0x0A0E000000000000UL, 0x141C000000000000UL,
            0x2838000000000000UL, 0x5070000000000000UL, 0xA0E0000000000000UL, 0x40C0000000000000UL
        };

        //Knight moves from any position
        //Starts at H1 and goes to A8
        public static readonly ulong[] knightMoves = {
            0x0000000000020400UL, 0x0000000000050800UL, 0x00000000000A1100UL, 0x0000000000142200UL,
            0x0000000000284400UL, 0x0000000000508800UL, 0x0000000000A01000UL, 0x0000000000402000UL,
            0x0000000002040004UL, 0x0000000005080008UL, 0x000000000A110011UL, 0x0000000014220022UL,
            0x0000000028440044UL, 0x0000000050880088UL, 0x00000000A0100010UL, 0x0000000040200020UL,
            0x0000000204000402UL, 0x0000000508000805UL, 0x0000000A1100110AUL, 0x0000001422002214UL,
            0x0000002844004428UL, 0x0000005088008850UL, 0x000000A0100010A0UL, 0x0000004020002040UL,
            0x0000020400040200UL, 0x0000050800080500UL, 0x00000A1100110A00UL, 0x0000142200221400UL,
            0x0000284400442800UL, 0x0000508800885000UL, 0x0000A0100010A000UL, 0x0000402000204000UL,
            0x0002040004020000UL, 0x0005080008050000UL, 0x000A1100110A0000UL, 0x0014220022140000UL,
            0x0028440044280000UL, 0x0050880088500000UL, 0x00A0100010A00000UL, 0x0040200020400000UL,
            0x0204000402000000UL, 0x0508000805000000UL, 0x0A1100110A000000UL, 0x1422002214000000UL,
            0x2844004428000000UL, 0x5088008850000000UL, 0xA0100010A0000000UL, 0x4020002040000000UL,
            0x0400040200000000UL, 0x0800080500000000UL, 0x1100110A00000000UL, 0x2200221400000000UL,
            0x4400442800000000UL, 0x8800885000000000UL, 0x100010A000000000UL, 0x2000204000000000UL,
            0x0004020000000000UL, 0x0008050000000000UL, 0x00110A0000000000UL, 0x0022140000000000UL,
            0x0044280000000000UL, 0x0088500000000000UL, 0x0010A00000000000UL, 0x0020400000000000UL
        };

        //White pawn single moves and move-promitions from any position (for rank 1 and rank 8, no valid single pawn moves)
        //Starts at H1 and goes to A8
        public static readonly ulong[] whiteSinglePawnMovesAndPromotionMoves = {
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000010000UL, 0x0000000000020000UL, 0x0000000000040000UL, 0x0000000000080000UL,
            0x0000000000100000UL, 0x0000000000200000UL, 0x0000000000400000UL, 0x0000000000800000UL,
            0x0000000001000000UL, 0x0000000002000000UL, 0x0000000004000000UL, 0x0000000008000000UL,
            0x0000000010000000UL, 0x0000000020000000UL, 0x0000000040000000UL, 0x0000000080000000UL,
            0x0000000100000000UL, 0x0000000200000000UL, 0x0000000400000000UL, 0x0000000800000000UL,
            0x0000001000000000UL, 0x0000002000000000UL, 0x0000004000000000UL, 0x0000008000000000UL,
            0x0000010000000000UL, 0x0000020000000000UL, 0x0000040000000000UL, 0x0000080000000000UL,
            0x0000100000000000UL, 0x0000200000000000UL, 0x0000400000000000UL, 0x0000800000000000UL,
            0x0001000000000000UL, 0x0002000000000000UL, 0x0004000000000000UL, 0x0008000000000000UL,
            0x0010000000000000UL, 0x0020000000000000UL, 0x0040000000000000UL, 0x0080000000000000UL,
            0x0100000000000000UL, 0x0200000000000000UL, 0x0400000000000000UL, 0x0800000000000000UL,
            0x1000000000000000UL, 0x2000000000000000UL, 0x4000000000000000UL, 0x8000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL
        };

        //Black pawn single moves  and move-promotions from any position (for rank 1 and 8, no valid pawn single moves)
        //Starts at H1 and goes to A8
        public static readonly ulong[] blackSinglePawnMovesAndPromotionMoves = {
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000001UL, 0x0000000000000002UL, 0x0000000000000004UL, 0x0000000000000008UL, 
            0x0000000000000010UL, 0x0000000000000020UL, 0x0000000000000040UL, 0x0000000000000080UL, 
            0x0000000000000100UL, 0x0000000000000200UL, 0x0000000000000400UL, 0x0000000000000800UL,
            0x0000000000001000UL, 0x0000000000002000UL, 0x0000000000004000UL, 0x0000000000008000UL,
            0x0000000000010000UL, 0x0000000000020000UL, 0x0000000000040000UL, 0x0000000000080000UL,
            0x0000000000100000UL, 0x0000000000200000UL, 0x0000000000400000UL, 0x0000000000800000UL,
            0x0000000001000000UL, 0x0000000002000000UL, 0x0000000004000000UL, 0x0000000008000000UL,
            0x0000000010000000UL, 0x0000000020000000UL, 0x0000000040000000UL, 0x0000000080000000UL,
            0x0000000100000000UL, 0x0000000200000000UL, 0x0000000400000000UL, 0x0000000800000000UL,
            0x0000001000000000UL, 0x0000002000000000UL, 0x0000004000000000UL, 0x0000008000000000UL,
            0x0000010000000000UL, 0x0000020000000000UL, 0x0000040000000000UL, 0x0000080000000000UL,
            0x0000100000000000UL, 0x0000200000000000UL, 0x0000400000000000UL, 0x0000800000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL
        };

        //White pawn captures and capture-promotions from any position (for rank 1 and 8, no valid moves)
        //Starts at H1 and goes to A8
        public static readonly ulong[] whiteCapturesAndCapturePromotions = {
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000020000UL, 0x0000000000050000UL, 0x00000000000A0000UL, 0x0000000000140000UL,
            0x0000000000280000UL, 0x0000000000500000UL, 0x0000000000A00000UL, 0x0000000000400000UL,
            0x0000000002000000UL, 0x0000000005000000UL, 0x000000000A000000UL, 0x0000000014000000UL,
            0x0000000028000000UL, 0x0000000050000000UL, 0x00000000A0000000UL, 0x0000000040000000UL,
            0x0000000200000000UL, 0x0000000500000000UL, 0x0000000A00000000UL, 0x0000001400000000UL,
            0x0000002800000000UL, 0x0000005000000000UL, 0x000000A000000000UL, 0x0000004000000000UL,
            0x0000020000000000UL, 0x0000050000000000UL, 0x00000A0000000000UL, 0x0000140000000000UL,
            0x0000280000000000UL, 0x0000500000000000UL, 0x0000A00000000000UL, 0x0000400000000000UL,
            0x0002000000000000UL, 0x0005000000000000UL, 0x000A000000000000UL, 0x0014000000000000UL,
            0x0028000000000000UL, 0x0050000000000000UL, 0x00A0000000000000UL, 0x0040000000000000UL,
            0x0200000000000000UL, 0x0500000000000000UL, 0x0A00000000000000UL, 0x1400000000000000UL,
            0x2800000000000000UL, 0x5000000000000000UL, 0xA000000000000000UL, 0x4000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL
        };

        //Black pawn captures and capture-promotions from any position (for rank 1 and 8, no valid moves)
        //Starts at H1 and goes to A8
        public static readonly ulong[] blackCapturesAndCapturePromotions = {
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000002UL, 0x0000000000000005UL, 0x000000000000000AUL, 0x0000000000000014UL,
            0x0000000000000028UL, 0x0000000000000050UL, 0x00000000000000A0UL, 0x0000000000000040UL,
            0x0000000000000200UL, 0x0000000000000500UL, 0x0000000000000A00UL, 0x0000000000001400UL,
            0x0000000000002800UL, 0x0000000000005000UL, 0x000000000000A000UL, 0x0000000000004000UL,
            0x0000000000020000UL, 0x0000000000050000UL, 0x00000000000A0000UL, 0x0000000000140000UL,
            0x0000000000280000UL, 0x0000000000500000UL, 0x0000000000A00000UL, 0x0000000000400000UL,
            0x0000000002000000UL, 0x0000000005000000UL, 0x000000000A000000UL, 0x0000000014000000UL,
            0x0000000028000000UL, 0x0000000050000000UL, 0x00000000A0000000UL, 0x0000000040000000UL,
            0x0000000200000000UL, 0x0000000500000000UL, 0x0000000A00000000UL, 0x0000001400000000UL,
            0x0000002800000000UL, 0x0000005000000000UL, 0x000000A000000000UL, 0x0000004000000000UL,
            0x0000020000000000UL, 0x0000050000000000UL, 0x00000A0000000000UL, 0x0000140000000000UL,
            0x0000280000000000UL, 0x0000500000000000UL, 0x0000A00000000000UL, 0x0000400000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL,
            0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL
        };

        //Rook occupancy mask
        //Starts at H1 and goes to A8
        public static readonly ulong[] rookOccupancyMask = {
            0x000101010101017EUL, 0x000202020202027CUL, 0x000404040404047AUL, 0x0008080808080876UL,
            0x001010101010106EUL, 0x002020202020205EUL, 0x004040404040403EUL, 0x008080808080807EUL,
            0x0001010101017E00UL, 0x0002020202027C00UL, 0x0004040404047A00UL, 0x0008080808087600UL,
            0x0010101010106E00UL, 0x0020202020205E00UL, 0x0040404040403E00UL, 0x0080808080807E00UL,
            0x00010101017E0100UL, 0x00020202027C0200UL, 0x00040404047A0400UL, 0x0008080808760800UL,
            0x00101010106E1000UL, 0x00202020205E2000UL, 0x00404040403E4000UL, 0x00808080807E8000UL,
            0x000101017E010100UL, 0x000202027C020200UL, 0x000404047A040400UL, 0x0008080876080800UL,
            0x001010106E101000UL, 0x002020205E202000UL, 0x004040403E404000UL, 0x008080807E808000UL,
            0x0001017E01010100UL, 0x0002027C02020200UL, 0x0004047A04040400UL, 0x0008087608080800UL,
            0x0010106E10101000UL, 0x0020205E20202000UL, 0x0040403E40404000UL, 0x0080807E80808000UL,
            0x00017E0101010100UL, 0x00027C0202020200UL, 0x00047A0404040400UL, 0x0008760808080800UL,
            0x00106E1010101000UL, 0x00205E2020202000UL, 0x00403E4040404000UL, 0x00807E8080808000UL,
            0x007E010101010100UL, 0x007C020202020200UL, 0x007A040404040400UL, 0x0076080808080800UL,
            0x006E101010101000UL, 0x005E202020202000UL, 0x003E404040404000UL, 0x007E808080808000UL,
            0x7E01010101010100UL, 0x7C02020202020200UL, 0x7A04040404040400UL, 0x7608080808080800UL,
            0x6E10101010101000UL, 0x5E20202020202000UL, 0x3E40404040404000UL, 0x7E80808080808000UL
        };

        //Bishop occupancy mask
        //Starts at H1 and goes to A8
        public static readonly ulong[] bishopOccupancyMask = {
            0x0040201008040200UL, 0x0000402010080400UL, 0x0000004020100A00UL, 0x0000000040221400UL,
            0x0000000002442800UL, 0x0000000204085000UL, 0x0000020408102000UL, 0x0002040810204000UL,
            0x0020100804020000UL, 0x0040201008040000UL, 0x00004020100A0000UL, 0x0000004022140000UL,
            0x0000000244280000UL, 0x0000020408500000UL, 0x0002040810200000UL, 0x0004081020400000UL,
            0x0010080402000200UL, 0x0020100804000400UL, 0x004020100A000A00UL, 0x0000402214001400UL,
            0x0000024428002800UL, 0x0002040850005000UL, 0x0004081020002000UL, 0x0008102040004000UL,
            0x0008040200020400UL, 0x0010080400040800UL, 0x0020100A000A1000UL, 0x0040221400142200UL,
            0x0002442800284400UL, 0x0004085000500800UL, 0x0008102000201000UL, 0x0010204000402000UL,
            0x0004020002040800UL, 0x0008040004081000UL, 0x00100A000A102000UL, 0x0022140014224000UL,
            0x0044280028440200UL, 0x0008500050080400UL, 0x0010200020100800UL, 0x0020400040201000UL,
            0x0002000204081000UL, 0x0004000408102000UL, 0x000A000A10204000UL, 0x0014001422400000UL,
            0x0028002844020000UL, 0x0050005008040200UL, 0x0020002010080400UL, 0x0040004020100800UL,
            0x0000020408102000UL, 0x0000040810204000UL, 0x00000A1020400000UL, 0x0000142240000000UL,
            0x0000284402000000UL, 0x0000500804020000UL, 0x0000201008040200UL, 0x0000402010080400UL,
            0x0002040810204000UL, 0x0004081020400000UL, 0x000A102040000000UL, 0x0014224000000000UL,
            0x0028440200000000UL, 0x0050080402000000UL, 0x0020100804020000UL, 0x0040201008040200UL
        };

        //Rook magic numbers
        //Starts at H1 and goes to A8
        public static readonly ulong[] rookMagicNumbers = {
             0xA180022080400230UL, 0x0040100040022000UL, 0x0080088020001002UL, 0x0080080280841000UL, 
             0x4200042010460008UL, 0x04800A0003040080UL, 0x0400110082041008UL, 0x008000A041000880UL, 
             0x10138001A080C010UL, 0x0000804008200480UL, 0x00010011012000C0UL, 0x0022004128102200UL, 
             0x000200081201200CUL, 0x202A001048460004UL, 0x0081000100420004UL, 0x4000800380004500UL, 
             0x0000208002904001UL, 0x0090004040026008UL, 0x0208808010002001UL, 0x2002020020704940UL, 
             0x8048010008110005UL, 0x6820808004002200UL, 0x0A80040008023011UL, 0x00B1460000811044UL, 
             0x4204400080008EA0UL, 0xB002400180200184UL, 0x2020200080100380UL, 0x0010080080100080UL, 
             0x2204080080800400UL, 0x0000A40080360080UL, 0x02040604002810B1UL, 0x008C218600004104UL, 
             0x8180004000402000UL, 0x488C402000401001UL, 0x4018A00080801004UL, 0x1230002105001008UL, 
             0x8904800800800400UL, 0x0042000C42003810UL, 0x008408110400B012UL, 0x0018086182000401UL, 
             0x2240088020C28000UL, 0x001001201040C004UL, 0x0A02008010420020UL, 0x0010003009010060UL, 
             0x0004008008008014UL, 0x0080020004008080UL, 0x0282020001008080UL, 0x50000181204A0004UL, 
             0x0102042111804200UL, 0x40002010004001C0UL, 0x0019220045508200UL, 0x020030010060A900UL, 
             0x0008018028040080UL, 0x0088240002008080UL, 0x0010301802830400UL, 0x00332A4081140200UL, 
             0x008080010A601241UL, 0x0001008010400021UL, 0x0004082001007241UL, 0x0211009001200509UL, 
             0x8015001002441801UL, 0x0801000804000603UL, 0x0C0900220024A401UL, 0x0001000200608243UL
        };

        //Bishop magic numbers
        //Starts at H1 and goes to A8
        public static readonly ulong[] bishopMagicNumbers = {
            0x2910054208004104UL, 0x02100630A7020180UL, 0x5822022042000000UL, 0x2CA804A100200020UL, 
            0x0204042200000900UL, 0x2002121024000002UL, 0x80404104202000E8UL, 0x812A020205010840UL, 
            0x8005181184080048UL, 0x1001C20208010101UL, 0x1001080204002100UL, 0x1810080489021800UL, 
            0x0062040420010A00UL, 0x5028043004300020UL, 0xC0080A4402605002UL, 0x08A00A0104220200UL, 
            0x0940000410821212UL, 0x001808024A280210UL, 0x040C0422080A0598UL, 0x4228020082004050UL, 
            0x0200800400E00100UL, 0x020B001230021040UL, 0x00090A0201900C00UL, 0x004940120A0A0108UL,
            0x0020208050A42180UL, 0x001004804B280200UL, 0x2048020024040010UL, 0x0102C04004010200UL, 
            0x020408204C002010UL, 0x02411100020080C1UL, 0x102A008084042100UL, 0x0941030000A09846UL,
            0x0244100800400200UL, 0x4000901010080696UL, 0x0000280404180020UL, 0x0800042008240100UL, 
            0x0220008400088020UL, 0x04020182000904C9UL, 0x0023010400020600UL, 0x0041040020110302UL, 
            0x0412101004020818UL, 0x8022080A09404208UL, 0x1401210240484800UL, 0x0022244208010080UL,
            0x1105040104000210UL, 0x2040088800C40081UL, 0x8184810252000400UL, 0x4004610041002200UL,
            0x040201A444400810UL, 0x4611010802020008UL, 0x80000B0401040402UL, 0x0020004821880A00UL, 
            0x8200002022440100UL, 0x0009431801010068UL, 0x1040C20806108040UL, 0x0804901403022A40UL, 
            0x2400202602104000UL, 0x0208520209440204UL, 0x040C000022013020UL, 0x2000104000420600UL, 
            0x0400000260142410UL, 0x0800633408100500UL, 0x00002404080A1410UL, 0x0138200122002900UL  
        };

        //Rook shift number
        //Starts at H1 and goes to A8
        public static readonly int[] rookMagicShiftNumber = {
            52, 53, 53, 53, 53, 53, 53, 52,
            53, 54, 54, 54, 54, 54, 54, 53,
            53, 54, 54, 54, 54, 54, 54, 53,
            53, 54, 54, 54, 54, 54, 54, 53,
            53, 54, 54, 54, 54, 54, 54, 53,
            53, 54, 54, 54, 54, 54, 54, 53,
            53, 54, 54, 54, 54, 54, 54, 53,
            52, 53, 53, 53, 53, 53, 53, 52
        };

        //Bishop shift number
        //Starts at H1 and goes to A8
        public static readonly int[] bishopMagicShiftNumber = {
            58, 59, 59, 59, 59, 59, 59, 58,
            59, 59, 59, 59, 59, 59, 59, 59,
            59, 59, 57, 57, 57, 57, 59, 59,
            59, 59, 57, 55, 55, 57, 59, 59,
            59, 59, 57, 55, 55, 57, 59, 59,
            59, 59, 57, 57, 57, 57, 59, 59,
            59, 59, 59, 59, 59, 59, 59, 59,
            58, 59, 59, 59, 59, 59, 59, 58
        };

        //All rook occupancy variations for all squares (does not use magic indexing)
        //Starts at H1 and goes to A8
        public static ulong [][] rookOccupancyVariations = new ulong [64][];

        //All bishop occupancy variations for all squares (does not use magic indexing)
        //Starts at H1 and goes to A8
        public static ulong[][] bishopOccupancyVariations = new ulong[64][];

        //Rook move array for all occupancy variations for all squares (does not use magic indexing)
        //Starts at H1 and goes to A8
        public static ulong[][] rookMoves = new ulong[64][];

        //Bishop move array for all occupancy variations for all squares (does not use magic indexing)
        //Starts at H1 and goes to A8
        public static ulong[][] bishopMoves = new ulong[64][];


        //METHODS-------------------------------------------------------------------------------------

        //INITIALIZATION METHODS-----------------------------------------------------------------------

        //Populates the occupancy variation arrays and piece move arrays
        public static void initializeConstants() {
            populateRookOccupancyVariation(rookOccupancyVariations);
            populateBishopOccupancyVariation(bishopOccupancyVariations);
            populateRookMove(rookMoves);
            populateBishopMove(bishopMoves);
        }

        //Populates the rook occupancy variation array for every square (not using magic indexing)
        public static void populateRookOccupancyVariation(ulong[][] rookOccupancyVariation) {

            //loops over every square
            for (int i = 0; i <= 63; i++) {

                List<ulong> permutationsForParticularSquare = new List<ulong>();

                //Takes in the rook occupancy mask for that particular square and generates all permutations/variations of the "1" bits, and stores it in array list
                Test.generateBinaryPermutations(rookOccupancyMask[i], bitScan(rookOccupancyMask[i]), permutationsForParticularSquare);

                //Sorts the array list of permutations/variations, converts it to an array, and puts in the rook occupancy variations array
                permutationsForParticularSquare.Sort();
                ulong[] permutationForParticularSquareArray = permutationsForParticularSquare.ToArray();
                rookOccupancyVariation[i] = permutationForParticularSquareArray;
            }
        }

        //Populates the bishop occupancy variation array for every square (not using magic indexing)
        public static void populateBishopOccupancyVariation(ulong[][] bishopOccupancyVariation) {

            for (int i = 0; i <= 63; i++) {
                
                List<ulong> permutationsForParticularSquare = new List<ulong>();

                Test.generateBinaryPermutations(bishopOccupancyMask[i], bitScan(bishopOccupancyMask[i]), permutationsForParticularSquare);

                permutationsForParticularSquare.Sort();
                ulong[] permutationsForParticularSquareArray = permutationsForParticularSquare.ToArray();
                bishopOccupancyVariation[i] = permutationsForParticularSquareArray;

            }
        }

        //Populates the rook move array for every square (not using magic indexing)
        public static void populateRookMove(ulong[][] rookMovesArray) {

            //loops over every square in the rook occupancy variation array
            for (int i = 0; i <= 63; i++) {

                rookMovesArray[i] = new ulong[rookOccupancyVariations[i].Length];

                for (int j = 0; j < rookOccupancyVariations[i].Length; j++) {

                    ulong rookMove = 0x0UL;
                    ulong square = 0x1UL << i;

                    //If (index/8 <= 6), shift up 7 - (index)/8 times (if in 7th rank or lower, shift up 8-rank times)
                    //If a "1" is encountered in the occupancy variation, break
                    if (i/8 <= 6) {
                        for (int k = 0; k <= 7 - i/8; k++) {
                           
                            rookMove |= square << (8 * k);
                            
                            //If a "1" is encountered in the corresponding occupancy variation, then break
                            if ((rookOccupancyVariations[i][j] & square << (8 * k)) == square << (8 * k)) {
                                break;
                            }
                        }
                    }
                    //If (index/8 >= 1) shift down 0 + (index/8) times (if in 2nd rank or higher, shift down rank - 1 times)
                    //If a "1" is encountered in the occupancy variation, break
                    if (i/8 >= 1) {
                        for (int k = 0; k <= i/8; k++) {
                            rookMove |= square >> (8*k);
                            if ((rookOccupancyVariations[i][j] & square >> (8 * k)) == square >> (8 * k)) {
                                break;
                            }
                        }
                    }
                    //If (index %8 <= 6) shift left 7 - (index % 8) times (if in B file or higher, shift left file - 1 times)
                    //If a "1" is encountered in the occupancy variation, break
                    if (i%8 <= 6) {
                        for (int k = 0; k <= 7 - (i%8); k++) {
                            rookMove |= square << k;
                            if ((rookOccupancyVariations[i][j] & square << k) == square << k) {
                                break;
                            }
                        }
                    }
                    //If (index % 8 >= 1) shift right (index % 8) times
                    if (i%8 >= 1) {
                        for (int k = 0; k <= (i%8); k++) {
                            rookMove |= square >> k;
                            if ((rookOccupancyVariations[i][j] & square >> k) == square >> k) {
                                break;
                            }
                        }
                    }
                    rookMove &= ~square;
                    rookMovesArray[i][j] = rookMove;
                }
            }
        }

         //Populates the rook move array for every square (not using magic indexing)
        public static void populateBishopMove(ulong[][] bishopMovesArray) {

            //loops over every square in the rook occupancy variation array
            for (int i = 0; i <= 63; i++) {

                bishopMovesArray[i] = new ulong[bishopOccupancyVariations[i].Length];

                for (int j = 0; j < bishopOccupancyVariations[i].Length; j++) {

                    ulong bishopMove = 0x0UL;
                    ulong square = 0x1UL << i;

                    //If (index/8 <= 6) && (index % 8 <= 6), shift up-left 7-(index)/8 && 7 - (index % 8) times (min of the two)
                    if (i / 8 <= 6 && i % 8 <= 6) {
                        for (int k = 0; (k <= 7 - i / 8 && k <= 7 - (i % 8)); k++) {
                            bishopMove |= square << (9 * k);

                            //If a "1" is encountered in the corresponding occupancy variation, then break
                            if ((bishopOccupancyVariations[i][j] & square << (9 * k)) == square << (9 * k)) {
                                break;
                            }
                        }
                    }
                    //If (index/8 >= 1) && (index % 8 <= 6), shift down-right 0 + (index/8) &&  (index % 8) times (min of the two)
                    if (i / 8 >= 1 && i % 8 >= 1) {
                        for (int k = 0; (k <= (i / 8)  && k <= (i % 8)); k++) {
                            bishopMove |= square >> (9 * k);
                            if ((bishopOccupancyVariations[i][j] & square >> (9 * k)) == square >> (9 * k)) {
                                break;
                            }
                        }
                    }
                    //If (index % 8 <= 6) and (index / 8 >= 1). shift down-left 7 - (index % 8) && (index/8) times (min of the two)
                    if (i / 8 >= 1 && i % 8 <= 6) {
                        for (int k = 0; (k <= (i / 8)  && k <= 7 - (i % 8)); k++) {
                            bishopMove |= square >> (7 * k);
                            if ((bishopOccupancyVariations[i][j] & square >> (7 * k)) == square >> (7 * k)) {
                                break;
                            }
                        }
                    }
                    //If (index % 8 >= 1) and (index / 8 <= 6), shift up-right (index % 8) && (7-(index)/8) times (min ov the two)
                    if (i / 8 <= 6 && i % 8 >= 1) {
                        for (int k = 0; (k <= 7 - (i / 8) && k <= i % 8); k++) {
                            bishopMove |= square << (7 * k);
                            if ((bishopOccupancyVariations[i][j] & square << (7 * k)) == square << (7 * k)) {
                                break;
                            }
                        }
                    }

                    bishopMove &= ~square;
                    bishopMovesArray[i][j] = bishopMove;

                }
            }
        }

        //BIT MANIPULATION METHODS----------------------------------------------------------------------------

        //gets first set (index of least significant bit)
        private static int findFirstSet(ulong bitboard) {
            const ulong deBruijn64 = 0x03f79d71b4cb0a89UL;
            return index64[((bitboard ^ (bitboard - 1)) * deBruijn64) >> 58];
        }

        //gets arraylist containing index of all 1s
        public static List<int> bitScan(ulong bitboard) {
            var indices = new List<int>();
            
            while (bitboard != 0) {
                indices.Add(findFirstSet(bitboard));
                bitboard &= bitboard - 1;
            }
            return indices;
        }

      
        //Finds the popcount (number of 1s in the bit)
        //This method was copied directly from stockfish
        public static int popcount(ulong bitboard) {
            bitboard -= (bitboard >> 1) & 0x5555555555555555UL;
            bitboard = ((bitboard >> 2) & 0x3333333333333333UL) + (bitboard & 0x3333333333333333UL);
            bitboard = ((bitboard >> 4) + bitboard) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((bitboard * 0x0101010101010101UL) >> 56);
        }
    }
}
