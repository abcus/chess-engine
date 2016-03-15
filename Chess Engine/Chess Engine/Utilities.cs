using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Chess_Engine {
    internal static class Utilities {

        // Takes a midgame score and an endgame score and combines it into one 32-bit integet
        internal static int makeScore(int midScore, int endScore) {
            return ((midScore << 16) + endScore);
        }

        // Takes an integer encoding both a midgame and endgame score and returns the midgame score
        internal static Int32 midgameValue(Int32 score) {
            return (((score + 32768) & ~0xffff) / 0x10000);
        }

        // Takes an integer encoding both a midgame and endgame score and returns the endgame score
        internal static Int32 endgameValue(Int32 score) {
            return ((Int16)(score & 0xffff));
        }

        // Takes in an int for color and an int for piece type (without color encoding) and returns an int for piece type (with color encoding)
        internal static Int32 makePieceIndex(Int32 color, Int32 pieceType) {
            return (pieceType + color * Constants.WHITE_KING);
        }

        // Takes a square from one side's perspective and returns square from other side's perspective (flipped about line between 4th and 5th rank)
        internal static Int32 flipSquare(Int32 square) {
            return square ^ 56;
        }

        // apply_weight() applies an evaluation weight to a value trying to prevent overflow
        internal static Int32 applyWeight(Int32 v, Int32 w) {
            return (((((int)((((v + 32768) & ~0xffff) / 0x10000)) * (((w + 32768) & ~0xffff) / 0x10000)) / 0x100) << 16) + (((int)(((Int16)(v & 0xffff))) * ((Int16)(w & 0xffff))) / 0x100));
        }

        // PSQT [pieceType][Square] contains middlegame and endgame piece-square scores encoded as an integer
        // Defined for the white side
        // Index: 0 = empty, 1 = pawn, 2 = knight, 3 = bishiop, 4 = rook, 5 = queen, 6 = king
        // Squares from H1 - A8
        /*internal static readonly Int32[][] PIECE_SQUARE_TABLE = {
          new Int32[] { },

          new Int32[] { 
           Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore(0,  0), Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore(  0, 0),
           Utilities.makeScore(-28,-8), Utilities.makeScore(-6,-8), Utilities.makeScore( 4,-8), Utilities.makeScore(14,-8), Utilities.makeScore(14,-8), Utilities.makeScore( 4,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(-28,-8),
           Utilities.makeScore(-28,-8), Utilities.makeScore(-6,-8), Utilities.makeScore( 9,-8), Utilities.makeScore(36,-8), Utilities.makeScore(36,-8), Utilities.makeScore( 9,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(-28,-8),
           Utilities.makeScore(-28,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(17,-8), Utilities.makeScore(58,-8), Utilities.makeScore(58,-8), Utilities.makeScore(17,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(-28,-8),
           Utilities.makeScore(-28,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(17,-8), Utilities.makeScore(36,-8), Utilities.makeScore(36,-8), Utilities.makeScore(17,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(-28,-8),
           Utilities.makeScore(-28,-8), Utilities.makeScore(-6,-8), Utilities.makeScore( 9,-8), Utilities.makeScore(14,-8), Utilities.makeScore(14,-8), Utilities.makeScore( 9,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(-28,-8),
           Utilities.makeScore(-28,-8), Utilities.makeScore(-6,-8), Utilities.makeScore( 4,-8), Utilities.makeScore(14,-8), Utilities.makeScore(14,-8), Utilities.makeScore( 4,-8), Utilities.makeScore(-6,-8), Utilities.makeScore(-28,-8),
           Utilities.makeScore(  0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore(0,  0), Utilities.makeScore( 0, 0), Utilities.makeScore( 0, 0), Utilities.makeScore(  0, 0)
          },

          new Int32[]{ 
           Utilities.makeScore(-135,-104), Utilities.makeScore(-107,-79), Utilities.makeScore(-80,-55), Utilities.makeScore(-67,-42), Utilities.makeScore(-67,-42), Utilities.makeScore(-80,-55), Utilities.makeScore(-107,-79), Utilities.makeScore(-135,-104),
           Utilities.makeScore( -93, -79), Utilities.makeScore( -67,-55), Utilities.makeScore(-39,-30), Utilities.makeScore(-25,-17), Utilities.makeScore(-25,-17), Utilities.makeScore(-39,-30), Utilities.makeScore( -67,-55), Utilities.makeScore( -93, -79),
           Utilities.makeScore( -53, -55), Utilities.makeScore( -25,-30), Utilities.makeScore(  1, -6), Utilities.makeScore( 13,  5), Utilities.makeScore( 13,  5), Utilities.makeScore(  1, -6), Utilities.makeScore( -25,-30), Utilities.makeScore( -53, -55),
           Utilities.makeScore( -25, -42), Utilities.makeScore(   1,-17), Utilities.makeScore( 27,  5), Utilities.makeScore( 41, 18), Utilities.makeScore( 41, 18), Utilities.makeScore( 27,  5), Utilities.makeScore(   1,-17), Utilities.makeScore( -25, -42),
           Utilities.makeScore( -11, -42), Utilities.makeScore(  13,-17), Utilities.makeScore( 41,  5), Utilities.makeScore( 55, 18), Utilities.makeScore( 55, 18), Utilities.makeScore( 41,  5), Utilities.makeScore(  13,-17), Utilities.makeScore( -11, -42),
           Utilities.makeScore( -11, -55), Utilities.makeScore(  13,-30), Utilities.makeScore( 41, -6), Utilities.makeScore( 55,  5), Utilities.makeScore( 55,  5), Utilities.makeScore( 41, -6), Utilities.makeScore(  13,-30), Utilities.makeScore( -11, -55),
           Utilities.makeScore( -53, -79), Utilities.makeScore( -25,-55), Utilities.makeScore(  1,-30), Utilities.makeScore( 13,-17), Utilities.makeScore( 13,-17), Utilities.makeScore(  1,-30), Utilities.makeScore( -25,-55), Utilities.makeScore( -53, -79),
           Utilities.makeScore(-193,-104), Utilities.makeScore( -67,-79), Utilities.makeScore(-39,-55), Utilities.makeScore(-25,-42), Utilities.makeScore(-25,-42), Utilities.makeScore(-39,-55), Utilities.makeScore( -67,-79), Utilities.makeScore(-193,-104)
          },

          new Int32[]{ 
           Utilities.makeScore(-40,-59), Utilities.makeScore(-40,-42), Utilities.makeScore(-35,-35), Utilities.makeScore(-30,-26), Utilities.makeScore(-30,-26), Utilities.makeScore(-35,-35), Utilities.makeScore(-40,-42), Utilities.makeScore(-40,-59),
           Utilities.makeScore(-17,-42), Utilities.makeScore(  0,-26), Utilities.makeScore( -4,-18), Utilities.makeScore(  0,-11), Utilities.makeScore(  0,-11), Utilities.makeScore( -4,-18), Utilities.makeScore(  0,-26), Utilities.makeScore(-17,-42),
           Utilities.makeScore(-13,-35), Utilities.makeScore( -4,-18), Utilities.makeScore(  8,-11), Utilities.makeScore(  4, -4), Utilities.makeScore(  4, -4), Utilities.makeScore(  8,-11), Utilities.makeScore( -4,-18), Utilities.makeScore(-13,-35),
           Utilities.makeScore( -8,-26), Utilities.makeScore(  0,-11), Utilities.makeScore(  4, -4), Utilities.makeScore( 17,  4), Utilities.makeScore( 17,  4), Utilities.makeScore(  4, -4), Utilities.makeScore(  0,-11), Utilities.makeScore( -8,-26),
           Utilities.makeScore( -8,-26), Utilities.makeScore(  0,-11), Utilities.makeScore(  4, -4), Utilities.makeScore( 17,  4), Utilities.makeScore( 17,  4), Utilities.makeScore(  4, -4), Utilities.makeScore(  0,-11), Utilities.makeScore( -8,-26),
           Utilities.makeScore(-13,-35), Utilities.makeScore( -4,-18), Utilities.makeScore(  8,-11), Utilities.makeScore(  4, -4), Utilities.makeScore(  4, -4), Utilities.makeScore(  8,-11), Utilities.makeScore( -4,-18), Utilities.makeScore(-13,-35),
           Utilities.makeScore(-17,-42), Utilities.makeScore(  0,-26), Utilities.makeScore( -4,-18), Utilities.makeScore(  0,-11), Utilities.makeScore(  0,-11), Utilities.makeScore( -4,-18), Utilities.makeScore(  0,-26), Utilities.makeScore(-17,-42),
           Utilities.makeScore(-17,-59), Utilities.makeScore(-17,-42), Utilities.makeScore(-13,-35), Utilities.makeScore( -8,-26), Utilities.makeScore( -8,-26), Utilities.makeScore(-13,-35), Utilities.makeScore(-17,-42), Utilities.makeScore(-17,-59)
          },

          new Int32[]{ 
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3),
           Utilities.makeScore(-12, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(2, 3), Utilities.makeScore(-2, 3), Utilities.makeScore(-7, 3), Utilities.makeScore(-12, 3)
          },

          new Int32[]{ 
           Utilities.makeScore(8,-80), Utilities.makeScore(8,-54), Utilities.makeScore(8,-42), Utilities.makeScore(8,-30), Utilities.makeScore(8,-30), Utilities.makeScore(8,-42), Utilities.makeScore(8,-54), Utilities.makeScore(8,-80),
           Utilities.makeScore(8,-54), Utilities.makeScore(8,-30), Utilities.makeScore(8,-18), Utilities.makeScore(8, -6), Utilities.makeScore(8, -6), Utilities.makeScore(8,-18), Utilities.makeScore(8,-30), Utilities.makeScore(8,-54),
           Utilities.makeScore(8,-42), Utilities.makeScore(8,-18), Utilities.makeScore(8, -6), Utilities.makeScore(8,  6), Utilities.makeScore(8,  6), Utilities.makeScore(8, -6), Utilities.makeScore(8,-18), Utilities.makeScore(8,-42),
           Utilities.makeScore(8,-30), Utilities.makeScore(8, -6), Utilities.makeScore(8,  6), Utilities.makeScore(8, 18), Utilities.makeScore(8, 18), Utilities.makeScore(8,  6), Utilities.makeScore(8, -6), Utilities.makeScore(8,-30),
           Utilities.makeScore(8,-30), Utilities.makeScore(8, -6), Utilities.makeScore(8,  6), Utilities.makeScore(8, 18), Utilities.makeScore(8, 18), Utilities.makeScore(8,  6), Utilities.makeScore(8, -6), Utilities.makeScore(8,-30),
           Utilities.makeScore(8,-42), Utilities.makeScore(8,-18), Utilities.makeScore(8, -6), Utilities.makeScore(8,  6), Utilities.makeScore(8,  6), Utilities.makeScore(8, -6), Utilities.makeScore(8,-18), Utilities.makeScore(8,-42),
           Utilities.makeScore(8,-54), Utilities.makeScore(8,-30), Utilities.makeScore(8,-18), Utilities.makeScore(8, -6), Utilities.makeScore(8, -6), Utilities.makeScore(8,-18), Utilities.makeScore(8,-30), Utilities.makeScore(8,-54),
           Utilities.makeScore(8,-80), Utilities.makeScore(8,-54), Utilities.makeScore(8,-42), Utilities.makeScore(8,-30), Utilities.makeScore(8,-30), Utilities.makeScore(8,-42), Utilities.makeScore(8,-54), Utilities.makeScore(8,-80)
          },

          new Int32[]{ 
           Utilities.makeScore(287, 18), Utilities.makeScore(311, 77), Utilities.makeScore(262,105), Utilities.makeScore(214,135), Utilities.makeScore(214,135), Utilities.makeScore(262,105), Utilities.makeScore(311, 77), Utilities.makeScore(287, 18),
           Utilities.makeScore(262, 77), Utilities.makeScore(287,135), Utilities.makeScore(238,165), Utilities.makeScore(190,193), Utilities.makeScore(190,193), Utilities.makeScore(238,165), Utilities.makeScore(287,135), Utilities.makeScore(262, 77),
           Utilities.makeScore(214,105), Utilities.makeScore(238,165), Utilities.makeScore(190,193), Utilities.makeScore(142,222), Utilities.makeScore(142,222), Utilities.makeScore(190,193), Utilities.makeScore(238,165), Utilities.makeScore(214,105),
           Utilities.makeScore(190,135), Utilities.makeScore(214,193), Utilities.makeScore(167,222), Utilities.makeScore(119,251), Utilities.makeScore(119,251), Utilities.makeScore(167,222), Utilities.makeScore(214,193), Utilities.makeScore(190,135),
           Utilities.makeScore(167,135), Utilities.makeScore(190,193), Utilities.makeScore(142,222), Utilities.makeScore( 94,251), Utilities.makeScore( 94,251), Utilities.makeScore(142,222), Utilities.makeScore(190,193), Utilities.makeScore(167,135),
           Utilities.makeScore(142,105), Utilities.makeScore(167,165), Utilities.makeScore(119,193), Utilities.makeScore( 69,222), Utilities.makeScore( 69,222), Utilities.makeScore(119,193), Utilities.makeScore(167,165), Utilities.makeScore(142,105),
           Utilities.makeScore(119, 77), Utilities.makeScore(142,135), Utilities.makeScore( 94,165), Utilities.makeScore( 46,193), Utilities.makeScore( 46,193), Utilities.makeScore( 94,165), Utilities.makeScore(142,135), Utilities.makeScore(119, 77),
           Utilities.makeScore(94,  18), Utilities.makeScore(119, 77), Utilities.makeScore( 69,105), Utilities.makeScore( 21,135), Utilities.makeScore( 21,135), Utilities.makeScore( 69,105), Utilities.makeScore(119, 77), Utilities.makeScore( 94, 18)
          }
        };*/

        internal static readonly Int32[][] PIECE_SQUARE_TABLE = {
            new Int32[] {},

            new Int32[] {Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(10, 0),Utilities.makeScore(20, 0),Utilities.makeScore(20, 0),Utilities.makeScore(10, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(20, 0),Utilities.makeScore(40, 0),Utilities.makeScore(40, 0),Utilities.makeScore(20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(10, 0),Utilities.makeScore(20, 0),Utilities.makeScore(20, 0),Utilities.makeScore(10, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(-20, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0),Utilities.makeScore(0, 0)},
            
            new Int32[] {Utilities.makeScore(-144, -98),Utilities.makeScore(-109, -83),Utilities.makeScore(-85, -51),Utilities.makeScore(-73, -16),Utilities.makeScore(-73, -16),Utilities.makeScore(-85, -51),Utilities.makeScore(-109, -83),Utilities.makeScore(-144, -98),Utilities.makeScore(-88, -68),Utilities.makeScore(-43, -53),Utilities.makeScore(-19, -21),Utilities.makeScore(-7, 14),Utilities.makeScore(-7, 14),Utilities.makeScore(-19, -21),Utilities.makeScore(-43, -53),Utilities.makeScore(-88, -68),Utilities.makeScore(-69, -53),Utilities.makeScore(-24, -38),Utilities.makeScore(0, -6),Utilities.makeScore(12, 29),Utilities.makeScore(12, 29),Utilities.makeScore(0, -6),Utilities.makeScore(-24, -38),Utilities.makeScore(-69, -53),Utilities.makeScore(-28, -42),Utilities.makeScore(17, -27),Utilities.makeScore(41, 5),Utilities.makeScore(53, 40),Utilities.makeScore(53, 40),Utilities.makeScore(41, 5),Utilities.makeScore(17, -27),Utilities.makeScore(-28, -42),Utilities.makeScore(-30, -42),Utilities.makeScore(15, -27),Utilities.makeScore(39, 5),Utilities.makeScore(51, 40),Utilities.makeScore(51, 40),Utilities.makeScore(39, 5),Utilities.makeScore(15, -27),Utilities.makeScore(-30, -42),Utilities.makeScore(-10, -53),Utilities.makeScore(35, -38),Utilities.makeScore(59, -6),Utilities.makeScore(71, 29),Utilities.makeScore(71, 29),Utilities.makeScore(59, -6),Utilities.makeScore(35, -38),Utilities.makeScore(-10, -53),Utilities.makeScore(-64, -68),Utilities.makeScore(-19, -53),Utilities.makeScore(5, -21),Utilities.makeScore(17, 14),Utilities.makeScore(17, 14),Utilities.makeScore(5, -21),Utilities.makeScore(-19, -53),Utilities.makeScore(-64, -68),Utilities.makeScore(-200, -98),Utilities.makeScore(-65, -83),Utilities.makeScore(-41, -51),Utilities.makeScore(-29, -16),Utilities.makeScore(-29, -16),Utilities.makeScore(-41, -51),Utilities.makeScore(-65, -83),Utilities.makeScore(-200, -98)},
            
            new Int32[] {Utilities.makeScore(-54, -65),Utilities.makeScore(-27, -42),Utilities.makeScore(-34, -44),Utilities.makeScore(-43, -26),Utilities.makeScore(-43, -26),Utilities.makeScore(-34, -44),Utilities.makeScore(-27, -42),Utilities.makeScore(-54, -65),Utilities.makeScore(-29, -43),Utilities.makeScore(8, -20),Utilities.makeScore(1, -22),Utilities.makeScore(-8, -4),Utilities.makeScore(-8, -4),Utilities.makeScore(1, -22),Utilities.makeScore(8, -20),Utilities.makeScore(-29, -43),Utilities.makeScore(-20, -33),Utilities.makeScore(17, -10),Utilities.makeScore(10, -12),Utilities.makeScore(1, 6),Utilities.makeScore(1, 6),Utilities.makeScore(10, -12),Utilities.makeScore(17, -10),Utilities.makeScore(-20, -33),Utilities.makeScore(-19, -35),Utilities.makeScore(18, -12),Utilities.makeScore(11, -14),Utilities.makeScore(2, 4),Utilities.makeScore(2, 4),Utilities.makeScore(11, -14),Utilities.makeScore(18, -12),Utilities.makeScore(-19, -35),Utilities.makeScore(-22, -35),Utilities.makeScore(15, -12),Utilities.makeScore(8, -14),Utilities.makeScore(-1, 4),Utilities.makeScore(-1, 4),Utilities.makeScore(8, -14),Utilities.makeScore(15, -12),Utilities.makeScore(-22, -35),Utilities.makeScore(-28, -33),Utilities.makeScore(9, -10),Utilities.makeScore(2, -12),Utilities.makeScore(-7, 6),Utilities.makeScore(-7, 6),Utilities.makeScore(2, -12),Utilities.makeScore(9, -10),Utilities.makeScore(-28, -33),Utilities.makeScore(-32, -43),Utilities.makeScore(5, -20),Utilities.makeScore(-2, -22),Utilities.makeScore(-11, -4),Utilities.makeScore(-11, -4),Utilities.makeScore(-2, -22),Utilities.makeScore(5, -20),Utilities.makeScore(-32, -43),Utilities.makeScore(-49, -65),Utilities.makeScore(-22, -42),Utilities.makeScore(-29, -44),Utilities.makeScore(-38, -26),Utilities.makeScore(-38, -26),Utilities.makeScore(-29, -44),Utilities.makeScore(-22, -42),Utilities.makeScore(-49, -6530)},
            
            new Int32[] {Utilities.makeScore(-22, 3),Utilities.makeScore(-17, 3),Utilities.makeScore(-12, 3),Utilities.makeScore(-8, 3),Utilities.makeScore(-8, 3),Utilities.makeScore(-12, 3),Utilities.makeScore(-17, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(2, 3),Utilities.makeScore(-2, 3),Utilities.makeScore(-7, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-11, 3),Utilities.makeScore(4, 3),Utilities.makeScore(9, 3),Utilities.makeScore(13, 3),Utilities.makeScore(13, 3),Utilities.makeScore(9, 3),Utilities.makeScore(4, 3),Utilities.makeScore(-11, 3),Utilities.makeScore(-22, 3),Utilities.makeScore(-17, 3),Utilities.makeScore(-12, 3),Utilities.makeScore(-8, 3),Utilities.makeScore(-8, 3),Utilities.makeScore(-12, 3),Utilities.makeScore(-17, 3),Utilities.makeScore(-22, 3)},
            
            new Int32[] {Utilities.makeScore(-2, -80),Utilities.makeScore(-2, -54),Utilities.makeScore(-2, -42),Utilities.makeScore(-2, -30),Utilities.makeScore(-2, -30),Utilities.makeScore(-2, -42),Utilities.makeScore(-2, -54),Utilities.makeScore(-2, -80),Utilities.makeScore(-2, -54),Utilities.makeScore(8, -30),Utilities.makeScore(8, -18),Utilities.makeScore(8, -6),Utilities.makeScore(8, -6),Utilities.makeScore(8, -18),Utilities.makeScore(8, -30),Utilities.makeScore(-2, -54),Utilities.makeScore(-2, -42),Utilities.makeScore(8, -18),Utilities.makeScore(8, -6),Utilities.makeScore(8, 6),Utilities.makeScore(8, 6),Utilities.makeScore(8, -6),Utilities.makeScore(8, -18),Utilities.makeScore(-2, -42),Utilities.makeScore(-2, -30),Utilities.makeScore(8, -6),Utilities.makeScore(8, 6),Utilities.makeScore(8, 18),Utilities.makeScore(8, 18),Utilities.makeScore(8, 6),Utilities.makeScore(8, -6),Utilities.makeScore(-2, -30),Utilities.makeScore(-2, -30),Utilities.makeScore(8, -6),Utilities.makeScore(8, 6),Utilities.makeScore(8, 18),Utilities.makeScore(8, 18),Utilities.makeScore(8, 6),Utilities.makeScore(8, -6),Utilities.makeScore(-2, -30),Utilities.makeScore(-2, -42),Utilities.makeScore(8, -18),Utilities.makeScore(8, -6),Utilities.makeScore(8, 6),Utilities.makeScore(8, 6),Utilities.makeScore(8, -6),Utilities.makeScore(8, -18),Utilities.makeScore(-2, -42),Utilities.makeScore(-2, -54),Utilities.makeScore(8, -30),Utilities.makeScore(8, -18),Utilities.makeScore(8, -6),Utilities.makeScore(8, -6),Utilities.makeScore(8, -18),Utilities.makeScore(8, -30),Utilities.makeScore(-2, -54),Utilities.makeScore(-2, -80),Utilities.makeScore(-2, -54),Utilities.makeScore(-2, -42),Utilities.makeScore(-2, -30),Utilities.makeScore(-2, -30),Utilities.makeScore(-2, -42),Utilities.makeScore(-2, -54),Utilities.makeScore(-2, -80)},
        
            new Int32[] {Utilities.makeScore(298, 27),Utilities.makeScore(332, 81),Utilities.makeScore(273, 108),Utilities.makeScore(225, 116),Utilities.makeScore(225, 116),Utilities.makeScore(273, 108),Utilities.makeScore(332, 81),Utilities.makeScore(298, 27),Utilities.makeScore(287, 74),Utilities.makeScore(321, 128),Utilities.makeScore(262, 155),Utilities.makeScore(214, 163),Utilities.makeScore(214, 163),Utilities.makeScore(262, 155),Utilities.makeScore(321, 128),Utilities.makeScore(287, 74),Utilities.makeScore(224, 111),Utilities.makeScore(258, 165),Utilities.makeScore(199, 192),Utilities.makeScore(151, 200),Utilities.makeScore(151, 200),Utilities.makeScore(199, 192),Utilities.makeScore(258, 165),Utilities.makeScore(224, 111),Utilities.makeScore(196, 135),Utilities.makeScore(230, 189),Utilities.makeScore(171, 216),Utilities.makeScore(123, 224),Utilities.makeScore(123, 224),Utilities.makeScore(171, 216),Utilities.makeScore(230, 189),Utilities.makeScore(196, 135),Utilities.makeScore(173, 135),Utilities.makeScore(207, 189),Utilities.makeScore(148, 216),Utilities.makeScore(100, 224),Utilities.makeScore(100, 224),Utilities.makeScore(148, 216),Utilities.makeScore(207, 189),Utilities.makeScore(173, 135),Utilities.makeScore(146, 111),Utilities.makeScore(180, 165),Utilities.makeScore(121, 192),Utilities.makeScore(73, 200),Utilities.makeScore(73, 200),Utilities.makeScore(121, 192),Utilities.makeScore(180, 165),Utilities.makeScore(146, 111),Utilities.makeScore(119, 74),Utilities.makeScore(153, 128),Utilities.makeScore(94, 155),Utilities.makeScore(46, 163),Utilities.makeScore(46, 163),Utilities.makeScore(94, 155),Utilities.makeScore(153, 128),Utilities.makeScore(119, 74),Utilities.makeScore(98, 27),Utilities.makeScore(132, 81),Utilities.makeScore(73, 108),Utilities.makeScore(25, 116),Utilities.makeScore(25, 116),Utilities.makeScore(73, 108),Utilities.makeScore(132, 81),Utilities.makeScore(98, 27)}
        };
    }
}
