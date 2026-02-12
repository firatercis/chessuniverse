using UnityEngine;
using System.Collections.Generic;

public class MoveValidator : MonoBehaviour
{
    public static MoveValidator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public List<Vector2Int> GetLegalMoves(ChessPiece piece, ChessPiece[,] board, Vector2Int? enPassantTarget)
    {
        var rawMoves = piece.GetRawMoves(board);
        var legalMoves = new List<Vector2Int>();

        foreach (var move in rawMoves)
        {
            if (IsMoveLegal(piece, move, board))
                legalMoves.Add(move);
        }

        // En passant
        if (piece.type == PieceType.Pawn && enPassantTarget.HasValue)
        {
            int dir = piece.color == PieceColor.White ? 1 : -1;
            Vector2Int ep = enPassantTarget.Value;
            if (ep.y == piece.y + dir && Mathf.Abs(ep.x - piece.x) == 1)
            {
                if (IsEnPassantLegal(piece, ep, board))
                    legalMoves.Add(ep);
            }
        }

        // Castling
        if (piece.type == PieceType.King && !piece.hasMoved)
        {
            // Kingside
            if (CanCastle(piece, board, true))
                legalMoves.Add(new Vector2Int(piece.x + 2, piece.y));
            // Queenside
            if (CanCastle(piece, board, false))
                legalMoves.Add(new Vector2Int(piece.x - 2, piece.y));
        }

        return legalMoves;
    }

    private bool IsMoveLegal(ChessPiece piece, Vector2Int target, ChessPiece[,] board)
    {
        // Simulate move and check if own king is in check
        ChessPiece captured = board[target.x, target.y];
        int origX = piece.x, origY = piece.y;

        board[target.x, target.y] = piece;
        board[origX, origY] = null;
        int savedX = piece.x, savedY = piece.y;
        piece.x = target.x;
        piece.y = target.y;

        bool inCheck = IsKingInCheck(piece.color, board);

        // Undo
        piece.x = savedX;
        piece.y = savedY;
        board[origX, origY] = piece;
        board[target.x, target.y] = captured;

        // Restore piece coords
        piece.x = origX;
        piece.y = origY;

        return !inCheck;
    }

    private bool IsEnPassantLegal(ChessPiece pawn, Vector2Int epTarget, ChessPiece[,] board)
    {
        int dir = pawn.color == PieceColor.White ? 1 : -1;
        int capturedY = epTarget.y - dir;

        ChessPiece captured = board[epTarget.x, capturedY];
        int origX = pawn.x, origY = pawn.y;

        board[epTarget.x, epTarget.y] = pawn;
        board[origX, origY] = null;
        board[epTarget.x, capturedY] = null;
        pawn.x = epTarget.x;
        pawn.y = epTarget.y;

        bool inCheck = IsKingInCheck(pawn.color, board);

        // Undo
        pawn.x = origX;
        pawn.y = origY;
        board[origX, origY] = pawn;
        board[epTarget.x, epTarget.y] = null;
        board[epTarget.x, capturedY] = captured;

        return !inCheck;
    }

    private bool CanCastle(ChessPiece king, ChessPiece[,] board, bool kingside)
    {
        int y = king.y;
        int rookX = kingside ? 7 : 0;
        ChessPiece rook = board[rookX, y];

        if (rook == null || rook.type != PieceType.Rook || rook.color != king.color || rook.hasMoved)
            return false;

        // Check if king is in check
        if (IsKingInCheck(king.color, board))
            return false;

        int dir = kingside ? 1 : -1;
        int startX = king.x + dir;
        int endX = kingside ? 6 : 2;
        int clearEnd = kingside ? 6 : 1;

        // Check squares are empty
        int minX = Mathf.Min(startX, clearEnd);
        int maxX = Mathf.Max(startX, clearEnd);
        for (int x = minX; x <= maxX; x++)
        {
            if (board[x, y] != null)
                return false;
        }

        // Check king doesn't pass through or land on attacked square
        for (int x = king.x; x != endX + dir; x += dir)
        {
            if (x == king.x) continue;
            // Simulate king at this square
            int origX = king.x;
            board[origX, y] = null;
            board[x, y] = king;
            king.x = x;

            bool attacked = IsKingInCheck(king.color, board);

            king.x = origX;
            board[x, y] = null;
            board[origX, y] = king;

            if (attacked) return false;
        }

        return true;
    }

    public bool IsKingInCheck(PieceColor color, ChessPiece[,] board)
    {
        Vector2Int kingPos = FindKing(color, board);
        if (kingPos.x == -1) return false;

        PieceColor enemy = color == PieceColor.White ? PieceColor.Black : PieceColor.White;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p != null && p.color == enemy)
                {
                    var moves = p.GetRawMoves(board);
                    foreach (var m in moves)
                    {
                        if (m.x == kingPos.x && m.y == kingPos.y)
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public bool HasAnyLegalMove(PieceColor color, ChessPiece[,] board, Vector2Int? enPassantTarget)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p != null && p.color == color && !p.isSeed)
                {
                    var moves = GetLegalMoves(p, board, enPassantTarget);
                    if (moves.Count > 0)
                        return true;
                }
            }
        }

        // In SeedChess, planting counts as a legal move (unless in check)
        if (GameBootstrap.CurrentMode == GameMode.SeedChess
            && !IsKingInCheck(color, board))
        {
            Vector2Int kingPos = FindKing(color, board);
            if (kingPos.x != -1)
            {
                ChessPiece king = board[kingPos.x, kingPos.y];
                var plantable = SeedManager.Instance.GetPlantableSquares(king);
                if (plantable.Count > 0)
                    return true;
            }
        }

        return false;
    }

    private Vector2Int FindKing(PieceColor color, ChessPiece[,] board)
    {
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (board[x, y] != null && board[x, y].type == PieceType.King && board[x, y].color == color)
                    return new Vector2Int(x, y);
        return new Vector2Int(-1, -1);
    }
}
