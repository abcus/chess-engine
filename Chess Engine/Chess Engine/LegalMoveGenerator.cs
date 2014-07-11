using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {

    class LegalMoveGenerator {

        public static Boolean kingInCheck(Board inputBoard, int colourOfKingToCheck) {
            
            //gets side to move and array of bitboards
            int sideToMove = inputBoard.getSideToMove();
            ulong[] bitboardArray = inputBoard.getArrayOfPieceBitboards();
            ulong[] aggregateBitboardArray = inputBoard.getArrayOfAggregatePieceBitboards();

            if (colourOfKingToCheck == Constants.WHITE) {

                //determines the index of the white king
                ulong whiteKingBitboard = bitboardArray[Constants.WHITE_KING - 1];
                int indexOfWhiteKing = Constants.bitScan(whiteKingBitboard).ElementAt(0);

                //Looks up horizontal/vertical attack set from king position, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = aggregateBitboardArray[2] & Constants.rookOccupancyMask[indexOfWhiteKing];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy*Constants.rookMagicNumbers[indexOfWhiteKing]) >> Constants.rookMagicShiftNumber[indexOfWhiteKing]);
                ulong rookMovesFromKingPosition = Constants.rookMoves[indexOfWhiteKing][rookMoveIndex];

                if (((rookMovesFromKingPosition & bitboardArray[Constants.BLACK_ROOK - 1]) != 0) || ((rookMovesFromKingPosition & bitboardArray[Constants.BLACK_QUEEN -1]) != 0)) {
                    return true;
                }
                //Looks up diagonal attack set from king position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = aggregateBitboardArray[2] & Constants.bishopOccupancyMask[indexOfWhiteKing];
                int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[indexOfWhiteKing]) >> Constants.bishopMagicShiftNumber[indexOfWhiteKing]);
                ulong bishopMovesFromKingPosition = Constants.bishopMoves[indexOfWhiteKing][bishopMoveIndex];

                if (((bishopMovesFromKingPosition & bitboardArray[Constants.BLACK_BISHOP - 1]) != 0) || ((bishopMovesFromKingPosition & bitboardArray[Constants.BLACK_QUEEN - 1]) != 0)) {
                    return true;
                }
                //Looks up knight attack set from king position, and intersects with opponent's knight bitboard
                ulong knightMoveFromKingPosition = Constants.knightMoves[indexOfWhiteKing];

                if ((knightMoveFromKingPosition & bitboardArray[Constants.BLACK_KNIGHT - 1]) != 0) {
                    return true;
                }
                //Looks up white pawn attack set from king position, and intersects with opponent's pawn bitboard
                ulong whitePawnMoveFromKingPosition = Constants.whiteCapturesAndCapturePromotions[indexOfWhiteKing];

                if ((whitePawnMoveFromKingPosition & bitboardArray[Constants.BLACK_PAWN - 1]) != 0) {
                    return true;
                }
                //Looks up king attack set from king position, and intersects with opponent's king bitboard
                ulong kingMoveFromKingPosition = Constants.kingMoves[indexOfWhiteKing];

                if ((kingMoveFromKingPosition & bitboardArray[Constants.BLACK_KING - 1]) != 0) {
                    return true;
                } 
                //If all of the intersections resulted in 0, then the king is not in check, and we return false
                return false;
            }

            else if (colourOfKingToCheck == Constants.BLACK) {
                //determines the index of the black king
                ulong blackKingBitboard = bitboardArray[Constants.BLACK_KING - 1];
                int indexOfBlackKing = Constants.bitScan(blackKingBitboard).ElementAt(0);

                //Looks up horizontal/vertical attack set from king position, and intersects with white's rook/queen bitboard
                ulong horizontalVerticalOccupancy = aggregateBitboardArray[2] & Constants.rookOccupancyMask[indexOfBlackKing];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[indexOfBlackKing]) >> Constants.rookMagicShiftNumber[indexOfBlackKing]);
                ulong rookMovesFromKingPosition = Constants.rookMoves[indexOfBlackKing][rookMoveIndex];

                if (((rookMovesFromKingPosition & bitboardArray[Constants.WHITE_ROOK - 1]) != 0) || ((rookMovesFromKingPosition & bitboardArray[Constants.WHITE_QUEEN - 1]) != 0)) {
                    return true;
                }
                //Looks up diagonal attack set from king position, and intersects with white's bishop/queen bitboard
                ulong diagonalOccupancy = aggregateBitboardArray[2] & Constants.bishopOccupancyMask[indexOfBlackKing];
                int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[indexOfBlackKing]) >> Constants.bishopMagicShiftNumber[indexOfBlackKing]);
                ulong bishopMovesFromKingPosition = Constants.bishopMoves[indexOfBlackKing][bishopMoveIndex];

                if (((bishopMovesFromKingPosition & bitboardArray[Constants.WHITE_BISHOP - 1]) != 0) || ((bishopMovesFromKingPosition & bitboardArray[Constants.WHITE_QUEEN - 1]) != 0)) {
                    return true;
                }
                //Looks up knight attack set from king position, and intersects with opponent's knight bitboard
                ulong knightMoveFromKingPosition = Constants.knightMoves[indexOfBlackKing];

                if ((knightMoveFromKingPosition & bitboardArray[Constants.WHITE_KNIGHT - 1]) != 0) {
                    return true;
                }
                //Looks up white pawn attack set from king position, and intersects with opponent's pawn bitboard
                ulong blackPawnMoveFromKingPosition = Constants.blackCapturesAndCapturePromotions[indexOfBlackKing];

                if ((blackPawnMoveFromKingPosition & bitboardArray[Constants.WHITE_PAWN - 1]) != 0) {
                    return true;
                }
                //Looks up king attack set from king position, and intersects with opponent's king bitboard
                ulong kingMoveFromKingPosition = Constants.kingMoves[indexOfBlackKing];

                if ((kingMoveFromKingPosition & bitboardArray[Constants.WHITE_KING - 1]) != 0) {
                    return true;
                }
                //If all of the intersections resulted in 0, then the king is not in check, and we return false
                return false;
            }

            return false;
        }

        public static List<uint> generateListOfLegalMoves(Board inputBoard) {
            List<uint> listOfLegalMoves = new List<uint>();
            int sideToMove = inputBoard.getSideToMove();
            ulong[] bitboardArray = inputBoard.getArrayOfPieceBitboards();
            ulong[] aggregateBitboardArray = inputBoard.getArrayOfAggregatePieceBitboards();
            int[] pieceArray = inputBoard.getPieceArray();

            //Checks to see if the king is in check in the current position
            Boolean isKingInCheck = kingInCheck(inputBoard, sideToMove);

            //Generates legal knight moves and adds them to the list
            generateListOfKnightMovesAndCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);

            //Generates legal king moves and adds them to the list
            generateListOfKingMovesAndCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);

            //Generates legal pawn moves and adds them to the list
            generateListOfPawnSingleMoves(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);
            generateListOfPawnDoubleMoves(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);
            generateListOfPawnCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);
            generateListOfPawnEnPassantCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);
            generateListOfPawnPromotions(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);
            generateListOfPawnPromotionCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);

            //Generates legal bishop moves and adds them to the list
            generateListOfBishipMovesAndCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);

            //Generates legal rook moves and adds them to the list
            generateListOfRookMovesAndCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);

            //Generates legal queen moves and adds them to the list
            generateListOfQueenMovesAndCaptures(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);

            //If the king is not in check, then generates legal castling moves and adds them to the list
            if (isKingInCheck == false) {
                generateListOfKingCastling(inputBoard, sideToMove, bitboardArray, aggregateBitboardArray, pieceArray, listOfLegalMoves);
            }

            //returns the list of legal moves
            return listOfLegalMoves;
        }

        //Generates legal knight moves and captures and adds them to the list of legal moves
        private static void generateListOfKnightMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            
            if (sideToMove == Constants.WHITE) {
                ulong whiteKnightBitboard = bitboardArrayInput[Constants.WHITE_KNIGHT - 1];
                List<int> indicesOfWhiteKnight = Constants.bitScan(whiteKnightBitboard);
                
                foreach (int knightIndex in indicesOfWhiteKnight) {
                    ulong knightMovementFromIndex = Constants.knightMoves[knightIndex];
                    ulong pseudoLegalKnightMovementFromIndex = knightMovementFromIndex &= (~whitePieces);
                    List<int> indicesOfWhiteKnightMovesFromIndex = Constants.bitScan(pseudoLegalKnightMovementFromIndex);
                    foreach (int knightMoveIndex in indicesOfWhiteKnightMovesFromIndex) {

                        uint moveRepresentation = 0x0;
                        
                        if (pieceArrayInput[knightMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_KNIGHT, knightIndex, knightMoveIndex,Constants.QUIET_MOVE, Constants.EMPTY);
                        } else if (pieceArrayInput[knightMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_KNIGHT, knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArrayInput[knightMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                    
                }

            } else if (sideToMove == Constants.BLACK) {
                ulong blackKnightBitboard = bitboardArrayInput[Constants.BLACK_KNIGHT - 1];
                List<int> indicesOfBlackKnight = Constants.bitScan(blackKnightBitboard);

                foreach (int knightIndex in indicesOfBlackKnight) {
                    ulong knightMovementFromIndex = Constants.knightMoves[knightIndex];
                    ulong pseudoLegalKnightMovementFromIndex = knightMovementFromIndex &= (~blackPieces);
                    List<int> indicesOfBlackKnightMovesFromIndex = Constants.bitScan(pseudoLegalKnightMovementFromIndex);
                    foreach (int knightMoveIndex in indicesOfBlackKnightMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[knightMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_KNIGHT, knightIndex, knightMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        } else if (pieceArrayInput[knightMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_KNIGHT, knightIndex, knightMoveIndex, Constants.CAPTURE, pieceArrayInput[knightMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            }
        }

        private static void generateListOfKingMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            
            if (sideToMove == Constants.WHITE) {
                ulong whiteKingBitboard = bitboardArrayInput[Constants.WHITE_KING - 1];
                List<int> indicesOfWhiteKing = Constants.bitScan(whiteKingBitboard);

                foreach (int kingIndex in indicesOfWhiteKing) {
                    ulong kingMovementFromIndex = Constants.kingMoves[kingIndex];
                    ulong pseudoLegalKingMovementFromIndex = kingMovementFromIndex &= (~whitePieces);
                    List<int> indicesOfWhiteKingMovesFromIndex = Constants.bitScan(pseudoLegalKingMovementFromIndex);
                    foreach (int kingMoveIndex in indicesOfWhiteKingMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[kingMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_KING, kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        }
                        else if (pieceArrayInput[kingMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_KING, kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArrayInput[kingMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            } else if (sideToMove == Constants.BLACK) {
                ulong blackKingBitboard = bitboardArrayInput[Constants.BLACK_KING - 1];
                List<int> indicesOfBlackKing = Constants.bitScan(blackKingBitboard);

                foreach (int kingIndex in indicesOfBlackKing) {
                    ulong kingMovementFromIndex = Constants.kingMoves[kingIndex];
                    ulong pseudoLegalKingMovementFromIndex = kingMovementFromIndex &= (~blackPieces);
                    List<int> indicesOfBlackKingMovesFromIndex = Constants.bitScan(pseudoLegalKingMovementFromIndex);
                    foreach (int kingMoveIndex in indicesOfBlackKingMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[kingMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_KING, kingIndex, kingMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        } else if (pieceArrayInput[kingMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_KING, kingIndex, kingMoveIndex, Constants.CAPTURE, pieceArrayInput[kingMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            }
        }

        private static void generateListOfPawnSingleMoves(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong allPieces = aggregateBitboardArray[2];
            
            if (sideToMove == Constants.WHITE) {
                ulong whitePawnBitboard = bitboardArrayInput[Constants.WHITE_PAWN - 1];
                ulong whitePawnBitboardOnSecontToSixthRank = whitePawnBitboard &= (Constants.RANK_2 | Constants.RANK_3 | Constants.RANK_4 | Constants.RANK_5 | Constants.RANK_6);
                List<int> indicesOfWhitePawn = Constants.bitScan(whitePawnBitboardOnSecontToSixthRank);

                foreach (int pawnIndex in indicesOfWhitePawn) {
                    ulong pawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (~allPieces);
                    List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex) {

                        uint moveRepresentation = 0x0;
                        moveRepresentation = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                      
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }
            } else if (sideToMove == Constants.BLACK) {
                ulong blackPawnBitboard = bitboardArrayInput[Constants.BLACK_PAWN - 1];
                ulong blackPawnBitboardOnThirdToSeventhRank = blackPawnBitboard &= (Constants.RANK_3 | Constants.RANK_4 | Constants.RANK_5 | Constants.RANK_6 | Constants.RANK_7);
                List<int> indicesOfBlackPawn = Constants.bitScan(blackPawnBitboard);

                foreach (int pawnIndex in indicesOfBlackPawn) {
                    ulong pawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (~allPieces);
                    List<int> indicesOfBlackPawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfBlackPawnMovesFromIndex) {

                        uint moveRepresentation = 0x0;
                        moveRepresentation = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);

                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            }
        }

        private static void generateListOfPawnDoubleMoves(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong allPieces = aggregateBitboardArray[2];
            
            if (sideToMove == Constants.WHITE) {
                ulong whitePawnBitboard = bitboardArrayInput[Constants.WHITE_PAWN - 1];
                ulong whitePawnBitboardOnSecondRank = whitePawnBitboard &= Constants.RANK_2;
                List<int> indicesOfWhitePawn = Constants.bitScan(whitePawnBitboardOnSecondRank);

                foreach (int pawnIndex in indicesOfWhitePawn) {
                    ulong singlePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                    ulong pseudoLegalSinglePawnMovementFromIndex = singlePawnMovementFromIndex &= (~allPieces);
                    if (pseudoLegalSinglePawnMovementFromIndex != 0) {
                        ulong doublePawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex + 8];
                        ulong pseudoLegalDoublePawnMovementFromIndex = doublePawnMovementFromIndex &= (~allPieces);

                        List<int> indicesOfWhitePawnDoubleMovesFromIndex = Constants.bitScan(pseudoLegalDoublePawnMovementFromIndex);
                        foreach (int pawnDoubleMoveIndex in indicesOfWhitePawnDoubleMovesFromIndex) {

                            uint moveRepresentation = 0x0;
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnDoubleMoveIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);

                            uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                            if (kingInCheck(inputBoard, Constants.WHITE) == false) {
                                listOfLegalMoves.Add(moveRepresentation);
                            }
                            Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                        }
                    }
                }
            } else if (sideToMove == Constants.BLACK) {
                ulong blackPawnBitboard = bitboardArrayInput[Constants.BLACK_PAWN - 1];
                ulong blackPawnBitboardOnSeventhRank = blackPawnBitboard &= Constants.RANK_7;
                List<int> indicesOfBlackPawn = Constants.bitScan(blackPawnBitboardOnSeventhRank);

                foreach (int pawnIndex in indicesOfBlackPawn) {
                    ulong singlePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                    ulong pseudoLegalSinglePawnMovementFromIndex = singlePawnMovementFromIndex &= (~allPieces);
                    if (pseudoLegalSinglePawnMovementFromIndex != 0) {
                        ulong doublePawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex - 8];
                        ulong pseudoLegalDoublePawnMovementFromIndex = doublePawnMovementFromIndex &= (~allPieces);

                        List<int> indicesOfBlackPawnDoubleMovesFromIndex = Constants.bitScan(pseudoLegalDoublePawnMovementFromIndex);
                        foreach (int pawnDoubleMoveIndex in indicesOfBlackPawnDoubleMovesFromIndex) {

                            uint moveRepresentation = 0x0;
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnDoubleMoveIndex, Constants.DOUBLE_PAWN_PUSH, Constants.EMPTY);

                            uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                            if (kingInCheck(inputBoard, Constants.BLACK) == false) {
                                listOfLegalMoves.Add(moveRepresentation);
                            }
                            Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                        }
                    }
                }
            }
        }

        private static void generateListOfPawnCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            
            if (sideToMove == Constants.WHITE) {

                ulong whitePawnBitboard = bitboardArrayInput[Constants.WHITE_PAWN - 1];
                ulong whitePawnBitboardOnSecondToSixthRank = whitePawnBitboard &= (Constants.RANK_2 | Constants.RANK_3 | Constants.RANK_4 | Constants.RANK_5 | Constants.RANK_6);
                List<int> indicesOfWhitePawn = Constants.bitScan(whitePawnBitboardOnSecondToSixthRank);
                
                foreach (int pawnIndex in indicesOfWhitePawn) {
                    ulong pawnMovementFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (~whitePieces);
                    List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                         if (pieceArrayInput[pawnMoveIndex] != Constants.EMPTY) 
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }

            } else if (sideToMove == Constants.BLACK) {
               
                ulong blackPawnBitboard = bitboardArrayInput[Constants.BLACK_PAWN - 1];
                ulong blackPawnBitboardOnThirdToSeventhRank = blackPawnBitboard &= (Constants.RANK_3 | Constants.RANK_4 | Constants.RANK_5 | Constants.RANK_6 | Constants.RANK_7);
                List<int> indicesOfBlackPawn = Constants.bitScan(blackPawnBitboardOnThirdToSeventhRank);

                foreach (int pawnIndex in indicesOfBlackPawn) {
                    ulong pawnMovementFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (~blackPieces);
                    List<int> indicesOfBlackPawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfBlackPawnMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[pawnMoveIndex] != Constants.EMPTY)
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArrayInput[pawnMoveIndex]);

                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == false) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }
            }
        }

        private static void generateListOfPawnEnPassantCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {
            
            //only if en passant variable is true
            
            if (sideToMove == Constants.WHITE) {
                //only if pawn is on 5th rank
            } else if (sideToMove == Constants.BLACK) {
                //only if pawn is on 4th rank
            }
        }

        private static void generateListOfPawnPromotions(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {
            
            ulong allPieces = aggregateBitboardArray[2];

            if (sideToMove == Constants.WHITE) {
                ulong whitePawnBitboard = bitboardArrayInput[Constants.WHITE_PAWN - 1];
                ulong whitePawnBitboardOnSeventhRank = whitePawnBitboard &= Constants.RANK_7;
                List<int> indicesOfWhitePawn = Constants.bitScan(whitePawnBitboardOnSeventhRank);

                foreach (int pawnIndex in indicesOfWhitePawn) {
                    ulong pawnMovementFromIndex = Constants.whiteSinglePawnMovesAndPromotionMoves[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (~allPieces);
                    List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex) {

                        uint moveRepresentationKnightPromotion = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.KNIGHT_PROMOTION, Constants.EMPTY);
                        uint moveRepresentationBishopPromotion = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.BISHOP_PROMOTION, Constants.EMPTY);
                        uint moveRepresentationRookPromotion = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.ROOK_PROMOTION, Constants.EMPTY);
                        uint moveRepresentationQueenPromotion = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.QUEEN_PROMOTION, Constants.EMPTY);

                        //Only have to check one of the four promotion types to see if it leaves the king in check
                        uint boardRestoreData = Move.makeMove(moveRepresentationKnightPromotion, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == false) {
                            listOfLegalMoves.Add(moveRepresentationKnightPromotion);
                            listOfLegalMoves.Add(moveRepresentationBishopPromotion);
                            listOfLegalMoves.Add(moveRepresentationRookPromotion);
                            listOfLegalMoves.Add(moveRepresentationQueenPromotion);
                        }
                        Move.unmakeMove(moveRepresentationKnightPromotion, inputBoard, boardRestoreData);
                    }
                }
            } else if (sideToMove == Constants.BLACK) {
                ulong blackPawnBitboard = bitboardArrayInput[Constants.BLACK_PAWN - 1];
                ulong blackPawnBitboardOnSecondRank = blackPawnBitboard &= Constants.RANK_2;
                List<int> indicesOfBlackPawn = Constants.bitScan(blackPawnBitboardOnSecondRank);

                foreach (int pawnIndex in indicesOfBlackPawn) {
                    ulong pawnMovementFromIndex = Constants.blackSinglePawnMovesAndPromotionMoves[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (~allPieces);
                    List<int> indicesOfBlackPawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfBlackPawnMovesFromIndex) {

                        uint moveRepresentationKnightPromotion = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.KNIGHT_PROMOTION, Constants.EMPTY);
                        uint moveRepresentationBishopPromotion = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.BISHOP_PROMOTION, Constants.EMPTY);
                        uint moveRepresentationRookPromotion = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.ROOK_PROMOTION, Constants.EMPTY);
                        uint moveRepresentationQueenPromotion = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.QUEEN_PROMOTION, Constants.EMPTY);

                        //Only have to check one of the four promotion types to see if it leaves the king in check
                        uint boardRestoreData = Move.makeMove(moveRepresentationKnightPromotion, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == false) {
                            listOfLegalMoves.Add(moveRepresentationKnightPromotion);
                            listOfLegalMoves.Add(moveRepresentationBishopPromotion);
                            listOfLegalMoves.Add(moveRepresentationRookPromotion);
                            listOfLegalMoves.Add(moveRepresentationQueenPromotion);
                        }
                        Move.unmakeMove(moveRepresentationKnightPromotion, inputBoard, boardRestoreData);
                    }

                }
            }
        }

        private static void generateListOfPawnPromotionCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {
            if (sideToMove == Constants.WHITE) {
                //only if pawn is on 7th rank
            } else if (sideToMove == Constants.BLACK) {
                //only if pawn is on 2nd rank
            }
        }

        private static void generateListOfBishipMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {
            if (sideToMove == Constants.WHITE) {

            } else if (sideToMove == Constants.BLACK) {

            }
        }

        private static void generateListOfRookMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {
            if (sideToMove == Constants.WHITE) {

            } else if (sideToMove == Constants.BLACK) {

            }
        }

        private static void generateListOfQueenMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {
            if (sideToMove == Constants.WHITE) {

            } else if (sideToMove == Constants.BLACK) {

            }
        }

        private static void generateListOfKingCastling(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            //only if castling variable is true
            
            if (sideToMove == Constants.WHITE) {
                
            } else if (sideToMove == Constants.BLACK) {

            }
        }
    }
}
