using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Engine {

    class LegalMoveGenerator {

        public static int timesSquareIsAttacked(Board inputBoard, int colourUnderAttack, int squareToCheck) {

            int numberOfTimesAttacked = 0;

            //gets side to move and array of bitboards
            ulong[] bitboardArray = inputBoard.getArrayOfPieceBitboards();
            ulong[] aggregateBitboardArray = inputBoard.getArrayOfAggregatePieceBitboards();

            if (colourUnderAttack == Constants.WHITE) {

                //Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = aggregateBitboardArray[2] & Constants.rookOccupancyMask[squareToCheck];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
                ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

                numberOfTimesAttacked += Constants.popcount(rookMovesFromSquare & bitboardArray[Constants.BLACK_ROOK - 1]);
                numberOfTimesAttacked += Constants.popcount(rookMovesFromSquare & bitboardArray[Constants.BLACK_QUEEN - 1]);

                //Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = aggregateBitboardArray[2] & Constants.bishopOccupancyMask[squareToCheck];
                int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
                ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

                numberOfTimesAttacked += Constants.popcount(bishopMovesFromSquare & bitboardArray[Constants.BLACK_BISHOP - 1]);
                numberOfTimesAttacked += Constants.popcount(bishopMovesFromSquare & bitboardArray[Constants.BLACK_QUEEN - 1]);

                //Looks up knight attack set from square, and intersects with opponent's knight bitboard
                ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

                numberOfTimesAttacked += Constants.popcount(knightMoveFromSquare & bitboardArray[Constants.BLACK_KNIGHT - 1]);

                //Looks up white pawn attack set from square, and intersects with opponent's pawn bitboard
                ulong whitePawnMoveFromSquare = Constants.whiteCapturesAndCapturePromotions[squareToCheck];

                numberOfTimesAttacked += Constants.popcount(whitePawnMoveFromSquare & bitboardArray[Constants.BLACK_PAWN - 1]);

                //Looks up king attack set from square, and intersects with opponent's king bitboard
                ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

                numberOfTimesAttacked += Constants.popcount(kingMoveFromSquare & bitboardArray[Constants.BLACK_KING - 1]);

                return numberOfTimesAttacked;

           }

            else if (colourUnderAttack == Constants.BLACK) {

                //Looks up horizontal/vertical attack set from square, and intersects with opponent's rook/queen bitboard
                ulong horizontalVerticalOccupancy = aggregateBitboardArray[2] & Constants.rookOccupancyMask[squareToCheck];
                int rookMoveIndex = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[squareToCheck]) >> Constants.rookMagicShiftNumber[squareToCheck]);
                ulong rookMovesFromSquare = Constants.rookMoves[squareToCheck][rookMoveIndex];

                numberOfTimesAttacked += Constants.popcount(rookMovesFromSquare & bitboardArray[Constants.WHITE_ROOK - 1]);
                numberOfTimesAttacked += Constants.popcount(rookMovesFromSquare & bitboardArray[Constants.WHITE_QUEEN - 1]);

                //Looks up diagonal attack set from square position, and intersects with opponent's bishop/queen bitboard
                ulong diagonalOccupancy = aggregateBitboardArray[2] & Constants.bishopOccupancyMask[squareToCheck];
                int bishopMoveIndex = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[squareToCheck]) >> Constants.bishopMagicShiftNumber[squareToCheck]);
                ulong bishopMovesFromSquare = Constants.bishopMoves[squareToCheck][bishopMoveIndex];

                numberOfTimesAttacked += Constants.popcount(bishopMovesFromSquare & bitboardArray[Constants.WHITE_BISHOP - 1]);
                numberOfTimesAttacked += Constants.popcount(bishopMovesFromSquare & bitboardArray[Constants.WHITE_QUEEN - 1]);

                //Looks up knight attack set from square, and intersects with opponent's knight bitboard
                ulong knightMoveFromSquare = Constants.knightMoves[squareToCheck];

                numberOfTimesAttacked += Constants.popcount(knightMoveFromSquare & bitboardArray[Constants.WHITE_KNIGHT - 1]);

                //Looks up black pawn attack set from square, and intersects with opponent's pawn bitboard
                ulong blackPawnMoveFromSquare = Constants.blackCapturesAndCapturePromotions[squareToCheck];

                numberOfTimesAttacked += Constants.popcount(blackPawnMoveFromSquare & bitboardArray[Constants.WHITE_PAWN - 1]);

                //Looks up king attack set from square, and intersects with opponent's king bitboard
                ulong kingMoveFromSquare = Constants.kingMoves[squareToCheck];

                numberOfTimesAttacked += Constants.popcount(kingMoveFromSquare & bitboardArray[Constants.WHITE_KING - 1]);

                return numberOfTimesAttacked;
            }
            return 0;
        }

        public static int kingInCheck(Board inputBoard, int colourOfKingUnderAttack) {

            //gets side to move and array of bitboards
            int sideToMove = inputBoard.getSideToMove();
            ulong[] bitboardArray = inputBoard.getArrayOfPieceBitboards();

            int numberOfChecks = 0;

            if (colourOfKingUnderAttack == Constants.WHITE) {

                //determines the index of the white king
                ulong whiteKingBitboard = bitboardArray[Constants.WHITE_KING - 1];
                int indexOfWhiteKing = Constants.bitScan(whiteKingBitboard).ElementAt(0);
                
                //Passes this to the times square attacked method
                numberOfChecks = timesSquareIsAttacked(inputBoard, Constants.WHITE, indexOfWhiteKing); 
                
                //If number of checks is 0, then the king is not in check
                //If number of checks is 1, then king is in check
                //If number of checks is 2, then king is in double-check
                if (numberOfChecks == 0) {
                    return Constants.NOT_IN_CHECK;
                } else if (numberOfChecks == 1) {
                    return Constants.CHECK;
                } else if (numberOfChecks == 2) {
                    return Constants.DOUBLE_CHECK;
                }
            }

            else if (colourOfKingUnderAttack == Constants.BLACK) {
                
                //determines the index of the black king
                ulong blackKingBitboard = bitboardArray[Constants.BLACK_KING - 1];
                int indexOfBlackKing = Constants.bitScan(blackKingBitboard).ElementAt(0);

                //Passes this to the times square attacked method
                numberOfChecks = timesSquareIsAttacked(inputBoard, Constants.BLACK, indexOfBlackKing);

                //If number of checks is 0, then the king is not in check
                //If number of checks is 1, then king is in check
                //If number of checks is 2, then king is in double-check
                if (numberOfChecks == 0) {
                    return Constants.NOT_IN_CHECK;
                } else if (numberOfChecks == 1) {
                    return Constants.CHECK;
                } else if (numberOfChecks == 2) {
                    return Constants.DOUBLE_CHECK;
                }
            }
            return Constants.NOT_IN_CHECK;
        }

        public static List<uint> generateListOfLegalMoves(Board inputBoard) {
            List<uint> listOfLegalMoves = new List<uint>();
            int sideToMove = inputBoard.getSideToMove();
            ulong[] bitboardArray = inputBoard.getArrayOfPieceBitboards();
            ulong[] aggregateBitboardArray = inputBoard.getArrayOfAggregatePieceBitboards();
            int[] pieceArray = inputBoard.getPieceArray();

            //Checks to see if the king is in check in the current position
            int kingCheckStatus = kingInCheck(inputBoard, sideToMove);

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
            if (kingCheckStatus == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
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
                            if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
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
                            if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
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
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (blackPieces);
                    List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        moveRepresentation = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
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
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (whitePieces);
                    List<int> indicesOfBlackPawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfBlackPawnMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        moveRepresentation = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.CAPTURE, pieceArrayInput[pawnMoveIndex]);

                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }
            }
        }

        private static void generateListOfPawnEnPassantCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong enPassantBitboard = inputBoard.getEnPassant();

            if (enPassantBitboard != 0) {
                ulong whitePieces = aggregateBitboardArray[0];
                ulong blackPieces = aggregateBitboardArray[1];

                if (sideToMove == Constants.WHITE) {
                    ulong whitePawnBitboard = bitboardArrayInput[Constants.WHITE_PAWN - 1];
                    ulong whitePawnBitboardOnFifthRank = whitePawnBitboard &= (Constants.RANK_5);
                    List<int> indicesOfWhitePawn = Constants.bitScan(whitePawnBitboardOnFifthRank);

                    foreach (int pawnIndex in indicesOfWhitePawn)
                    {
                        ulong pawnMovementFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
                        ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (enPassantBitboard);
                        List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                        foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex)
                        {

                            uint moveRepresentation = 0x0;

                            moveRepresentation = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.EN_PASSANT_CAPTURE, Constants.BLACK_PAWN);

                            uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                            if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK)
                            {
                                listOfLegalMoves.Add(moveRepresentation);
                            }
                            Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                        }

                    }

                }
                else if (sideToMove == Constants.BLACK) {
                    
                    ulong blackPawnBitboard = bitboardArrayInput[Constants.BLACK_PAWN - 1];
                    ulong blackPawnBitboardOnFourthRank = blackPawnBitboard &= (Constants.RANK_4);
                    List<int> indicesOfBlackPawn = Constants.bitScan(blackPawnBitboardOnFourthRank);

                    foreach (int pawnIndex in indicesOfBlackPawn)
                    {
                        ulong pawnMovementFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
                        ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (enPassantBitboard);
                        List<int> indicesOfBlackPawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                        foreach (int pawnMoveIndex in indicesOfBlackPawnMovesFromIndex)
                        {

                            uint moveRepresentation = 0x0;

                            moveRepresentation = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.EN_PASSANT_CAPTURE, Constants.WHITE_PAWN);

                            uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                            if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK)
                            {
                                listOfLegalMoves.Add(moveRepresentation);
                            }
                            Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                        }
                    }
                }
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
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
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
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
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

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];

            if (sideToMove == Constants.WHITE) {
                
                ulong whitePawnBitboard = bitboardArrayInput[Constants.WHITE_PAWN - 1];
                ulong whitePawnBitboardOnSeventhRank = whitePawnBitboard &= (Constants.RANK_7);
                List<int> indicesOfWhitePawn = Constants.bitScan(whitePawnBitboardOnSeventhRank);

                foreach (int pawnIndex in indicesOfWhitePawn)
                {
                    ulong pawnMovementFromIndex = Constants.whiteCapturesAndCapturePromotions[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (blackPieces);
                    List<int> indicesOfWhitePawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfWhitePawnMovesFromIndex)
                    {

                        uint moveRepresentationKnightPromotionCapture = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.KNIGHT_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        uint moveRepresentationBishopPromotionCapture = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.BISHOP_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        uint moveRepresentationRookPromotionCapture = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.ROOK_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        uint moveRepresentationQueenPromotionCapture = Move.moveEncoder(Constants.WHITE_PAWN, pawnIndex, pawnMoveIndex, Constants.QUEEN_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);

                        uint boardRestoreData = Move.makeMove(moveRepresentationKnightPromotionCapture, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK)
                        {
                            listOfLegalMoves.Add(moveRepresentationKnightPromotionCapture);
                            listOfLegalMoves.Add(moveRepresentationBishopPromotionCapture);
                            listOfLegalMoves.Add(moveRepresentationRookPromotionCapture);
                            listOfLegalMoves.Add(moveRepresentationQueenPromotionCapture);
                        }
                        Move.unmakeMove(moveRepresentationKnightPromotionCapture, inputBoard, boardRestoreData);
                    }

                }

            } else if (sideToMove == Constants.BLACK) {
                ulong blackPawnBitboard = bitboardArrayInput[Constants.BLACK_PAWN - 1];
                ulong blackPawnBitboardOnSecondRank = blackPawnBitboard &= (Constants.RANK_2);
                List<int> indicesOfBlackPawn = Constants.bitScan(blackPawnBitboardOnSecondRank);

                foreach (int pawnIndex in indicesOfBlackPawn)
                {
                    ulong pawnMovementFromIndex = Constants.blackCapturesAndCapturePromotions[pawnIndex];
                    ulong pseudoLegalPawnMovementFromIndex = pawnMovementFromIndex &= (whitePieces);
                    List<int> indicesOfBlackPawnMovesFromIndex = Constants.bitScan(pseudoLegalPawnMovementFromIndex);
                    foreach (int pawnMoveIndex in indicesOfBlackPawnMovesFromIndex)
                    {

                        uint moveRepresentationKnightPromotionCapture = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.KNIGHT_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        uint moveRepresentationBishopPromotionCapture = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.BISHOP_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        uint moveRepresentationRookPromotionCapture = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.ROOK_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);
                        uint moveRepresentationQueenPromotionCapture = Move.moveEncoder(Constants.BLACK_PAWN, pawnIndex, pawnMoveIndex, Constants.QUEEN_PROMOTION_CAPTURE, pieceArrayInput[pawnMoveIndex]);

                        uint boardRestoreData = Move.makeMove(moveRepresentationKnightPromotionCapture, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK)
                        {
                            listOfLegalMoves.Add(moveRepresentationKnightPromotionCapture);
                            listOfLegalMoves.Add(moveRepresentationBishopPromotionCapture);
                            listOfLegalMoves.Add(moveRepresentationRookPromotionCapture);
                            listOfLegalMoves.Add(moveRepresentationQueenPromotionCapture);
                        }
                        Move.unmakeMove(moveRepresentationKnightPromotionCapture, inputBoard, boardRestoreData);
                    }
                }
            }
        }

        private static void generateListOfBishipMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            ulong allPieces = aggregateBitboardArray[2];
            
            if (sideToMove == Constants.WHITE) {

                ulong whiteBishopBitboard = bitboardArrayInput[Constants.WHITE_BISHOP - 1];
                List<int> indicesOfWhiteBishop = Constants.bitScan(whiteBishopBitboard);
                
                foreach (int bishopIndex in indicesOfWhiteBishop) {
                    ulong diagonalOccupancy = allPieces & Constants.bishopOccupancyMask[bishopIndex];
                    int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[bishopIndex]) >> Constants.bishopMagicShiftNumber[bishopIndex]);
                    ulong bishopMovesFromIndex = Constants.bishopMoves[bishopIndex][indexOfBishopMoveBitboard];
                    
                    ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~whitePieces);
                    List<int> indicesOfWhiteBishopMovesFromIndex = Constants.bitScan(pseudoLegalBishopMovementFromIndex);
                    foreach (int bishopMoveIndex in indicesOfWhiteBishopMovesFromIndex)
                    {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[bishopMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_BISHOP, bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        } 
                        //If not empty, then must be a black piece at that location, so generate a capture
                        else if (pieceArrayInput[bishopMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_BISHOP, bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArrayInput[bishopMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK)
                        {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }

            } else if (sideToMove == Constants.BLACK) {
                ulong blackBishopBitboard = bitboardArrayInput[Constants.BLACK_BISHOP - 1];
                List<int> indicesOfBlackBishop = Constants.bitScan(blackBishopBitboard);

                foreach (int bishopIndex in indicesOfBlackBishop)
                {
                    ulong diagonalOccupancy = allPieces & Constants.bishopOccupancyMask[bishopIndex];
                    int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[bishopIndex]) >> Constants.bishopMagicShiftNumber[bishopIndex]);
                    ulong bishopMovesFromIndex = Constants.bishopMoves[bishopIndex][indexOfBishopMoveBitboard];

                    ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~blackPieces);
                    List<int> indicesOfBlackBishopMovesFromIndex = Constants.bitScan(pseudoLegalBishopMovementFromIndex);
                    foreach (int bishopMoveIndex in indicesOfBlackBishopMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[bishopMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_BISHOP, bishopIndex, bishopMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        }
                        //If not empty, then must be a white piece at that location, so generate a capture
                        else if (pieceArrayInput[bishopMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_BISHOP, bishopIndex, bishopMoveIndex, Constants.CAPTURE, pieceArrayInput[bishopMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            }
        }

        private static void generateListOfRookMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            ulong allPieces = aggregateBitboardArray[2];
            
            if (sideToMove == Constants.WHITE) {
                ulong whiteRookBitboard = bitboardArrayInput[Constants.WHITE_ROOK - 1];
                List<int> indicesOfWhiteRook = Constants.bitScan(whiteRookBitboard);

                foreach (int rookIndex in indicesOfWhiteRook)
                {
                    ulong horizontalVerticalOccupancy = allPieces & Constants.rookOccupancyMask[rookIndex];
                    int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[rookIndex]) >> Constants.rookMagicShiftNumber[rookIndex]);
                    ulong rookMovesFromIndex = Constants.rookMoves[rookIndex][indexOfRookMoveBitboard];

                    ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~whitePieces);
                    List<int> indicesOfWhiteRookMovesFromIndex = Constants.bitScan(pseudoLegalRookMovementFromIndex);
                    foreach (int rookMoveIndex in indicesOfWhiteRookMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[rookMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_ROOK, rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        }
                        //If not empty, then must be a black piece at that location, so generate a capture
                        else if (pieceArrayInput[rookMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_ROOK, rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArrayInput[rookMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK)
                        {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }
            } else if (sideToMove == Constants.BLACK) {
                ulong blackRookBitboard = bitboardArrayInput[Constants.BLACK_ROOK - 1];
                List<int> indicesOfBlackRook = Constants.bitScan(blackRookBitboard);

                foreach (int rookIndex in indicesOfBlackRook)
                {
                    ulong horizontalVerticalOccupancy = allPieces & Constants.rookOccupancyMask[rookIndex];
                    int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[rookIndex]) >> Constants.rookMagicShiftNumber[rookIndex]);
                    ulong rookMovesFromIndex = Constants.rookMoves[rookIndex][indexOfRookMoveBitboard];

                    ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~blackPieces);
                    List<int> indicesOfBlackRookMovesFromIndex = Constants.bitScan(pseudoLegalRookMovementFromIndex);
                    foreach (int rookMoveIndex in indicesOfBlackRookMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[rookMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_ROOK, rookIndex, rookMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        }
                        //If not empty, then must be a white piece at that location, so generate a capture
                        else if (pieceArrayInput[rookMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_ROOK, rookIndex, rookMoveIndex, Constants.CAPTURE, pieceArrayInput[rookMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }

                }
            }
        }

        private static void generateListOfQueenMovesAndCaptures(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            ulong allPieces = aggregateBitboardArray[2];

            if (sideToMove == Constants.WHITE) {
                ulong whiteQueenBitboard = bitboardArrayInput[Constants.WHITE_QUEEN - 1];
                List<int> indicesOfWhiteQueen = Constants.bitScan(whiteQueenBitboard);

                foreach (int queenIndex in indicesOfWhiteQueen) {
                    ulong diagonalOccupancy = allPieces & Constants.bishopOccupancyMask[queenIndex];
                    int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[queenIndex]) >> Constants.bishopMagicShiftNumber[queenIndex]);
                    ulong bishopMovesFromIndex = Constants.bishopMoves[queenIndex][indexOfBishopMoveBitboard];

                    ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~whitePieces);
                   
                    ulong horizontalVerticalOccupancy = allPieces & Constants.rookOccupancyMask[queenIndex];
                    int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[queenIndex]) >> Constants.rookMagicShiftNumber[queenIndex]);
                    ulong rookMovesFromIndex = Constants.rookMoves[queenIndex][indexOfRookMoveBitboard];

                    ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~whitePieces);

                    ulong pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
                    List<int> indicesOfWhiteQueenMovesFromIndex = Constants.bitScan(pseudoLegalQueenMovementFromIndex);

                    foreach (int queenMoveIndex in indicesOfWhiteQueenMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[queenMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_QUEEN, queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        }
                        //If not empty, then must be a black piece at that location, so generate a capture
                        else if (pieceArrayInput[queenMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.WHITE_QUEEN, queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArrayInput[queenMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            } else if (sideToMove == Constants.BLACK) {
                ulong blackQueenBitboard = bitboardArrayInput[Constants.BLACK_QUEEN - 1];
                List<int> indicesOfBlackQueen = Constants.bitScan(blackQueenBitboard);

                foreach (int queenIndex in indicesOfBlackQueen)
                {
                    ulong diagonalOccupancy = allPieces & Constants.bishopOccupancyMask[queenIndex];
                    int indexOfBishopMoveBitboard = (int)((diagonalOccupancy * Constants.bishopMagicNumbers[queenIndex]) >> Constants.bishopMagicShiftNumber[queenIndex]);
                    ulong bishopMovesFromIndex = Constants.bishopMoves[queenIndex][indexOfBishopMoveBitboard];

                    ulong pseudoLegalBishopMovementFromIndex = bishopMovesFromIndex &= (~blackPieces);

                    ulong horizontalVerticalOccupancy = allPieces & Constants.rookOccupancyMask[queenIndex];
                    int indexOfRookMoveBitboard = (int)((horizontalVerticalOccupancy * Constants.rookMagicNumbers[queenIndex]) >> Constants.rookMagicShiftNumber[queenIndex]);
                    ulong rookMovesFromIndex = Constants.rookMoves[queenIndex][indexOfRookMoveBitboard];

                    ulong pseudoLegalRookMovementFromIndex = rookMovesFromIndex &= (~blackPieces);

                    ulong pseudoLegalQueenMovementFromIndex = pseudoLegalBishopMovementFromIndex | pseudoLegalRookMovementFromIndex;
                    List<int> indicesOfBlackQueenMovesFromIndex = Constants.bitScan(pseudoLegalQueenMovementFromIndex);

                    foreach (int queenMoveIndex in indicesOfBlackQueenMovesFromIndex) {

                        uint moveRepresentation = 0x0;

                        if (pieceArrayInput[queenMoveIndex] == Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_QUEEN, queenIndex, queenMoveIndex, Constants.QUIET_MOVE, Constants.EMPTY);
                        }
                        //If not empty, then must be a white piece at that location, so generate a capture
                        else if (pieceArrayInput[queenMoveIndex] != Constants.EMPTY) {
                            moveRepresentation = Move.moveEncoder(Constants.BLACK_QUEEN, queenIndex, queenMoveIndex, Constants.CAPTURE, pieceArrayInput[queenMoveIndex]);
                        }
                        uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                        if (kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) {
                            listOfLegalMoves.Add(moveRepresentation);
                        }
                        Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                    }
                }
            }
        }

        private static void generateListOfKingCastling(Board inputBoard, int sideToMove, ulong[] bitboardArrayInput, ulong[] aggregateBitboardArray, int[] pieceArrayInput, List<uint> listOfLegalMoves) {

            
            
            
           

            int[] castleRightsArray = inputBoard.getCastleRights();
            int whiteShortCastleRights = castleRightsArray[0];
            int whiteLongCastleRights = castleRightsArray[1];
            int blackShortCastleRights = castleRightsArray[2];
            int blackLongCastleRights = castleRightsArray[3];
            
            ulong whitePieces = aggregateBitboardArray[0];
            ulong blackPieces = aggregateBitboardArray[1];
            ulong allPieces = aggregateBitboardArray[2];

            if (sideToMove == Constants.WHITE) {

                if ((whiteShortCastleRights == Constants.CAN_CASTLE) && ((allPieces & Constants.WHITE_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    uint moveRepresentation = Move.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.G1, Constants.SHORT_CASTLE, Constants.EMPTY);
                    
                    uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                    if ((kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) && (timesSquareIsAttacked(inputBoard, Constants.WHITE, Constants.F1) == 0)) {
                        listOfLegalMoves.Add(moveRepresentation);
                    }
                    Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                }

                if ((whiteLongCastleRights == Constants.CAN_CASTLE) && ((allPieces & Constants.WHITE_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    uint moveRepresentation = Move.moveEncoder(Constants.WHITE_KING, Constants.E1, Constants.C1, Constants.LONG_CASTLE, Constants.EMPTY);

                    uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                    if ((kingInCheck(inputBoard, Constants.WHITE) == Constants.NOT_IN_CHECK) && (timesSquareIsAttacked(inputBoard, Constants.WHITE, Constants.D1) == 0)) {
                        listOfLegalMoves.Add(moveRepresentation);
                    }
                    Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                }

            } else if (sideToMove == Constants.BLACK) {

                if ((blackShortCastleRights == Constants.CAN_CASTLE) && ((allPieces & Constants.BLACK_SHORT_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    uint moveRepresentation = Move.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.G8, Constants.SHORT_CASTLE, Constants.EMPTY);

                    uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                    if ((kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) && (timesSquareIsAttacked(inputBoard, Constants.BLACK, Constants.F8) == 0)) {
                        listOfLegalMoves.Add(moveRepresentation);
                    }
                    Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                }

                if ((blackLongCastleRights == Constants.CAN_CASTLE) && ((allPieces & Constants.BLACK_LONG_CASTLE_REQUIRED_EMPTY_SQUARES) == 0)) {
                    uint moveRepresentation = Move.moveEncoder(Constants.BLACK_KING, Constants.E8, Constants.C8, Constants.LONG_CASTLE, Constants.EMPTY);

                    uint boardRestoreData = Move.makeMove(moveRepresentation, inputBoard);
                    if ((kingInCheck(inputBoard, Constants.BLACK) == Constants.NOT_IN_CHECK) && (timesSquareIsAttacked(inputBoard, Constants.BLACK, Constants.D8) == 0)) {
                        listOfLegalMoves.Add(moveRepresentation);
                    }
                    Move.unmakeMove(moveRepresentation, inputBoard, boardRestoreData);
                }
            }
        }
    }
}
