using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Describes a board position — which pieces sit where.
/// Used for starting positions and tutorial scenarios.
/// Create assets via Assets → Create → Chess → Board Setup.
/// </summary>
[CreateAssetMenu(fileName = "NewBoardSetup", menuName = "Chess/Board Setup")]
public class BoardSetup : ScriptableObject
{
    [Tooltip("Human-readable label (e.g. \"Seed Chess Start\", \"Bluffy Tutorial Step 3\").")]
    public string label;

    [Tooltip("List of pieces that should be placed on the board.")]
    public List<PiecePlacement> pieces = new();

    /// <summary>
    /// A single piece on the board: type, colour, and square.
    /// </summary>
    [System.Serializable]
    public class PiecePlacement
    {
        public PieceType type;
        public PieceColor color;
        [Range(0, 7)] public int x;
        [Range(0, 7)] public int y;
    }

    /// <summary>
    /// Convenience — applies this setup to the given board, clearing it first.
    /// </summary>
    public void ApplyTo(ChessBoard board)
    {
        board.ClearAllPieces();
        foreach (var p in pieces)
            board.CreatePiece(p.type, p.color, p.x, p.y);
    }
}
