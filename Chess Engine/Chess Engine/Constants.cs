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

		// Enumerated types for the generatePawnKnightBishopRookQueenKingMoves method
	    public const int
		    UNDER_PROMOTION = 8,
		    QUEEN_PROMOTION = 9,
		    UNDER_PROMOTION_CAPTURE = 10,
		    QUEEN_PROMOTION_CAPTURE = 11;
			

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

	    public const int ASP_WINDOW = Constants.PAWN_VALUE_MG/4;

		public const int TT_SIZE = 15485867;
	    public const int PV_TT_SIZE = 1000003;

	    public const int EXACT = 1;
	    public const int L_BOUND = 2;
	    public const int U_BOUND = 3;

	    public const int PV_NODE = 1;
	    public const int NON_PV_NODE = 2;

		public static ulong[,] pieceZobrist = new ulong[13,64];
		public static ulong[] enPassantZobrist = new ulong[64];
		public static ulong[] castleZobrist = new ulong[4];
		public static ulong[] sideToMoveZobrist = new ulong[1];

	    private static int seed = 1;
	    private static Random rnd = new Random(Constants.seed);

		public static int[] victimScore = { 0, 10, 20, 30, 40, 50, 60, 10, 20, 30, 40, 50, 60 };
		public static int[,] MvvLvaScore = new int[13, 13];

		public const int GOOD_PROMOTION_CAPTURE_SCORE = 125;
		public const int GOOD_PROMOTION_SCORE = 70;
		public const int GOOD_CAPTURE_SCORE = 60;
		public const int BAD_PROMOTION_CAPTURE_SCORE = 69;
		public const int BAD_PROMOTION_SCORE = 14;
	    public const int BAD_CAPTURE_SCORE = 4;

	    public const int GOOD_QUIET_SCORE = 0;
	    public const int BAD_QUIET_SCORE = 0;

		// Have to experiment with ordering of killer and bad capture
	    public const int KILLER_1_SCORE = 13;
	    public const int KILLER_2_SCORE = 12;
		
		// Move Generator Constants
		// For the main search:
		//    First generate captures, en-passant captures, capture-promotions, and promotions
		//    Then generate quiet moves, double pawn pushes, short castle, and long castle
		// For the quiescence search:
		//    For depth = 0, generate captures, en-passant captures, capture promotions, quiet queen promotions, and quiet checks (from quiet moves or double pawn pushes)
		//    For depth < 0, generate captures, en-passant captures, capture promotions, and quiet queen promotions
		//    Missing is quiet underpromotions, short castle, long castle, quiet no check (quiet move and double pawn push)
		
	    public const int MAIN_CAP_EPCAP_CAPPROMO_PROMO = 0;
	    public const int MAIN_QUIETMOVE_DOUBLEPAWNPUSH_SHORTCAS_LONGCAS = 1;

	    public const int QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO_QUIETCHECK = 2;
		public const int QUIESCENCE_CAP_EPCAP_QUEENCAPPROMO_QUIETQUEENPROMO = 3;
		public const int QUIESCENCE_QUIETUNDERPROMO_UNDERPROMOCAP_SHORTCAS_LONGCAS_QUIETNOCHECK = 4;
	    
		public const int ALL_MOVES = 5;
	    
		// Perft testing constants
	    public const int PERFT_ALL_MOVES = 0;
		public const int PERFT_MAIN = 1;
		public const int PERFT_QUIESCENCE = 2;

		// Null move depth reduction
	    public const int R = 2;

		// Search phase constants
	    public const int PHASE_HASH = 0;
	    public const int PHASE_GOOD_CAPTURE = 1;
	    public const int PHASE_KILLER_1 = 2;
	    public const int PHASE_KILLER_2 = 3;
	    public const int PHASE_QUIET = 4;
	    public const int PHASE_BAD_CAPTURE = 5;
	    public const int PHASE_CHECK_EVADE = 6;

	    
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
		// OPENING BOOK
		//--------------------------------------------------------------------------------------------------------------------------------------------
		//--------------------------------------------------------------------------------------------------------------------------------------------

		public static ulong[,] piecePolyglot = {
			{	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,
				0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL,	0x0UL},

			{	0x79AD695501E7D1E8UL,	0x8249A47AEE0E41F7UL,	0x637A7780DECFC0D9UL,	0x19FC8A768CF4B6D4UL,	0x7BCBC38DA25A7F3CUL,	0x5093417AA8A7ED5EUL,	0x07FB9F855A997142UL,	0x5355F900C2A82DC7UL,
				0xE99D662AF4243939UL,	0xA49CD132BFBF7CC4UL,	0x0CE26C0B95C980D9UL,	0xBB6E2924F03912EAUL,	0x24C3C94DF9C8D3F6UL,	0xDABF2AC8201752FCUL,	0xF145B6BECCDEA195UL,	0x14ACBAF4777D5776UL,
				0xF9B89D3E99A075C2UL,	0x70AC4CD9F04F21F5UL,	0x9A85AC909A24EAA1UL,	0xEE954D3C7B411F47UL,	0x72B12C32127FED2BUL,	0x54B3F4FA5F40D873UL,	0x8535F040B9744FF1UL,	0x27E6AD7891165C3FUL,
				0x8DE8DCA9F03CC54EUL,	0xFF07F64EF8ED14D0UL,	0x092237AC237F3859UL,	0x87BF02C6B49E2AE9UL,	0x1920C04D47267BBDUL,	0xAE4A9346CC3F7CF2UL,	0xA366E5B8C54F48B8UL,	0x87B3E2B2B5C907B1UL,
				0x6304D09A0B3738C4UL,	0x4F80F7A035DAFB04UL,	0x9A74ACB964E78CB3UL,	0x1E1032911FA78984UL,	0x5BFEA5B4712768E9UL,	0x390E5FB44D01144BUL,	0xB3F22C3D0B0B38EDUL,	0x9C1633264DB49C89UL,
				0x7B32F7D1E03680ECUL,	0xEF927DBCF00C20F2UL,	0xDFD395339CDBF4A7UL,	0x6503080440750644UL,	0x1881AFC9A3A701D6UL,	0x506AACF489889342UL,	0x5B9B63EB9CEFF80CUL,	0x2171E64683023A08UL,
				0xEDE6C87F8477609DUL,	0x3C79A0FF5580EF7FUL,	0xF538639CE705B824UL,	0xCF464CEC899A2F8AUL,	0x4A750A09CE9573F7UL,	0xB5889C6E15630A75UL,	0x05A7E8A57DB91B77UL,	0xB9FD7620E7316243UL,
				0x73A1921916591CBDUL,	0x70EB093B15B290CCUL,	0x920E449535DD359EUL,	0x043FCAE60CC0EBA0UL,	0xA246637CFF328532UL,	0x97D7374C60087B73UL,	0x86536B8CF3428A8CUL,	0x799E81F05BC93F31UL},
			
			{	0x3BBA57B68871B59DUL,	0xDF1D9F9D784BA010UL,	0x94061B871E04DF75UL,	0x9315E5EB3A129ACEUL,	0x08BD35CC38336615UL,	0xFE9A44E9362F05FAUL,	0x78E37644E7CAD29EUL,	0xC547F57E42A7444EUL,
				0x4F2A5CB07F6A35B3UL,	0xA2F61BB6E437FDB5UL,	0xA74049DAC312AC71UL,	0x336F52F8FF4728E7UL,	0xD95BE88CD210FFA7UL,	0xD7F4F2448C0CEB81UL,	0xF7A255D83BC373F8UL,	0xD2B7ADEEDED1F73FUL,
				0x4C0563B89F495AC3UL,	0x18FCF680573FA594UL,	0xFCAF55C1BF8A4424UL,	0x39B0BF7DDE437BA2UL,	0xF3A678CAD9A2E38CUL,	0x7BA2484C8A0FD54EUL,	0x16B9F7E06C453A21UL,	0x87D380BDA5BF7859UL,
				0x35CAB62109DD038AUL,	0x32095B6D4AB5F9B1UL,	0x3810E399B6F65BA2UL,	0x9D1D60E5076F5B6FUL,	0x7A1EE967D27579E2UL,	0x68CA39053261169FUL,	0x8CFFA9412EB642C1UL,	0x40E087931A00930DUL,
				0x9D1DFA2EFC557F73UL,	0x52AB92BEB9613989UL,	0x528F7C8602C5807BUL,	0xD941ACA44B20A45BUL,	0x4361C0CA3F692F12UL,	0x513E5E634C70E331UL,	0x77A225A07CC2C6BDUL,	0xA90B24499FCFAFB1UL,
				0x284C847B9D887AAEUL,	0x56FD23C8F9715A4CUL,	0x0CD9A497658A5698UL,	0x5A110C6058B920A0UL,	0x04208FE9E8F7F2D6UL,	0x7A249A57EC0C9BA2UL,	0x1D1260A51107FE97UL,	0x722FF175F572C348UL,
				0x5E11E86D5873D484UL,	0x0ED9B915C66ED37EUL,	0xB0183DB56FFC6A79UL,	0x506E6744CD974924UL,	0x881B82A13B51B9E2UL,	0x9A9632E65904AD3CUL,	0x742E1E651C60BA83UL,	0x04FEABFBBDB619CBUL,
				0x48CBFF086DDF285AUL,	0x99E7AFEABE000731UL,	0x93C42566AEF98FFBUL,	0xA865A54EDCC0F019UL,	0x0D151D86ADB73615UL,	0xDAB9FE6525D89021UL,	0x1B85D488D0F20CC5UL,	0xF678647E3519AC6EUL},

			{	0x2FE4B17170E59750UL,	0xE8D9ECBE2CF3D73FUL,	0xB57D2E985E1419C7UL,	0x0572B974F03CE0BBUL,	0xA8D7E4DAB780A08DUL,	0x4715ED43E8A45C0AUL,	0xC330DE426430F69DUL,	0x23B70EDB1955C4BFUL,
				0x49353FEA39BA63B1UL,	0xF85B2B4FBCDE44B7UL,	0xBE7444E39328A0ACUL,	0x3E2B8BCBF016D66DUL,	0x964E915CD5E2B207UL,	0x1725CABFCB045B00UL,	0x7FBF21EC8A1F45ECUL,	0x11317BA87905E790UL,
				0xE94C39A54A98307FUL,	0xAA70B5B4F89695A2UL,	0x3BDBB92C43B17F26UL,	0xCCCB7005C6B9C28DUL,	0x18A6A990C8B35EBDUL,	0xFC7C95D827357AFAUL,	0x1FCA8A92FD719F85UL,	0x1DD01AAFCD53486AUL,
				0xDBC0D2B6AB90A559UL,	0x94628D38D0C20584UL,	0x64972D68DEE33360UL,	0xB9C11D5B1E43A07EUL,	0x2DE0966DAF2F8B1CUL,	0x2E18BC1AD9704A68UL,	0xD4DBA84729AF48ADUL,	0xB7A0B174CFF6F36EUL,
				0xCFFE1939438E9B24UL,	0x79999CDFF70902CBUL,	0x8547EDDFB81CCB94UL,	0x7B77497B32503B12UL,	0x97FCAACBF030BC24UL,	0x6CED1983376FA72BUL,	0x7E75D99D94A70F4DUL,	0xD2733C4335C6A72FUL,
				0x9FF38FED72E9052FUL,	0x9F65789A6509A440UL,	0x0981DCD296A8736DUL,	0x5873888850659AE7UL,	0xC678B6D860284A1CUL,	0x63E22C147B9C3403UL,	0x92FAE24291F2B3F1UL,	0x829626E3892D95D7UL,
				0x7A76956C3EAFB413UL,	0x7F5126DBBA5E0CA7UL,	0x12153635B2C0CF57UL,	0x7B3F0195FC6F290FUL,	0x5544F7D774B14AEFUL,	0x56C074A581EA17FEUL,	0xE7F28ECD2D49EECDUL,	0xE479EE5B9930578CUL,
				0x7F9D1A2E1EBE1327UL,	0x5D0A12F27AD310D1UL,	0x3BC36E078F7515D7UL,	0x4DA8979A0041E8A9UL,	0x950113646D1D6E03UL,	0x7B4A38E32537DF62UL,	0x8A1B083821F40CB4UL,	0x3D5774A11D31AB39UL},

			{	0xD18D8549D140CAEAUL,	0x1CFC8BED0D681639UL,	0xCA1E3785A9E724E5UL,	0xB67C1FA481680AF8UL,	0xDFEA21EA9E7557E3UL,	0xD6B6D0ECC617C699UL,	0xFA7E393983325753UL,	0xA09E8C8C35AB96DEUL,
				0x7983EED3740847D5UL,	0x298AF231C85BAFABUL,	0x2680B122BAA28D97UL,	0x734DE8181F6EC39AUL,	0x53898E4C3910DA55UL,	0x1761F93A44D5AEFEUL,	0xE4DBF0634473F5D2UL,	0x4ED0FE7E9DC91335UL,
				0x261E4E4C0A333A9DUL,	0x219B97E26FFC81BDUL,	0x66B4835D9EAFEA22UL,	0x4CC317FB9CDDD023UL,	0x50B704CAB602C329UL,	0xEDB454E7BADC0805UL,	0x9E17E49642A3E4C1UL,	0x66C1A2A1A60CD889UL,
				0x36F60E2BA4FA6800UL,	0x38B6525C21A42B0EUL,	0xF4F5D05C10CAB243UL,	0xCF3F4688801EB9AAUL,	0x1DDC0325259B27DEUL,	0xB9571FA04DC089C8UL,	0xD7504DFA8816EDBBUL,	0x1FE2CCA76517DB90UL,
				0xE699ED85B0DFB40DUL,	0xD4347F66EC8941C3UL,	0xF4D14597E660F855UL,	0x8B889D624D44885DUL,	0x258E5A80C7204C4BUL,	0xAF0C317D32ADAA8AUL,	0x9C4CD6257C5A3603UL,	0xEB3593803173E0CEUL,
				0x0B090A7560A968E3UL,	0x2CF9C8CA052F6E9FUL,	0x116D0016CB948F09UL,	0xA59E0BD101731A28UL,	0x63767572AE3D6174UL,	0xAB4F6451CC1D45ECUL,	0xC2A1E7B5B459AEB5UL,	0x2472F6207C2D0484UL,
				0x804456AF10F5FB53UL,	0xD74BBE77E6116AC7UL,	0x7C0828DD624EC390UL,	0x14A195640116F336UL,	0x2EAB8CA63CE802D7UL,	0xC6E57A78FBD986E0UL,	0x58EFC10B06A2068DUL,	0xABEEDDB2DDE06FF1UL,
				0x12A8F216AF9418C2UL,	0xD4490AD526F14431UL,	0xB49C3B3995091A36UL,	0x5B45E522E4B1B4EFUL,	0xA1E9300CD8520548UL,	0x49787FEF17AF9924UL,	0x03219A39EE587A30UL,	0xEBE9EA2ADF4321C7UL},

			{	0x720BF5F26F4D2EAAUL,	0x1C2559E30F0946BEUL,	0xE328E230E3E2B3FBUL,	0x087E79E5A57D1D13UL,	0x08DD9BDFD96B9F63UL,	0x64D0E29EEA8838B3UL,	0xDDF957BC36D8B9CAUL,	0x6FFE73E81B637FB3UL,
				0x93B633ABFA3469F8UL,	0xE846963877671A17UL,	0x59AC2C7873F910A3UL,	0x660D3257380841EEUL,	0xD813F2FAB7F5C5CAUL,	0x4112CF68649A260EUL,	0x443F64EC5A371195UL,	0xB0774D261CC609DBUL,
				0xB5635C95FF7296E2UL,	0xED2DF21216235097UL,	0x4A29C6465A314CD1UL,	0xD83CC2687A19255FUL,	0x506C11B9D90E8B1DUL,	0x57277707199B8175UL,	0xCAF21ECD4377B28CUL,	0xC0C0F5A60EF4CDCFUL,
				0x7C45D833AFF07862UL,	0xA5B1CFDBA0AB4067UL,	0x6AD047C430A12104UL,	0x6C47BEC883A7DE39UL,	0x944F6DE09134DFB6UL,	0x9AEBA33AC6ECC6B0UL,	0x52E762596BF68235UL,	0x22AF003AB672E811UL,
				0x50065E535A213CF6UL,	0xDE0C89A556B9AE70UL,	0xD1E0CCD25BB9C169UL,	0x6B17B224BAD6BF27UL,	0x6B02E63195AD0CF8UL,	0x455A4B4CFE30E3F5UL,	0x9338E69C052B8E7BUL,	0x5092EF950A16DA0BUL,
				0x67FEF95D92607890UL,	0x31865CED6120F37DUL,	0x3A6853C7E70757A7UL,	0x32AB0EDB696703D3UL,	0xEE97F453F06791EDUL,	0x6DC93D9526A50E68UL,	0x78EDEFD694AF1EEDUL,	0x9C1169FA2777B874UL,
				0x6BFA9AAE5EC05779UL,	0x371F77E76BB8417EUL,	0x3550C2321FD6109CUL,	0xFB4A3D794A9A80D2UL,	0xF43C732873F24C13UL,	0xAA9119FF184CCCF4UL,	0xB69E38A8965C6B65UL,	0x1F2B1D1F15F6DC9CUL,
				0xB5B4071DBFC73A66UL,	0x8F9887E6078735A1UL,	0x08DE8A1C7797DA9BUL,	0xFCB6BE43A9F2FE9BUL,	0x049A7F41061A9E60UL,	0x9F91508BFFCFC14AUL,	0xE3273522064480CAUL,	0xCD04F3FF001A4778UL},

			{	0x2102AE466EBB1148UL,	0xE87FBB46217A360EUL,	0x310CB380DB6F7503UL,	0xB5FDFC5D3132C498UL,	0xDAF8E9829FE96B5FUL,	0xCAC09AFBDDD2CDB4UL,	0xB862225B055B6960UL,	0x55B6344CF97AAFAEUL,
				0x046E3ECAAF453CE9UL,	0x962ACEEFA82E1C84UL,	0xF5B4B0B0D2DEEEB4UL,	0x1AF3DBE25D8F45DAUL,	0xF9F4892ED96BD438UL,	0xC4C118BFE78FEAAEUL,	0x07A69AFDCC42261AUL,	0xF8549E1A3AA5E00DUL,
				0x486289DDCC3D6780UL,	0x222BBFAE61725606UL,	0x2BC60A63A6F3B3F2UL,	0x177E00F9FC32F791UL,	0x522E23F3925E319EUL,	0x9C2ED44081CE5FBDUL,	0x964781CE734B3C84UL,	0xF05D129681949A4CUL,
				0xD586BD01C5C217F6UL,	0x233003B5A6CFE6ADUL,	0x24C0E332B70019B0UL,	0x9DA058C67844F20CUL,	0xE4D9429322CD065AUL,	0x1FAB64EA29A2DDF7UL,	0x8AF38731C02BA980UL,	0x7DC7785B8EFDFC80UL,
				0x93CBE0B699C2585DUL,	0x1D95B0A5FCF90BC6UL,	0x17EFEE45B0DEE640UL,	0x9E4C1269BAA4BF37UL,	0xD79476A84EE20D06UL,	0x0A56A5F0BFE39272UL,	0x7EBA726D8C94094BUL,	0x5E5637885F29BC2BUL,
				0xC61BB3A141E50E8CUL,	0x2785338347F2BA08UL,	0x7CA9723FBB2E8988UL,	0xCE2F8642CA0712DCUL,	0x59300222B4561E00UL,	0xC2B5A03F71471A6FUL,	0xD5F9E858292504D5UL,	0x65FA4F227A2B6D79UL,
				0x71F1CE2490D20B07UL,	0xE6C42178C4BBB92EUL,	0x0A9C32D5EAE45305UL,	0x0C335248857FA9E7UL,	0x142DE49FFF7A7C3DUL,	0x64A53DC924FE7AC9UL,	0x9F6A419D382595F4UL,	0x150F361DAB9DEC26UL,
				0xD20D8C88C8FFE65FUL,	0x917F1DD5F8886C61UL,	0x56986E2EF3ED091BUL,	0x5FA7867CAF35E149UL,	0x81A1549FD6573DA5UL,	0x96FBF83A12884624UL,	0xE728E8C83C334074UL,	0xF1BCC3D275AFE51AUL},

			{	0xE83A908FF2FB60CAUL,	0x0FBBAD1F61042279UL,	0x3290AC3A203001BFUL,	0x75834465489C0C89UL,	0x9C15F73E62A76AE2UL,	0x44DB015024623547UL,	0x2AF7398005AAA5C7UL,	0x9D39247E33776D41UL,
				0x239F8B2D7FF719CCUL,	0x5DB4832046F3D9E5UL,	0x011355146FD56395UL,	0x40BDF15D4A672E32UL,	0xD021FF5CD13A2ED5UL,	0x9605D5F0E25EC3B0UL,	0x1A083822CEAFE02DUL,	0x0D7E765D58755C10UL,
				0x4BB38DE5E7219443UL,	0x331478F3AF51BBE6UL,	0xF3218F1C9510786CUL,	0x82C7709E781EB7CCUL,	0x7D11CDB1C3B7ADF0UL,	0x7449BBFF801FED0BUL,	0x679F848F6E8FC971UL,	0x05D1A1AE85B49AA1UL,
				0x24AA6C514DA27500UL,	0xC9452CA81A09D85DUL,	0x7B0500AC42047AC4UL,	0xB4AB30F062B19ABFUL,	0x19F3C751D3E92AE1UL,	0x87D2074B81D79217UL,	0x8DBD98A352AFD40BUL,	0xAA649C6EBCFD50FCUL,
				0x735E2B97A4C45A23UL,	0x3575668334A1DD3BUL,	0x09D1BC9A3DD90A94UL,	0x637B2B34FF93C040UL,	0x03488B95B0F1850FUL,	0xA71B9B83461CBD93UL,	0x14A68FD73C910841UL,	0x4C9F34427501B447UL,
				0xFCF7FE8A3430B241UL,	0x5C82C505DB9AB0FAUL,	0x51EBDC4AB9BA3035UL,	0x9F74D14F7454A824UL,	0xBF983FE0FE5D8244UL,	0xD310A7C2CE9B6555UL,	0x1FCBACD259BF02E7UL,	0x18727070F1BD400BUL,
				0x96D693460CC37E5DUL,	0x4DE0B0F40F32A7B8UL,	0x6568FCA92C76A243UL,	0x11D505D4C351BD7FUL,	0x7EF48F2B83024E20UL,	0xB9BC6C87167C33E7UL,	0x8C74C368081B3075UL,	0x3253A729B9BA3DDEUL,
				0xEC16CA8AEA98AD76UL,	0x63DC359D8D231B78UL,	0x93C5B5F47356388BUL,	0x39F890F579F92F88UL,	0x5F0F4A5898171BB6UL,	0x42880B0236E4D951UL,	0x6D2BDCDAE2919661UL,	0x42E240CB63689F2FUL},

			{	0xDD2C5BC84BC8D8FCUL,	0xAE623FD67468AA70UL,	0xFF6712FFCFD75EA1UL,	0x930F80F4E8EB7462UL,	0x45F20042F24F1768UL,	0xBB215798D45DF7AFUL,	0xEFAC4B70633B8F81UL,	0x56436C9FE1A1AA8DUL,
				0xAA969B5C691CCB7AUL,	0x43539603D6C55602UL,	0x1BEDE3A3AEF53302UL,	0xDEC468145B7605F6UL,	0x808BD68E6AC10365UL,	0xC91800E98FB99929UL,	0x22FE545401165F1CUL,	0x7EED120D54CF2DD9UL,
				0x28AED140BE0BB7DDUL,	0x10CFF333E0ED804AUL,	0x91B859E59ECB6350UL,	0xB415938D7DA94E3CUL,	0x21F08570F420E565UL,	0xDED2D633CAD004F6UL,	0x65942C7B3C7E11AEUL,	0xA87832D392EFEE56UL,
				0xAEF3AF4A563DFE43UL,	0x480412BAB7F5BE2AUL,	0xAF2042F5CC5C2858UL,	0xEF2F054308F6A2BCUL,	0x9BC5A38EF729ABD4UL,	0x2D255069F0B7DAB3UL,	0x5648F680F11A2741UL,	0xC5CC1D89724FA456UL,
				0x4DC4DE189B671A1CUL,	0x066F70B33FE09017UL,	0x9DA4243DE836994FUL,	0xBCE5D2248682C115UL,	0x11379625747D5AF3UL,	0xF4F076E65F2CE6F0UL,	0x52593803DFF1E840UL,	0x19AFE59AE451497FUL,
				0xF793C46702E086A0UL,	0x763C4A1371B368FDUL,	0x2DF16F761598AA4FUL,	0x21A007933A522A20UL,	0xB3819A42ABE61C87UL,	0xB46EE9C5E64A6E7CUL,	0xC07A3F80C31FB4B4UL,	0x51039AB7712457C3UL,
				0x9AE182C8BC9474E8UL,	0xB05CA3F564268D99UL,	0xCFC447F1E53C8E1BUL,	0x4850E73E03EB6064UL,	0x2C604A7A177326B3UL,	0x0BF692B38D079F23UL,	0xDE336A2A4BC1C44BUL,	0xD7288E012AEB8D31UL,
				0x6703DF9D2924E97EUL,	0x8EC97D2917456ED0UL,	0x9C684CB6C4D24417UL,	0xFC6A82D64B8655FBUL,	0xF9B5B7C4ACC67C96UL,	0x69B97DB1A4C03DFEUL,	0xE755178D58FC4E76UL,	0xA4FC4BD4FC5558CAUL},

			{	0x501F65EDB3034D07UL,	0x907F30421D78C5DEUL,	0x1A804AADB9CFA741UL,	0x0CE2A38C344A6EEDUL,	0xD363EFF5F0977996UL,	0x2CD16E2ABD791E33UL,	0x58627E1A149BBA21UL,	0x7F9B6AF1EBF78BAFUL,
				0x364F6FFA464EE52EUL,	0x6C3B8E3E336139D3UL,	0xF943AEE7FEBF21B8UL,	0x088E049589C432E0UL,	0xD49503536ABCA345UL,	0x3A6C27934E31188AUL,	0x957BAF61700CFF4EUL,	0x37624AE5A48FA6E9UL,
				0xB344C470397BBA52UL,	0xBAC7A9A18531294BUL,	0xECB53939887E8175UL,	0x565601C0364E3228UL,	0xEF1955914B609F93UL,	0x16F50EDF91E513AFUL,	0x56963B0DCA418FC0UL,	0xD60F6DCEDC314222UL,
				0x99170A5DC3115544UL,	0x59B97885E2F2EA28UL,	0xBC4097B116C524D2UL,	0x7A13F18BBEDC4FF5UL,	0x071582401C38434DUL,	0xB422061193D6F6A7UL,	0xB4B81B3FA97511E2UL,	0x65D34954DAF3CEBDUL,
				0xC7D9F16864A76E94UL,	0x7BD94E1D8E17DEBCUL,	0xD873DB391292ED4FUL,	0x30F5611484119414UL,	0x565C31F7DE89EA27UL,	0xD0E4366228B03343UL,	0x325928EE6E6F8794UL,	0x6F423357E7C6A9F9UL,
				0x35DD37D5871448AFUL,	0xB03031A8B4516E84UL,	0xB3F256D8ACA0B0B9UL,	0x0FD22063EDC29FCAUL,	0xD9A11FBB3D9808E4UL,	0x3A9BF55BA91F81CAUL,	0xC8C93882F9475F5FUL,	0x947AE053EE56E63CUL,
				0xBBE83F4ECC2BDECBUL,	0xCD454F8F19C5126AUL,	0xC62C58F97DD949BFUL,	0x693501D628297551UL,	0xB9AB4CE57F2D34F3UL,	0x9255ABB50D532280UL,	0xEBFAFA33D7254B59UL,	0xE9F6082B05542E4EUL,
				0x098954D51FFF6580UL,	0x8107FCCF064FCF56UL,	0x852F54934DA55CC9UL,	0x09C7E552BC76492FUL,	0xE9F6760E32CD8021UL,	0xA3BC941D0A5061CBUL,	0xBA89142E007503B8UL,	0xDC842B7E2819E230UL},

			{	0x10DCD78E3851A492UL,	0xB438C2B67F98E5E9UL,	0x43954B3252DC25E5UL,	0xAB9090168DD05F34UL,	0xCE68341F79893389UL,	0x36833336D068F707UL,	0xDCDD7D20903D0C25UL,	0xDA3A361B1C5157B1UL,
				0xAF08DA9177DDA93DUL,	0xAC12FB171817EEE7UL,	0x1FFF7AC80904BF45UL,	0xA9119B60369FFEBDUL,	0xBFCED1B0048EAC50UL,	0xB67B7896167B4C84UL,	0x9B3CDB65F82CA382UL,	0xDBC27AB5447822BFUL,
				0x6DD856D94D259236UL,	0x67378D8ECCEF96CBUL,	0x9FC477DE4ED681DAUL,	0xF3B8B6675A6507FFUL,	0xC3A9DC228CAAC9E9UL,	0xC37B45B3F8D6F2BAUL,	0xB559EB1D04E5E932UL,	0x1B0CAB936E65C744UL,
				0x7440FB816508C4FEUL,	0x9D266D6A1CC0542CUL,	0x4DDA48153C94938AUL,	0x74C04BF1790C0EFEUL,	0xE1925C71285279F5UL,	0x8A8E849EB32781A5UL,	0x073973751F12DD5EUL,	0xA319CE15B0B4DB31UL,
				0x94EBC8ABCFB56DAEUL,	0xD7A023A73260B45CUL,	0x72C8834A5957B511UL,	0x8F8419A348F296BFUL,	0x1E152328F3318DEAUL,	0x4838D65F6EF6748FUL,	0xD6BF7BAEE43CAC40UL,	0x13328503DF48229FUL,
				0xDD69A0D8AB3B546DUL,	0x65CA5B96B7552210UL,	0x2FD7E4B9E72CD38CUL,	0x51D2B1AB2DDFB636UL,	0x9D1D84FCCE371425UL,	0xA44CFE79AE538BBEUL,	0xDE68A2355B93CAE6UL,	0x9FC10D0F989993E0UL,
				0x3A938FEE32D29981UL,	0x2C5E9DEB57EF4743UL,	0x1E99B96E70A9BE8BUL,	0x764DBEAE7FA4F3A6UL,	0xAAC40A2703D9BEA0UL,	0x1A8C1E992B941148UL,	0x73AA8A564FB7AC9EUL,	0x604D51B25FBF70E2UL,
				0x8FE88B57305E2AB6UL,	0x89039D79D6FC5C5CUL,	0x9BFB227EBDF4C5CEUL,	0x7F7CC39420A3A545UL,	0x3F6C6AF859D80055UL,	0xC8763C5B08D1908CUL,	0x469356C504EC9F9DUL,	0x26E6DB8FFDF5ADFEUL},

			{	0x1BDA0492E7E4586EUL,	0xD23C8E176D113600UL,	0x252F59CF0D9F04BBUL,	0xB3598080CE64A656UL,	0x993E1DE72D36D310UL,	0xA2853B80F17F58EEUL,	0x1877B51E57A764D5UL,	0x001F837CC7350524UL,
				0x241260ED4AD1E87DUL,	0x64C8E531BFF53B55UL,	0xCA672B91E9E4FA16UL,	0x3871700761B3F743UL,	0xF95CFFA23AF5F6F4UL,	0x8D14DEDB30BE846EUL,	0x3B097ADAF088F94EUL,	0x21E0BD5026C619BFUL,
				0xB8D91274B9E9D4FBUL,	0x1DB956E450275779UL,	0x4FC8E9560F91B123UL,	0x63573FF03E224774UL,	0x0647DFEDCD894A29UL,	0x7884D9BC6CB569D8UL,	0x7FBA195410E5CA30UL,	0x106C09B972D2E822UL,
				0x98F076A4F7A2322EUL,	0x70CB6AF7C2D5BCF0UL,	0xB64BE8D8B25396C1UL,	0xA9AA4D20DB084E9BUL,	0x2E6D02C36017F67FUL,	0xEFED53D75FD64E6BUL,	0xD9F1F30CCD97FB09UL,	0xA2EBEE47E2FBFCE1UL,
				0xFC87614BAF287E07UL,	0x240AB57A8B888B20UL,	0xBF8D5108E27E0D48UL,	0x61BDD1307C66E300UL,	0xB925A6CD0421AFF3UL,	0x3E003E616A6591E9UL,	0x94C3251F06F90CF3UL,	0xBF84470805E69B5FUL,
				0x758F450C88572E0BUL,	0x1B6BACA2AE4E125BUL,	0x61CF4F94C97DF93DUL,	0x2738259634305C14UL,	0xD39BB9C3A48DB6CFUL,	0x8215E577001332C8UL,	0xA1082C0466DF6C0AUL,	0xEF02CDD06FFDB432UL,
				0x7976033A39F7D952UL,	0x106F72FE81E2C590UL,	0x8C90FD9B083F4558UL,	0xFD080D236DA814BAUL,	0x7B64978555326F9FUL,	0x60E8ED72C0DFF5D1UL,	0xB063E962E045F54DUL,	0x959F587D507A8359UL,
				0x1A4E4822EB4D7A59UL,	0x5D94337FBFAF7F5BUL,	0xD30C088BA61EA5EFUL,	0x9D765E419FB69F6DUL,	0x9E21F4F903B33FD9UL,	0xB4D8F77BC3E56167UL,	0x733EA705FAE4FA77UL,	0xA4EC0132764CA04BUL},

			{	0xD6B04D3B7651DD7EUL,	0xE34A1D250E7A8D6BUL,	0x53C065C6C8E63528UL,	0x1BDEA12E35F6A8C9UL,	0x21874B8B4D2DBC4FUL,	0x3A88A0FBBCB05C63UL,	0x43ED7F5A0FAE657DUL,	0x230E343DFBA08D33UL,
				0xD4C718BC4AE8AE5FUL,	0x9EEDECA8E272B933UL,	0x10E8B35AF3EEAB37UL,	0x0E09B88E1914F7AFUL,	0x3FA9DDFB67E2F199UL,	0xB10BB459132D0A26UL,	0x2C046F22062DC67DUL,	0x5E90277E7CB39E2DUL,
				0xB49B52E587A1EE60UL,	0xAC042E70F8B383F2UL,	0x89C350C893AE7DC1UL,	0xB592BF39B0364963UL,	0x190E714FADA5156EUL,	0xEC8177F83F900978UL,	0x91B534F885818A06UL,	0x81536D601170FC20UL,
				0x57E3306D881EDB4FUL,	0x0A804D18B7097475UL,	0xE74733427B72F0C1UL,	0x24B33C9D7ED25117UL,	0xE805A1E290CF2456UL,	0x3B544EBE544C19F9UL,	0x3E666E6F69AE2C15UL,	0xFB152FE3FF26DA89UL,
				0x1A4FF12616EEFC89UL,	0x990A98FD5071D263UL,	0x84547DDC3E203C94UL,	0x07A3AEC79624C7DAUL,	0x8A328A1CEDFE552CUL,	0xD1E649DE1E7F268BUL,	0x2D8D5432157064C8UL,	0x4AE7D6A36EB5DBCBUL,
				0x4659D2B743848A2CUL,	0x19EBB029435DCB0FUL,	0x4E9D2827355FC492UL,	0xCCEC0A73B49C9921UL,	0x46C9FEB55D120902UL,	0x8D2636B81555A786UL,	0x30C05B1BA332F41CUL,	0xF6F7FD1431714200UL,
				0xABBDCDD7ED5C0860UL,	0x9853EAB63B5E0B35UL,	0x352787BAA0D7C22FUL,	0xC7F6AA2DE59AEA61UL,	0x03727073C2E134B1UL,	0x5A0F544DD2B1FB18UL,	0x74F85198B05A2E7DUL,	0x963EF2C96B33BE31UL,
				0xFF577222C14F0A3AUL,	0x4E4B705B92903BA4UL,	0x730499AF921549FFUL,	0x13AE978D09FE5557UL,	0xD9E92AA246BF719EUL,	0x7A4C10EC2158C4A6UL,	0x49CAD48CEBF4A71EUL,	0xCF05DAF5AC8D77B0UL}
		};

		public static ulong[] enPassantPolyglot = {
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
			0x67A34DAC4356550BUL,	0x77C621CC9FB3A483UL,	0xD0E4427A5514FB72UL,	0xCF3145DE0ADD4289UL,	0x1C99DED33CB890A1UL,	0x003A93D8B2806962UL,	0xE21A6B35DF0C3AD7UL,	0x70CC73D90BC26E24UL,
		};

		public static ulong[] castlePolyglot = { 0x31D71DCE64B2C310UL, 0xF165B587DF898190UL, 0xA57E6339DD2CF3A0UL, 0x1EF6E6DBB1961EC9UL };

		public static ulong[] sideToMovePolyglot = { 0xF8D626AAAF278509UL };

	    public const int MAX_BOOK_MOVES = 32;

		// Array for printing the rank
		public static readonly char[] fileChar = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

		// Array for printing the file
		public static readonly char[] rankChar = { '1', '2', '3', '4', '5', '6', '7', '8' };

		// Array for the promotion piece
	    public static readonly char[] promotionPieceChar = {' ', 'n', 'b', 'r', 'q'};

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

		// Swaps endian-ness for a uint16
	    public static UInt16 swapUint16Endian(UInt16 val) {
			return (ushort)((val << 8) | (val >> 8));
	    }

		// Swaps endian-ness for a uint32
	    public static UInt32 swapUint32Endian(UInt32 val) {
			val = ((val << 8) & 0xFF00FF00) | ((val >> 8) & 0xFF00FF);
			return (val << 16) | (val >> 16);
	    }
		

		// Swaps endian-ness for a uint64
		public static UInt64 swapUint64Endian (UInt64 val) {
			val = ((val << 8) & 0xFF00FF00FF00FF00UL ) | ((val >> 8) & 0x00FF00FF00FF00FFUL);
			val = ((val << 16) & 0xFFFF0000FFFF0000UL ) | ((val >> 16) & 0x0000FFFF0000FFFFUL);
			return (val << 32) | (val >> 32);
		}
    }
}
