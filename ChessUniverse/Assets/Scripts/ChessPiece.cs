using UnityEngine;
using System.Collections.Generic;

public enum PieceType
{
    None,
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}

public enum PieceColor
{
    White,
    Black
}

public class ChessPiece : MonoBehaviour
{
    public PieceType type;
    public PieceColor color;
    public int x;
    public int y;
    public bool hasMoved;
    public bool isSeed;

    private SpriteRenderer spriteRenderer;

    private static readonly Dictionary<PieceType, string> PieceNames = new()
    {
        { PieceType.King, "king" },
        { PieceType.Queen, "queen" },
        { PieceType.Rook, "rook" },
        { PieceType.Bishop, "bishop" },
        { PieceType.Knight, "knight" },
        { PieceType.Pawn, "pawn" },
    };

    public void Init(PieceType type, PieceColor color, int x, int y)
    {
        this.type = type;
        this.color = color;
        this.x = x;
        this.y = y;
        this.hasMoved = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (!PieceNames.ContainsKey(type)) return;

        string colorPrefix = color == PieceColor.White ? "w" : "b";
        string spritePath = $"ChessPieces/{colorPrefix}_{PieceNames[type]}";
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 2;
            // Scale piece to fit within a square (sprite is 256px, we want ~0.85 world units)
            float pixelsPerUnit = sprite.pixelsPerUnit;
            float spriteWorldSize = sprite.rect.height / pixelsPerUnit;
            float targetSize = 0.85f;
            float scale = targetSize / spriteWorldSize;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void SetPosition(int newX, int newY)
    {
        x = newX;
        y = newY;
        transform.position = new Vector3(newX, newY, 0);
        hasMoved = true;
    }

    public List<Vector2Int> GetRawMoves(ChessPiece[,] board)
    {
        return type switch
        {
            PieceType.Pawn => GetPawnMoves(board),
            PieceType.Rook => GetLineMoves(board, new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }),
            PieceType.Bishop => GetLineMoves(board, new Vector2Int[] { new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) }),
            PieceType.Queen => GetLineMoves(board, new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) }),
            PieceType.Knight => GetKnightMoves(board),
            PieceType.King => GetKingMoves(board),
            _ => new List<Vector2Int>()
        };
    }

    private static bool IsEmpty(ChessPiece[,] board, int bx, int by)
    {
        return board[bx, by] == null || board[bx, by].isSeed;
    }

    private List<Vector2Int> GetPawnMoves(ChessPiece[,] board)
    {
        var moves = new List<Vector2Int>();
        int dir = color == PieceColor.White ? 1 : -1;
        int startRow = color == PieceColor.White ? 1 : 6;

        // Forward one (seeds don't block)
        if (InBounds(x, y + dir) && IsEmpty(board, x, y + dir))
        {
            moves.Add(new Vector2Int(x, y + dir));
            // Forward two from start
            if (y == startRow && IsEmpty(board, x, y + 2 * dir))
                moves.Add(new Vector2Int(x, y + 2 * dir));
        }

        // Diagonal captures (real pieces only, not seeds)
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int nx = x + dx;
            int ny = y + dir;
            if (InBounds(nx, ny) && board[nx, ny] != null && !board[nx, ny].isSeed && board[nx, ny].color != color)
                moves.Add(new Vector2Int(nx, ny));
        }

        return moves;
    }

    private List<Vector2Int> GetLineMoves(ChessPiece[,] board, Vector2Int[] directions)
    {
        var moves = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            while (InBounds(nx, ny))
            {
                if (IsEmpty(board, nx, ny))
                {
                    // Seeds are passable: add as move target and continue
                    moves.Add(new Vector2Int(nx, ny));
                }
                else
                {
                    if (board[nx, ny].color != color)
                        moves.Add(new Vector2Int(nx, ny));
                    break;
                }
                nx += dir.x;
                ny += dir.y;
            }
        }
        return moves;
    }

    private List<Vector2Int> GetKnightMoves(ChessPiece[,] board)
    {
        var moves = new List<Vector2Int>();
        int[] offsets = { -2, -1, 1, 2 };
        foreach (int dx in offsets)
        {
            foreach (int dy in offsets)
            {
                if (Mathf.Abs(dx) == Mathf.Abs(dy)) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (InBounds(nx, ny) && (IsEmpty(board, nx, ny) || board[nx, ny].color != color))
                    moves.Add(new Vector2Int(nx, ny));
            }
        }
        return moves;
    }

    private List<Vector2Int> GetKingMoves(ChessPiece[,] board)
    {
        var moves = new List<Vector2Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (InBounds(nx, ny) && (IsEmpty(board, nx, ny) || board[nx, ny].color != color))
                    moves.Add(new Vector2Int(nx, ny));
            }
        }
        return moves;
    }

    private static bool InBounds(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8;
}
