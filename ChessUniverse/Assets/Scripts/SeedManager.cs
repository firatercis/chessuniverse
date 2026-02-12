using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SeedManager : MonoBehaviour
{
    public static SeedManager Instance { get; private set; }

    public class SeedData
    {
        public int x;
        public int y;
        public PieceColor color;
        public PieceType targetType;
        public int turnsRemaining;
        public bool justPlanted;
        public GameObject visualObj;
    }

    private List<SeedData> seeds = new();

    public static readonly Dictionary<PieceType, int> GrowthTurns = new()
    {
        { PieceType.Pawn, 1 },
        { PieceType.Knight, 3 },
        { PieceType.Bishop, 3 },
        { PieceType.Rook, 5 },
        { PieceType.Queen, 9 }
    };

    private void Awake()
    {
        Instance = this;
    }

    public void PlantSeed(int x, int y, PieceColor color, PieceType targetType)
    {
        // Place a marker piece on the board (PieceType.None so it has no moves)
        var marker = ChessBoard.Instance.CreatePiece(PieceType.None, color, x, y);
        marker.isSeed = true;

        // Create seed visual
        var visualObj = CreateSeedVisual(x, y, color, targetType, GrowthTurns[targetType]);

        var data = new SeedData
        {
            x = x,
            y = y,
            color = color,
            targetType = targetType,
            turnsRemaining = GrowthTurns[targetType],
            justPlanted = true,
            visualObj = visualObj
        };
        seeds.Add(data);
    }

    public void OnTurnEnd(PieceColor color)
    {
        var toHatch = new List<SeedData>();

        foreach (var seed in seeds)
        {
            if (seed.color != color) continue;

            // Skip the turn the seed was planted
            if (seed.justPlanted)
            {
                seed.justPlanted = false;
                continue;
            }

            seed.turnsRemaining--;
            if (seed.turnsRemaining <= 0)
            {
                toHatch.Add(seed);
            }
            else
            {
                UpdateSeedVisual(seed);
            }
        }

        foreach (var seed in toHatch)
        {
            HatchSeed(seed);
        }
    }

    private void HatchSeed(SeedData seed)
    {
        // Remove marker piece
        ChessBoard.Instance.RemovePiece(seed.x, seed.y);

        // Destroy seed visual
        if (seed.visualObj != null)
            Destroy(seed.visualObj);

        // Create real piece
        var piece = ChessBoard.Instance.CreatePiece(seed.targetType, seed.color, seed.x, seed.y);
        // Pawn spawns with hasMoved=false so it can double-move
        if (seed.targetType != PieceType.Pawn)
            piece.hasMoved = true;

        seeds.Remove(seed);
    }

    public void RemoveSeedAt(int x, int y)
    {
        for (int i = seeds.Count - 1; i >= 0; i--)
        {
            if (seeds[i].x == x && seeds[i].y == y)
            {
                if (seeds[i].visualObj != null)
                    Destroy(seeds[i].visualObj);
                seeds.RemoveAt(i);
                return;
            }
        }
    }

    public List<Vector2Int> GetPlantableSquares(ChessPiece king)
    {
        var result = new List<Vector2Int>();
        var board = ChessBoard.Instance.board;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = king.x + dx;
                int ny = king.y + dy;
                if (nx >= 0 && nx < 8 && ny >= 0 && ny < 8 && board[nx, ny] == null)
                    result.Add(new Vector2Int(nx, ny));
            }
        }
        return result;
    }

    public bool HasAnySeed(PieceColor color)
    {
        foreach (var seed in seeds)
            if (seed.color == color)
                return true;
        return false;
    }

    public void ClearAll()
    {
        foreach (var seed in seeds)
        {
            if (seed.visualObj != null)
                Destroy(seed.visualObj);
        }
        seeds.Clear();
    }

    private GameObject CreateSeedVisual(int x, int y, PieceColor color, PieceType targetType, int turnsRemaining)
    {
        // Root container (unscaled, used to group sprite + text)
        var obj = new GameObject($"Seed_{color}_{targetType}");
        obj.transform.position = new Vector3(x, y, 0);

        // Sprite child, scaled down and semi-transparent
        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(obj.transform, false);
        var sr = spriteObj.AddComponent<SpriteRenderer>();
        string colorPrefix = color == PieceColor.White ? "w" : "b";
        string pieceName = targetType switch
        {
            PieceType.Pawn => "pawn",
            PieceType.Knight => "knight",
            PieceType.Bishop => "bishop",
            PieceType.Rook => "rook",
            PieceType.Queen => "queen",
            _ => "pawn"
        };
        Sprite sprite = Resources.Load<Sprite>($"ChessPieces/{colorPrefix}_{pieceName}");
        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            sr.color = new Color(1f, 1f, 1f, 0.5f);
            float pixelsPerUnit = sprite.pixelsPerUnit;
            float spriteWorldSize = sprite.rect.height / pixelsPerUnit;
            float targetSize = 0.5f;
            float scale = targetSize / spriteWorldSize;
            spriteObj.transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Turn counter text (direct child of root, not affected by sprite scale)
        var textObj = new GameObject("TurnCounter");
        textObj.transform.SetParent(obj.transform, false);
        textObj.transform.localPosition = new Vector3(0.3f, -0.3f, 0);
        var tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = turnsRemaining.ToString();
        tmp.fontSize = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.2f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.sortingOrder = 4;
        tmp.rectTransform.sizeDelta = new Vector2(0.6f, 0.6f);

        return obj;
    }

    private void UpdateSeedVisual(SeedData seed)
    {
        if (seed.visualObj == null) return;
        var textObj = seed.visualObj.transform.Find("TurnCounter");
        if (textObj != null)
        {
            var tmp = textObj.GetComponent<TextMeshPro>();
            if (tmp != null)
                tmp.text = seed.turnsRemaining.ToString();
        }
    }
}
