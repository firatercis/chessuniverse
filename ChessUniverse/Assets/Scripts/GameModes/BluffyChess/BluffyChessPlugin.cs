using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Plugin for Bluffy Chess — a variant where big pieces are masked and
/// opponents can call bluffs on each other's moves.
///
/// Wraps BluffyManager and BluffyAI. Most of the heavy lifting still
/// happens inside BluffyManager; this plugin provides the interface
/// hooks so that GameManager/ChessBoard don't need to know about Bluffy
/// internals directly.
/// </summary>
public class BluffyChessPlugin : IGameModePlugin
{
    // ─── Identity ──────────────────────────────────────────────

    public string ModeId => "BluffyChess";
    public GameModeDefinition Definition { get; set; }

    // ─── Lifecycle ─────────────────────────────────────────────

    public void Initialize()
    {
        // BluffyManager and BluffyAI are created by GameBootstrap.
    }

    public void OnGameStart(PlayMode playMode) { }

    public void OnGameEnd() { }

    // ─── Board Setup ───────────────────────────────────────────

    /// <summary>
    /// Bluffy Chess uses the standard 32-piece layout, then triggers
    /// BluffyManager.StartSetupPhase() so players can rearrange their
    /// back rank before masking.
    /// </summary>
    public void SetupBoard(ChessBoard board)
    {
        if (Definition.startingPosition != null)
        {
            Definition.startingPosition.ApplyTo(board);
            return;
        }

        // Default: full classic board (SetupPieces handles this when
        // mode is Bluffy, then calls StartSetupPhase).
        // Actual setup is still triggered from ChessBoard.SetupPieces()
        // for now, because the masking flow is tightly coupled.
    }

    // ─── Turn Hooks ────────────────────────────────────────────

    /// <summary>Bluffy Chess has no per-turn processing like seeds.</summary>
    public void OnTurnEnd(PieceColor color) { }

    // ─── Input ─────────────────────────────────────────────────

    /// <summary>
    /// Bluffy Chess routes input based on the current phase
    /// (Setup, Playing, Sacrifice, Rearrange). Returns true because
    /// GameManager's Bluffy block in Update() already handles everything.
    /// </summary>
    public bool HandleInput(Vector2Int square) => false;

    /// <summary>
    /// Bluffy mode overrides the entire Update flow with phase-based
    /// input routing. GameManager checks this and runs its Bluffy block.
    /// </summary>
    public bool OverridesUpdate() => true;

    // ─── Rules ─────────────────────────────────────────────────

    /// <summary>Bluffy doesn't use standard check/checkmate — wins by king capture.</summary>
    public bool UsesCheck => false;

    /// <summary>No castling in Bluffy Chess.</summary>
    public bool AllowsCastling => false;

    /// <summary>
    /// Big pieces in Bluffy can move in ANY direction (queen + knight
    /// combined). Pawns move normally.
    /// </summary>
    public List<Vector2Int> GetCustomMoves(ChessPiece piece, ChessPiece[,] board)
    {
        if (piece.type == PieceType.Pawn) return null; // standard pawn movement
        return piece.GetBluffyMoves(board);
    }

    // ─── AI ────────────────────────────────────────────────────

    /// <summary>Bluffy uses its own belief-based AI (BluffyAI).</summary>
    public bool HasCustomAI => true;

    public void PlayAITurn(ChessPiece[,] board, Vector2Int? enPassantTarget)
    {
        BluffyAI.Instance.PlayTurn();
    }

    // ─── UI ────────────────────────────────────────────────────

    /// <summary>Bluffy panels (setup, bluff, sacrifice, rearrange) are
    /// currently managed by UIManager. Future refactor will move them here.</summary>
    public void CreateGameUI(Canvas canvas) { }
    public void DestroyGameUI() { }
}
