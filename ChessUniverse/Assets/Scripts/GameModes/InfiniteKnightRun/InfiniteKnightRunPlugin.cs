using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InfiniteKnightRunPlugin : IGameModePlugin
{
    public string ModeId => "InfiniteKnightRun";
    public GameModeDefinition Definition { get; set; }

    private IKRSettings settings;
    private int BufferBelow => settings != null ? settings.bufferBelow : 3;
    private int BufferAbove => settings != null ? settings.bufferAbove : 10;
    private int SafeRows    => settings != null ? settings.safeRows : 5;
    private int RampRows    => settings != null ? settings.difficultyRampRows : 80;

    private const int Cols = 8;
    private int windowBottom, windowTop;

    private readonly Dictionary<long, CellData> cells = new();
    private readonly Dictionary<long, PieceType> blackPieces = new();
    private readonly HashSet<long> attackedSquares = new();

    private readonly Dictionary<long, GameObject> squareObjects = new();
    private readonly Dictionary<long, GameObject> pieceObjects = new();
    private readonly Dictionary<long, GameObject> highlightObjects = new();
    private readonly Dictionary<int, GameObject> rowLabelObjects = new();

    private int knightX, knightY;
    private GameObject knightObj;
    private int maxDistance;
    private int piecesEaten;
    private bool gameActive;
    private bool animating;
    private Vector3 animTarget;
    private bool pendingDeath;
    private Vector2Int pendingDeathPos; // the square where knight died

    // Death animation state
    private bool deathAnimating;
    private GameObject deathAttacker; // the piece that "captures" the knight
    private Vector3 deathAnimTarget;
    private float deathPauseTimer; // pause after capture before showing panel

    private Camera cam;
    private float cameraTargetY;

    private static readonly Color HoleColor        = new Color(0.08f, 0.08f, 0.10f);
    private static readonly Color SafeHighlight    = new Color(0.3f, 0.75f, 0.35f, 0.55f);
    private static readonly Color CaptureHighlight = new Color(0.95f, 0.85f, 0.15f, 0.6f);

    // UI
    private GameObject scorePanel;
    private TMPro.TextMeshProUGUI scoreText;
    private TMPro.TextMeshProUGUI bestText;
    private GameObject resignBtn;
    private GameObject gameOverPanel;
    private Canvas uiCanvas;

    private System.Random rng;

    private enum CellType { Normal, Hole }
    private struct CellData { public CellType type; }
    private enum MoveType { Safe, Capture, Danger }

    // ─── IGameModePlugin ─────────────────────────────────────

    public void Initialize() { }

    // Separate update helper that never gets disabled
    private GameObject updateObj;

    public void OnGameStart(PlayMode playMode)
    {
        settings = Resources.Load<IKRSettings>("IKRSettings");
        LoadBoardTheme();
        cam = Camera.main;
        rng = new System.Random();
        cells.Clear(); blackPieces.Clear(); attackedSquares.Clear();

        maxDistance = 0; piecesEaten = 0;
        gameActive = true; animating = false;
        pendingDeath = false; deathAnimating = false;
        deathPauseTimer = 0f;
        deathAttacker = null;

        knightX = rng.Next(1, 7); knightY = 2;
        windowBottom = 0; windowTop = -1;
        ExtendWindowTo(knightY + BufferAbove);
        EnsureStartingSafety();
        RebuildAttackMap();

        CreateAllVisuals();
        CreateKnight();
        HighlightLegalMoves();
        UpdateCamera(true);

        UIManager.Instance.ShowSplashText(
            "Ride as far north as you can!",
            2f, null, new Color(0.9f, 0.85f, 0.6f));
    }

    public void OnGameEnd()
    {
        gameActive = false; deathAnimating = false; deathPauseTimer = 0f;
        if (deathAttacker != null) { Object.Destroy(deathAttacker); deathAttacker = null; }
        if (updateObj != null) { Object.Destroy(updateObj); updateObj = null; }
        DestroyAllVisuals();
        DestroyKnight();
    }

    public void SetupBoard(ChessBoard board)
    {
        board.ClearAllPieces();
        board.gameObject.SetActive(false);
    }

    public void OnTurnEnd(PieceColor color) { }
    public bool OverridesUpdate() => true;
    public bool HandleInput(Vector2Int square) => false;
    public bool UsesCheck => false;
    public bool AllowsCastling => false;
    public List<Vector2Int> GetCustomMoves(ChessPiece piece, ChessPiece[,] board) => null;
    public bool HasCustomAI => false;
    public void PlayAITurn(ChessPiece[,] board, Vector2Int? enPassantTarget) { }

    // ─── UI ──────────────────────────────────────────────────

    public void CreateGameUI(Canvas canvas)
    {
        uiCanvas = canvas;

        // Score panel (top center)
        scorePanel = new GameObject("IKR_ScorePanel");
        scorePanel.transform.SetParent(canvas.transform, false);
        var panelImg = scorePanel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        var panelRt = panelImg.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 1);
        panelRt.anchorMax = new Vector2(0.5f, 1);
        panelRt.pivot = new Vector2(0.5f, 1);
        panelRt.anchoredPosition = new Vector2(0, -10);
        panelRt.sizeDelta = new Vector2(260, 70);

        var scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(scorePanel.transform, false);
        scoreText = scoreObj.AddComponent<TMPro.TextMeshProUGUI>();
        scoreText.text = "Distance: 0";
        scoreText.fontSize = 28;
        scoreText.fontStyle = TMPro.FontStyles.Bold;
        scoreText.color = Color.white;
        scoreText.alignment = TMPro.TextAlignmentOptions.Center;
        var scoreRt = scoreText.rectTransform;
        scoreRt.anchorMin = new Vector2(0, 0.45f);
        scoreRt.anchorMax = new Vector2(1, 1);
        scoreRt.offsetMin = new Vector2(8, 0);
        scoreRt.offsetMax = new Vector2(-8, -4);

        var bestObj = new GameObject("BestText");
        bestObj.transform.SetParent(scorePanel.transform, false);
        bestText = bestObj.AddComponent<TMPro.TextMeshProUGUI>();
        int pb = PlayerPrefs.GetInt("IKR_PersonalBest", 0);
        bestText.text = pb > 0 ? $"Best: {pb}" : "";
        bestText.fontSize = 16;
        bestText.color = new Color(0.7f, 0.7f, 0.75f);
        bestText.alignment = TMPro.TextAlignmentOptions.Center;
        var bestRt = bestText.rectTransform;
        bestRt.anchorMin = new Vector2(0, 0);
        bestRt.anchorMax = new Vector2(1, 0.45f);
        bestRt.offsetMin = new Vector2(8, 4);
        bestRt.offsetMax = new Vector2(-8, 0);

        // Resign button (bottom-left)
        resignBtn = new GameObject("IKR_ResignBtn");
        resignBtn.transform.SetParent(canvas.transform, false);
        var ri = resignBtn.AddComponent<Image>();
        ri.color = new Color(0.5f, 0.15f, 0.15f, 0.85f);
        var rrt = ri.rectTransform;
        rrt.anchorMin = new Vector2(0, 0); rrt.anchorMax = new Vector2(0, 0);
        rrt.pivot = new Vector2(0, 0);
        rrt.anchoredPosition = new Vector2(15, 15);
        rrt.sizeDelta = new Vector2(100, 40);
        var rb = resignBtn.AddComponent<Button>();
        rb.targetGraphic = ri;
        var rc = rb.colors;
        rc.highlightedColor = new Color(0.65f, 0.2f, 0.2f);
        rc.pressedColor = new Color(0.35f, 0.1f, 0.1f);
        rb.colors = rc;
        rb.onClick.AddListener(() => { if (gameActive) GameOver(); });
        var rto = new GameObject("Text");
        rto.transform.SetParent(resignBtn.transform, false);
        var rtmp = rto.AddComponent<TMPro.TextMeshProUGUI>();
        rtmp.text = "Resign"; rtmp.fontSize = 18;
        rtmp.alignment = TMPro.TextAlignmentOptions.Center;
        rtmp.color = new Color(1f, 0.7f, 0.7f);
        FillRect(rtmp.rectTransform);
    }

    private void CreateGameOverPanel(bool newBest, int personalBest, int goldEarned)
    {
        if (gameOverPanel != null) Object.Destroy(gameOverPanel);

        gameOverPanel = new GameObject("IKR_GameOverPanel");
        gameOverPanel.transform.SetParent(uiCanvas.transform, false);

        var bg = gameOverPanel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.03f, 0.05f, 0.95f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(440, 310);

        // Title
        MakeLabel(gameOverPanel.transform, "GAME OVER", 38, TMPro.FontStyles.Bold,
            new Color(0.95f, 0.85f, 0.7f), new Vector2(0, 110), new Vector2(400, 50));

        // Distance
        MakeLabel(gameOverPanel.transform, $"Distance: {maxDistance}", 30, TMPro.FontStyles.Bold,
            Color.white, new Vector2(0, 55), new Vector2(400, 42));

        // Best
        string bestLine = newBest ? "NEW BEST!" : $"Best: {personalBest}";
        var bestColor = newBest ? new Color(1f, 0.85f, 0.25f) : new Color(0.65f, 0.65f, 0.7f);
        MakeLabel(gameOverPanel.transform, bestLine, 22, newBest ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal,
            bestColor, new Vector2(0, 15), new Vector2(400, 32));

        // Gold
        if (goldEarned > 0)
            MakeLabel(gameOverPanel.transform, $"+{goldEarned} Golden Pawns", 20, TMPro.FontStyles.Bold,
                new Color(1f, 0.85f, 0.25f), new Vector2(0, -18), new Vector2(400, 28));

        // Buttons
        Color btnColor = new Color(0.2f, 0.5f, 0.25f);
        Color btnHi = new Color(0.3f, 0.65f, 0.35f);

        CreatePanelButton(gameOverPanel.transform, "Retry", new Vector2(-80, -90), btnColor, btnHi, () =>
        {
            Canvas savedCanvas = uiCanvas != null ? uiCanvas : UIManager.Instance.GetCanvas();
            Object.Destroy(gameOverPanel); gameOverPanel = null;
            DestroyGameUI();
            OnGameEnd();
            SetupBoard(ChessBoard.Instance);
            CreateGameUI(savedCanvas);
            OnGameStart(PlayMode.SinglePlayer);
        });

        CreatePanelButton(gameOverPanel.transform, "Back", new Vector2(80, -90),
            new Color(0.35f, 0.35f, 0.4f), new Color(0.45f, 0.45f, 0.52f), () =>
        {
            Object.Destroy(gameOverPanel); gameOverPanel = null;
            ChessBoard.Instance.gameObject.SetActive(true);
            GameManager.Instance.SetupCamera();
            GameManager.Instance.RestartGame();
        });
    }

    private void CreatePanelButton(Transform parent, string label, Vector2 pos,
        Color bgCol, Color hiCol, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(parent, false);
        var img = obj.AddComponent<Image>();
        img.color = bgCol;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(140, 44);
        rt.anchoredPosition = pos;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors; c.highlightedColor = hiCol; c.pressedColor = bgCol * 0.7f; btn.colors = c;
        btn.onClick.AddListener(onClick);
        var to = new GameObject("Text");
        to.transform.SetParent(obj.transform, false);
        var tmp = to.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 22; tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.color = Color.white;
        FillRect(tmp.rectTransform);
    }

    private TMPro.TextMeshProUGUI MakeLabel(Transform parent, string text, float size,
        TMPro.FontStyles style, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.color = color; tmp.alignment = TMPro.TextAlignmentOptions.Center;
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = pos;
        return tmp;
    }

    private static void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public void DestroyGameUI()
    {
        if (scorePanel != null) Object.Destroy(scorePanel);
        if (resignBtn != null) Object.Destroy(resignBtn);
        if (gameOverPanel != null) Object.Destroy(gameOverPanel);
        scorePanel = null; scoreText = null; bestText = null;
        resignBtn = null; gameOverPanel = null;
        // keep uiCanvas ref for Retry
    }

    // ─── Row Labels ──────────────────────────────────────────

    private void CreateRowLabel(int row)
    {
        if (rowLabelObjects.ContainsKey(row)) return;
        var obj = new GameObject($"IKR_Row_{row}");
        obj.transform.position = new Vector3(-0.8f, row, 0);
        var tmp = obj.AddComponent<TMPro.TextMeshPro>();
        tmp.text = row.ToString();
        tmp.fontSize = 3.5f;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineRight;
        tmp.color = new Color(0.45f, 0.45f, 0.5f);
        tmp.rectTransform.sizeDelta = new Vector2(1.2f, 1f);
        tmp.sortingOrder = 5;
        rowLabelObjects[row] = obj;
    }

    private void DestroyRowLabel(int row)
    {
        if (rowLabelObjects.TryGetValue(row, out var obj))
        { Object.Destroy(obj); rowLabelObjects.Remove(row); }
    }

    // ─── Procedural Generation ───────────────────────────────

    private long Key(int x, int y) => (long)y * Cols + x;

    private void ExtendWindowTo(int targetTop)
    {
        for (int row = windowTop + 1; row <= targetTop; row++)
            GenerateRow(row);
        windowTop = targetTop;
    }

    private void GenerateRow(int row)
    {
        float difficulty = Mathf.Clamp01((float)(row - SafeRows) / RampRows);
        float hMin = settings != null ? settings.holeChanceMin : 0.05f;
        float hMax = settings != null ? settings.holeChanceMax : 0.30f;
        float pMin = settings != null ? settings.pieceChanceMin : 0.03f;
        float pMax = settings != null ? settings.pieceChanceMax : 0.20f;

        for (int x = 0; x < Cols; x++)
        {
            long k = Key(x, row);
            if (cells.ContainsKey(k)) continue;
            bool isStart = (x == knightX && row == knightY);

            if (!isStart && row > SafeRows && rng.NextDouble() < Mathf.Lerp(hMin, hMax, difficulty))
            { cells[k] = new CellData { type = CellType.Hole }; continue; }

            cells[k] = new CellData { type = CellType.Normal };

            if (!isStart && row > SafeRows && rng.NextDouble() < Mathf.Lerp(pMin, pMax, difficulty))
                blackPieces[k] = PickBlackPieceType(difficulty);
        }
    }

    private PieceType PickBlackPieceType(float d)
    {
        double r = rng.NextDouble();
        if (d < 0.3f)
        {
            float p=settings!=null?settings.lowPawn:0.50f, n=settings!=null?settings.lowKnight:0.75f, b=settings!=null?settings.lowBishop:0.90f;
            if(r<p) return PieceType.Pawn; if(r<n) return PieceType.Knight; if(r<b) return PieceType.Bishop; return PieceType.Rook;
        }
        if (d < 0.6f)
        {
            float p=settings!=null?settings.midPawn:0.30f, n=settings!=null?settings.midKnight:0.50f, b=settings!=null?settings.midBishop:0.70f, rk=settings!=null?settings.midRook:0.90f;
            if(r<p) return PieceType.Pawn; if(r<n) return PieceType.Knight; if(r<b) return PieceType.Bishop; if(r<rk) return PieceType.Rook; return PieceType.Queen;
        }
        {
            float p=settings!=null?settings.highPawn:0.15f, n=settings!=null?settings.highKnight:0.30f, b=settings!=null?settings.highBishop:0.50f, rk=settings!=null?settings.highRook:0.75f;
            if(r<p) return PieceType.Pawn; if(r<n) return PieceType.Knight; if(r<b) return PieceType.Bishop; if(r<rk) return PieceType.Rook; return PieceType.Queen;
        }
    }

    private void EnsureStartingSafety()
    {
        var offsets = GetKnightMoveOffsets();
        var near = new List<long> { Key(knightX, knightY) };
        foreach (var o in offsets)
        { int nx=knightX+o.x, ny=knightY+o.y; if(nx>=0&&nx<Cols&&ny>=windowBottom&&ny<=windowTop) near.Add(Key(nx,ny)); }
        var deep = new List<long>(near);
        foreach (long k in near)
        { int cx=(int)(k%Cols), cy=(int)(k/Cols); foreach(var o in offsets){int nx=cx+o.x,ny=cy+o.y; if(nx>=0&&nx<Cols&&ny>=windowBottom&&ny<=windowTop) deep.Add(Key(nx,ny));} }
        foreach (long k in deep)
        { if(cells.ContainsKey(k)) cells[k]=new CellData{type=CellType.Normal}; blackPieces.Remove(k); }
    }

    // ─── Attack Map ──────────────────────────────────────────

    private void RebuildAttackMap()
    {
        attackedSquares.Clear();
        foreach (var kvp in blackPieces)
        {
            int px=(int)(kvp.Key%Cols), py=(int)(kvp.Key/Cols);
            if (py < windowBottom-2 || py > windowTop+2) continue;
            AddAttackedSquares(px, py, kvp.Value);
        }
    }

    private static readonly Vector2Int[] DiagDirs = { new(1,1), new(1,-1), new(-1,1), new(-1,-1) };
    private static readonly Vector2Int[] OrtoDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void AddAttackedSquares(int px, int py, PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn: TryAddAttack(px-1,py-1); TryAddAttack(px+1,py-1); break;
            case PieceType.Knight: foreach(var o in GetKnightMoveOffsets()) TryAddAttack(px+o.x,py+o.y); break;
            case PieceType.Bishop: AddLineAttacks(px,py,DiagDirs); break;
            case PieceType.Rook:   AddLineAttacks(px,py,OrtoDirs); break;
            case PieceType.Queen:  AddLineAttacks(px,py,DiagDirs); AddLineAttacks(px,py,OrtoDirs); break;
        }
    }

    private void TryAddAttack(int x, int y)
    { if(x>=0&&x<Cols&&y>=windowBottom&&y<=windowTop) attackedSquares.Add(Key(x,y)); }

    private void AddLineAttacks(int px, int py, Vector2Int[] dirs)
    {
        foreach (var dir in dirs)
            for (int s=1; s<=8; s++)
            {
                int nx=px+dir.x*s, ny=py+dir.y*s;
                if(nx<0||nx>=Cols||ny<windowBottom||ny>windowTop) break;
                long k=Key(nx,ny);
                if(cells.TryGetValue(k, out var c)&&c.type==CellType.Hole) break;
                attackedSquares.Add(k);
                if(blackPieces.ContainsKey(k)) break;
                if(nx==knightX&&ny==knightY) break;
            }
    }

    private bool IsPieceProtected(int px, int py)
    {
        long pk = Key(px,py);
        foreach (var kvp in blackPieces)
        {
            if(kvp.Key==pk) continue;
            int ox=(int)(kvp.Key%Cols), oy=(int)(kvp.Key/Cols);
            if(Mathf.Abs(oy-py)>8) continue;
            if(DoesAttack(ox,oy,kvp.Value,px,py)) return true;
        }
        return false;
    }

    private bool DoesAttack(int ax,int ay,PieceType type,int tx,int ty)
    {
        switch(type)
        {
            case PieceType.Pawn: return (ty==ay-1)&&(tx==ax-1||tx==ax+1);
            case PieceType.Knight: int dx=Mathf.Abs(tx-ax),dy=Mathf.Abs(ty-ay); return (dx==1&&dy==2)||(dx==2&&dy==1);
            case PieceType.Bishop: return CheckLine(ax,ay,tx,ty,true);
            case PieceType.Rook: return CheckLine(ax,ay,tx,ty,false);
            case PieceType.Queen: return CheckLine(ax,ay,tx,ty,true)||CheckLine(ax,ay,tx,ty,false);
        }
        return false;
    }

    private bool CheckLine(int ax,int ay,int tx,int ty,bool diag)
    {
        if(diag){if(Mathf.Abs(tx-ax)!=Mathf.Abs(ty-ay)||tx==ax)return false;}
        else{if(tx!=ax&&ty!=ay)return false;}
        int sx=tx>ax?1:(tx<ax?-1:0), sy=ty>ay?1:(ty<ay?-1:0);
        int cx=ax+sx, cy=ay+sy;
        while(cx!=tx||cy!=ty)
        {
            if(cx<0||cx>=Cols)return false;
            long k=Key(cx,cy);
            if(cells.TryGetValue(k,out var c)&&c.type==CellType.Hole)return false;
            if(blackPieces.ContainsKey(k))return false;
            if(cx==knightX&&cy==knightY)return false;
            cx+=sx; cy+=sy;
        }
        return true;
    }

    // ─── Legal Moves ─────────────────────────────────────────

    private List<(Vector2Int pos, MoveType type)> GetLegalMovesTyped()
    {
        var moves = new List<(Vector2Int, MoveType)>();
        foreach (var off in GetKnightMoveOffsets())
        {
            int nx=knightX+off.x, ny=knightY+off.y;
            if(nx<0||nx>=Cols||ny<windowBottom||ny>windowTop) continue;
            long k = Key(nx,ny);
            if(cells.TryGetValue(k, out var c)&&c.type==CellType.Hole) continue;

            bool isAttacked = attackedSquares.Contains(k);
            bool hasPiece = blackPieces.ContainsKey(k);
            bool prot = hasPiece && IsPieceProtected(nx,ny);

            if (hasPiece && !prot)
                moves.Add((new Vector2Int(nx,ny), MoveType.Capture));
            else if ((hasPiece && prot) || isAttacked)
                moves.Add((new Vector2Int(nx,ny), MoveType.Danger));
            else
                moves.Add((new Vector2Int(nx,ny), MoveType.Safe));
        }
        return moves;
    }

    private static Vector2Int[] GetKnightMoveOffsets() => new Vector2Int[]
    { new(1,2),new(2,1),new(2,-1),new(1,-2),new(-1,-2),new(-2,-1),new(-2,1),new(-1,2) };

    // ─── Find attacker for death animation ───────────────────

    private (int ax, int ay, PieceType type) FindNearestAttacker(int tx, int ty)
    {
        float bestDist = float.MaxValue;
        int bx = -1, by = -1; PieceType bt = PieceType.Pawn;
        foreach (var kvp in blackPieces)
        {
            int px=(int)(kvp.Key%Cols), py=(int)(kvp.Key/Cols);
            if (!DoesAttack(px, py, kvp.Value, tx, ty)) continue;
            float dist = Mathf.Abs(px-tx) + Mathf.Abs(py-ty);
            if (dist < bestDist) { bestDist=dist; bx=px; by=py; bt=kvp.Value; }
        }
        return (bx, by, bt);
    }

    // ─── Visuals ─────────────────────────────────────────────

    private Color themeLight = new Color(0.941f, 0.851f, 0.710f);
    private Color themeDark  = new Color(0.710f, 0.533f, 0.388f);

    private void LoadBoardTheme()
    {
        string activeId = PlayerPrefs.GetString("Active_BoardTheme", "theme_classic");

        // Try registry first
        var registry = Resources.Load<MarketRegistry>("MarketRegistry");
        if (registry != null && registry.items != null)
        {
            foreach (var item in registry.items)
            {
                if (item != null && item.itemId == activeId && item.category == MarketCategory.BoardTheme)
                { themeLight = item.lightSquareColor; themeDark = item.darkSquareColor; return; }
            }
        }

        // Built-in themes (same as UIManager.CreateBuiltInMarketItems)
        switch (activeId)
        {
            case "theme_dark":
                themeLight = new Color(0.227f, 0.227f, 0.290f);
                themeDark  = new Color(0.149f, 0.149f, 0.208f);
                return;
            case "theme_forest":
                themeLight = new Color(0.659f, 0.835f, 0.635f);
                themeDark  = new Color(0.357f, 0.549f, 0.353f);
                return;
        }
        // theme_classic or unknown → keep defaults
    }

    private Color GetSquareColor(int x, int y)
    {
        bool isLight = (x+y)%2 != 0;
        return isLight ? themeLight : themeDark;
    }

    private void CreateAllVisuals()
    {
        DestroyAllVisuals();
        for (int y=windowBottom; y<=windowTop; y++)
        { for (int x=0; x<Cols; x++) CreateSquareVisual(x,y); CreateRowLabel(y); }
    }

    private void CreateSquareVisual(int x, int y)
    {
        long k = Key(x,y);
        if (squareObjects.ContainsKey(k)) return;

        var sq = new GameObject($"IKR_Sq_{x}_{y}");
        sq.transform.position = new Vector3(x,y,0);
        var sr = sq.AddComponent<SpriteRenderer>();
        sr.sprite = GetFallbackSprite(); sr.sortingOrder = 0;
        sr.color = (cells.TryGetValue(k, out var c)&&c.type==CellType.Hole) ? HoleColor : GetSquareColor(x,y);
        squareObjects[k] = sq;

        var hl = new GameObject($"IKR_Hl_{x}_{y}");
        hl.transform.SetParent(sq.transform,false); hl.transform.localPosition = Vector3.zero;
        var hlSr = hl.AddComponent<SpriteRenderer>();
        hlSr.sprite = GetFallbackSprite(); hlSr.color = Color.clear; hlSr.sortingOrder = 1;
        highlightObjects[k] = hl;

        sq.AddComponent<BoxCollider2D>();
        if (blackPieces.ContainsKey(k)) CreatePieceVisual(x,y,blackPieces[k]);
    }

    private void CreatePieceVisual(int x, int y, PieceType type)
    {
        long k = Key(x,y); if(pieceObjects.ContainsKey(k)) return;
        var obj = new GameObject($"IKR_P_{x}_{y}");
        obj.transform.position = new Vector3(x,y,0);
        var sr = obj.AddComponent<SpriteRenderer>(); sr.sortingOrder = 2;
        Sprite sprite = ChessPiece.GetPieceSprite(PieceColor.Black, type);
        if(sprite!=null){sr.sprite=sprite; float s=0.935f/(sprite.rect.height/sprite.pixelsPerUnit); obj.transform.localScale=new Vector3(s,s,1f);}
        pieceObjects[k] = obj;
    }

    private void DestroyPieceVisual(long k)
    { if(pieceObjects.TryGetValue(k,out var o)){Object.Destroy(o);pieceObjects.Remove(k);} }

    private void CreateKnight()
    {
        // Update helper on a separate always-active object
        if (updateObj != null) Object.Destroy(updateObj);
        updateObj = new GameObject("IKR_UpdateHelper");
        updateObj.AddComponent<IKRUpdateHelper>().plugin = this;

        knightObj = new GameObject("IKR_Knight");
        knightObj.transform.position = new Vector3(knightX,knightY,0);
        var sr = knightObj.AddComponent<SpriteRenderer>(); sr.sortingOrder = 3;
        Sprite sprite = ChessPiece.GetPieceSprite(PieceColor.White, PieceType.Knight);
        if(sprite!=null){sr.sprite=sprite; float s=0.935f/(sprite.rect.height/sprite.pixelsPerUnit); knightObj.transform.localScale=new Vector3(s,s,1f);}
    }

    private void DestroyKnight() { if(knightObj!=null) Object.Destroy(knightObj); knightObj=null; }

    private void DestroyAllVisuals()
    {
        foreach(var kvp in squareObjects) if(kvp.Value!=null) Object.Destroy(kvp.Value);
        squareObjects.Clear(); highlightObjects.Clear();
        foreach(var kvp in pieceObjects) if(kvp.Value!=null) Object.Destroy(kvp.Value);
        pieceObjects.Clear();
        foreach(var kvp in rowLabelObjects) if(kvp.Value!=null) Object.Destroy(kvp.Value);
        rowLabelObjects.Clear();
    }

    private static Sprite _ikrSprite;
    private static Sprite GetFallbackSprite()
    {
        if(_ikrSprite!=null) return _ikrSprite;
        var tex=new Texture2D(4,4); var px=new Color[16];
        for(int i=0;i<16;i++) px[i]=Color.white;
        tex.SetPixels(px); tex.Apply(); tex.filterMode=FilterMode.Point;
        _ikrSprite=Sprite.Create(tex,new Rect(0,0,4,4),new Vector2(0.5f,0.5f),4);
        return _ikrSprite;
    }

    // ─── Highlights ──────────────────────────────────────────

    private void ClearHighlights()
    { foreach(var kvp in highlightObjects){var sr=kvp.Value?.GetComponent<SpriteRenderer>(); if(sr!=null) sr.color=Color.clear;} }

    private void HighlightLegalMoves()
    {
        ClearHighlights();
        foreach (var (pos, type) in GetLegalMovesTyped())
        {
            long k = Key(pos.x, pos.y);
            if (!highlightObjects.TryGetValue(k, out var hlObj)) continue;
            var sr = hlObj.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            // All moves look the same — no red for danger squares
            sr.color = (type == MoveType.Capture) ? CaptureHighlight : SafeHighlight;
        }
    }

    // ─── Camera ──────────────────────────────────────────────

    private void UpdateCamera(bool instant)
    {
        float offset = settings != null ? settings.cameraOffsetY : 1.5f;
        cameraTargetY = knightY + offset;
        if (instant && cam != null)
        { cam.transform.position = new Vector3(3.5f, cameraTargetY, -10f); cam.orthographicSize = GetCamSize(); }
    }

    private float GetCamSize()
    { if(cam==null)return 6f; float a=cam.aspect; return a<1f?Mathf.Max(6.5f,4.4f/a):6.5f; }

    // ─── Update ──────────────────────────────────────────────

    public void OnUpdate()
    {
        // Camera always smooth-follows even during death anim
        float smooth = settings != null ? settings.cameraSmoothSpeed : 5f;
        if (cam != null)
        {
            var pos = cam.transform.position;
            cam.transform.position = new Vector3(3.5f, Mathf.Lerp(pos.y, cameraTargetY, smooth*Time.deltaTime), -10f);
            cam.orthographicSize = GetCamSize();
        }

        // Death animation: attacker piece slides to knight position
        if (deathAnimating && deathAttacker != null)
        {
            deathAttacker.transform.position = Vector3.MoveTowards(
                deathAttacker.transform.position, deathAnimTarget, 8f * Time.deltaTime);
            if (Vector3.Distance(deathAttacker.transform.position, deathAnimTarget) < 0.01f)
            {
                deathAttacker.transform.position = deathAnimTarget;
                deathAnimating = false;
                if (knightObj != null) knightObj.SetActive(false);
                deathPauseTimer = 1f; // pause before game over panel
            }
            return;
        }

        // Post-capture pause
        if (deathPauseTimer > 0f)
        {
            deathPauseTimer -= Time.deltaTime;
            if (deathPauseTimer <= 0f)
                ShowGameOverPanel();
            return;
        }

        if (!gameActive) return;

        // Knight move animation
        if (animating && knightObj != null)
        {
            knightObj.transform.position = Vector3.MoveTowards(
                knightObj.transform.position, animTarget, 12f * Time.deltaTime);
            if (Vector3.Distance(knightObj.transform.position, animTarget) < 0.01f)
            {
                knightObj.transform.position = animTarget;
                animating = false;
                AfterMove();
            }
            return;
        }

        // Click input
        if (Input.GetMouseButtonDown(0) && !animating)
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            int cx = Mathf.RoundToInt(wp.x), cy = Mathf.RoundToInt(wp.y);
            var target = new Vector2Int(cx, cy);
            foreach (var (pos, type) in GetLegalMovesTyped())
            {
                if (pos == target)
                {
                    pendingDeath = (type == MoveType.Danger);
                    if (pendingDeath) pendingDeathPos = pos;
                    MoveKnightTo(cx, cy);
                    break;
                }
            }
        }
    }

    // ─── Move ────────────────────────────────────────────────

    private void MoveKnightTo(int tx, int ty)
    {
        knightX = tx; knightY = ty;
        animTarget = new Vector3(tx, ty, 0);
        animating = true;
        if (ty > maxDistance) maxDistance = ty;
        UpdateCamera(false);
    }

    private void AfterMove()
    {
        if (pendingDeath)
        {
            pendingDeath = false;
            // Start death animation: find nearest attacker, animate it to knight
            StartDeathAnimation(pendingDeathPos.x, pendingDeathPos.y);
            return;
        }

        // Capture
        long tk = Key(knightX, knightY);
        if (blackPieces.ContainsKey(tk))
        { blackPieces.Remove(tk); DestroyPieceVisual(tk); piecesEaten++; }

        // Extend
        int neededTop = knightY + BufferAbove;
        if (neededTop > windowTop)
        {
            ExtendWindowTo(neededTop);
            for (int y = windowTop-BufferAbove+1; y <= windowTop; y++)
            { for (int x=0; x<Cols; x++) CreateSquareVisual(x,y); CreateRowLabel(y); }
        }

        // Prune
        int newBot = knightY - BufferBelow;
        if (newBot > windowBottom)
        {
            for (int y=windowBottom; y<newBot; y++)
            {
                for (int x=0; x<Cols; x++)
                {
                    long k=Key(x,y); cells.Remove(k); blackPieces.Remove(k);
                    if(squareObjects.TryGetValue(k,out var sq)){Object.Destroy(sq);squareObjects.Remove(k);highlightObjects.Remove(k);}
                    DestroyPieceVisual(k);
                }
                DestroyRowLabel(y);
            }
            windowBottom = newBot;
        }

        RebuildAttackMap();
        UpdateScoreUI();
        HighlightLegalMoves();

        if (GetLegalMovesTyped().Count == 0) GameOver();
    }

    private void StartDeathAnimation(int tx, int ty)
    {
        gameActive = false;
        ClearHighlights();
        if (resignBtn != null) resignBtn.SetActive(false);

        var (ax, ay, atype) = FindNearestAttacker(tx, ty);
        if (ax < 0)
        {
            if (knightObj != null) knightObj.SetActive(false);
            FinishGameOver();
            return;
        }

        // Use the original piece object — just raise its sorting order and animate it
        long attackerKey = Key(ax, ay);
        if (pieceObjects.TryGetValue(attackerKey, out var existingPiece))
        {
            deathAttacker = existingPiece;
            pieceObjects.Remove(attackerKey); // detach from dict so cleanup won't destroy it early
            var sr = deathAttacker.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 4;
        }
        else
        {
            // Fallback: create one if not found (shouldn't happen)
            deathAttacker = new GameObject("IKR_DeathAttacker");
            deathAttacker.transform.position = new Vector3(ax, ay, 0);
            var sr = deathAttacker.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            Sprite sprite = ChessPiece.GetPieceSprite(PieceColor.Black, atype);
            if (sprite != null)
            { sr.sprite = sprite; float s = 0.935f/(sprite.rect.height/sprite.pixelsPerUnit); deathAttacker.transform.localScale = new Vector3(s,s,1f); }
        }

        deathAnimTarget = new Vector3(tx, ty, 0);
        deathAnimating = true;
    }

    private void ShowGameOverPanel()
    {
        // Clean up death attacker
        if (deathAttacker != null) { Object.Destroy(deathAttacker); deathAttacker = null; }
        FinishGameOver();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = $"Distance: {maxDistance}";
    }

    // ─── Game Over ───────────────────────────────────────────

    private void GameOver()
    {
        gameActive = false;
        ClearHighlights();
        if (resignBtn != null) resignBtn.SetActive(false);
        if (knightObj != null) knightObj.SetActive(false);
        FinishGameOver();
    }

    private void FinishGameOver()
    {
        int personalBest = PlayerPrefs.GetInt("IKR_PersonalBest", 0);
        bool newBest = maxDistance > personalBest;
        if (newBest) { PlayerPrefs.SetInt("IKR_PersonalBest", maxDistance); PlayerPrefs.Save(); }

        int goldPerDist = settings != null ? settings.goldPerDistance : 5;
        int goldEarned = (maxDistance > 0 && goldPerDist > 0) ? Mathf.Max(1, maxDistance / goldPerDist) : 0;
        if (goldEarned > 0) UIManager.AddGold(goldEarned);

        SyncIKRLeaderboard();
        UIManager.Instance.SetGoldEarned(goldEarned);

        CreateGameOverPanel(newBest, Mathf.Max(maxDistance, personalBest), goldEarned);
    }

    private void SyncIKRLeaderboard()
    {
        var ns = Resources.Load<NetworkSettings>("NetworkSettings");
        if (ns == null || string.IsNullOrEmpty(ns.firebaseProjectUrl)) return;
        string did = SystemInfo.deviceUniqueIdentifier.Replace("-","");
        string sid = did.Substring(0, Mathf.Min(16, did.Length));
        string name = PlayerPrefs.GetString("PlayerName","Anonymous");
        int best = PlayerPrefs.GetInt("IKR_PersonalBest", 0);
        string json = $"{{\"name\":\"{name}\",\"bestDistance\":{best},\"lastUpdated\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}";
        if (knightObj != null)
        { var h = knightObj.GetComponent<IKRUpdateHelper>(); if(h!=null) h.StartCoroutine(PatchCo($"{ns.firebaseProjectUrl}/ikr_leaderboard/{sid}.json", json)); }
    }

    private static System.Collections.IEnumerator PatchCo(string url, string json)
    {
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        using var req = new UnityEngine.Networking.UnityWebRequest(url, "PATCH");
        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(body);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }
}

public class IKRUpdateHelper : MonoBehaviour
{
    public InfiniteKnightRunPlugin plugin;
    private void Update() => plugin?.OnUpdate();
}
