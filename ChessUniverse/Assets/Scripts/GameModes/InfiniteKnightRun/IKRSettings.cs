using UnityEngine;

/// <summary>
/// Tunable parameters for the Infinite Knight Run procedural generation.
/// Create an asset in Resources/ via  Create → Chess → IKR Settings.
///
/// === HOW THE ALGORITHM WORKS ===
///
/// The board is 8 columns wide and extends infinitely upward.
/// Only a sliding window of rows around the knight is kept in memory.
///
/// Each row is generated once, when the window first reaches it.
/// For every cell in a row the generator rolls two dice:
///
///   1. HOLE DICE  — chance = lerp(holeChanceMin → holeChanceMax, difficulty)
///      If it hits, the cell becomes a hole (impassable, blocks line attacks).
///
///   2. PIECE DICE — only for non-hole cells.
///      Chance = lerp(pieceChanceMin → pieceChanceMax, difficulty).
///      If it hits, a random black piece is placed.
///      Piece type weights shift with difficulty:
///        Low  difficulty → mostly Pawns / Knights
///        Mid  difficulty → balanced mix, Rooks appear
///        High difficulty → heavy Rooks / Queens
///
/// "difficulty" is a 0→1 float:  clamp01((row − safeRows) / difficultyRampRows).
/// The first `safeRows` rows always have difficulty = 0 (no holes, no enemies).
/// After that it climbs linearly, reaching 1.0 at safeRows + difficultyRampRows.
///
/// At game start the area within 2 knight-jumps of the spawn is forcibly cleared
/// of holes and enemies so the player always has several safe opening moves.
/// </summary>
[CreateAssetMenu(fileName = "IKRSettings", menuName = "Chess/IKR Settings")]
public class IKRSettings : ScriptableObject
{
    [Header("Window")]
    [Tooltip("Rows kept below the knight before pruning.")]
    public int bufferBelow = 3;

    [Tooltip("Rows generated ahead of the knight.")]
    public int bufferAbove = 10;

    [Header("Safe Zone")]
    [Tooltip("First N rows have no holes or enemies (difficulty = 0).")]
    public int safeRows = 5;

    [Tooltip("How many rows after safeRows for difficulty to reach 1.0.")]
    public int difficultyRampRows = 80;

    [Header("Holes")]
    [Tooltip("Hole probability at difficulty 0 (just past safe zone).")]
    [Range(0f, 1f)] public float holeChanceMin = 0.05f;

    [Tooltip("Hole probability at difficulty 1.")]
    [Range(0f, 1f)] public float holeChanceMax = 0.30f;

    [Header("Black Pieces")]
    [Tooltip("Black piece spawn probability at difficulty 0.")]
    [Range(0f, 1f)] public float pieceChanceMin = 0.03f;

    [Tooltip("Black piece spawn probability at difficulty 1.")]
    [Range(0f, 1f)] public float pieceChanceMax = 0.20f;

    [Header("Piece Type Weights (Low Difficulty < 0.3)")]
    [Tooltip("Cumulative thresholds for Pawn/Knight/Bishop/Rook. Remainder = Rook.")]
    public float lowPawn   = 0.50f;
    public float lowKnight = 0.75f;
    public float lowBishop = 0.90f;

    [Header("Piece Type Weights (Mid Difficulty 0.3–0.6)")]
    public float midPawn   = 0.30f;
    public float midKnight = 0.50f;
    public float midBishop = 0.70f;
    public float midRook   = 0.90f;

    [Header("Piece Type Weights (High Difficulty > 0.6)")]
    public float highPawn   = 0.15f;
    public float highKnight = 0.30f;
    public float highBishop = 0.50f;
    public float highRook   = 0.75f;

    [Header("Scoring")]
    [Tooltip("Gold awarded = max(1, distance / goldPerDistance). 0 = no gold.")]
    public int goldPerDistance = 5;

    [Header("Camera")]
    [Tooltip("How quickly the camera follows the knight.")]
    public float cameraSmoothSpeed = 5f;

    [Tooltip("Vertical offset: knight sits this many units below camera center.")]
    public float cameraOffsetY = 1.5f;
}
