using System.Collections.Generic;
using UnityEngine;

public static class OpeningBook
{
    // Key: move history string (e.g. "e2e4,e7e5,g1f3")
    // Value: list of candidate responses (from, to)
    static readonly Dictionary<string, List<(Vector2Int from, Vector2Int to)>> Openings = new()
    {
        // === Black responses to 1.e4 ===
        // After 1.e4 → Sicilian Defense (c7c5) or e7e5
        { "e2e4", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(4, 6), new Vector2Int(4, 4)),  // e7e5 (symmetrical)
            (new Vector2Int(2, 6), new Vector2Int(2, 4)),  // c7c5 (Sicilian)
            (new Vector2Int(4, 6), new Vector2Int(4, 5)),  // e7e6 (French)
        }},

        // After 1.e4 e5 2.Nf3 → Nc6 (most common)
        { "e2e4,e7e5,g1f3", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(1, 7), new Vector2Int(2, 5)),  // Nb8c6
        }},

        // After 1.e4 e5 2.Nf3 Nc6 3.Bc4 (Italian) → Bc5 or Nf6
        { "e2e4,e7e5,g1f3,b8c6,f1c4", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(5, 7), new Vector2Int(2, 4)),  // Bf8c5 (Giuoco Piano)
            (new Vector2Int(6, 7), new Vector2Int(5, 5)),  // Ng8f6 (Two Knights)
        }},

        // After 1.e4 e5 2.Nf3 Nc6 3.Bb5 (Ruy Lopez) → a6 or Nf6
        { "e2e4,e7e5,g1f3,b8c6,f1b5", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(0, 6), new Vector2Int(0, 5)),  // a7a6 (Morphy Defense)
            (new Vector2Int(6, 7), new Vector2Int(5, 5)),  // Ng8f6 (Berlin)
        }},

        // === Black responses to 1.d4 ===
        { "d2d4", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(3, 6), new Vector2Int(3, 4)),  // d7d5 (Queen's Gambit setup)
            (new Vector2Int(6, 7), new Vector2Int(5, 5)),  // Ng8f6 (Indian Defense)
        }},

        // After 1.d4 d5 2.c4 (Queen's Gambit) → e6 (QGD) or c6 (Slav)
        { "d2d4,d7d5,c2c4", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(4, 6), new Vector2Int(4, 5)),  // e7e6 (Queen's Gambit Declined)
            (new Vector2Int(2, 6), new Vector2Int(2, 5)),  // c7c6 (Slav Defense)
        }},

        // After 1.d4 Nf6 2.c4 → g6 (King's Indian) or e6 (Nimzo/Queen's Indian)
        { "d2d4,g8f6,c2c4", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(6, 6), new Vector2Int(6, 5)),  // g7g6 (King's Indian)
            (new Vector2Int(4, 6), new Vector2Int(4, 5)),  // e7e6 (Nimzo-Indian setup)
        }},

        // After 1.d4 Nf6 2.c4 g6 3.Nc3 → Bg7
        { "d2d4,g8f6,c2c4,g7g6,b1c3", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(5, 7), new Vector2Int(6, 6)),  // Bf8g7 (King's Indian)
        }},

        // === Sicilian variations ===
        // After 1.e4 c5 2.Nf3 → d6 or Nc6
        { "e2e4,c7c5,g1f3", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(3, 6), new Vector2Int(3, 5)),  // d7d6 (Najdorf/Dragon setup)
            (new Vector2Int(1, 7), new Vector2Int(2, 5)),  // Nb8c6 (Classical Sicilian)
        }},

        // === French Defense ===
        // After 1.e4 e6 2.d4 → d5
        { "e2e4,e7e6,d2d4", new List<(Vector2Int, Vector2Int)> {
            (new Vector2Int(3, 6), new Vector2Int(3, 4)),  // d7d5
        }},
    };

    /// <summary>
    /// Returns a book move for the current position, or null if not in book.
    /// </summary>
    public static (Vector2Int from, Vector2Int to)? GetBookMove(List<string> moveHistory)
    {
        string key = string.Join(",", moveHistory);

        if (Openings.TryGetValue(key, out var candidates) && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            return candidates[index];
        }

        return null;
    }

    /// <summary>
    /// Converts board coordinates to algebraic notation (e.g. 4,1 → "e2").
    /// </summary>
    public static string ToNotation(Vector2Int pos)
    {
        char file = (char)('a' + pos.x);
        char rank = (char)('1' + pos.y);
        return $"{file}{rank}";
    }

    /// <summary>
    /// Converts a move to notation string (e.g. "e2e4").
    /// </summary>
    public static string MoveToNotation(Vector2Int from, Vector2Int to)
    {
        return ToNotation(from) + ToNotation(to);
    }
}
