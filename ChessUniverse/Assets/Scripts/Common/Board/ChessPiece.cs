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
    public GameObject maskObj;

    // Animation
    private Vector3? animTarget;
    public static float AnimSpeed = 12f;

    private static readonly Dictionary<PieceType, string> PieceNames = new()
    {
        { PieceType.King, "king" },
        { PieceType.Queen, "queen" },
        { PieceType.Rook, "rook" },
        { PieceType.Bishop, "bishop" },
        { PieceType.Knight, "knight" },
        { PieceType.Pawn, "pawn" },
    };

    // Sprite cache — loaded from sprite sheet (sliced at import time)
    private static Dictionary<string, Sprite> _spriteCache;

    public static Sprite GetPieceSprite(PieceColor color, PieceType type)
    {
        if (!PieceNames.ContainsKey(type)) return null;

        if (_spriteCache == null)
        {
            _spriteCache = new Dictionary<string, Sprite>();
            Sprite[] all = Resources.LoadAll<Sprite>("ChessPieces/Chess_Pieces_Sprite");
            foreach (var s in all)
                _spriteCache[s.name] = s;
        }

        string spriteName = $"{(color == PieceColor.White ? "w" : "b")}_{PieceNames[type]}";
        return _spriteCache.TryGetValue(spriteName, out var s2) ? s2 : null;
    }

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

        Sprite sprite = GetPieceSprite(color, type);
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 2;
            float spriteWorldSize = sprite.rect.height / sprite.pixelsPerUnit;
            float scale = 0.935f / spriteWorldSize;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void UpdateType(PieceType newType)
    {
        type = newType;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Sprite sprite = GetPieceSprite(color, newType);
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            float spriteWorldSize = sprite.rect.height / sprite.pixelsPerUnit;
            float scale = 0.935f / spriteWorldSize;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void SetPosition(int newX, int newY)
    {
        x = newX;
        y = newY;
        hasMoved = true;
        animTarget = ChessBoard.Instance.VisualPos(newX, newY);
    }

    public void SetPositionImmediate(int newX, int newY)
    {
        x = newX;
        y = newY;
        hasMoved = true;
        animTarget = null;
        transform.position = ChessBoard.Instance.VisualPos(newX, newY);
    }

    private void Update()
    {
        if (animTarget.HasValue)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, animTarget.Value, AnimSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, animTarget.Value) < 0.001f)
            {
                transform.position = animTarget.Value;
                animTarget = null;
            }
        }
    }

    public void ShowMask(Sprite maskSprite, Color color, bool overlay = false)
    {
        if (!overlay)
            spriteRenderer.enabled = false;
        else
            spriteRenderer.enabled = true;

        if (maskObj == null)
        {
            maskObj = new GameObject("Mask");
            maskObj.transform.SetParent(transform, false);
            maskObj.transform.localPosition = Vector3.zero;
            var sr = maskObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
        }
        var maskSr = maskObj.GetComponent<SpriteRenderer>();
        maskSr.sprite = maskSprite;
        maskSr.color = color;
        maskSr.sortingOrder = 3;
        float pixelsPerUnit = maskSprite.pixelsPerUnit;
        float spriteWorldSize = maskSprite.rect.height / pixelsPerUnit;
        float targetSize = overlay ? 0.55f : 0.935f;
        float parentScale = transform.localScale.x;
        float scale = (targetSize / spriteWorldSize) / (parentScale > 0 ? parentScale : 1f);
        maskObj.transform.localScale = new Vector3(scale, scale, 1f);
        maskObj.transform.localPosition = overlay ? new Vector3(0, 0.15f / parentScale, 0) : Vector3.zero;
        maskObj.SetActive(true);
    }

    public void HideMask()
    {
        spriteRenderer.enabled = true;
        if (maskObj != null)
            maskObj.SetActive(false);
    }

    public List<Vector2Int> GetRawMoves(ChessPiece[,] board)
    {
        if (GameBootstrap.CurrentMode == GameMode.BluffyChess && type != PieceType.Pawn)
        {
            return GetBluffyMoves(board);
        }

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

    /// <summary>All-direction + L-shape moves for Bluffy Chess super-pieces.
    /// Public so BluffyChessPlugin can call it via IGameModePlugin.GetCustomMoves.</summary>
    public List<Vector2Int> GetBluffyMoves(ChessPiece[,] board)
    {
        var moves = new List<Vector2Int>();

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };
        foreach (var dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            while (InBounds(nx, ny))
            {
                if (board[nx, ny] == null || board[nx, ny].isSeed)
                {
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

        int[] offsets = { -2, -1, 1, 2 };
        foreach (int dx in offsets)
        {
            foreach (int dy in offsets)
            {
                if (Mathf.Abs(dx) == Mathf.Abs(dy)) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (InBounds(nx, ny) && (board[nx, ny] == null || board[nx, ny].isSeed || board[nx, ny].color != color))
                {
                    if (!moves.Contains(new Vector2Int(nx, ny)))
                        moves.Add(new Vector2Int(nx, ny));
                }
            }
        }

        return moves;
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

        if (InBounds(x, y + dir) && IsEmpty(board, x, y + dir))
        {
            moves.Add(new Vector2Int(x, y + dir));
            if (y == startRow && IsEmpty(board, x, y + 2 * dir))
                moves.Add(new Vector2Int(x, y + 2 * dir));
        }

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
