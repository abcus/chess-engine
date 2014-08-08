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
            if (inputBoard.sideToMove == Constants.WHITE) {
                return interpolatedScore;
            } else {
                return -interpolatedScore;
            }
        }	
    }
}
