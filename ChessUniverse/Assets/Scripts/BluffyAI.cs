using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BluffyAI : MonoBehaviour
{
    public static BluffyAI Instance { get; private set; }

    // Settings loaded from Resources (with fallback)
    private BluffySettings settings;

    // Belief system: for each opponent (White) big piece, probability distribution over types
    private Dictionary<ChessPiece, Dictionary<PieceType, float>> beliefs = new();

    // Piece values for evaluation
    private static readonly Dictionary<PieceType, int> PieceValues = new()
    {
        { PieceType.Pawn, 100 },
        { PieceType.Knight, 320 },
        { PieceType.Bishop, 330 },
        { PieceType.Rook, 500 },
        { PieceType.Queen, 900 },
        { PieceType.King, 20000 }
    };

    private void Awake()
    {
        Instance = this;
        settings = Resources.Load<BluffySettings>("BluffySettings");
        if (settings == null)
        {
            // Create runtime fallback with defaults
            settings = ScriptableObject.CreateInstance<BluffySettings>();
        }
        ChessPiece.AnimSpeed = settings.pieceAnimSpeed;
    }

    // ─── Belief System ───

    public void InitBeliefs()
    {
        beliefs.Clear();

        var board = ChessBoard.Instance.board;
        var opponentBigPieces = new List<ChessPiece>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null) continue;
                if (p.color != PieceColor.White) continue;
                if (p.type == PieceType.Pawn) continue;
                opponentBigPieces.Add(p);
            }
        }

        // Initial pool: K, Q, R×2, B×2, N×2
        Dictionary<PieceType, int> pool = new()
        {
            { PieceType.King, 1 },
            { PieceType.Queen, 1 },
            { PieceType.Rook, 2 },
            { PieceType.Bishop, 2 },
            { PieceType.Knight, 2 }
        };

        float totalPool = pool.Values.Sum();

        foreach (var piece in opponentBigPieces)
        {
            var dist = new Dictionary<PieceType, float>();
            foreach (var kvp in pool)
            {
                dist[kvp.Key] = kvp.Value / totalPool;
            }
            beliefs[piece] = dist;
        }
    }

    public void UpdateBeliefsOnMove(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        if (piece.color != PieceColor.White) return;
        if (piece.type == PieceType.Pawn) return;
        if (!beliefs.ContainsKey(piece)) return;

        var dist = beliefs[piece];
        var board = ChessBoard.Instance.board;

        var toRemove = new List<PieceType>();
        foreach (var kvp in dist)
        {
            if (kvp.Value <= 0) continue;
            if (!IsMoveValidForType(kvp.Key, from, to, board, piece))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var t in toRemove)
        {
            dist[t] = 0f;
        }

        // Normalize
        float total = dist.Values.Sum();
        if (total > 0)
        {
            var keys = dist.Keys.ToList();
            foreach (var k in keys)
                dist[k] /= total;
        }
    }

    public void UpdateBeliefsOnLoss(ChessPiece piece)
    {
        beliefs.Remove(piece);
    }

    public void ResetBeliefsForSwap(ChessPiece p1, ChessPiece p2)
    {
        // After opponent rearrange swap, AI lost track of which piece is behind
        // which mask. Reset both to uniform prior based on remaining pool.
        var uniformDist = GetCurrentUniformPrior();
        if (beliefs.ContainsKey(p1))
            beliefs[p1] = new Dictionary<PieceType, float>(uniformDist);
        if (beliefs.ContainsKey(p2))
            beliefs[p2] = new Dictionary<PieceType, float>(uniformDist);
    }

    private Dictionary<PieceType, float> GetCurrentUniformPrior()
    {
        // Build uniform prior from remaining alive opponent big pieces
        // Count how many pieces still have each type as possible
        Dictionary<PieceType, int> pool = new()
        {
            { PieceType.King, 1 },
            { PieceType.Queen, 1 },
            { PieceType.Rook, 2 },
            { PieceType.Bishop, 2 },
            { PieceType.Knight, 2 }
        };

        float totalPool = 0;
        foreach (var kvp in pool) totalPool += kvp.Value;

        var dist = new Dictionary<PieceType, float>();
        foreach (var kvp in pool)
            dist[kvp.Key] = kvp.Value / totalPool;

        return dist;
    }

    public float GetBluffProbability(ChessPiece piece, Vector2Int from, Vector2Int to)
    {
        if (!beliefs.ContainsKey(piece)) return 0f;

        var dist = beliefs[piece];
        var board = ChessBoard.Instance.board;

        float legalWeight = 0f;
        float illegalWeight = 0f;

        foreach (var kvp in dist)
        {
            if (kvp.Value <= 0) continue;
            if (IsMoveValidForType(kvp.Key, from, to, board, piece))
                legalWeight += kvp.Value;
            else
                illegalWeight += kvp.Value;
        }

        float total = legalWeight + illegalWeight;
        if (total <= 0) return 0f;
        return illegalWeight / total;
    }

    // ─── Bluff Calling Decision ───

    public bool DecideBluff()
    {
        var bm = BluffyManager.Instance;
        var piece = bm.PendingMovePiece;
        if (piece == null) return false;

        // If our king was captured, always call bluff — accepting means game over
        if (bm.IsKingCaptured(out PieceColor w) && w == PieceColor.White)
            return true;

        // Confidence check: don't call bluff if beliefs are too uncertain
        // Need at least some types eliminated before making a call
        if (beliefs.ContainsKey(piece))
        {
            var dist = beliefs[piece];
            int nonzeroTypes = 0;
            foreach (var kvp in dist)
                if (kvp.Value > 0.01f) nonzeroTypes++;

            // If 4+ types still possible, we haven't observed enough — don't call
            if (nonzeroTypes >= 4) return false;
        }

        float pBluff = GetBluffProbability(piece, bm.PendingFrom, bm.PendingTo);

        // Don't even consider calling if probability is below threshold
        if (pBluff < settings.minBluffProbToCall)
            return false;

        // EV calculation
        int capturedValue = 0;
        if (bm.CapturedByPending != null)
        {
            PieceType realType = bm.realTypes.ContainsKey(bm.CapturedByPending)
                ? bm.realTypes[bm.CapturedByPending]
                : bm.CapturedByPending.type;
            capturedValue = GetPieceValue(realType);
        }

        // If bluff is caught: opponent piece dies (we gain its expected value)
        // Use capped King value so King belief doesn't inflate EV unreasonably
        float expectedGainIfBluff = EstimateExpectedValue(piece);

        // If bluff call fails: we lose a big piece (sacrifice penalty)
        int sacrificeCost = GetLowestBigPieceValue(PieceColor.Black);

        float ev = pBluff * (expectedGainIfBluff + capturedValue) - (1f - pBluff) * sacrificeCost;

        return ev > 0;
    }

    private float EstimateExpectedValue(ChessPiece piece)
    {
        if (!beliefs.ContainsKey(piece)) return 300f;

        var dist = beliefs[piece];
        float expected = 0f;
        foreach (var kvp in dist)
        {
            // Cap King value in belief EV calculation to prevent always-call
            int val = kvp.Key == PieceType.King ? settings.beliefKingValue : GetPieceValue(kvp.Key);
            expected += kvp.Value * val;
        }
        return expected;
    }

    // ─── AI Move Selection (Greedy 1-ply) ───

    public void PlayTurn()
    {
        StartCoroutine(PlayTurnCoroutine());
    }

    private IEnumerator PlayTurnCoroutine()
    {
        yield return new WaitForSeconds(settings.aiMoveDelay);

        var board = ChessBoard.Instance.board;
        var bestMove = ChooseMove(board);

        if (bestMove.piece == null)
        {
            Debug.LogWarning("BluffyAI: No valid moves found!");
            yield break;
        }

        GameManager.Instance.ExecuteBluffyMoveForAI(bestMove.piece, bestMove.target);
    }

    private (ChessPiece piece, Vector2Int target, bool isBluff) ChooseMove(ChessPiece[,] board)
    {
        var safeMoves = new List<(ChessPiece piece, Vector2Int target, float score)>();
        var bluffMoves = new List<(ChessPiece piece, Vector2Int target, float score)>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece piece = board[x, y];
                if (piece == null || piece.color != PieceColor.Black) continue;
                if (piece.isSeed) continue;

                var moves = MoveValidator.Instance.GetLegalMoves(piece, board, null);

                foreach (var target in moves)
                {
                    bool isBigPiece = piece.type != PieceType.Pawn;
                    bool isSafe = true;

                    PieceType realType = BluffyManager.Instance.realTypes.ContainsKey(piece)
                        ? BluffyManager.Instance.realTypes[piece]
                        : piece.type;

                    if (isBigPiece)
                    {
                        isSafe = IsMoveValidForType(realType, new Vector2Int(piece.x, piece.y), target, board, piece);
                    }

                    float eval = EvaluateMove(piece, target, board);

                    // Capture bonus: AI prefers captures (subtract = lower = better for AI which minimizes)
                    if (board[target.x, target.y] != null && !board[target.x, target.y].isSeed)
                    {
                        eval -= settings.captureBonus * settings.aggressionMultiplier;
                    }

                    if (isBigPiece && !isSafe)
                    {
                        // Bluff move: risk-adjusted score
                        float catchRate = settings.expectedCatchRate;
                        int pieceVal = GetPieceValue(realType);
                        if (pieceVal >= 500)
                            catchRate *= (1f + settings.valuablePieceBluffReduction);
                        catchRate = Mathf.Min(catchRate, 0.8f);

                        float losingEval = EvaluateWithoutPiece(piece, board);
                        eval = eval * (1f - catchRate) + losingEval * catchRate;

                        bluffMoves.Add((piece, target, eval));
                    }
                    else
                    {
                        safeMoves.Add((piece, target, eval));
                    }
                }
            }
        }

        if (safeMoves.Count == 0 && bluffMoves.Count == 0)
            return (null, default, false);

        // Sort both lists (ascending, AI minimizes)
        safeMoves.Sort((a, b) => a.score.CompareTo(b.score));
        bluffMoves.Sort((a, b) => a.score.CompareTo(b.score));

        // Decide whether to pick a bluff move
        // Compare best safe vs best bluff within randomness range
        float bestSafe = safeMoves.Count > 0 ? safeMoves[0].score : float.MaxValue;
        float bestBluff = bluffMoves.Count > 0 ? bluffMoves[0].score : float.MaxValue;

        bool pickBluff = false;
        if (bluffMoves.Count > 0)
        {
            // Bluff move is competitive if it's within randomness range of best safe move
            if (bestBluff <= bestSafe + settings.moveRandomness)
            {
                // Roll for bluff chance
                float bluffChance = settings.bluffMoveChance * settings.aggressionMultiplier;

                // Reduce bluff chance for valuable pieces
                var topBluff = bluffMoves[0];
                PieceType topBluffReal = BluffyManager.Instance.realTypes.ContainsKey(topBluff.piece)
                    ? BluffyManager.Instance.realTypes[topBluff.piece]
                    : topBluff.piece.type;
                if (GetPieceValue(topBluffReal) >= 500)
                    bluffChance *= settings.valuablePieceBluffReduction;

                pickBluff = Random.value < bluffChance;
            }

            // If bluff is significantly better than safe, increase chance
            if (bestBluff < bestSafe - settings.moveRandomness)
            {
                pickBluff = Random.value < 0.6f * settings.aggressionMultiplier;
            }
        }

        if (pickBluff && bluffMoves.Count > 0)
        {
            // Pick from top bluff moves within randomness range
            var topBluffs = bluffMoves.Where(m => m.score <= bluffMoves[0].score + settings.moveRandomness).ToList();
            var chosen = topBluffs[Random.Range(0, topBluffs.Count)];
            return (chosen.piece, chosen.target, true);
        }

        if (safeMoves.Count > 0)
        {
            // Pick from top safe moves within randomness range
            var topSafe = safeMoves.Where(m => m.score <= safeMoves[0].score + settings.moveRandomness).ToList();
            var chosen = topSafe[Random.Range(0, topSafe.Count)];
            return (chosen.piece, chosen.target, false);
        }

        // Only bluff moves available - must bluff
        var fallback = bluffMoves[0];
        return (fallback.piece, fallback.target, true);
    }

    private float EvaluateMove(ChessPiece piece, Vector2Int target, ChessPiece[,] board)
    {
        int origX = piece.x, origY = piece.y;
        ChessPiece captured = board[target.x, target.y];

        board[target.x, target.y] = piece;
        board[origX, origY] = null;
        piece.x = target.x;
        piece.y = target.y;

        float score = EvaluateBoard(board);

        // Undo
        piece.x = origX;
        piece.y = origY;
        board[origX, origY] = piece;
        board[target.x, target.y] = captured;

        return score;
    }

    private float EvaluateWithoutPiece(ChessPiece piece, ChessPiece[,] board)
    {
        int px = piece.x, py = piece.y;
        board[px, py] = null;

        float score = EvaluateBoard(board);

        board[px, py] = piece;
        return score;
    }

    private float EvaluateBoard(ChessPiece[,] board)
    {
        return BoardEvaluator.Evaluate(board);
    }

    // ─── Setup ───

    public void ShuffleBackRank(ChessPiece[,] board)
    {
        var bigPieces = new List<ChessPiece>();
        for (int x = 0; x < 8; x++)
        {
            ChessPiece p = board[x, 7];
            if (p != null && p.color == PieceColor.Black && p.type != PieceType.Pawn)
            {
                bigPieces.Add(p);
            }
        }

        // Fisher-Yates shuffle of positions
        var positions = bigPieces.Select(p => new Vector2Int(p.x, p.y)).ToList();
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // Clear all from board
        foreach (var p in bigPieces)
            board[p.x, p.y] = null;

        // Place at new positions
        for (int i = 0; i < bigPieces.Count; i++)
        {
            var p = bigPieces[i];
            var pos = positions[i];
            board[pos.x, pos.y] = p;
            p.x = pos.x;
            p.y = pos.y;
            p.transform.position = ChessBoard.Instance.VisualPos(pos.x, pos.y);
        }
    }

    // ─── Sacrifice ───

    public ChessPiece ChooseSacrifice(PieceColor color)
    {
        var board = ChessBoard.Instance.board;
        ChessPiece cheapest = null;
        int cheapestValue = int.MaxValue;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null || p.color != color || p.type == PieceType.Pawn) continue;

                PieceType realType = BluffyManager.Instance.realTypes.ContainsKey(p)
                    ? BluffyManager.Instance.realTypes[p]
                    : p.type;
                int val = GetPieceValue(realType);

                if (val < cheapestValue)
                {
                    cheapestValue = val;
                    cheapest = p;
                }
            }
        }

        return cheapest;
    }

    // ─── Swap ───

    public ChessPiece ChooseSwapTarget(ChessPiece movedPiece)
    {
        var board = ChessBoard.Instance.board;
        var candidates = new List<ChessPiece>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null || p.color != movedPiece.color || p.type == PieceType.Pawn) continue;
                if (p == movedPiece) continue;
                candidates.Add(p);
            }
        }

        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }

    // ─── Bluff Decision Coroutine (for human moves) ───

    public void StartBluffDecision()
    {
        StartCoroutine(BluffDecisionCoroutine());
    }

    private IEnumerator BluffDecisionCoroutine()
    {
        yield return new WaitForSeconds(settings.bluffCallDelay);

        bool callBluff = DecideBluff();

        if (callBluff)
        {
            GameManager.Instance.OnBluffCalled();
        }
        else
        {
            GameManager.Instance.OnMoveAccepted();
        }
    }

    // ─── Helpers ───

    public bool IsMoveValidForType(PieceType type, Vector2Int from, Vector2Int to, ChessPiece[,] board, ChessPiece piece)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        int absDx = Mathf.Abs(dx);
        int absDy = Mathf.Abs(dy);

        switch (type)
        {
            case PieceType.King:
                return absDx <= 1 && absDy <= 1 && (absDx + absDy > 0);

            case PieceType.Queen:
                return IsValidSlidingMove(from, to, board, piece, true, true);

            case PieceType.Rook:
                return (dx == 0 || dy == 0) && IsValidSlidingMove(from, to, board, piece, true, false);

            case PieceType.Bishop:
                return absDx == absDy && absDx > 0 && IsValidSlidingMove(from, to, board, piece, false, true);

            case PieceType.Knight:
                return (absDx == 1 && absDy == 2) || (absDx == 2 && absDy == 1);

            case PieceType.Pawn:
                return false;

            default:
                return false;
        }
    }

    private bool IsValidSlidingMove(Vector2Int from, Vector2Int to, ChessPiece[,] board, ChessPiece piece, bool straight, bool diagonal)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        int absDx = Mathf.Abs(dx);
        int absDy = Mathf.Abs(dy);

        bool isStraight = dx == 0 || dy == 0;
        bool isDiagonal = absDx == absDy && absDx > 0;

        if (isStraight && !straight) return false;
        if (isDiagonal && !diagonal) return false;
        if (!isStraight && !isDiagonal) return false;

        int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        int cx = from.x + stepX;
        int cy = from.y + stepY;

        while (cx != to.x || cy != to.y)
        {
            if (board[cx, cy] != null && !board[cx, cy].isSeed)
                return false;
            cx += stepX;
            cy += stepY;
        }

        // Target square: empty, enemy, or the piece itself (already moved there)
        ChessPiece target = board[to.x, to.y];
        if (target != null && target != piece && !target.isSeed && target.color == piece.color)
            return false;

        return true;
    }

    private int GetPieceValue(PieceType type)
    {
        return PieceValues.TryGetValue(type, out int val) ? val : 0;
    }

    private int GetLowestBigPieceValue(PieceColor color)
    {
        var board = ChessBoard.Instance.board;
        int lowest = int.MaxValue;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPiece p = board[x, y];
                if (p == null || p.color != color || p.type == PieceType.Pawn) continue;

                PieceType realType = BluffyManager.Instance.realTypes.ContainsKey(p)
                    ? BluffyManager.Instance.realTypes[p]
                    : p.type;
                int val = GetPieceValue(realType);
                if (val < lowest) lowest = val;
            }
        }

        return lowest == int.MaxValue ? 300 : lowest;
    }

    public BluffySettings Settings => settings;
}
