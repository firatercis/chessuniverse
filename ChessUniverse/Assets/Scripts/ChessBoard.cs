using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    public static ChessBoard Instance { get; private set; }

    [Header("Colors")]
    public Color selectedColor = new Color(0.45f, 0.75f, 0.35f, 0.8f);
    public Color moveHighlightColor = new Color(0.3f, 0.6f, 0.9f, 0.6f);
    public Color captureHighlightColor = new Color(0.9f, 0.3f, 0.3f, 0.6f);
    public Color checkHighlightColor = new Color(0.95f, 0.2f, 0.2f, 0.7f);
    public Color plantHighlightColor = new Color(0.95f, 0.85f, 0.2f, 0.6f);
    public Color lastMoveHighlightColor = new Color(0.85f, 0.75f, 0.3f, 0.3f);

    public ChessPiece[,] board = new ChessPiece[8, 8];
    private GameObject[,] squares = new GameObject[8, 8];
    private SpriteRenderer[,] squareRenderers = new SpriteRenderer[8, 8];
    private GameObject[,] highlights = new GameObject[8, 8];
    private SpriteRenderer[,] highlightRenderers = new SpriteRenderer[8, 8];

    public bool isFlipped { get; private set; }

    // Last move tracking
    private Vector2Int lastMoveFrom;
    private Vector2Int lastMoveTo;
    private bool hasLastMove;

    private Sprite lightSquareSprite;
    private Sprite darkSquareSprite;
    private Sprite _highlightSprite;

    private void Awake()
    {
        Instance = this;
        lightSquareSprite = Resources.Load<Sprite>("ChessPieces/square_light");
        darkSquareSprite = Resources.Load<Sprite>("ChessPieces/square_dark");
    }

    public void CreateBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject sq = new GameObject($"Square_{x}_{y}");
                sq.transform.parent = transform;
                sq.transform.position = new Vector3(x, y, 0);
                var sr = sq.AddComponent<SpriteRenderer>();

                bool isLight = (x + y) % 2 != 0;
                Sprite sqSprite = isLight ? lightSquareSprite : darkSquareSprite;
                if (sqSprite != null)
                {
                    sr.sprite = sqSprite;
                    float spriteWorldSize = sqSprite.rect.width / sqSprite.pixelsPerUnit;
                    float scale = 1f / spriteWorldSize;
                    sq.transform.localScale = new Vector3(scale, scale, 1f);
                    sr.color = Color.white;
                }
                else
                {
                    sr.sprite = CreateFallbackSprite();
                    sr.color = isLight ? new Color(0.93f, 0.84f, 0.71f) : new Color(0.70f, 0.53f, 0.36f);
                }
                sr.sortingOrder = 0;
                squares[x, y] = sq;
                squareRenderers[x, y] = sr;

                GameObject hl = new GameObject($"Highlight_{x}_{y}");
                hl.transform.parent = sq.transform;
                hl.transform.localPosition = Vector3.zero;
                var hlSr = hl.AddComponent<SpriteRenderer>();
                hlSr.sprite = GetHighlightSprite();
                hlSr.color = Color.clear;
                hlSr.sortingOrder = 1;
                highlights[x, y] = hl;
                highlightRenderers[x, y] = hlSr;

                var col = sq.AddComponent<BoxCollider2D>();
                col.size = Vector2.one / sq.transform.localScale.x;
            }
        }
    }

    public void SetupPieces()
    {
        ClearAllPieces();
        hasLastMove = false;

        if (GameBootstrap.CurrentMode == GameMode.SeedChess)
        {
            CreatePiece(PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(PieceType.King, PieceColor.Black, 4, 7);
            return;
        }

        for (int x = 0; x < 8; x++)
        {
            CreatePiece(PieceType.Pawn, PieceColor.White, x, 1);
            CreatePiece(PieceType.Pawn, PieceColor.Black, x, 6);
        }

        PieceType[] backRank = { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen, PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook };
        for (int x = 0; x < 8; x++)
        {
            CreatePiece(backRank[x], PieceColor.White, x, 0);
            CreatePiece(backRank[x], PieceColor.Black, x, 7);
        }

        if (GameBootstrap.CurrentMode == GameMode.BluffyChess)
        {
            BluffyManager.Instance.StartSetupPhase();
        }
    }

    public ChessPiece CreatePiece(PieceType type, PieceColor color, int x, int y)
    {
        GameObject pieceObj = new GameObject($"{color}_{type}");
        pieceObj.transform.position = VisualPos(x, y);

        var piece = pieceObj.AddComponent<ChessPiece>();
        piece.Init(type, color, x, y);

        board[x, y] = piece;
        return piece;
    }

    public void RemovePiece(int x, int y)
    {
        if (board[x, y] != null)
        {
            if (board[x, y].isSeed)
                SeedManager.Instance.RemoveSeedAt(x, y);
            Destroy(board[x, y].gameObject);
            board[x, y] = null;
        }
    }

    public void SetLastMove(Vector2Int from, Vector2Int to)
    {
        lastMoveFrom = from;
        lastMoveTo = to;
        hasLastMove = true;
    }

    public void MovePiece(ChessPiece piece, int toX, int toY)
    {
        Vector2Int from = new Vector2Int(piece.x, piece.y);
        board[piece.x, piece.y] = null;
        if (board[toX, toY] != null)
            RemovePiece(toX, toY);
        board[toX, toY] = piece;
        piece.SetPosition(toX, toY);
        SetLastMove(from, new Vector2Int(toX, toY));
    }

    public ChessPiece MovePieceBluffy(ChessPiece piece, int toX, int toY)
    {
        Vector2Int from = new Vector2Int(piece.x, piece.y);
        board[piece.x, piece.y] = null;
        ChessPiece captured = board[toX, toY];
        if (captured != null)
        {
            captured.gameObject.SetActive(false);
        }
        board[toX, toY] = piece;
        piece.SetPosition(toX, toY);
        SetLastMove(from, new Vector2Int(toX, toY));
        return captured;
    }

    public void ClearAllPieces()
    {
        hasLastMove = false;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board[x, y] != null)
                {
                    Destroy(board[x, y].gameObject);
                    board[x, y] = null;
                }
            }
        }
    }

    public void ClearHighlights()
    {
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                highlightRenderers[x, y].color = Color.clear;

        // Re-apply persistent last move highlight
        if (hasLastMove)
        {
            highlightRenderers[lastMoveFrom.x, lastMoveFrom.y].color = lastMoveHighlightColor;
            highlightRenderers[lastMoveTo.x, lastMoveTo.y].color = lastMoveHighlightColor;
        }
    }

    public void HighlightSquare(int x, int y, Color color)
    {
        highlightRenderers[x, y].color = color;
    }

    public Vector3 VisualPos(int x, int y)
    {
        if (isFlipped)
            return new Vector3(7 - x, 7 - y, 0);
        return new Vector3(x, y, 0);
    }

    public Vector2Int? GetSquareFromWorldPos(Vector3 worldPos)
    {
        int vx = Mathf.RoundToInt(worldPos.x);
        int vy = Mathf.RoundToInt(worldPos.y);
        int x = isFlipped ? 7 - vx : vx;
        int y = isFlipped ? 7 - vy : vy;
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
            return new Vector2Int(x, y);
        return null;
    }

    public void SetFlipped(bool flipped)
    {
        if (isFlipped == flipped) return;
        isFlipped = flipped;
        RefreshVisualPositions();
    }

    private void RefreshVisualPositions()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (squares[x, y] != null)
                    squares[x, y].transform.position = VisualPos(x, y);

                ChessPiece p = board[x, y];
                if (p != null)
                    p.transform.position = VisualPos(x, y);
            }
        }

        if (SeedManager.Instance != null)
            SeedManager.Instance.RefreshSeedVisualPositions();
    }

    private Sprite GetHighlightSprite()
    {
        if (_highlightSprite != null) return _highlightSprite;
        _highlightSprite = CreateFallbackSprite();
        return _highlightSprite;
    }

    private static Sprite _fallbackSprite;
    private Sprite CreateFallbackSprite()
    {
        if (_fallbackSprite != null) return _fallbackSprite;

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _fallbackSprite;
    }
}
