using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {
    internal class PawnEntry {

        internal UInt64 key;
        internal UInt64 passedPawnsWhite, passedPawnsBlack;
        internal UInt64 pawnAttacksWhite, pawnAttacksBlack;
        internal Int32 kingSquaresWhite, kingSquaresBlack;
        internal Int32 halfOpenFilesWhite, halfOpenFilesBlack;
        internal Int32 kingSafetyWhite, kingSafetyBlack;
        internal Int32 castleRightsWhiteShort, castleRightsWhiteLong, castleRightsBlackShort, castleRightsBlackLong;
        internal Int32 value;

        internal Int32 pawnValue() {
            return value;
        }

        // Returns bitboard of passed pawns
        internal UInt64 passedPawn(int color) {
            return color == Constants.WHITE ? passedPawnsWhite : passedPawnsBlack;
        }

        internal Int32 kingSafety(Int32 color, Board inputBoard, Int32 kingSquare) {
            if (color == Constants.WHITE) {
                return ((kingSquaresWhite == kingSquare) && (castleRightsWhiteShort == inputBoard.whiteShortCastleRights) && (castleRightsWhiteLong == inputBoard.whiteLongCastleRights))
                    ? kingSafetyWhite : updateSafetyWhite(inputBoard, kingSquare);
            } else {
                return ((kingSquaresBlack == kingSquare) && (castleRightsBlackShort == inputBoard.blackShortCastleRights) && (castleRightsBlackLong == inputBoard.blackLongCastleRights))
                    ? kingSafetyBlack : updateSafetyBlack(inputBoard, kingSquare);
            }
        }

        // Calculates shelter and storm penalties for the file the king is on, and the two adjacent files
        internal static Int32 shelterStorm(Int32 color, Board inputBoard, Int32 kingSquare) {
            return 0;

        }

        // Calculates a bonus for king safety, called when the king square changes (occurs in 20% of kingSafety() calls)
        internal Int32 updateSafetyWhite(Board inputBoard, Int32 kingSquare) {
            kingSquaresWhite = kingSquare; // updates kingSquareWhite
            return 0;
        }

        internal Int32 updateSafetyBlack(Board inputBoard, Int32 kingSquare) {
            return 0;
        }
    }
}