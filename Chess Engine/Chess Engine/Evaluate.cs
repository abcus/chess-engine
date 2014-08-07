using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chess_Engine {
    public static class Evaluate {

        // Returns the evaluation of the position from the point of view of the side to move
        public static int evaluationFunction(Board inputBoard) {
            
            int evaluationScoreMG = 0;
            int evaluationScoreEG = 0;
            int interpolatedScore = 0;

			int[] pieceCount = new int[13];
	        int whiteMidgameMaterial = 0;
	        int whiteEndgameMaterial = 0;
	        int blackMidgameMaterial = 0;
	        int blackEndgameMaterial = 0;

			int[] midgamePSQ = new int[13];
			int[] endgamePSQ = new int[13];

			// Calculates the number of pieces
            for (int i = Constants.WHITE_PAWN; i <= Constants.BLACK_KING; i++) {
                pieceCount[i] = Constants.popcount(inputBoard.arrayOfBitboards[i]);
            }
			// Multiplies the number of pieces by the material value
            for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_QUEEN; i++) {
                whiteMidgameMaterial += pieceCount[i] * Constants.arrayOfPieceValuesMG[i];
                whiteEndgameMaterial += pieceCount[i] * Constants.arrayOfPieceValuesEG[i];
            }

            for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_QUEEN; i++) {
                blackMidgameMaterial += pieceCount[i] * Constants.arrayOfPieceValuesMG[i];
                blackEndgameMaterial += pieceCount[i] * Constants.arrayOfPieceValuesEG[i];
            }

			// loops through every square and finds the piece on that square
            // Sets the appropriate PSQ to the PSQ value for that square
            for (int i = 0; i < 64; i++) {
                int piece = inputBoard.pieceArray[i];

                if (piece >= Constants.WHITE_PAWN && piece <= Constants.BLACK_KING) {
                    midgamePSQ[piece] += Constants.arrayOfPSQMidgame[piece][i];
                    endgamePSQ[piece] += Constants.arrayOfPSQEndgame[piece][i];
                }
            }
        
			// Calculates the middlegame evaluation score
           evaluationScoreMG += whiteMidgameMaterial - blackMidgameMaterial;
	        
	        // Piece square tables
            int pieceSquareTableMG = 0;
            for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_KING; i++) {
                pieceSquareTableMG += midgamePSQ[i];
            }

            for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_KING; i++) {
                pieceSquareTableMG -= midgamePSQ[i];
            }

           evaluationScoreMG += pieceSquareTableMG;

            // Calculates the endgame evaluation score

            // Material
            evaluationScoreEG += whiteEndgameMaterial - blackEndgameMaterial;

            // Piece square tables
            int pieceSquareTableEG = 0;

            for (int i = Constants.WHITE_PAWN; i <= Constants.WHITE_KING; i++) {
                pieceSquareTableEG += endgamePSQ[i];
            }

            for (int i = Constants.BLACK_PAWN; i <= Constants.BLACK_KING; i++) {
                pieceSquareTableEG -= endgamePSQ[i];
            }

            evaluationScoreEG += pieceSquareTableEG;


            // Calculates the sum of the number of white pieces and black pieces for each piece type
            int pawnSum = (pieceCount[Constants.WHITE_PAWN] + pieceCount[Constants.BLACK_PAWN]);
            int knightSum = (pieceCount[Constants.WHITE_KNIGHT] + pieceCount[Constants.BLACK_KNIGHT]);
            int bishopSum = (pieceCount[Constants.WHITE_BISHOP] + pieceCount[Constants.BLACK_BISHOP]);
            int rookSum = (pieceCount[Constants.WHITE_ROOK] + pieceCount[Constants.BLACK_ROOK]);
            int queenSum = (pieceCount[Constants.WHITE_QUEEN] + pieceCount[Constants.BLACK_QUEEN]);

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
            if (inputBoard.sideToMove == Constants.WHITE) {
                return interpolatedScore;
            } else {
                return -interpolatedScore;
            }
			 
        }	
    }
}
