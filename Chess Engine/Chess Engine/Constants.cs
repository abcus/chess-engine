using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Value = System.Int32;

namespace Chess_Engine {
    
    internal static class Constants {

		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// BOARD CONSTANTS
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

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
	    public const ulong RANK_2_TO_6 = (RANK_2 | RANK_3 | RANK_4 | RANK_5 | RANK_6);
		public const ulong RANK_3_TO_7 = (RANK_3 | RANK_4 | RANK_5 | RANK_6 | RANK_7);
		
        //Light and dark squares
        public const ulong LIGHT_SQUARES = 0xAA55AA55AA55AA55UL;
        public const ulong DARK_SQUARES = 0x55AA55AA55AA55AAUL;

        //Castling squares
        public const ulong WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES = 0x0000000000000006UL;
        public const ulong WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES = 0x0000000000000070UL;
        public const ulong BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES = 0x0600000000000000UL;
        public const ulong BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES = 0x7000000000000000UL;

        //Rook moves in castling
        public const ulong A1_BITBOARD = 0x0000000000000080UL;
        public const ulong D1_BITBOARD = 0x0000000000000010UL;
        public const ulong F1_BITBOARD = 0x0000000000000004UL;
        public const ulong H1_BITBOARD = 0x0000000000000001UL;

        public const ulong A8_BITBOARD = 0x8000000000000000UL;
        public const ulong D8_BITBOARD = 0x1000000000000000UL;
        public const ulong F8_BITBOARD = 0x0400000000000000UL;
        public const ulong H8_BITBOARD = 0x0100000000000000UL;

        //To convert from unsigned long to signed, subtract 18446744073709551616 if the unsigned long is bigger than 9223372036854775807

        //FEN for starting position
        public const string FEN_START = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        //FEN for a random position
        public const string FEN_KIWIPETE = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -";

        //Enumerated type for types of moves
        public const int
            QUIET_MOVE = 0,
            DOUBLE_PAWN_PUSH = 1,
            SHORT_CASTLE = 2,
            LONG_CASTLE = 3,
            CAPTURE = 4,
            EN_PASSANT_CAPTURE = 5,
            PROMOTION = 6,
            PROMOTION_CAPTURE = 7;
        
		//Move representation masks
        public const int
            START_SQUARE_MASK = 0x3F,
            DESTINATION_SQUARE_MASK = 0xFC0,
            FLAG_MASK = 0xF000,
            PIECE_CAPTURED_MASK = 0xF0000,
            PIECE_PROMOTED_MASK = 0xF00000,
			MOVE_SCORE_MASK = 0x7F000000;
			
		// Shift numbers
	    public const int
		    START_SQUARE_SHIFT = 0,
		    DESTINATION_SQUARE_SHIFT = 6,
		    FLAG_SHIFT = 12,
		    PIECE_CAPTURED_SHIFT = 16,
		    PIECE_PROMOTED_SHIFT = 20,
		    MOVE_SCORE_SHIFT = 24;

		//Enumerated types for pieces
        public const int
               EMPTY = 0,
               WHITE_PAWN = 1,
               WHITE_KNIGHT = 2,
               WHITE_BISHOP = 3,
               WHITE_ROOK = 4,
               WHITE_QUEEN = 5,
               WHITE_KING = 6,
               BLACK_PAWN = 7,
               BLACK_KNIGHT = 8,
               BLACK_BISHOP = 9,
               BLACK_ROOK = 10,
               BLACK_QUEEN = 11,
               BLACK_KING = 12;

	    public static readonly char[] pieceCharacter = {' ', 'P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k'};

        public const int
            PAWN = 1,
            KNIGHT = 2,
            BISHOP = 3,
            ROOK = 4,
            QUEEN = 5,
            KING = 6;

        //Enumerated type for side to move
        public const int WHITE = 0, BLACK = 1, ALL = 2;

        //Enumberated type for checks (multiple check is there because making a king move could result in it moving into multiple checks)
	    public const int NOT_IN_CHECK = 0, CHECK = 1, DOUBLE_CHECK = 2, MULTIPLE_CHECK = 3;

        //Enumerated type for castling rights
        public const int CANNOT_CASTLE = 0, CAN_CASTLE = 1;

		//Enmerated type for aggregate bitboard array
	    public const int WHITE_PIECES = 0, BLACK_PIECES = 1, ALL_PIECES = 2;

        //Max moves from a position
        public const int MAX_MOVES_FROM_POSITION = 220;

        //Enumerated type for squares
        public const int
            H1 = 00, G1 = 01, F1 = 02, E1 = 03, D1 = 04, C1 = 05, B1 = 06, A1 = 07, 
            H2 = 08, G2 = 09, F2 = 10, E2 = 11, D2 = 12, C2 = 13, B2 = 14, A2 = 15, 
            H3 = 16, G3 = 17, F3 = 18, E3 = 19, D3 = 20, C3 = 21, B3 = 22, A3 = 23, 
            H4 = 24, G4 = 25, F4 = 26, E4 = 27, D4 = 28, C4 = 29, B4 = 30, A4 = 31, 
            H5 = 32, G5 = 33, F5 = 34, E5 = 35, D5 = 36, C5 = 37, B5 = 38, A5 = 39, 
            H6 = 40, G6 = 41, F6 = 42, E6 = 43, D6 = 44, C6 = 45, B6 = 46, A6 = 47, 
            H7 = 48, G7 = 49, F7 = 50, E7 = 51, D7 = 52, C7 = 53, B7 = 54, A7 = 55, 
            H8 = 56, G8 = 57, F8 = 58, E8 = 59, D8 = 60, C8 = 61, B8 = 62, A8 = 63;

		//De Bruijn shift number
		public const ulong deBruijn64 = 0x03f79d71b4cb0a89UL;

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
            0x0002000000000000UL, 0x0005000000000000UL, 0x000A000000000000UL, 0x0014000000000000UL,
            0x0028000000000000UL, 0x0050000000000000UL, 0x00A0000000000000UL, 0x0040000000000000UL

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

        //Rook move array for all occupancy variations for all squares 
        //Starts at H1 and goes to A8
        public static ulong[][] rookMoves = new ulong[64][];

        //Bishop move array for all occupancy variations for all squares
        //Starts at H1 and goes to A8
        public static ulong[][] bishopMoves = new ulong[64][];

		//Populates the occupancy variation arrays and piece move arrays
		public static void initBoardConstants() {
			populateRookOccupancyVariation(rookOccupancyVariations);
			populateBishopOccupancyVariation(bishopOccupancyVariations);
			populateRookMove(rookMoves);
			populateBishopMove(bishopMoves);
		}

		//Generates all permutations for the "1 bits" of a binary number 
		private static void generateBinaryPermutations(ulong inputBinaryNumber, List<int> remainingIndices, List<ulong> permutations) {

			//If there are no more indices left to be swapped, it prints out the number
			if (remainingIndices.Count == 0) {
				permutations.Add(inputBinaryNumber);
				//Console.WriteLine(Convert.ToString((long) inputBinaryNumber, 2));
			} else {
				string[] onOff = { "0", "1" };

				//Sets the first "1" bit to either 0 or 1
				foreach (string bit in onOff) {
					if (bit == "0") {
						inputBinaryNumber &= ~(0x1UL << remainingIndices[0]);
					}
					//removes the index of the first "1" bit and makes a recursive call to set the next "1" bit to either 0 or 1
					int temp = remainingIndices[0];
					remainingIndices.RemoveAt(0);
					generateBinaryPermutations(inputBinaryNumber, remainingIndices, permutations);

					//Unmakes move by re-inserting the index of the first "1" bit, and resetting the input binary number
					remainingIndices.Insert(0, temp);
					if (bit == "0") {
						inputBinaryNumber |= (0x1UL << remainingIndices[0]);
					}
				}
			}
		}

		//Populates the rook occupancy variation array for every square
		public static void populateRookOccupancyVariation(ulong[][] rookOccupancyVariation) {

			//loops over every square
			for (int i = 0; i <= 63; i++) {

				List<ulong> permutationsForParticularSquare = new List<ulong>();

				//Takes in the rook occupancy mask for that particular square and generates all permutations/variations of the "1" bits, and stores it in array list
				generateBinaryPermutations(rookOccupancyMask[i], bitScan(rookOccupancyMask[i]), permutationsForParticularSquare);

				//Sorts the array list of permutations/variations, converts it to an array, and puts in the rook occupancy variations array
				permutationsForParticularSquare.Sort();
				ulong[] permutationForParticularSquareArray = permutationsForParticularSquare.ToArray();
				rookOccupancyVariation[i] = permutationForParticularSquareArray;
			}
		}

		//Populates the bishop occupancy variation array for every square
		public static void populateBishopOccupancyVariation(ulong[][] bishopOccupancyVariation) {

			for (int i = 0; i <= 63; i++) {

				List<ulong> permutationsForParticularSquare = new List<ulong>();

				generateBinaryPermutations(bishopOccupancyMask[i], bitScan(bishopOccupancyMask[i]), permutationsForParticularSquare);

				permutationsForParticularSquare.Sort();
				ulong[] permutationsForParticularSquareArray = permutationsForParticularSquare.ToArray();
				bishopOccupancyVariation[i] = permutationsForParticularSquareArray;

			}
		}

		//Populates the rook move array for every square 
		public static void populateRookMove(ulong[][] rookMovesArray) {

			//loops over every square in the rook occupancy variation array
			for (int i = 0; i <= 63; i++) {

				rookMovesArray[i] = new ulong[rookOccupancyVariations[i].Length];

				for (int j = 0; j < rookOccupancyVariations[i].Length; j++) {

					ulong rookMove = 0x0UL;
					ulong square = 0x1UL << i;

					//If (index/8 <= 6), shift up 7 - (index)/8 times (if in 7th rank or lower, shift up 8-rank times)
					//If a "1" is encountered in the occupancy variation, break
					if (i / 8 <= 6) {
						for (int k = 0; k <= 7 - i / 8; k++) {

							rookMove |= square << (8 * k);

							//If a "1" is encountered in the corresponding occupancy variation, then break
							if ((rookOccupancyVariations[i][j] & square << (8 * k)) == square << (8 * k)) {
								break;
							}
						}
					}
					//If (index/8 >= 1) shift down 0 + (index/8) times (if in 2nd rank or higher, shift down rank - 1 times)
					//If a "1" is encountered in the occupancy variation, break
					if (i / 8 >= 1) {
						for (int k = 0; k <= i / 8; k++) {
							rookMove |= square >> (8 * k);
							if ((rookOccupancyVariations[i][j] & square >> (8 * k)) == square >> (8 * k)) {
								break;
							}
						}
					}
					//If (index %8 <= 6) shift left 7 - (index % 8) times (if in B file or higher, shift left file - 1 times)
					//If a "1" is encountered in the occupancy variation, break
					if (i % 8 <= 6) {
						for (int k = 0; k <= 7 - (i % 8); k++) {
							rookMove |= square << k;
							if ((rookOccupancyVariations[i][j] & square << k) == square << k) {
								break;
							}
						}
					}
					//If (index % 8 >= 1) shift right (index % 8) times
					if (i % 8 >= 1) {
						for (int k = 0; k <= (i % 8); k++) {
							rookMove |= square >> k;
							if ((rookOccupancyVariations[i][j] & square >> k) == square >> k) {
								break;
							}
						}
					}
					rookMove &= ~square;

					//Hash function that calculates the array index from the table of magic numbers and table of shifts
					int arrayIndex = (int)((rookOccupancyVariations[i][j] * rookMagicNumbers[i]) >>
									 (rookMagicShiftNumber[i]));

					rookMovesArray[i][arrayIndex] = rookMove;
				}
			}
		}

		//Populates the bishop move array for every square 
		public static void populateBishopMove(ulong[][] bishopMovesArray) {

			//loops over every square in the bishop occupancy variation array
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
						for (int k = 0; (k <= (i / 8) && k <= (i % 8)); k++) {
							bishopMove |= square >> (9 * k);
							if ((bishopOccupancyVariations[i][j] & square >> (9 * k)) == square >> (9 * k)) {
								break;
							}
						}
					}
					//If (index % 8 <= 6) and (index / 8 >= 1). shift down-left 7 - (index % 8) && (index/8) times (min of the two)
					if (i / 8 >= 1 && i % 8 <= 6) {
						for (int k = 0; (k <= (i / 8) && k <= 7 - (i % 8)); k++) {
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

					//Hash function that calculates the array index from the table of magic numbers and table of shifts
					int arrayIndex = (int)((bishopOccupancyVariations[i][j] * bishopMagicNumbers[i]) >>
									 (bishopMagicShiftNumber[i]));

					bishopMovesArray[i][arrayIndex] = bishopMove;

				}
			}
		}

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        // EVALUATION CONSTANTS
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------

        // Values of draws, stalemates, and checkmates
        public const Value DRAW = 0;
        public const Value STALEMATE = 0;
        public const Value CHECKMATE = 30000;

        public const Value KING_VALUE = 12500;

        // Material value for each piece in the middlegame
        public const Value PAWN_VALUE_MG = 198;
        public const Value KNIGHT_VALUE_MG = 817;
        public const Value BISHOP_VALUE_MG = 836;
        public const Value ROOK_VALUE_MG = 1270;
        public const Value QUEEN_VALUE_MG = 2521;
        
        // Material value for each piece in the endgame
        public const Value PAWN_VALUE_EG = 258;
        public const Value KNIGHT_VALUE_EG = 846;
        public const Value BISHOP_VALUE_EG = 857;
        public const Value ROOK_VALUE_EG = 1278;
        public const Value QUEEN_VALUE_EG = 2558;

        // Array of piece values [piece]
        public static Value[] arrayOfPieceValuesMG = new Value[13];
        public static Value[] arrayOfPieceValuesEG = new Value[13];
	    public static Value[] arrayOfPieceValuesSEE = {0, 100, 325, 330, 500, 1000, 10000, 100, 325, 330, 500, 1000, 10000};

        // White piece square tables for the middlegame 
        public static int[] wPawnMidgamePSQ = {
            0, 0, 0, 0, 0, 0, 0, 0,
            -20, 0, 0, 0, 0, 0, 0, -20,
            -20, 0, 10, 20, 20, 10, 0, -20,
            -20, 0, 20, 40, 40, 20, 0, -20,
            -20, 0, 10, 20, 20, 10, 0, -20,
            -20, 0, 0, 0, 0, 0, 0, -20,
            -20, 0, 0, 0, 0, 0, 0, -20,
            0, 0, 0, 0, 0, 0, 0, 0,
        };

        public static int[] wKnightMidgamePSQ = {
            -144, -109, -85, -73, -73, -85, -109, -144,
            -88, -43, -19, -7, -7, -19, -43, -88,
            -69, -24, 0, 12, 12, 0, -24, -69,
            -28, 17, 41, 53, 53, 41, 17, -28,
            -30, 15, 39, 51, 51, 39, 15, -30,
            -10, 35, 59, 71, 71, 59, 35, -10,
            -64, -19, 5, 17, 17, 5, -19, -64,
            -200, -65, -41, -29, -29, -41, -65, -200,
        };

        public static int[] wBishopMidgamePSQ = {
            -54, -27, -34, -43, -43, -34, -27, -54,
            -29, 8, 1, -8, -8, 1, 8, -29,
            -20, 17, 10, 1, 1, 10, 17, -20,
            -19, 18, 11, 2, 2, 11, 18, -19,
            -22, 15, 8, -1, -1, 8, 15, -22,
            -28, 9, 2, -7, -7, 2, 9, -28,
            -32, 5, -2, -11, -11, -2, 5, -32,
            -49, -22, -29, -38, -38, -29, -22, -49
        };

        public static int[] wRookMidgamePSQ = {
            -22, -17, -12, -8, -8, -12, -17, -22,
            -22, -7, -2, 2, 2, -2, -7, -22,
            -22, -7, -2, 2, 2, -2, -7, -22,
            -22, -7, -2, 2, 2, -2, -7, -22,
            -22, -7, -2, 2, 2, -2, -7, -22,
            -22, -7, -2, 2, 2, -2, -7, -22,
            -11, 4, 9, 13, 13, 9, 4, -11,
            -22, -17, -12, -8, -8, -12, -17, -22
        };

        public static int[] wQueenMidgamePSQ = {
            -2, -2, -2, -2, -2, -2, -2, -2,
            -2, 8, 8, 8, 8, 8, 8, -2,
            -2, 8, 8, 8, 8, 8, 8, -2,
            -2, 8, 8, 8, 8, 8, 8, -2,
            -2, 8, 8, 8, 8, 8, 8, -2,
            -2, 8, 8, 8, 8, 8, 8, -2,
            -2, 8, 8, 8, 8, 8, 8, -2,
            -2, -2, -2, -2, -2, -2, -2, -2
        };

        public static int[] wKingMidgamePSQ = {
            298, 332, 273, 225, 225, 273, 332, 298,
            287, 321, 262, 214, 214, 262, 321, 287,
            224, 258, 199, 151, 151, 199, 258, 224,
            196, 230, 171, 123, 123, 171, 230, 196,
            173, 207, 148, 100, 100, 148, 207, 173,
            146, 180, 121, 73, 73, 121, 180, 146,
            119, 153, 94, 46, 46, 94, 153, 119,
            98, 132, 73, 25, 25, 73, 132, 98
        };

        // White piece square values for the endgame
        public static int[] wPawnEndgamePSQ = {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        public static int[] wKnightEndgamePSQ = {
            -98, -83, -51, -16, -16, -51, -83, -98,
            -68, -53, -21, 14, 14, -21, -53, -68,
            -53, -38, -6, 29, 29, -6, -38, -53,
            -42, -27, 5, 40, 40, 5, -27, -42,
            -42, -27, 5, 40, 40, 5, -27, -42,
            -53, -38, -6, 29, 29, -6, -38, -53,
            -68, -53, -21, 14, 14, -21, -53, -68,
            -98, -83, -51, -16, -16, -51, -83, -98
        };

        public static int[] wBishopEndgamePSQ = {
            -65, -42, -44, -26, -26, -44, -42, -65,
            -43, -20, -22, -4, -4, -22, -20, -43,
            -33, -10, -12, 6, 6, -12, -10, -33,
            -35, -12, -14, 4, 4, -14, -12, -35,
            -35, -12, -14, 4, 4, -14, -12, -35,
            -33, -10, -12, 6, 6, -12, -10, -33,
            -43, -20, -22, -4, -4, -22, -20, -43,
            -65, -42, -44, -26, -26, -44, -42, -6530
        };

        public static int[] wRookEndgamePSQ = {
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 3, 3
        };

        public static int[] wQueenEndgamePSQ = {
            -80, -54, -42, -30, -30, -42, -54, -80,
            -54, -30, -18, -6, -6, -18, -30, -54,
            -42, -18, -6, 6, 6, -6, -18, -42,
            -30, -6, 6, 18, 18, 6, -6, -30,
            -30, -6, 6, 18, 18, 6, -6, -30,
            -42, -18, -6, 6, 6, -6, -18, -42,
            -54, -30, -18, -6, -6, -18, -30, -54,
            -80, -54, -42, -30, -30, -42, -54, -80
        };

        public static int[] wKingEndgamePSQ = {
            27, 81, 108, 116, 116, 108, 81, 27,
            74, 128, 155, 163, 163, 155, 128, 74,
            111, 165, 192, 200, 200, 192, 165, 111,
            135, 189, 216, 224, 224, 216, 189, 135,
            135, 189, 216, 224, 224, 216, 189, 135,
            111, 165, 192, 200, 200, 192, 165, 111,
            74, 128, 155, 163, 163, 155, 128, 74,
            27, 81, 108, 116, 116, 108, 81, 27
        };

        // Black piece square tables for the middlegame
        public static int[] bpawnMidgamePSQ = new int[64];
        public static int[] bKnightMidgamePSQ = new int[64];
        public static int[] bBishopMidgamePSQ = new int[64];
        public static int[] bRookMidgamePSQ = new int[64];
        public static int[] bQueenMidgamePSQ = new int[64];
        public static int[] bKingMidgamePSQ = new int[64];

        // Black piece square tables for the endgame
        public static int[] bPawnEndgamePSQ = new int[64];
        public static int[] bKnightEndgamePSQ = new int[64];
        public static int[] bBishopEndgamePSQ = new int[64];
        public static int[] bRookEndgamePSQ = new int[64];
        public static int[] bQueenEndgamePSQ = new int[64];
        public static int[] bKingEndgamePSQ = new int[64];

        // Array of piece square tables [piece][square]
        public static int[][] arrayOfPSQMidgame = new int[13][];
        public static int[][] arrayOfPSQEndgame = new int[13][];

        // Relative contribution that each piece makes when determining game phase
        public const int pawnPhase = 0;
        public const int knightPhase = 1;
        public const int bishopPhase = 1;
        public const int rookPhase = 2;
        public const int queenPhase = 4;

        public const int totalPhase = 16 * pawnPhase + 4 * knightPhase + 4 * bishopPhase + 4 * rookPhase + 2 * queenPhase;

	    public const int LARGE_INT = Int32.MaxValue - 1;

        // Initializes the evaluation constants
        public static void initEvalConstants() {

            arrayOfPieceValuesMG[1] = PAWN_VALUE_MG;
            arrayOfPieceValuesMG[2] = KNIGHT_VALUE_MG;
            arrayOfPieceValuesMG[3] = BISHOP_VALUE_MG;
            arrayOfPieceValuesMG[4] = ROOK_VALUE_MG;
            arrayOfPieceValuesMG[5] = QUEEN_VALUE_MG;
	        arrayOfPieceValuesMG[6] = KING_VALUE;
            arrayOfPieceValuesMG[7] = PAWN_VALUE_MG;
            arrayOfPieceValuesMG[8] = KNIGHT_VALUE_MG;
            arrayOfPieceValuesMG[9] = BISHOP_VALUE_MG;
            arrayOfPieceValuesMG[10] = ROOK_VALUE_MG;
            arrayOfPieceValuesMG[11] = QUEEN_VALUE_MG;
	        arrayOfPieceValuesMG[12] = KING_VALUE;

            arrayOfPieceValuesEG[1] = PAWN_VALUE_EG;
            arrayOfPieceValuesEG[2] = KNIGHT_VALUE_EG;
            arrayOfPieceValuesEG[3] = BISHOP_VALUE_EG;
            arrayOfPieceValuesEG[4] = ROOK_VALUE_EG;
            arrayOfPieceValuesEG[5] = QUEEN_VALUE_EG;
	        arrayOfPieceValuesEG[6] = KING_VALUE;
            arrayOfPieceValuesEG[7] = PAWN_VALUE_EG;
            arrayOfPieceValuesEG[8] = KNIGHT_VALUE_EG;
            arrayOfPieceValuesEG[9] = BISHOP_VALUE_EG;
            arrayOfPieceValuesEG[10] = ROOK_VALUE_EG; 
            arrayOfPieceValuesEG[11] = QUEEN_VALUE_EG;
	        arrayOfPieceValuesEG[12] = KING_VALUE;
            
            // Initializes the black piece square tables by flipping the corresponding white table about the midpoint
            for (int i = 0; i < 64; i += 8) {
                for (int j = 0; j < 8; j++) {
                    bpawnMidgamePSQ[i + j] = wPawnMidgamePSQ[56 - i + j];
                    bKnightMidgamePSQ[i + j] = wKnightMidgamePSQ[56 - i + j];
                    bBishopMidgamePSQ[i + j] = wBishopMidgamePSQ[56 - i + j];
                    bRookMidgamePSQ[i + j] = wRookMidgamePSQ[56 - i + j];
                    bQueenMidgamePSQ[i + j] = wQueenMidgamePSQ[56 - i + j];
                    bKingMidgamePSQ[i + j] = wKingMidgamePSQ[56 - i + j];
                    bPawnEndgamePSQ[i + j] = wPawnEndgamePSQ[56 - i + j];
                    bKnightEndgamePSQ[i + j] = wKnightEndgamePSQ[56 - i + j];
                    bBishopEndgamePSQ[i + j] = wBishopEndgamePSQ[56 - i + j];
                    bRookEndgamePSQ[i + j] = wRookEndgamePSQ[56 - i + j];
                    bQueenEndgamePSQ[i + j] = wQueenEndgamePSQ[56 - i + j];
                    bKingEndgamePSQ[i + j] = wKingEndgamePSQ[56 - i + j];
                }
            }
            arrayOfPSQMidgame[1] = wPawnMidgamePSQ;
            arrayOfPSQMidgame[2] = wKnightMidgamePSQ;
            arrayOfPSQMidgame[3] = wBishopMidgamePSQ;
            arrayOfPSQMidgame[4] = wRookMidgamePSQ;
            arrayOfPSQMidgame[5] = wQueenMidgamePSQ;
            arrayOfPSQMidgame[6] = wKingMidgamePSQ;

            arrayOfPSQMidgame[7] = bpawnMidgamePSQ;
            arrayOfPSQMidgame[8] = bKnightMidgamePSQ;
            arrayOfPSQMidgame[9] = bBishopMidgamePSQ;
            arrayOfPSQMidgame[10] = bRookMidgamePSQ;
            arrayOfPSQMidgame[11] = bQueenMidgamePSQ;
            arrayOfPSQMidgame[12] = bKingMidgamePSQ;

            arrayOfPSQEndgame[1] = wPawnEndgamePSQ;
            arrayOfPSQEndgame[2] = wKnightEndgamePSQ;
            arrayOfPSQEndgame[3] = wBishopEndgamePSQ;
            arrayOfPSQEndgame[4] = wRookEndgamePSQ;
            arrayOfPSQEndgame[5] = wQueenEndgamePSQ;
            arrayOfPSQEndgame[6] = wKingEndgamePSQ;

            arrayOfPSQEndgame[7] = bPawnEndgamePSQ;
            arrayOfPSQEndgame[8] = bKnightEndgamePSQ;
            arrayOfPSQEndgame[9] = bBishopEndgamePSQ;
            arrayOfPSQEndgame[10] = bRookEndgamePSQ;
            arrayOfPSQEndgame[11] = bQueenEndgamePSQ;
            arrayOfPSQEndgame[12] = bKingEndgamePSQ;

        }
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------
		// SEARCH CONSTANTS
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

	    public const int MAX_DEPTH = 64;

	    public const int ASP_WINDOW = Constants.PAWN_VALUE_MG/2;

		public const int TT_SIZE = 15485867;
	    public const int PV_TT_SIZE = 1000003;

	    public const int PV_NODE = 1;
	    public const int CUT_NODE = 2;
	    public const int ALL_NODE = 3;

		public static ulong[,] pieceZobrist = new ulong[13,64];
		public static ulong[] enPassantZobrist = new ulong[64];
		public static ulong[] castleZobrist = new ulong[4];
		public static ulong[] sideToMoveZobrist = new ulong[1];

	    private static int seed = 1;
	    private static Random rnd = new Random(Constants.seed);

		public static int[] victimScore = { 0, 10, 20, 30, 40, 50, 60, 10, 20, 30, 40, 50, 60 };
		public static int[,] MvvLvaScore = new int[13, 13];

	    public const int HASH_MOVE_SCORE = 127;
		public const int GOOD_PROMOTION_SCORE = 125;
	    public const int GOOD_PROMOTION_CAPTURE_SCORE = 125;
		public const int GOOD_CAPTURE_SCORE = 60;
		public const int BAD_PROMOTION_SCORE = 69;
		public const int BAD_PROMOTION_CAPTURE_SCORE = 69;
	    public const int BAD_CAPTURE_SCORE = 4;

		public const int EN_PASSANT_SCORE = 75;
		

		// Have to experiment with ordering of killer and bad capture
	    public const int KILLER_1_SCORE = 13;
	    public const int KILLER_2_SCORE = 12;

		// Extension method that generates a random ulong
	    public static UInt64 NextUInt64(this Random rnd) {
		    var buffer = new byte[sizeof (UInt64)];
			rnd.NextBytes(buffer);
		    return BitConverter.ToUInt64(buffer, 0);
	    }

		// Initializes the MVV/LVA array
	    private static void initializeMvvLvaArray() {
		    for (int victim = Constants.WHITE_PAWN; victim <= Constants.BLACK_KING; victim ++) {
			    for (int attacker = Constants.WHITE_PAWN; attacker <= Constants.BLACK_KING; attacker++) {
				    Constants.MvvLvaScore[victim, attacker] = (Constants.victimScore[victim] + 6 - ((Constants.victimScore[attacker])/10));
			    }
		    }
	    }

		// Initializes the zobrist random number arrays and MVV/LVA array
		public static void initSearchConstants() {
			
			// Populates the piece Zobrist array
			for (int i = 1; i <= 12; i++) {
				for (int j = 0; j < 64; j++) {
					Constants.pieceZobrist[i, j] = rnd.NextUInt64();
				}
			}
	
			// Populates the en Passant Zobrist array
			for (int i = 0; i < 64; i++) {
				Constants.enPassantZobrist[i] = rnd.NextUInt64();
			}

			// Populates the castle Zobrist array
			for (int i = 0; i < 4; i++) {
				Constants.castleZobrist[i] = rnd.NextUInt64();
			}

			// Populates the side to Move Zobrist array
			Constants.sideToMoveZobrist[0] = rnd.NextUInt64();

			Constants.initializeMvvLvaArray();
		}

	    

		//--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //BIT MANIPULATION METHODS
        //--------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------

        // returns array list containing the indices of all 1s
        public static List<int> bitScan(ulong bitboard) {
            List<int> indices = new List<int>(28);
            while (bitboard != 0) {
                indices.Add(index64[((bitboard ^ (bitboard - 1))*deBruijn64) >> 58]);
                
                // removes the least significant bit
                bitboard &= bitboard - 1;
            }
            return indices;
        }

        // returns the index of the least significant 1
        public static int findFirstSet(ulong bitboard) {
            return index64[((bitboard ^ (bitboard - 1)) * deBruijn64) >> 58];
        }

        // returns the popcount
        // copied from stockfish
        public static int popcount(ulong bitboard) {
            bitboard -= (bitboard >> 1) & 0x5555555555555555UL;
            bitboard = ((bitboard >> 2) & 0x3333333333333333UL) + (bitboard & 0x3333333333333333UL);
            bitboard = ((bitboard >> 4) + bitboard) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((bitboard * 0x0101010101010101UL) >> 56);
        }



       
    }
}
