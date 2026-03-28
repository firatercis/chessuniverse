using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Plugin for Seed Chess — a variant where the board starts with only two
/// kings and players plant seeds that grow into pieces over several turns.
///
/// Wraps the existing SeedManager and delegates lifecycle hooks to it.
/// </summary>
public class SeedChessPlugin : IGameModePlugin
{
    // ─── Identity ──────────────────────────────────────────────

    public string ModeId => "SeedChess";
    public GameModeDefinition Definition { get; set; }

    // ─── Lifecycle ─────────────────────────────────────────────

    public void Initialize()
    {
        // SeedManager is already created by GameBootstrap as a singleton.
        // Nothing extra to initialise here.
    }

    public void OnGameStart(PlayMode playMode)
    {
        SeedManager.Instance.ClearAll();
    }

    public void OnGameEnd()
    {
        SeedManager.Instance.ClearAll();
    }

    // ─── Board Setup ───────────────────────────────────────────

    /// <summary>
    /// Seed Chess starts with only two kings (one per colour).
    /// If a custom BoardSetup is provided, use that instead.
    /// </summary>
    public void SetupBoard(ChessBoard board)
    {
        if (Definition.startingPosition != null)
        {
            Definition.startingPosition.ApplyTo(board);
            return;
        }

        // Default: two kings only
        board.ClearAllPieces();
        board.CreatePiece(PieceType.King, PieceColor.White, 4, 0);
        board.CreatePiece(PieceType.King, PieceColor.Black, 4, 7);
    }

    // ─── Turn Hooks ────────────────────────────────────────────

    /// <summary>Tick seed growth timers and hatch ready seeds.</summary>
    public void OnTurnEnd(PieceColor color)
    {
        SeedManager.Instance.OnTurnEnd(color);
    }

    // ─── Input ─────────────────────────────────────────────────

    /// <summary>Seed Chess uses standard chess input routing.</summary>
    public bool HandleInput(Vector2Int square) => false;

    /// <summary>No custom Update override needed.</summary>
    public bool OverridesUpdate() => false;

    // ─── Rules ─────────────────────────────────────────────────

    public bool UsesCheck => true;
    public bool AllowsCastling => true;

    /// <summary>Seed Chess uses standard piece movement.</summary>
    public List<Vector2Int> GetCustomMoves(ChessPiece piece, ChessPiece[,] board) => null;

    // ─── AI ────────────────────────────────────────────────────

    /// <summary>Uses the standard ChessAI (with seed evaluation).</summary>
    public bool HasCustomAI => false;

    public void PlayAITurn(ChessPiece[,] board, Vector2Int? enPassantTarget) { }

    // ─── UI ────────────────────────────────────────────────────

    /// <summary>Seed buttons are currently managed by UIManager.
    /// Future refactor will move them here.</summary>
    public void CreateGameUI(Canvas canvas) { }
    public void DestroyGameUI() { }
}
