using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {

    internal sealed class EvaluationInfo {

        internal MaterialEntry matEntery = null;
        internal PawnEntry pEntry = null;

        // attackedBy [color][pieceType] is a bitboard of all squares attacked by a piece of a certain color ant type
        // attackedBy [color][0] is a bitboard of all squares attacked by a color
        internal readonly UInt64[,] attackedBy = new UInt64 [2,8];

        // kingRing[color] is a bitboard of the squares immediately adjacent to the king and the 3 squares two ranks in front of the king (2 squares if king is on A or H file)
        internal readonly UInt64[] kingRing = new UInt64[2];

        // kingAttackersCount[color] is the number of pieces of a certain color which attack a square in the kingRing of the opposing king
        internal readonly Int32[] kingAttackersCount = new Int32[2];

        // kingAttackersWeight[color] is the sum of the weights of pieces of a certain color attacking a square in the kingRing of the opposing king (see below for weight)
        internal readonly Int32[] kingAttackersWeight = new Int32[2];

        // kingAdjacentZoneAttacksCount[color] is the number of attacks of opposing pieces to squares directly adjacent to a king of a certain color
        // Opposing pieces which attack multiple squares have each of their attacks counted
        internal readonly Int32[] kingAdjacentZoneAttacksCount = new Int32[2];
    }

    public static class Evaluate {

        // Evaluation weights (0 = mobility, 1 = passed_pawns, 2 = space, 3 = king_danger_us, 4 = king_danger_them)
        internal static readonly Int32[] weights = {
            Utilities.makeScore(252, 344), Utilities.makeScore(216, 266), Utilities.makeScore(46, 0), Utilities.makeScore(247, 0), Utilities.makeScore(259, 0)
        };

        // MobilityBonus[pieceType][attacked] contains mobility bonuses for middle and endgame, indexed by piece type and number of squares attacked by that piece (not occupied by friendly piece)
        // (index: 0 = empty, 1 = pawn, 2 = knight, 3 = bishop, 4 = rook, 5 = queen)
        // Knights have 0-8 squares, bishops have 0-13 squares, rooks have 0-14 squares, queens have 0-27 squares
        internal static readonly Int32[][] MobilityBonus = {
            new Int32[]{}, 
            new Int32[]{},
            new Int32[]{   Utilities.makeScore(-38,-33), Utilities.makeScore(-25,-23), Utilities.makeScore(-12,-13), Utilities.makeScore( 0, -3), Utilities.makeScore(12,  7), Utilities.makeScore(25, 17), 
                           Utilities.makeScore( 31, 22), Utilities.makeScore( 38, 27), Utilities.makeScore( 38, 27) },
             
            new Int32[]{   Utilities.makeScore(-25,-30), Utilities.makeScore(-11,-16), Utilities.makeScore(  3, -2), Utilities.makeScore(17, 12), Utilities.makeScore(31, 26), Utilities.makeScore(45, 40), 
                           Utilities.makeScore( 57, 52), Utilities.makeScore( 65, 60), Utilities.makeScore( 71, 65), Utilities.makeScore(74, 69), Utilities.makeScore(76, 71), Utilities.makeScore(78, 73),
                           Utilities.makeScore( 79, 74), Utilities.makeScore( 80, 75)},
             
            new Int32[]{   Utilities.makeScore(-20,-36), Utilities.makeScore(-14,-19), Utilities.makeScore( -8, -3), Utilities.makeScore(-2, 13), Utilities.makeScore( 4, 29), Utilities.makeScore(10, 46), 
                           Utilities.makeScore( 14, 62), Utilities.makeScore( 19, 79), Utilities.makeScore( 23, 95), Utilities.makeScore(26,106), Utilities.makeScore(27,111), Utilities.makeScore(28,114),
                           Utilities.makeScore( 29,116), Utilities.makeScore( 30,117), Utilities.makeScore( 31,118)},
             
            new Int32[]{   Utilities.makeScore(-10,-18), Utilities.makeScore( -8,-13), Utilities.makeScore( -6, -7), Utilities.makeScore(-3, -2), Utilities.makeScore(-1,  3), Utilities.makeScore( 1,  8), 
                           Utilities.makeScore(  3, 13), Utilities.makeScore(  5, 19), Utilities.makeScore(  8, 23), Utilities.makeScore(10, 27), Utilities.makeScore(12, 32), Utilities.makeScore(15, 34),
                           Utilities.makeScore( 16, 35), Utilities.makeScore( 17, 35), Utilities.makeScore( 18, 35), Utilities.makeScore(20, 35), Utilities.makeScore(20, 35), Utilities.makeScore(20, 35),
                           Utilities.makeScore( 20, 35), Utilities.makeScore( 20, 35), Utilities.makeScore( 20, 35), Utilities.makeScore(20, 35), Utilities.makeScore(20, 35), Utilities.makeScore(20, 35),
                           Utilities.makeScore( 20, 35), Utilities.makeScore( 20, 35), Utilities.makeScore( 20, 35), Utilities.makeScore(20, 35)}
        };

        // OutpostBonus[PieceType][Square] contains outpost bonuses of knights and bishops, indexed by piece type and square (flip for black).
        // Goes from H1 (index [,0]) to A8 (index [,63])
        internal static readonly Int32[,] outpostBonus = {
            {0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 4, 8, 8, 4, 0, 0,
            0, 4,17,26,26,17, 4, 0,
            0, 8,26,35,35,26, 8, 0,
            0, 4,17,17,17,17, 4, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0},
          
            {0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 5, 5, 5, 5, 0, 0,
            0, 5,10,10,10,10, 5, 0,
            0,10,21,21,21,21,10, 0,
            0, 5, 8, 8, 8, 8, 5, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0}
        };

        // ThreatBonus[attacking][attacked] contains threat bonuses according to which piece type attacks which one.
        // Index is 0 = empty, 1 = pawn, 2 = knight, 3 = bishop, 4 = rook, 5 = queen
        
        internal static readonly Int32[][] ThreatBonus = {
            new Int32[]{}, 
            new Int32[]{},
            new Int32[]{ Utilities.makeScore(0, 0), Utilities.makeScore( 7, 39), Utilities.makeScore( 0,  0), Utilities.makeScore(24, 49), Utilities.makeScore(41,100), Utilities.makeScore(41,100) }, 
            new Int32[]{ Utilities.makeScore(0, 0), Utilities.makeScore( 7, 39), Utilities.makeScore(24, 49), Utilities.makeScore( 0,  0), Utilities.makeScore(41,100), Utilities.makeScore(41,100) }, 
            new Int32[]{ Utilities.makeScore(0, 0), Utilities.makeScore(-1, 29), Utilities.makeScore(15, 49), Utilities.makeScore(15, 49), Utilities.makeScore( 0,  0), Utilities.makeScore(24, 49) }, 
            new Int32[]{ Utilities.makeScore(0, 0), Utilities.makeScore(15, 39), Utilities.makeScore(15, 39), Utilities.makeScore(15, 39), Utilities.makeScore(15, 39), Utilities.makeScore( 0,  0) }  
          };

        // ThreatenedByPawnPenalty[PieceType] contains a penalty according to which piece type is attacked by an enemy pawn.
        internal static readonly Int32[] ThreatenedByPawnPenalty = {
            Utilities.makeScore(0, 0), Utilities.makeScore(0, 0), Utilities.makeScore(56, 70), Utilities.makeScore(56, 70), Utilities.makeScore(76, 99), Utilities.makeScore(86, 118)
          };

        // Bonus for having the side to move
        internal static Int32 Tempo = Utilities.makeScore(24, 11);

        // Bonus for rooks and queens on the 7th rank
        internal static Int32 rookOn7thBonus = Utilities.makeScore(47, 98);
        internal static Int32 queenOn7thBonus = Utilities.makeScore(27, 54);

        // Bonus for rooks on open files
        internal static Int32 rookOpenFileBonus = Utilities.makeScore(43, 21);
        internal static Int32 rookHalfOpenFileBonus = Utilities.makeScore(19, 10);

        // Penalty for rooks trapped on the outside of a friendly king which has lost the right to castle
        internal static Int32 trappedRookPenalty = 180;

        // Penalty for an undefenced bishop or knight
        internal static Int32 undefendedMinorPenalty = Utilities.makeScore(25, 10);

        // The SpaceMask[Color] contains the area of the board which is considered by the space evaluation
        // In the middle game, each side is given a bonus based on how many squares inside this area are safe and available for friendly minor pieces.
        internal static readonly UInt64[] SpaceMask = {
            (1UL << Constants.C2) | (1UL << Constants.D2) | (1UL << Constants.E2) | (1UL << Constants.F2) |
            (1UL << Constants.C3) | (1UL << Constants.D3) | (1UL << Constants.E3) | (1UL << Constants.F3) |
            (1UL << Constants.C4) | (1UL << Constants.D4) | (1UL << Constants.E4) | (1UL << Constants.F4),

            (1UL << Constants.C7) | (1UL << Constants.D7) | (1UL << Constants.E7) | (1UL << Constants.F7) |
            (1UL << Constants.C6) | (1UL << Constants.D6) | (1UL << Constants.E6) | (1UL << Constants.F6) |
            (1UL << Constants.C5) | (1UL << Constants.D5) | (1UL << Constants.E5) | (1UL << Constants.F5)
        };

        // King danger constants and variables
        // Scores measuring the strength of an enemy attack are added up and used as an index to kingDamterTable[]

        // kingAttackWeights[pieceType] contains king attack weights by piece type (used above in kingAttackersWeight[])
        // index: 0 = empty, 1 = pawn, 2 = knight, 3 = bishop, 4 = rook, 5 = queen
        internal static readonly Int32[] kingAttackWeights = {0, 0, 2, 2, 3, 5};

        // Bonuses for enemy's safe checks
        internal const int queenContactCheckBonus = 6;
        internal const int rookContactCheckBonus = 4;
        internal const int queenCheckBonus = 3;
        internal const int rookCheckBonus = 2;
        internal const int bishopCheckBonus = 1;
        internal const int knightCheckBonus = 1;

        // InitKingDanger[Square] contains penalties based on the position of the defending king, indexed by king's square
        // Index: 0 = H1, 63 = A8
        internal static readonly int[] InitKingDanger = new int[] {
             2,  0,  2,  5,  5,  2,  0,  2,
             2,  2,  4,  8,  8,  4,  2,  2,
             7, 10, 12, 12, 12, 12, 10,  7,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15
        };

        // kingDamgerTable[Color][attackUnits] is an array containing the king danger scores indexed by color and the danger score
        internal static readonly Int32[][] kingDangerTable = new Int32[2][];

        // ???
        internal const Int32 maxSlope = 30;
        internal const Int32 peak = 1280;

        // ???
        internal static Int32 rootColor;

        internal static void initialize() {
            
            // Allocate arrays
            kingDangerTable[Constants.WHITE] = new Int32[128];
            kingDangerTable[Constants.BLACK] = new Int32[128];

            // Initialize the king danger table array
            for (int t = 0, i = 1; i < 100 ; i++) {
                t = Math.Min(peak, Math.Min((int) (0.4 * i * i), t + maxSlope));
                kingDangerTable[1][i] = Utilities.applyWeight(Utilities.makeScore(t, 0), weights[EvaluationWeights.KING_DANGER_US]);
                kingDangerTable[0][i] = Utilities.applyWeight(Utilities.makeScore(t, 0), weights[EvaluationWeights.KING_DANGER_THEM]);
            }
        }

        // Returns the evaluation of the position from the point of view of the side to move
        public static int evaluate(Board inputBoard) {
            
            EvaluationInfo eInfo = new EvaluationInfo();

            // Margins are the uncertainty in the evaluation of the position which is used for pruning in the search
            Int32 whiteMargin = 0, blackMargin = 0;
            Int32 score = 0, whiteMobility = 0, blackMobility = 0;

            // Add the tempo to the score
            score += inputBoard.sideToMove == Constants.WHITE ? Tempo : -Tempo;
            
            // Add the material values to the score

            // Add the piece square values to the score




            int evaluationScoreMG = 0;
            int evaluationScoreEG = 0;
            int interpolatedScore = 0;

			// Calculates the middlegame evaluation score
           evaluationScoreMG += inputBoard.whiteMidgameMaterial - inputBoard.blackMidgameMaterial;
	        
	        // Piece square tables
            int pieceSquareTableMG = 0;
            for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_KING; i++) {
                pieceSquareTableMG += inputBoard.midgamePSQ[i];
            }

            for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_KING; i++) {
                pieceSquareTableMG -= inputBoard.midgamePSQ[i];
            }

            evaluationScoreMG += pieceSquareTableMG;
            evaluationScoreMG = Utilities.midgameValue(inputBoard.material_PSQ_Value);
            

            // Calculates the endgame evaluation score

            // Material
            evaluationScoreEG += inputBoard.whiteEndgameMaterial - inputBoard.blackEndgameMaterial;

            // Piece square tables
            int pieceSquareTableEG = 0;

            for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_KING; i++) {
                pieceSquareTableEG += inputBoard.endgamePSQ[i];
            }

            for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_KING; i++) {
                pieceSquareTableEG -= inputBoard.endgamePSQ[i];
            }

            evaluationScoreEG += pieceSquareTableEG;
            evaluationScoreEG = Utilities.endgameValue(inputBoard.material_PSQ_Value);
            

            // Calculates the sum of the number of white pieces and black pieces for each piece type
			int pawnSum = (inputBoard.pieceCount[Constants.WHITE_PAWN] + inputBoard.pieceCount[Constants.BLACK_PAWN]);
			int knightSum = (inputBoard.pieceCount[Constants.WHITE_KNIGHT] + inputBoard.pieceCount[Constants.BLACK_KNIGHT]);
			int bishopSum = (inputBoard.pieceCount[Constants.WHITE_BISHOP] + inputBoard.pieceCount[Constants.BLACK_BISHOP]);
			int rookSum = (inputBoard.pieceCount[Constants.WHITE_ROOK] + inputBoard.pieceCount[Constants.BLACK_ROOK]);
			int queenSum = (inputBoard.pieceCount[Constants.WHITE_QUEEN] + inputBoard.pieceCount[Constants.BLACK_QUEEN]);

            // Calculates the game phase (phase of 0 means opening, and phase of 64 means endgame)
            // Interpolates the score based on game phase
            int phase = Constants.totalPhase;
            phase -= pawnSum * (Constants.pawnPhase);
            phase -= knightSum * (Constants.knightPhase);
            phase -= bishopSum * (Constants.bishopPhase);
            phase -= rookSum * (Constants.rookPhase);
            phase -= queenSum * (Constants.queenPhase);

            if (phase < 0) {
                phase = 0;
            }

            int scaledPhase = ((phase * 64 + Constants.totalPhase/2)/Constants.totalPhase);

            interpolatedScore = ((64 - scaledPhase) * evaluationScoreMG + scaledPhase * evaluationScoreEG)/64;

	        // Evaluation is done from white's perspective
            // If the side to move is black, multiply the score by -1
            return inputBoard.sideToMove == Constants.WHITE ? interpolatedScore : -interpolatedScore;

        }	
    }
}
