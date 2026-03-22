using UnityEngine;
using System.Collections.Generic;

public enum GameState
{
    Playing,
    Check,
    Checkmate,
    Stalemate
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PieceColor currentTurn = PieceColor.White;
    public GameState gameState = GameState.Playing;

    private ChessPiece selectedPiece;
    private List<Vector2Int> currentLegalMoves = new();
    public Vector2Int? enPassantTarget { get; private set; }
    private bool waitingForPromotion;
    private ChessPiece promotingPawn;
    private Vector2Int promotionMoveFrom;

    // Play mode
    public PlayMode currentPlayMode = PlayMode.Local;
    private bool waitingForAI;

    // Seed Chess state
    private bool isPlantingMode;
    private PieceType pendingSeedType;
    private bool showingMenu = true;

    private bool isBluffyMode => GameBootstrap.CurrentMode == GameMode.BluffyChess;
    private bool isOnline => currentPlayMode == PlayMode.Online;

    private Camera mainCam;

    // Replay mode
    private bool isReplayMode;
    private List<ReplayAction> replayActions;
    private int replayIndex;
    private string replayResult;

    // Tutorial
    private bool isTutorialMode;
    private int tutorialStep;
    private bool isBluffyTutorial;

    private void Awake()
    {
        Instance = this;
    }

    // ─── PlayerPrefs versioning ───
    // Bump this constant whenever stored data format changes.
    // Add a migration block below for each version increment.
    private const int AppDataVersion = 1;

    private static void MigratePlayerPrefsIfNeeded()
    {
        int stored = PlayerPrefs.GetInt("AppDataVersion", 0);
        if (stored >= AppDataVersion) return;

        if (stored < 1)
        {
            // v0 → v1: PlayerName key introduced.
            // Nothing to migrate — new users will be prompted for their name.
        }

        // Future migrations go here:
        // if (stored < 2) { ... }

        PlayerPrefs.SetInt("AppDataVersion", AppDataVersion);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        mainCam = Camera.main;
        SetupCamera();
        MigratePlayerPrefsIfNeeded();
        ChessBoard.Instance.CreateBoard();
        UIManager.Instance.ShowMainMenu();
    }

    public void StartGame(GameMode mode, PlayMode playMode = PlayMode.Local)
    {
        GameBootstrap.CurrentMode = mode;
        currentPlayMode = playMode;
        showingMenu = false;
        waitingForAI = false;
        UIManager.Instance.HideMainMenu();
        UIManager.Instance.ShowCurrencyDisplay();

        currentTurn = PieceColor.White;
        gameState = GameState.Playing;
        selectedPiece = null;
        currentLegalMoves.Clear();
        enPassantTarget = null;
        waitingForPromotion = false;
        promotingPawn = null;
        isPlantingMode = false;

        ChessAI.Instance.ResetHistory();
        SeedManager.Instance.ClearAll();

        // Online: guest sees board flipped (Black at bottom)
        if (isOnline && !NetworkManager.Instance.IsHost)
            ChessBoard.Instance.SetFlipped(true);
        else
            ChessBoard.Instance.SetFlipped(false);

        ChessBoard.Instance.SetupPieces();

        GameLogger.Instance?.StartGame(mode, playMode);

        if (!isBluffyMode)
            UIManager.Instance.UpdateTurnText(currentTurn);
        // Bluffy mode: SetupPieces calls BluffyManager.StartSetupPhase which handles UI

        UpdateSeedButtonsVisibility();

        // Start tutorial for SeedChess SP (first time only)
        if (mode == GameMode.SeedChess && playMode == PlayMode.SinglePlayer
            && PlayerPrefs.GetInt("SeedTutorialDone", 0) == 0)
        {
            StartTutorial();
        }

        // Start tutorial for BluffyChess SP (first time only)
        if (mode == GameMode.BluffyChess && playMode == PlayMode.SinglePlayer
            && PlayerPrefs.GetInt("BluffyTutorialDone", 0) == 0)
        {
            StartBluffyTutorial();
        }
    }

    private void SetupCamera()
    {
        mainCam.orthographic = true;
        mainCam.transform.position = new Vector3(3.5f, 3.5f, -10);
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
        UpdateCameraSize();
    }

    public void UpdateCameraSize()
    {
        if (mainCam == null) return;
        float aspect = mainCam.aspect; // width / height
        // Portrait: board must fit horizontally → orthographicSize = halfBoardWidth / aspect
        // Landscape: fixed size that fits the board comfortably
        mainCam.orthographicSize = aspect < 1f ? Mathf.Max(5.5f, 4.4f / aspect) : 5.5f;
    }

    private void Update()
    {
        if (isReplayMode) return;
        if (showingMenu) return;
        if (waitingForPromotion) return;
        if (gameState == GameState.Checkmate || gameState == GameState.Stalemate) return;
        if (waitingForAI) return;
        // Tutorial: block input during message/overlay steps
        if (isTutorialMode)
        {
            if (isBluffyTutorial && (tutorialStep == 0 || tutorialStep == 2 || tutorialStep == 3)) return;
            if (!isBluffyTutorial && (tutorialStep == 0 || tutorialStep == 1 || tutorialStep == 4 || tutorialStep == 6)) return;
        }

        // Online mode: block input when it's opponent's turn
        if (isOnline && !isBluffyMode && !NetworkManager.Instance.IsMyTurn()) return;

        // Bluffy mode: check phase-based input blocking
        if (isBluffyMode)
        {
            var phase = BluffyManager.Instance.currentPhase;

            // Bluffy tutorial: detect setup → playing transition
            if (isTutorialMode && isBluffyTutorial && tutorialStep == 1
                && phase == BluffyPhase.Playing && currentTurn == PieceColor.White)
            {
                tutorialStep = 2;
                UIManager.Instance.HideFinger();
                UIManager.Instance.ShowTutorialMessage(
                    "Big pieces can move in any direction\n(Queen + Knight combined).\n\nAfter a big piece moves, opponent\ncan call Bluff! or Accept.");
                return;
            }

            if (phase == BluffyPhase.PassDevice
                || phase == BluffyPhase.WaitingBluff
                || phase == BluffyPhase.GameOver)
                return;

            // SP Bluffy: block input during AI's turn
            if (currentPlayMode == PlayMode.SinglePlayer
                && currentTurn == PieceColor.Black
                && phase == BluffyPhase.Playing)
                return;

            // Online Bluffy: block input during opponent's turn (playing phase)
            if (isOnline && phase == BluffyPhase.Playing && !NetworkManager.Instance.IsMyTurn())
                return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int? clicked = ChessBoard.Instance.GetSquareFromWorldPos(worldPos);
                if (!clicked.HasValue) return;

                switch (phase)
                {
                    case BluffyPhase.Setup:
                        BluffyManager.Instance.HandleSetupClick(clicked.Value);
                        break;
                    case BluffyPhase.Playing:
                        HandleSquareClick(clicked.Value);
                        break;
                    case BluffyPhase.Sacrifice:
                        BluffyManager.Instance.HandleSacrificeClick(clicked.Value);
                        break;
                    case BluffyPhase.Rearrange:
                        BluffyManager.Instance.HandleRearrangeClick(clicked.Value);
                        break;
                }
            }
            return;
        }

        // AI turn: trigger automatically (not for Bluffy - BluffyManager handles AI)
        if (currentPlayMode == PlayMode.SinglePlayer
            && !isBluffyMode
            && currentTurn == PieceColor.Black
            && gameState != GameState.Checkmate
            && gameState != GameState.Stalemate)
        {
            // Tutorial step 3: AI plants Knight instead of normal play
            if (isTutorialMode && tutorialStep == 3)
            {
                waitingForAI = true;
                ChessPiece bKing = FindKing(PieceColor.Black);
                if (bKing != null)
                {
                    var plantable = SeedManager.Instance.GetPlantableSquares(bKing);
                    if (plantable.Count > 0)
                    {
                        var sq = plantable[0];
                        ApplyAIPlanting(sq, PieceType.Knight);
                        tutorialStep = 4;
                        UIManager.Instance.ShowTutorialMessage(
                            "Grow time is proportional\nto the value of the piece.\n\nPawn: 1  |  Knight/Bishop: 3\nRook: 5  |  Queen: 9");
                    }
                }
                return;
            }

            waitingForAI = true;
            ChessAI.Instance.PlayTurn(ChessBoard.Instance.board, enPassantTarget);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int? clicked = ChessBoard.Instance.GetSquareFromWorldPos(worldPos);

            if (clicked.HasValue)
                HandleSquareClick(clicked.Value);
        }
    }

    private void HandleSquareClick(Vector2Int pos)
    {
        ChessPiece clickedPiece = ChessBoard.Instance.board[pos.x, pos.y];

        // Planting mode: clicking on a plantable square plants directly
        if (isPlantingMode)
        {
            ChessPiece king = FindKing(currentTurn);
            if (king != null)
            {
                var plantable = SeedManager.Instance.GetPlantableSquares(king);
                if (plantable.Contains(pos))
                {
                    SeedManager.Instance.PlantSeed(pos.x, pos.y, currentTurn, pendingSeedType);
                    if (isOnline)
                        NetworkManager.Instance.PushAction(NetworkAction.SeedPlant(pos.x, pos.y, pendingSeedType.ToString()));
                    GameLogger.Instance?.LogSeedPlant(pos.x, pos.y, pendingSeedType);
                    DeselectPiece();
                    EndTurn();
                    return;
                }
            }
            // Tutorial step 2: must plant, don't allow exiting planting mode
            if (isTutorialMode && tutorialStep == 2)
                return;
            ExitPlantingMode();
            // Fall through to normal click handling
        }

        // If we have a selected piece and clicked on a valid move
        if (selectedPiece != null && currentLegalMoves.Contains(pos))
        {
            ExecuteMove(selectedPiece, pos);
            return;
        }

        // Select a new piece (skip seeds)
        if (clickedPiece != null && clickedPiece.color == currentTurn && !clickedPiece.isSeed)
        {
            SelectPiece(clickedPiece);
            return;
        }

        // Deselect
        DeselectPiece();
    }

    private void SelectPiece(ChessPiece piece)
    {
        DeselectPiece();
        selectedPiece = piece;
        currentLegalMoves = MoveValidator.Instance.GetLegalMoves(piece, ChessBoard.Instance.board, enPassantTarget);

        // Highlight selected square
        ChessBoard.Instance.HighlightSquare(piece.x, piece.y, ChessBoard.Instance.selectedColor);

        // Highlight legal moves
        foreach (var move in currentLegalMoves)
        {
            bool isCapture = ChessBoard.Instance.board[move.x, move.y] != null;
            if (piece.type == PieceType.Pawn && enPassantTarget.HasValue && move == enPassantTarget.Value)
                isCapture = true;

            ChessBoard.Instance.HighlightSquare(move.x, move.y,
                isCapture ? ChessBoard.Instance.captureHighlightColor : ChessBoard.Instance.moveHighlightColor);
        }

    }

    private void DeselectPiece()
    {
        selectedPiece = null;
        currentLegalMoves.Clear();
        isPlantingMode = false;
        ChessBoard.Instance.ClearHighlights();

        // Re-highlight check if applicable
        if (gameState == GameState.Check)
            HighlightKingInCheck();
    }

    public void OnSeedButtonClick(PieceType type)
    {
        // Tutorial: only allow Pawn in step 1
        if (isTutorialMode && tutorialStep == 1 && type != PieceType.Pawn)
            return;

        if (isPlantingMode && pendingSeedType == type)
        {
            ExitPlantingMode();
            return;
        }

        ChessPiece king = FindKing(currentTurn);
        if (king == null) return;

        // Deselect any currently selected piece
        if (selectedPiece != null)
        {
            selectedPiece = null;
            currentLegalMoves.Clear();
        }

        pendingSeedType = type;
        isPlantingMode = true;
        ChessBoard.Instance.ClearHighlights();

        var plantable = SeedManager.Instance.GetPlantableSquares(king);
        foreach (var sq in plantable)
            ChessBoard.Instance.HighlightSquare(sq.x, sq.y, ChessBoard.Instance.plantHighlightColor);

        // Tutorial: advance to step 2 (planting on board)
        if (isTutorialMode && tutorialStep == 1)
            AdvanceTutorial();
    }

    private void ExitPlantingMode()
    {
        isPlantingMode = false;
        ChessBoard.Instance.ClearHighlights();
        if (gameState == GameState.Check)
            HighlightKingInCheck();
    }

    private ChessPiece FindKing(PieceColor color)
    {
        var board = ChessBoard.Instance.board;
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                var p = board[x, y];
                if (p != null && p.type == PieceType.King && p.color == color)
                    return p;
            }
        return null;
    }

    private void UpdateSeedButtonsVisibility()
    {
        if (GameBootstrap.CurrentMode != GameMode.SeedChess
            || gameState == GameState.Checkmate
            || gameState == GameState.Stalemate
            || showingMenu
            || isReplayMode)
        {
            UIManager.Instance.HideSeedButtons();
            return;
        }

        if (gameState == GameState.Check)
        {
            UIManager.Instance.HideSeedButtons();
            return;
        }

        if (currentPlayMode == PlayMode.SinglePlayer && currentTurn == PieceColor.Black)
        {
            UIManager.Instance.HideSeedButtons();
            return;
        }

        if (isOnline && !NetworkManager.Instance.IsMyTurn())
        {
            UIManager.Instance.HideSeedButtons();
            return;
        }

        ChessPiece king = FindKing(currentTurn);
        if (king != null && SeedManager.Instance.GetPlantableSquares(king).Count > 0)
        {
            UIManager.Instance.ShowSeedButtons();
            return;
        }

        UIManager.Instance.HideSeedButtons();
    }

    private void ExecuteMove(ChessPiece piece, Vector2Int target)
    {
        // Bluffy mode: special handling
        if (isBluffyMode)
        {
            ExecuteBluffyMove(piece, target);
            return;
        }

        // Record move for opening book
        Vector2Int moveFrom = new Vector2Int(piece.x, piece.y);
        ChessAI.Instance.RecordMove(moveFrom, target);

        bool isPawnDoubleMove = false;
        bool isEnPassant = false;
        bool isCastling = false;

        if (piece.type == PieceType.Pawn)
        {
            if (Mathf.Abs(target.y - piece.y) == 2)
                isPawnDoubleMove = true;
            if (enPassantTarget.HasValue && target == enPassantTarget.Value)
                isEnPassant = true;
        }

        if (piece.type == PieceType.King && Mathf.Abs(target.x - piece.x) == 2)
            isCastling = true;

        if (isEnPassant)
        {
            int capturedY = piece.color == PieceColor.White ? target.y - 1 : target.y + 1;
            ChessBoard.Instance.RemovePiece(target.x, capturedY);
        }

        if (isCastling)
        {
            bool kingside = target.x > piece.x;
            int rookFromX = kingside ? 7 : 0;
            int rookToX = kingside ? target.x - 1 : target.x + 1;
            ChessPiece rook = ChessBoard.Instance.board[rookFromX, piece.y];
            ChessBoard.Instance.MovePiece(rook, rookToX, piece.y);
        }

        ChessBoard.Instance.MovePiece(piece, target.x, target.y);

        enPassantTarget = isPawnDoubleMove
            ? new Vector2Int(piece.x, piece.color == PieceColor.White ? piece.y - 1 : piece.y + 1)
            : null;

        if (piece.type == PieceType.Pawn && (target.y == 0 || target.y == 7))
        {
            promotingPawn = piece;
            promotionMoveFrom = moveFrom;
            waitingForPromotion = true;
            UIManager.Instance.ShowPromotionPanel(piece.color);
            ChessBoard.Instance.ClearHighlights();
            UIManager.Instance.HideSeedButtons();
            return;
        }

        // Online: push move action
        if (isOnline)
            NetworkManager.Instance.PushAction(NetworkAction.Move(moveFrom.x, moveFrom.y, target.x, target.y));

        GameLogger.Instance?.LogMove(moveFrom.x, moveFrom.y, target.x, target.y, piece.type);
        DeselectPiece();
        EndTurn();
    }

    private bool applyingRemoteAction;
    public void SetRemoteActionFlag(bool value) { applyingRemoteAction = value; }

    private void ExecuteBluffyMove(ChessPiece piece, Vector2Int target)
    {
        bool isBigPiece = piece.type != PieceType.Pawn;
        Vector2Int from = new Vector2Int(piece.x, piece.y);
        bool hadMoved = piece.hasMoved;

        // Online: push move action (before applying locally)
        if (isOnline && !applyingRemoteAction)
            NetworkManager.Instance.PushAction(NetworkAction.Move(from.x, from.y, target.x, target.y));

        // Use bluffy move to hide captured piece instead of destroying
        ChessPiece captured = ChessBoard.Instance.MovePieceBluffy(piece, target.x, target.y);

        GameLogger.Instance?.LogMove(from.x, from.y, target.x, target.y, piece.type);

        // Unregister captured piece from BluffyManager (but keep the object for undo)
        if (captured != null)
            BluffyManager.Instance.UnregisterPiece(captured);

        // Handle pawn promotion in Bluffy mode
        if (piece.type == PieceType.Pawn && (target.y == 0 || target.y == 7))
        {
            // For Bluffy mode, auto-promote to Queen (no bluff on pawns anyway)
            int px = piece.x;
            int py = piece.y;
            PieceColor pcolor = piece.color;
            PieceType realType = BluffyManager.Instance.realTypes.ContainsKey(piece) ?
                BluffyManager.Instance.realTypes[piece] : PieceType.Pawn;
            BluffyManager.Instance.UnregisterPiece(piece);
            Destroy(piece.gameObject);
            ChessBoard.Instance.board[px, py] = null;

            var newPiece = ChessBoard.Instance.CreatePiece(PieceType.Queen, pcolor, px, py);
            BluffyManager.Instance.RegisterPiece(newPiece, PieceType.Queen,
                BluffyManager.Instance.maskIndices.Count);

            // Destroy captured permanently for pawn moves (no bluff)
            if (captured != null)
            {
                Destroy(captured.gameObject);
                captured = null;
            }

            DeselectPiece();
            BluffyManager.Instance.EndBluffyTurn(false, pcolor);
            return;
        }

        if (isBigPiece)
        {
            // Store pending move for bluff resolution
            BluffyManager.Instance.StorePendingMove(piece, from, target, captured, hadMoved);
        }
        else
        {
            // Pawn move - destroy captured permanently (no bluff possible)
            if (captured != null)
            {
                Destroy(captured.gameObject);
                captured = null;
            }
        }

        DeselectPiece();
        BluffyManager.Instance.EndBluffyTurn(isBigPiece, piece.color);
    }

    public void ExecuteBluffyMoveForAI(ChessPiece piece, Vector2Int target)
    {
        ExecuteBluffyMove(piece, target);
    }

    public void OnBluffCalled()
    {
        // Online: push bluff call (only if local player initiated it)
        if (isOnline && !applyingRemoteAction)
            NetworkManager.Instance.PushAction(NetworkAction.BluffCall());

        GameLogger.Instance?.LogBluffCall();
        UIManager.Instance.HideBluffPanel();

        // Show dramatic "BLUFF!!" splash, then reveal piece and resolve
        UIManager.Instance.ShowSplashText("BLUFF !!", 1.2f, () =>
        {
            bool isLegal = BluffyManager.Instance.ValidateBluff();

            // Reveal the real piece behind the mask
            var piece = BluffyManager.Instance.PendingMovePiece;
            if (piece != null) piece.HideMask();

            // Show result splash with revealed piece visible
            if (!isLegal)
            {
                UIManager.Instance.ShowSplashText("Caught Bluffing!", 1.5f, () =>
                {
                    BluffyManager.Instance.ResolveCaughtBluffing();
                }, new Color(1f, 0.3f, 0.1f));
            }
            else
            {
                UIManager.Instance.ShowSplashText("Not a Bluff!", 1.5f, () =>
                {
                    BluffyManager.Instance.ResolveSuccessfulDefense();
                }, new Color(0.2f, 0.9f, 0.3f));
            }
        });
    }

    public void OnMoveAccepted()
    {
        // Online: push accept (only if local player initiated it)
        if (isOnline && !applyingRemoteAction)
            NetworkManager.Instance.PushAction(NetworkAction.BluffAccept());

        GameLogger.Instance?.LogBluffAccept();
        BluffyManager.Instance.OnMoveAccepted();
    }

    public void OnPromotionSelected(PieceType type)
    {
        if (promotingPawn == null) return;

        int x = promotingPawn.x;
        int y = promotingPawn.y;
        PieceColor color = promotingPawn.color;

        ChessBoard.Instance.RemovePiece(x, y);
        ChessBoard.Instance.CreatePiece(type, color, x, y);

        // Online: push move with promotion
        if (isOnline)
            NetworkManager.Instance.PushAction(
                NetworkAction.Move(promotionMoveFrom.x, promotionMoveFrom.y, x, y, type.ToString()));

        GameLogger.Instance?.LogMove(promotionMoveFrom.x, promotionMoveFrom.y, x, y, PieceType.Pawn, type);
        promotingPawn = null;
        waitingForPromotion = false;
        UIManager.Instance.HidePromotionPanel();

        EndTurn();
    }

    public void ApplyAIMove(ChessPiece piece, Vector2Int target, PieceType? promotionType)
    {
        int logFromX = piece.x, logFromY = piece.y;
        PieceType logPieceType = piece.type;
        ChessAI.Instance.RecordMove(new Vector2Int(piece.x, piece.y), target);

        bool isPawnDoubleMove = false;
        bool isEnPassant = false;
        bool isCastling = false;

        if (piece.type == PieceType.Pawn)
        {
            if (Mathf.Abs(target.y - piece.y) == 2)
                isPawnDoubleMove = true;
            if (enPassantTarget.HasValue && target == enPassantTarget.Value)
                isEnPassant = true;
        }

        if (piece.type == PieceType.King && Mathf.Abs(target.x - piece.x) == 2)
            isCastling = true;

        if (isEnPassant)
        {
            int capturedY = piece.color == PieceColor.White ? target.y - 1 : target.y + 1;
            ChessBoard.Instance.RemovePiece(target.x, capturedY);
        }

        if (isCastling)
        {
            bool kingside = target.x > piece.x;
            int rookFromX = kingside ? 7 : 0;
            int rookToX = kingside ? target.x - 1 : target.x + 1;
            ChessPiece rook = ChessBoard.Instance.board[rookFromX, piece.y];
            ChessBoard.Instance.MovePiece(rook, rookToX, piece.y);
        }

        ChessBoard.Instance.MovePiece(piece, target.x, target.y);

        enPassantTarget = isPawnDoubleMove
            ? new Vector2Int(piece.x, piece.color == PieceColor.White ? piece.y - 1 : piece.y + 1)
            : null;

        if (promotionType.HasValue)
        {
            int px = piece.x;
            int py = piece.y;
            PieceColor pcolor = piece.color;
            ChessBoard.Instance.RemovePiece(px, py);
            ChessBoard.Instance.CreatePiece(promotionType.Value, pcolor, px, py);
        }

        GameLogger.Instance?.LogMove(logFromX, logFromY, target.x, target.y, logPieceType, promotionType);
        waitingForAI = false;
        DeselectPiece();
        EndTurn();
    }

    public void ApplyAIPlanting(Vector2Int square, PieceType seedType)
    {
        SeedManager.Instance.PlantSeed(square.x, square.y, PieceColor.Black, seedType);
        GameLogger.Instance?.LogSeedPlant(square.x, square.y, seedType);
        waitingForAI = false;
        DeselectPiece();
        EndTurn();
    }

    // ─── Online Remote Actions ───

    public void ApplyRemoteMove(int fromX, int fromY, int toX, int toY, PieceType? promotionType)
    {
        ChessPiece piece = ChessBoard.Instance.board[fromX, fromY];
        if (piece == null) return;

        applyingRemoteAction = true;

        if (isBluffyMode)
        {
            Vector2Int target = new Vector2Int(toX, toY);
            ExecuteBluffyMove(piece, target);
            applyingRemoteAction = false;
            return;
        }

        // Reuse AI move logic (handles en passant, castling, promotion)
        Vector2Int moveTarget = new Vector2Int(toX, toY);
        ChessAI.Instance.RecordMove(new Vector2Int(fromX, fromY), moveTarget);

        bool isPawnDoubleMove = false;
        bool isEnPassant = false;
        bool isCastling = false;

        if (piece.type == PieceType.Pawn)
        {
            if (Mathf.Abs(toY - piece.y) == 2)
                isPawnDoubleMove = true;
            if (enPassantTarget.HasValue && moveTarget == enPassantTarget.Value)
                isEnPassant = true;
        }

        if (piece.type == PieceType.King && Mathf.Abs(toX - piece.x) == 2)
            isCastling = true;

        if (isEnPassant)
        {
            int capturedY = piece.color == PieceColor.White ? toY - 1 : toY + 1;
            ChessBoard.Instance.RemovePiece(toX, capturedY);
        }

        if (isCastling)
        {
            bool kingside = toX > piece.x;
            int rookFromX = kingside ? 7 : 0;
            int rookToX = kingside ? toX - 1 : toX + 1;
            ChessPiece rook = ChessBoard.Instance.board[rookFromX, piece.y];
            ChessBoard.Instance.MovePiece(rook, rookToX, piece.y);
        }

        ChessBoard.Instance.MovePiece(piece, toX, toY);

        enPassantTarget = isPawnDoubleMove
            ? new Vector2Int(piece.x, piece.color == PieceColor.White ? piece.y - 1 : piece.y + 1)
            : null;

        if (promotionType.HasValue)
        {
            int px = piece.x;
            int py = piece.y;
            PieceColor pcolor = piece.color;
            ChessBoard.Instance.RemovePiece(px, py);
            ChessBoard.Instance.CreatePiece(promotionType.Value, pcolor, px, py);
        }

        applyingRemoteAction = false;
        DeselectPiece();
        EndTurn();
    }

    public void ApplyRemoteSeedPlant(int x, int y, PieceType seedType)
    {
        PieceColor plantColor = currentTurn;
        SeedManager.Instance.PlantSeed(x, y, plantColor, seedType);
        DeselectPiece();
        EndTurn();
    }

    private void EndTurn()
    {
        // Process seeds BEFORE switching turns
        if (GameBootstrap.CurrentMode == GameMode.SeedChess)
            SeedManager.Instance.OnTurnEnd(currentTurn);

        currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        selectedPiece = null;
        currentLegalMoves.Clear();
        ChessBoard.Instance.ClearHighlights();

        // Check game state
        bool inCheck = MoveValidator.Instance.IsKingInCheck(currentTurn, ChessBoard.Instance.board);
        bool hasLegalMove = MoveValidator.Instance.HasAnyLegalMove(currentTurn, ChessBoard.Instance.board, enPassantTarget);

        if (inCheck && !hasLegalMove)
        {
            gameState = GameState.Checkmate;
            PieceColor winner = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            GameLogger.Instance?.EndGame($"{winner.ToString().ToLower()}_wins");
            UIManager.Instance.ShowGameOver(winner);
        }
        else if (!inCheck && !hasLegalMove)
        {
            gameState = GameState.Stalemate;
            GameLogger.Instance?.EndGame("stalemate");
            UIManager.Instance.ShowStalemate();
        }
        else if (inCheck)
        {
            gameState = GameState.Check;
            HighlightKingInCheck();
            UIManager.Instance.UpdateTurnText(currentTurn, true);
        }
        else
        {
            gameState = GameState.Playing;
            UIManager.Instance.UpdateTurnText(currentTurn);
        }

        UpdateSeedButtonsVisibility();

        // Tutorial progression
        if (isTutorialMode)
        {
            // After player plants pawn (step 2), trigger AI turn (step 3)
            if (tutorialStep == 2 && currentTurn == PieceColor.Black)
            {
                UIManager.Instance.HideFinger();
                tutorialStep = 3;
            }
            // After player moves in step 5, pawn should have hatched → show completion
            else if (tutorialStep == 5 && currentTurn == PieceColor.Black)
            {
                tutorialStep = 6;
                UIManager.Instance.ShowTutorialMessage(
                    "That's all!\nThe rest is classic chess.", "End Tutorial");
            }
        }
    }

    private void HighlightKingInCheck()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = ChessBoard.Instance.board[x, y];
                if (p != null && p.type == PieceType.King && p.color == currentTurn)
                {
                    ChessBoard.Instance.HighlightSquare(x, y, ChessBoard.Instance.checkHighlightColor);
                    return;
                }
            }
        }
    }

    // ─── Tutorial ───

    private void StartTutorial()
    {
        isTutorialMode = true;
        tutorialStep = 0;
        UIManager.Instance.ShowTutorialMessage(
            "You can either move your King\nor plant the seed of a piece.");
    }

    public void AdvanceTutorial()
    {
        tutorialStep++;
        if (isBluffyTutorial)
        {
            AdvanceBluffyTutorial();
            return;
        }

        switch (tutorialStep)
        {
            case 1:
                // Focus on Pawn seed button only, show finger
                UIManager.Instance.ShowTutorialOverlayOnly();
                UIManager.Instance.FocusSeedButtons();
                UIManager.Instance.SetSeedButtonsInteractable(PieceType.Pawn);
                UIManager.Instance.ShowFingerOnPawnButton();
                break;

            case 2:
                // Player clicked Pawn button, now planting mode is active
                // Hide overlay, show finger on a plantable square
                UIManager.Instance.HideTutorialOverlay();
                UIManager.Instance.UnfocusSeedButtons();
                {
                    ChessPiece king = FindKing(currentTurn);
                    if (king != null)
                    {
                        var plantable = SeedManager.Instance.GetPlantableSquares(king);
                        if (plantable.Count > 0)
                            UIManager.Instance.ShowFingerOnBoardSquare(plantable[0].x, plantable[0].y);
                    }
                }
                break;

            case 3:
                // Player planted pawn → AI's turn → AI plants Knight
                // This is handled in Update()
                break;

            case 4:
                // After AI planted, show growth time message
                UIManager.Instance.ShowTutorialMessage(
                    "Grow time is proportional\nto the value of the piece.\n\nPawn: 1 turn  |  Knight/Bishop: 3\nRook: 5  |  Queen: 9");
                break;

            case 5:
                // Player can now move freely. Overlay hidden.
                UIManager.Instance.HideTutorialOverlay();
                break;

            case 6:
                // Pawn hatched, show completion message
                UIManager.Instance.ShowTutorialMessage(
                    "That's all!\nThe rest is classic chess.", "End Tutorial");
                break;

            case 7:
                // End tutorial
                EndTutorial();
                break;
        }
    }

    public void SkipTutorial()
    {
        EndTutorial();
    }

    private void EndTutorial()
    {
        string key = isBluffyTutorial ? "BluffyTutorialDone" : "SeedTutorialDone";
        isTutorialMode = false;
        isBluffyTutorial = false;
        tutorialStep = 0;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        UIManager.Instance.HideTutorialAll();
        UpdateSeedButtonsVisibility();
    }

    private void StartBluffyTutorial()
    {
        isTutorialMode = true;
        isBluffyTutorial = true;
        tutorialStep = 0;
        UIManager.Instance.ShowTutorialMessage(
            "In Bluffy Chess, all big pieces are masked.\nYour opponent can't see your real pieces!\n\nArrange your back rank, then Confirm.");
    }

    private void AdvanceBluffyTutorial()
    {
        switch (tutorialStep)
        {
            case 1:
                // Setup phase active — show finger on Confirm button
                UIManager.Instance.HideTutorialOverlay();
                UIManager.Instance.ShowFingerOnBluffyConfirm();
                break;

            case 2:
                // Detected in Update when phase becomes Playing
                // Message already shown there
                break;

            case 3:
                UIManager.Instance.ShowTutorialMessage(
                    "Caught bluffing \u2192 your piece dies.\nWrong call \u2192 you sacrifice a piece.\n\nThat's all!", "End Tutorial");
                break;

            case 4:
                EndTutorial();
                break;
        }
    }

    // ─── Replay Mode ───

    public void StartReplay(List<ReplayAction> actions, string modeStr, string result = null)
    {
        isReplayMode  = true;
        replayActions = actions;
        replayIndex   = 0;
        replayResult  = result ?? "";

        GameMode mode = GameMode.Classic;
        System.Enum.TryParse(modeStr, out mode);

        GameBootstrap.CurrentMode = mode;
        currentPlayMode  = PlayMode.Local;
        showingMenu      = false;
        currentTurn      = PieceColor.White;
        gameState        = GameState.Playing;
        selectedPiece    = null;
        enPassantTarget  = null;
        waitingForPromotion = false;
        promotingPawn    = null;
        isPlantingMode   = false;
        waitingForAI     = false;

        UIManager.Instance.HideMainMenu();
        UIManager.Instance.HideCurrencyDisplay();
        ChessAI.Instance.ResetHistory();
        SeedManager.Instance.ClearAll();
        ChessBoard.Instance.SetFlipped(false);
        ChessBoard.Instance.ClearAllPieces();

        // Set up initial board based on mode
        // Bluffy: force Classic to avoid triggering setup phase UI
        SetupBoardForReplay();

        UIManager.Instance.ShowReplayControls(replayIndex, replayActions.Count);
    }

    public void ReplayStepForward()
    {
        if (replayActions == null || replayIndex >= replayActions.Count) return;
        ApplyReplayAction(replayActions[replayIndex]);
        replayIndex++;
        UIManager.Instance.UpdateReplayProgress(replayIndex, replayActions.Count);

        // Show game result when replay reaches the end
        if (replayIndex >= replayActions.Count && !string.IsNullOrEmpty(replayResult) && replayResult != "ongoing")
        {
            string resultText;
            if (replayResult == "stalemate")
                resultText = "Stalemate!\nDraw!";
            else if (replayResult.Contains("white"))
                resultText = "White Wins!";
            else if (replayResult.Contains("black"))
                resultText = "Black Wins!";
            else
                resultText = replayResult;

            UIManager.Instance.ShowSplashText(resultText, 2.5f, null, new Color(0.9f, 0.8f, 0.2f));
        }
    }

    public void ReplayStepBackward()
    {
        if (replayIndex <= 0) return;
        replayIndex--;
        ResetBoardForReplay();
        for (int i = 0; i < replayIndex; i++)
            ApplyReplayAction(replayActions[i]);
        UIManager.Instance.UpdateReplayProgress(replayIndex, replayActions.Count);
    }

    public void StopReplay()
    {
        isReplayMode  = false;
        replayActions = null;
        replayIndex   = 0;
        replayResult  = "";
        UIManager.Instance.HideReplayControls();
        RestartGame();
    }

    private void ResetBoardForReplay()
    {
        currentTurn    = PieceColor.White;
        enPassantTarget = null;
        SeedManager.Instance.ClearAll();
        ChessBoard.Instance.ClearAllPieces();
        SetupBoardForReplay();
    }

    private void SetupBoardForReplay()
    {
        // Bluffy: force Classic to avoid triggering StartSetupPhase UI
        if (GameBootstrap.CurrentMode == GameMode.BluffyChess)
        {
            var saved = GameBootstrap.CurrentMode;
            GameBootstrap.CurrentMode = GameMode.Classic;
            ChessBoard.Instance.SetupPieces();
            GameBootstrap.CurrentMode = saved;
        }
        else
        {
            // SeedChess → 2 kings only, Classic → full 32 pieces
            ChessBoard.Instance.SetupPieces();
        }
    }

    private void ApplyReplayAction(ReplayAction action)
    {
        switch (action.type)
        {
            case "move":
                ApplyReplayMove(action);
                break;
            case "seedPlant":
                if (System.Enum.TryParse(action.seedType, out PieceType seedT))
                    SeedManager.Instance.PlantSeed(action.fx, action.fy, currentTurn, seedT);
                if (GameBootstrap.CurrentMode == GameMode.SeedChess)
                    SeedManager.Instance.OnTurnEnd(currentTurn);
                currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                break;
            case "sacrifice":
                ChessBoard.Instance.RemovePiece(action.fx, action.fy);
                break;
            case "rearrangeSwap":
            {
                ChessPiece rp1 = ChessBoard.Instance.board[action.x1, action.y1];
                ChessPiece rp2 = ChessBoard.Instance.board[action.x2, action.y2];
                if (rp1 != null && rp2 != null)
                    BluffyManager.Instance.SwapPiecePositions(rp1, rp2);
                break;
            }
            case "bluffySetup":
            {
                PieceColor setupCol = action.color == "White" ? PieceColor.White : PieceColor.Black;
                int        backY    = setupCol == PieceColor.White ? 0 : 7;
                string[]   types   = action.positions.Split(',');
                var        board   = ChessBoard.Instance.board;
                for (int x = 0; x < 8 && x < types.Length; x++)
                {
                    ChessPiece p = board[x, backY];
                    if (p == null || p.type == PieceType.Pawn) continue;
                    if (System.Enum.TryParse(types[x], out PieceType pt))
                    {
                        BluffyManager.Instance.realTypes[p] = pt;
                        p.UpdateType(pt);
                    }
                }
                break;
            }
            case "bluffAccept":
                if (isReplayMode)
                    UIManager.Instance.ShowSplashText("Accepted", 0.6f, null, new Color(0.5f, 0.8f, 0.5f));
                currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                break;
            case "bluffCall":
                if (isReplayMode)
                    UIManager.Instance.ShowSplashText("BLUFF !!", 0.6f);
                break;
            case "bluffCaught":
            {
                if (isReplayMode)
                    UIManager.Instance.ShowSplashText("Caught Bluffing!", 0.6f, null, new Color(1f, 0.3f, 0.1f));
                // Undo the move: destroy piece at target (the bluffer)
                ChessPiece atTarget = ChessBoard.Instance.board[action.tx, action.ty];
                if (atTarget != null)
                {
                    ChessBoard.Instance.board[action.tx, action.ty] = null;
                    Object.Destroy(atTarget.gameObject);
                }
                // Turn stays with the same player (mover gets another chance)
                break;
            }
            case "rearrangeDone":
                if (isReplayMode)
                    UIManager.Instance.ShowSplashText("Not a Bluff!", 0.6f, null, new Color(0.2f, 0.9f, 0.3f));
                currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                break;
        }
    }

    private void ApplyReplayMove(ReplayAction action)
    {
        ChessPiece piece = ChessBoard.Instance.board[action.fx, action.fy];
        if (piece == null) return;

        bool isBigPiece = piece.type != PieceType.Pawn;

        // Handle castling (king moves 2 squares horizontally)
        if (piece.type == PieceType.King && Mathf.Abs(action.tx - action.fx) == 2)
        {
            bool       kingside  = action.tx > action.fx;
            int        rookFromX = kingside ? 7 : 0;
            int        rookToX   = kingside ? action.tx - 1 : action.tx + 1;
            ChessPiece rook      = ChessBoard.Instance.board[rookFromX, action.fy];
            if (rook != null)
                ChessBoard.Instance.MovePiece(rook, rookToX, action.fy);
        }

        ChessBoard.Instance.MovePiece(piece, action.tx, action.ty);

        // Handle promotion
        if (!string.IsNullOrEmpty(action.promotion) && action.promotion != "null"
            && System.Enum.TryParse(action.promotion, out PieceType promoType))
        {
            PieceColor pcolor = piece.color;
            ChessBoard.Instance.RemovePiece(action.tx, action.ty);
            ChessBoard.Instance.CreatePiece(promoType, pcolor, action.tx, action.ty);
        }

        // In Bluffy mode, big piece moves don't switch turns
        // (bluffAccept or rearrangeDone handles turn switching)
        bool isBluffy = GameBootstrap.CurrentMode == GameMode.BluffyChess;
        if (isBluffy && isBigPiece)
            return;

        // Process seeds before switching turns (same as EndTurn)
        if (GameBootstrap.CurrentMode == GameMode.SeedChess)
            SeedManager.Instance.OnTurnEnd(currentTurn);

        currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }

    public void RestartGame()
    {
        // Leave online room if connected
        if (isOnline && NetworkManager.Instance.IsOnline)
            NetworkManager.Instance.LeaveRoom();

        currentTurn = PieceColor.White;
        gameState = GameState.Playing;
        selectedPiece = null;
        currentLegalMoves.Clear();
        enPassantTarget = null;
        waitingForPromotion = false;
        promotingPawn = null;
        isPlantingMode = false;
        waitingForAI = false;
        isTutorialMode = false;
        isBluffyTutorial = false;
        tutorialStep = 0;
        showingMenu = true;
        ChessAI.Instance.ResetHistory();

        SeedManager.Instance.ClearAll();
        ChessBoard.Instance.ClearHighlights();
        ChessBoard.Instance.ClearAllPieces();
        UIManager.Instance.HideGameOver();
        UIManager.Instance.HidePromotionPanel();
        UIManager.Instance.HideSeedButtons();
        UIManager.Instance.HideBluffyPanels();
        ChessBoard.Instance.SetFlipped(false);
        UIManager.Instance.ShowMainMenu();
    }
}
