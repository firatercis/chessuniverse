using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Contract that every game mode must implement.
/// GameModeManager discovers plugins via GameModeDefinition.pluginTypeName
/// and calls these hooks at the right moments in the game lifecycle.
///
/// Plugins wrap game-specific managers (SeedManager, BluffyManager, etc.)
/// and keep all mode-specific logic isolated from Common code.
/// </summary>
public interface IGameModePlugin
{
    // ─── Identity ──────────────────────────────────────────────

    /// <summary>Unique key that matches GameModeDefinition.modeId.</summary>
    string ModeId { get; }

    /// <summary>The ScriptableObject asset for this mode.</summary>
    GameModeDefinition Definition { get; set; }

    // ─── Lifecycle ─────────────────────────────────────────────

    /// <summary>Called once at app startup by GameModeManager.
    /// Create singletons, load settings, etc.</summary>
    void Initialize();

    /// <summary>Called when a game of this mode starts.</summary>
    void OnGameStart(PlayMode playMode);

    /// <summary>Called when the game ends or the player returns to the menu.</summary>
    void OnGameEnd();

    // ─── Board Setup ───────────────────────────────────────────

    /// <summary>
    /// Place the initial pieces on the board.
    /// If Definition.startingPosition is set, use that;
    /// otherwise use the mode's default layout.
    /// </summary>
    void SetupBoard(ChessBoard board);

    // ─── Turn Hooks ────────────────────────────────────────────

    /// <summary>Called at the end of a turn, BEFORE the turn colour switches.
    /// Seed Chess uses this to tick down seed growth timers.</summary>
    void OnTurnEnd(PieceColor color);

    // ─── Input ─────────────────────────────────────────────────

    /// <summary>
    /// Called when the player taps a board square during this mode.
    /// Return true if the plugin handled the tap (e.g. Bluffy setup click),
    /// false to let GameManager handle it with standard chess logic.
    /// </summary>
    bool HandleInput(Vector2Int square);

    /// <summary>
    /// Called every frame. Return true to block GameManager's Update
    /// (e.g. Bluffy phases that need their own input routing).
    /// </summary>
    bool OverridesUpdate();

    // ─── Rules Customisation ───────────────────────────────────

    /// <summary>True if this mode uses standard check / checkmate rules.</summary>
    bool UsesCheck { get; }

    /// <summary>True if castling is allowed in this mode.</summary>
    bool AllowsCastling { get; }

    /// <summary>
    /// Return custom raw moves for a piece, or null to use standard chess
    /// movement (queen, rook, bishop, knight, pawn, king).
    /// </summary>
    List<Vector2Int> GetCustomMoves(ChessPiece piece, ChessPiece[,] board);

    // ─── AI ────────────────────────────────────────────────────

    /// <summary>
    /// Called when it's the AI's turn. Return true if the plugin provides
    /// its own AI (e.g. BluffyAI); false = use the standard ChessAI.
    /// </summary>
    bool HasCustomAI { get; }

    /// <summary>Trigger the custom AI to play a move.
    /// Only called when HasCustomAI is true.</summary>
    void PlayAITurn(ChessPiece[,] board, Vector2Int? enPassantTarget);

    // ─── Game-Specific UI ──────────────────────────────────────

    /// <summary>Create any panels this mode needs (seed buttons, bluff panel, etc.).</summary>
    void CreateGameUI(Canvas canvas);

    /// <summary>Tear down game-specific panels when returning to the menu.</summary>
    void DestroyGameUI();
}
