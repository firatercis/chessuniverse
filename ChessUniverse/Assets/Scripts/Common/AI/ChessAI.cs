using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct AIMove
{
    public bool isPlanting;
    // Normal move
    public ChessPiece piece;
    public Vector2Int target;
    // Planting
    public Vector2Int plantSquare;
    public PieceType plantType;
}

public class ChessAI : MonoBehaviour
{
    public static ChessAI Instance;

    private AISettings settings;
    private int maxDepth;
    private float aiDelay;
    private float seedDiscountBase;

    private bool isThinking;
    private List<string> moveHistory = new();

    // Simulated seeds for minimax search
    private List<SimSeed> simSeeds = new();

    private void Awake()
    {
        Instance = this;
        settings = Resources.Load<AISettings>("AISettings");
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (settings != null)
        {
            aiDelay = settings.aiDelay;
            seedDiscountBase = settings.seedDiscountBase;
        }
        else
        {
            aiDelay = 0.5f;
            seedDiscountBase = 0.85f;
        }
    }

    private int GetSearchDepth()
    {
        if (GameBootstrap.CurrentMode == GameMode.SeedChess)
            return settings != null ? settings.seedSearchDepth : 3;
        return settings != null ? settings.classicSearchDepth : 4;
    }

    public bool IsThinking() => isThinking;

    public void ResetHistory()
    {
        moveHistory.Clear();
    }

    public void RecordMove(Vector2Int from, Vector2Int to)
    {
        moveHistory.Add(OpeningBook.MoveToNotation(from, to));
    }

    public void PlayTurn(ChessPiece[,] board, Vector2Int? enPassantTarget)
    {
        if (isThinking) return;
        StartCoroutine(ThinkAndPlay(board, enPassantTarget));
    }

    private IEnumerator ThinkAndPlay(ChessPiece[,] board, Vector2Int? enPassantTarget)
    {
        isThinking = true;
        simSeeds.Clear();

        bool isSeedChess = GameBootstrap.CurrentMode == GameMode.SeedChess;

        // Try opening book first (Classic mode only)
        if (!isSeedChess)
        {
            var bookMove = OpeningBook.GetBookMove(moveHistory);
            if (bookMove.HasValue)
            {
                yield return new WaitForSeconds(aiDelay);
                var (from, to) = bookMove.Value;
                ChessPiece piece = board[from.x, from.y];
                if (piece != null && piece.color == PieceColor.Black)
                {
                    PieceType? promotion = GetPromotionType(piece, to);
                    isThinking = false;
                    GameManager.Instance.ApplyAIMove(piece, to, promotion);
                    yield break;
                }
            }
        }

        // Minimax search
        maxDepth = GetSearchDepth();
        AIMove bestAIMove = default;
        int bestScore = int.MaxValue; // AI is black, minimizing
        bool foundMove = false;

        var allMoves = GetAllMovesOrdered(board, PieceColor.Black, enPassantTarget);

        foreach (var aiMove in allMoves)
        {
            int score;
            if (aiMove.isPlanting)
            {
                int undoIndex = SimulatePlanting(aiMove.plantSquare, PieceColor.Black, aiMove.plantType);
                score = Minimax(board, maxDepth - 1, int.MinValue, int.MaxValue, true, enPassantTarget);
                UndoPlanting(undoIndex);
            }
            else
            {
                var newEP = GetNewEnPassant(aiMove.piece, aiMove.target);
                var undo = SimulateMove(board, aiMove.piece, aiMove.target, enPassantTarget);
                score = Minimax(board, maxDepth - 1, int.MinValue, int.MaxValue, true, newEP);
                UndoMove(board, undo);
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestAIMove = aiMove;
                foundMove = true;
            }
        }

        yield return new WaitForSeconds(aiDelay);

        isThinking = false;

        if (foundMove)
        {
            if (bestAIMove.isPlanting)
            {
                GameManager.Instance.ApplyAIPlanting(bestAIMove.plantSquare, bestAIMove.plantType);
            }
            else
            {
                PieceType? promotion = GetPromotionType(bestAIMove.piece, bestAIMove.target);
                GameManager.Instance.ApplyAIMove(bestAIMove.piece, bestAIMove.target, promotion);
            }
        }
    }

    private int Minimax(ChessPiece[,] board, int depth, int alpha, int beta, bool isMaximizing, Vector2Int? enPassant)
    {
        if (depth == 0)
            return BoardEvaluator.Evaluate(board, simSeeds.Count > 0 ? simSeeds : null, seedDiscountBase);

        PieceColor color = isMaximizing ? PieceColor.White : PieceColor.Black;
        var allMoves = GetAllMovesOrdered(board, color, enPassant);

        // No moves: checkmate or stalemate
        if (allMoves.Count == 0)
        {
            bool inCheck = MoveValidator.Instance.IsKingInCheck(color, board);
            if (inCheck)
            {
                return isMaximizing ? -BoardEvaluator.CHECKMATE_SCORE - depth : BoardEvaluator.CHECKMATE_SCORE + depth;
            }
            return 0; // Stalemate
        }

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (var aiMove in allMoves)
            {
                int eval;
                if (aiMove.isPlanting)
                {
                    int undoIndex = SimulatePlanting(aiMove.plantSquare, color, aiMove.plantType);
                    eval = Minimax(board, depth - 1, alpha, beta, false, enPassant);
                    UndoPlanting(undoIndex);
                }
                else
                {
                    var newEP = GetNewEnPassant(aiMove.piece, aiMove.target);
                    var undo = SimulateMove(board, aiMove.piece, aiMove.target, enPassant);
                    eval = Minimax(board, depth - 1, alpha, beta, false, newEP);
                    UndoMove(board, undo);
                }

                if (eval > maxEval) maxEval = eval;
                if (eval > alpha) alpha = eval;
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var aiMove in allMoves)
            {
                int eval;
                if (aiMove.isPlanting)
                {
                    int undoIndex = SimulatePlanting(aiMove.plantSquare, color, aiMove.plantType);
                    eval = Minimax(board, depth - 1, alpha, beta, true, enPassant);
                    UndoPlanting(undoIndex);
                }
                else
                {
                    var newEP = GetNewEnPassant(aiMove.piece, aiMove.target);
                    var undo = SimulateMove(board, aiMove.piece, aiMove.target, enPassant);
                    eval = Minimax(board, depth - 1, alpha, beta, true, newEP);
                    UndoMove(board, undo);
                }

                if (eval < minEval) minEval = eval;
                if (eval < beta) beta = eval;
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }

    #region Move Simulation

    private struct MoveUndo
    {
        public int fromX, fromY, toX, toY;
        public ChessPiece movedPiece;
        public ChessPiece capturedPiece;
        public bool hadMoved;
        // Castling
        public ChessPiece castlingRook;
        public int rookFromX, rookFromY, rookToX;
        public bool rookHadMoved;
        // En passant
        public bool wasEnPassant;
        public int enPassantCaptureX, enPassantCaptureY;
        public ChessPiece enPassantCaptured;
        // Promotion
        public bool wasPawnPromotion;
        public PieceType originalType;
    }

    private MoveUndo SimulateMove(ChessPiece[,] board, ChessPiece piece, Vector2Int target, Vector2Int? enPassant)
    {
        var undo = new MoveUndo
        {
            fromX = piece.x,
            fromY = piece.y,
            toX = target.x,
            toY = target.y,
            movedPiece = piece,
            hadMoved = piece.hasMoved,
            capturedPiece = board[target.x, target.y]
        };

        // En passant capture
        if (piece.type == PieceType.Pawn && enPassant.HasValue && target == enPassant.Value)
        {
            int dir = piece.color == PieceColor.White ? 1 : -1;
            int capturedY = target.y - dir;
            undo.wasEnPassant = true;
            undo.enPassantCaptureX = target.x;
            undo.enPassantCaptureY = capturedY;
            undo.enPassantCaptured = board[target.x, capturedY];
            board[target.x, capturedY] = null;
        }

        // Castling
        if (piece.type == PieceType.King && Mathf.Abs(target.x - piece.x) == 2)
        {
            bool kingside = target.x > piece.x;
            int rookFromX = kingside ? 7 : 0;
            int rookToX = kingside ? target.x - 1 : target.x + 1;
            ChessPiece rook = board[rookFromX, piece.y];
            undo.castlingRook = rook;
            undo.rookFromX = rookFromX;
            undo.rookFromY = piece.y;
            undo.rookToX = rookToX;
            undo.rookHadMoved = rook.hasMoved;

            // Move rook
            board[rookFromX, piece.y] = null;
            board[rookToX, piece.y] = rook;
            rook.x = rookToX;
            rook.hasMoved = true;
        }

        // Move piece
        board[piece.x, piece.y] = null;
        board[target.x, target.y] = piece;
        piece.x = target.x;
        piece.y = target.y;
        piece.hasMoved = true;

        // Promotion (always queen in simulation for best evaluation)
        if (piece.type == PieceType.Pawn && (target.y == 0 || target.y == 7))
        {
            undo.wasPawnPromotion = true;
            undo.originalType = PieceType.Pawn;
            piece.type = PieceType.Queen;
        }

        return undo;
    }

    private void UndoMove(ChessPiece[,] board, MoveUndo undo)
    {
        ChessPiece piece = undo.movedPiece;

        // Undo promotion
        if (undo.wasPawnPromotion)
            piece.type = undo.originalType;

        // Undo piece move
        board[undo.toX, undo.toY] = undo.capturedPiece;
        board[undo.fromX, undo.fromY] = piece;
        piece.x = undo.fromX;
        piece.y = undo.fromY;
        piece.hasMoved = undo.hadMoved;

        // Undo castling
        if (undo.castlingRook != null)
        {
            board[undo.rookToX, undo.rookFromY] = null;
            board[undo.rookFromX, undo.rookFromY] = undo.castlingRook;
            undo.castlingRook.x = undo.rookFromX;
            undo.castlingRook.hasMoved = undo.rookHadMoved;
        }

        // Undo en passant
        if (undo.wasEnPassant)
        {
            board[undo.enPassantCaptureX, undo.enPassantCaptureY] = undo.enPassantCaptured;
        }
    }

    #endregion

    #region Planting Simulation

    private int SimulatePlanting(Vector2Int square, PieceColor color, PieceType type)
    {
        simSeeds.Add(new SimSeed
        {
            x = square.x,
            y = square.y,
            color = color,
            targetType = type,
            turnsRemaining = SeedManager.GrowthTurns[type]
        });
        return simSeeds.Count - 1;
    }

    private void UndoPlanting(int index)
    {
        simSeeds.RemoveAt(index);
    }

    private bool IsSquareOccupiedBySeed(int x, int y)
    {
        // Check simulated seeds
        foreach (var seed in simSeeds)
        {
            if (seed.x == x && seed.y == y)
                return true;
        }
        return false;
    }

    #endregion

    #region Move Generation & Ordering

    private static readonly PieceType[] PlantableTypes = {
        PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen
    };

    private List<AIMove> GetAllMovesOrdered(
        ChessPiece[,] board, PieceColor color, Vector2Int? enPassant)
    {
        var promotions = new List<AIMove>();
        var captures = new List<(AIMove move, int score)>();
        var plantingMoves = new List<(AIMove move, int turnsRemaining)>();
        var quietMoves = new List<AIMove>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece piece = board[x, y];
                if (piece == null || piece.color != color || piece.isSeed) continue;

                var legalMoves = MoveValidator.Instance.GetLegalMoves(piece, board, enPassant);
                foreach (var move in legalMoves)
                {
                    var aiMove = new AIMove { isPlanting = false, piece = piece, target = move };

                    // Promotion moves (highest priority)
                    if (piece.type == PieceType.Pawn && (move.y == 0 || move.y == 7))
                    {
                        promotions.Add(aiMove);
                        continue;
                    }

                    // Capture moves (with MVV-LVA ordering)
                    ChessPiece target = board[move.x, move.y];
                    bool isCapture = target != null && !target.isSeed;
                    if (!isCapture && piece.type == PieceType.Pawn && enPassant.HasValue && move == enPassant.Value)
                        isCapture = true;

                    if (isCapture)
                    {
                        int victimVal = target != null && !target.isSeed ? GetPieceValue(target.type) : 100;
                        int attackerVal = GetPieceValue(piece.type);
                        int mvvLva = victimVal * 10 - attackerVal;
                        captures.Add((aiMove, mvvLva));
                    }
                    else
                    {
                        quietMoves.Add(aiMove);
                    }
                }
            }
        }

        // Add planting moves in SeedChess mode (only if not in check)
        if (GameBootstrap.CurrentMode == GameMode.SeedChess)
        {
            bool inCheck = MoveValidator.Instance.IsKingInCheck(color, board);
            if (!inCheck)
            {
                // Find king
                ChessPiece king = null;
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 8; y++)
                    {
                        ChessPiece p = board[x, y];
                        if (p != null && p.type == PieceType.King && p.color == color)
                        { king = p; break; }
                    }

                if (king != null)
                {
                    // Get plantable squares (adjacent empty squares)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = king.x + dx;
                            int ny = king.y + dy;
                            if (nx < 0 || nx >= 8 || ny < 0 || ny >= 8) continue;
                            if (board[nx, ny] != null) continue;
                            if (IsSquareOccupiedBySeed(nx, ny)) continue;

                            foreach (var type in PlantableTypes)
                            {
                                var aiMove = new AIMove
                                {
                                    isPlanting = true,
                                    plantSquare = new Vector2Int(nx, ny),
                                    plantType = type
                                };
                                plantingMoves.Add((aiMove, SeedManager.GrowthTurns[type]));
                            }
                        }
                    }
                }
            }
        }

        // Sort captures by MVV-LVA (descending)
        captures.Sort((a, b) => b.score.CompareTo(a.score));

        // Sort planting by turns remaining (ascending - shorter hatching first)
        plantingMoves.Sort((a, b) => a.turnsRemaining.CompareTo(b.turnsRemaining));

        // Build final ordered list: promotions > captures > planting > quiet
        int totalCount = promotions.Count + captures.Count + plantingMoves.Count + quietMoves.Count;
        var result = new List<AIMove>(totalCount);
        result.AddRange(promotions);
        foreach (var (move, _) in captures)
            result.Add(move);
        foreach (var (move, _) in plantingMoves)
            result.Add(move);
        result.AddRange(quietMoves);

        return result;
    }

    private static int GetPieceValue(PieceType type) => type switch
    {
        PieceType.Pawn => 100,
        PieceType.Knight => 320,
        PieceType.Bishop => 330,
        PieceType.Rook => 500,
        PieceType.Queen => 900,
        PieceType.King => 20000,
        _ => 0
    };

    #endregion

    #region Helpers

    private Vector2Int? GetNewEnPassant(ChessPiece piece, Vector2Int target)
    {
        if (piece.type == PieceType.Pawn && Mathf.Abs(target.y - piece.y) == 2)
        {
            int epY = piece.color == PieceColor.White ? piece.y + 1 : piece.y - 1;
            return new Vector2Int(piece.x, epY);
        }
        return null;
    }

    private PieceType? GetPromotionType(ChessPiece piece, Vector2Int target)
    {
        if (piece.type == PieceType.Pawn && (target.y == 0 || target.y == 7))
            return PieceType.Queen; // AI always promotes to queen
        return null;
    }

    #endregion
}
