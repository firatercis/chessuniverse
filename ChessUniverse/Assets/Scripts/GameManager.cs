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
    private Vector2Int? enPassantTarget;
    private bool waitingForPromotion;
    private ChessPiece promotingPawn;

    // Seed Chess state
    private bool isPlantingMode;
    private PieceType pendingSeedType;
    private bool showingMenu = true;

    private Camera mainCam;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;
        SetupCamera();
        ChessBoard.Instance.CreateBoard();
        // Show main menu instead of starting directly
        UIManager.Instance.ShowMainMenu();
    }

    public void StartGame(GameMode mode)
    {
        GameBootstrap.CurrentMode = mode;
        showingMenu = false;
        UIManager.Instance.HideMainMenu();

        currentTurn = PieceColor.White;
        gameState = GameState.Playing;
        selectedPiece = null;
        currentLegalMoves.Clear();
        enPassantTarget = null;
        waitingForPromotion = false;
        promotingPawn = null;
        isPlantingMode = false;

        SeedManager.Instance.ClearAll();
        ChessBoard.Instance.SetupPieces();
        UIManager.Instance.UpdateTurnText(currentTurn);
    }

    private void SetupCamera()
    {
        mainCam.orthographic = true;
        mainCam.orthographicSize = 5.5f;
        mainCam.transform.position = new Vector3(3.5f, 3.5f, -10);
        mainCam.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
    }

    private void Update()
    {
        if (showingMenu) return;
        if (waitingForPromotion) return;
        if (gameState == GameState.Checkmate || gameState == GameState.Stalemate) return;

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
            var plantable = SeedManager.Instance.GetPlantableSquares(selectedPiece);
            if (plantable.Contains(pos))
            {
                SeedManager.Instance.PlantSeed(pos.x, pos.y, currentTurn, pendingSeedType);
                DeselectPiece();
                EndTurn();
                return;
            }
            // Clicking elsewhere exits planting mode
            ExitPlantingMode();
            return;
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
            // En passant is also a capture
            if (piece.type == PieceType.Pawn && enPassantTarget.HasValue && move == enPassantTarget.Value)
                isCapture = true;

            ChessBoard.Instance.HighlightSquare(move.x, move.y,
                isCapture ? ChessBoard.Instance.captureHighlightColor : ChessBoard.Instance.moveHighlightColor);
        }

        // Show seed plant buttons for king in SeedChess (not when in check)
        if (GameBootstrap.CurrentMode == GameMode.SeedChess
            && piece.type == PieceType.King
            && gameState != GameState.Check)
        {
            var plantable = SeedManager.Instance.GetPlantableSquares(piece);
            if (plantable.Count > 0)
                UIManager.Instance.ShowSeedButtons();
        }
    }

    private void DeselectPiece()
    {
        selectedPiece = null;
        currentLegalMoves.Clear();
        isPlantingMode = false;
        ChessBoard.Instance.ClearHighlights();
        UIManager.Instance.HideSeedButtons();

        // Re-highlight check if applicable
        if (gameState == GameState.Check)
            HighlightKingInCheck();
    }

    public void OnSeedButtonClick(PieceType type)
    {
        if (selectedPiece == null || selectedPiece.type != PieceType.King) return;

        // If already planting the same type, toggle off
        if (isPlantingMode && pendingSeedType == type)
        {
            ExitPlantingMode();
            return;
        }

        pendingSeedType = type;
        isPlantingMode = true;
        ChessBoard.Instance.ClearHighlights();

        // Highlight king
        ChessBoard.Instance.HighlightSquare(selectedPiece.x, selectedPiece.y, ChessBoard.Instance.selectedColor);

        // Highlight plantable squares
        var plantable = SeedManager.Instance.GetPlantableSquares(selectedPiece);
        foreach (var sq in plantable)
            ChessBoard.Instance.HighlightSquare(sq.x, sq.y, ChessBoard.Instance.plantHighlightColor);
    }

    private void ExitPlantingMode()
    {
        isPlantingMode = false;
        if (selectedPiece != null)
            SelectPiece(selectedPiece); // Re-show normal move highlights
        else
            DeselectPiece();
    }

    private void ExecuteMove(ChessPiece piece, Vector2Int target)
    {
        bool isPawnDoubleMove = false;
        bool isEnPassant = false;
        bool isCastling = false;

        // Detect special moves
        if (piece.type == PieceType.Pawn)
        {
            // Double move
            if (Mathf.Abs(target.y - piece.y) == 2)
                isPawnDoubleMove = true;

            // En passant capture
            if (enPassantTarget.HasValue && target == enPassantTarget.Value)
                isEnPassant = true;
        }

        if (piece.type == PieceType.King && Mathf.Abs(target.x - piece.x) == 2)
            isCastling = true;

        // Execute the move
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

        // Update en passant target
        enPassantTarget = isPawnDoubleMove
            ? new Vector2Int(piece.x, piece.color == PieceColor.White ? piece.y - 1 : piece.y + 1)
            : null;

        // Check for pawn promotion
        if (piece.type == PieceType.Pawn && (target.y == 0 || target.y == 7))
        {
            promotingPawn = piece;
            waitingForPromotion = true;
            UIManager.Instance.ShowPromotionPanel(piece.color);
            ChessBoard.Instance.ClearHighlights();
            UIManager.Instance.HideSeedButtons();
            return;
        }

        DeselectPiece();
        EndTurn();
    }

    public void OnPromotionSelected(PieceType type)
    {
        if (promotingPawn == null) return;

        int x = promotingPawn.x;
        int y = promotingPawn.y;
        PieceColor color = promotingPawn.color;

        ChessBoard.Instance.RemovePiece(x, y);
        ChessBoard.Instance.CreatePiece(type, color, x, y);

        promotingPawn = null;
        waitingForPromotion = false;
        UIManager.Instance.HidePromotionPanel();

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
            UIManager.Instance.ShowGameOver(winner);
        }
        else if (!inCheck && !hasLegalMove)
        {
            gameState = GameState.Stalemate;
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

    public void RestartGame()
    {
        currentTurn = PieceColor.White;
        gameState = GameState.Playing;
        selectedPiece = null;
        currentLegalMoves.Clear();
        enPassantTarget = null;
        waitingForPromotion = false;
        promotingPawn = null;
        isPlantingMode = false;
        showingMenu = true;

        SeedManager.Instance.ClearAll();
        ChessBoard.Instance.ClearHighlights();
        ChessBoard.Instance.ClearAllPieces();
        UIManager.Instance.HideGameOver();
        UIManager.Instance.HidePromotionPanel();
        UIManager.Instance.HideSeedButtons();
        UIManager.Instance.ShowMainMenu();
    }
}
