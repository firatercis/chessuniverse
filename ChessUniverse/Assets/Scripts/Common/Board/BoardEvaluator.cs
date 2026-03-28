using System.Collections.Generic;
using UnityEngine;

public struct SimSeed
{
    public int x, y;
    public PieceColor color;
    public PieceType targetType;
    public int turnsRemaining;
}

public static class BoardEvaluator
{
    static readonly Dictionary<PieceType, int> PieceValues = new()
    {
        { PieceType.Pawn, 100 },
        { PieceType.Knight, 320 },
        { PieceType.Bishop, 330 },
        { PieceType.Rook, 500 },
        { PieceType.Queen, 900 },
        { PieceType.King, 20000 }
    };

    // Piece-square tables (from white's perspective, row 0 = rank 1)
    // Flipped vertically for black

    static readonly int[,] PawnTable = {
        {  0,  0,  0,  0,  0,  0,  0,  0 },
        {  5, 10, 10,-20,-20, 10, 10,  5 },
        {  5, -5,-10,  0,  0,-10, -5,  5 },
        {  0,  0,  0, 20, 20,  0,  0,  0 },
        {  5,  5, 10, 25, 25, 10,  5,  5 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        {  0,  0,  0,  0,  0,  0,  0,  0 }
    };

    static readonly int[,] KnightTable = {
        {-50,-40,-30,-30,-30,-30,-40,-50 },
        {-40,-20,  0,  5,  5,  0,-20,-40 },
        {-30,  0, 10, 15, 15, 10,  0,-30 },
        {-30,  5, 15, 20, 20, 15,  5,-30 },
        {-30,  0, 15, 20, 20, 15,  0,-30 },
        {-30,  5, 10, 15, 15, 10,  5,-30 },
        {-40,-20,  0,  0,  0,  0,-20,-40 },
        {-50,-40,-30,-30,-30,-30,-40,-50 }
    };

    static readonly int[,] BishopTable = {
        {-20,-10,-10,-10,-10,-10,-10,-20 },
        {-10,  5,  0,  0,  0,  0,  5,-10 },
        {-10, 10, 10, 10, 10, 10, 10,-10 },
        {-10,  0, 10, 10, 10, 10,  0,-10 },
        {-10,  5,  5, 10, 10,  5,  5,-10 },
        {-10,  0,  5, 10, 10,  5,  0,-10 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-20,-10,-10,-10,-10,-10,-10,-20 }
    };

    static readonly int[,] RookTable = {
        {  0,  0,  0,  5,  5,  0,  0,  0 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        { -5,  0,  0,  0,  0,  0,  0, -5 },
        {  5, 10, 10, 10, 10, 10, 10,  5 },
        {  0,  0,  0,  0,  0,  0,  0,  0 }
    };

    static readonly int[,] QueenTable = {
        {-20,-10,-10, -5, -5,-10,-10,-20 },
        {-10,  0,  5,  0,  0,  0,  0,-10 },
        {-10,  5,  5,  5,  5,  5,  0,-10 },
        {  0,  0,  5,  5,  5,  5,  0, -5 },
        { -5,  0,  5,  5,  5,  5,  0, -5 },
        {-10,  0,  5,  5,  5,  5,  0,-10 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-20,-10,-10, -5, -5,-10,-10,-20 }
    };

    static readonly int[,] KingMiddleTable = {
        { 20, 30, 10,  0,  0, 10, 30, 20 },
        { 20, 20,  0,  0,  0,  0, 20, 20 },
        {-10,-20,-20,-20,-20,-20,-20,-10 },
        {-20,-30,-30,-40,-40,-30,-30,-20 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 },
        {-30,-40,-40,-50,-50,-40,-40,-30 }
    };

    static readonly int[,] KingEndTable = {
        {-50,-30,-30,-30,-30,-30,-30,-50 },
        {-30,-30,  0,  0,  0,  0,-30,-30 },
        {-30,-10, 20, 30, 30, 20,-10,-30 },
        {-30,-10, 30, 40, 40, 30,-10,-30 },
        {-30,-10, 30, 40, 40, 30,-10,-30 },
        {-30,-10, 20, 30, 30, 20,-10,-30 },
        {-30,-20,-10,  0,  0,-10,-20,-30 },
        {-50,-40,-30,-20,-20,-30,-40,-50 }
    };

    public const int CHECKMATE_SCORE = 999999;

    public static int Evaluate(ChessPiece[,] board, List<SimSeed> simSeeds = null, float seedDiscountBase = 0.85f)
    {
        int score = 0;
        int whiteMaterial = 0;
        int blackMaterial = 0;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece piece = board[x, y];
                if (piece == null || piece.isSeed) continue;

                if (!PieceValues.TryGetValue(piece.type, out int value)) continue;

                int positional = GetPositionalScore(piece, x, y, ref whiteMaterial, ref blackMaterial);

                if (piece.color == PieceColor.White)
                {
                    score += value + positional;
                    whiteMaterial += value;
                }
                else
                {
                    score -= value + positional;
                    blackMaterial += value;
                }
            }
        }

        // Evaluate real seeds (from SeedManager) in Seed Chess mode
        if (GameBootstrap.CurrentMode == GameMode.SeedChess && SeedManager.Instance != null)
        {
            foreach (var seed in SeedManager.Instance.Seeds)
            {
                int seedValue = GetSeedValue(seed.targetType, seed.turnsRemaining, seedDiscountBase);
                if (seed.color == PieceColor.White)
                    score += seedValue;
                else
                    score -= seedValue;
            }
        }

        // Evaluate simulated seeds (from AI search)
        if (simSeeds != null)
        {
            foreach (var seed in simSeeds)
            {
                int seedValue = GetSeedValue(seed.targetType, seed.turnsRemaining, seedDiscountBase);
                if (seed.color == PieceColor.White)
                    score += seedValue;
                else
                    score -= seedValue;
            }
        }

        return score;
    }

    static int GetSeedValue(PieceType targetType, int turnsRemaining, float discountBase)
    {
        if (!PieceValues.TryGetValue(targetType, out int baseValue)) return 0;
        return Mathf.RoundToInt(baseValue * Mathf.Pow(discountBase, turnsRemaining));
    }

    static int GetPositionalScore(ChessPiece piece, int x, int y, ref int whiteMat, ref int blackMat)
    {
        // For white: table row = y, col = x
        // For black: table row = 7-y, col = x (mirror vertically)
        int row = piece.color == PieceColor.White ? y : 7 - y;

        return piece.type switch
        {
            PieceType.Pawn => PawnTable[row, x],
            PieceType.Knight => KnightTable[row, x],
            PieceType.Bishop => BishopTable[row, x],
            PieceType.Rook => RookTable[row, x],
            PieceType.Queen => QueenTable[row, x],
            PieceType.King => KingMiddleTable[row, x], // simplified: use middle game table
            _ => 0
        };
    }
}
