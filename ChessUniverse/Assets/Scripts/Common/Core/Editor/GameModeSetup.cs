#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// One-time helper to create all game-mode ScriptableObject assets.
/// Run from the menu bar: Chess → Setup Game Modes.
/// Safe to delete this file after running it once.
/// </summary>
public static class GameModeSetup
{
    [MenuItem("Chess/Setup Game Modes")]
    public static void CreateAllAssets()
    {
        const string folder = "Assets/Resources/GameModes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "GameModes");

        // ─── Reusable Badge Assets ─────────────────────────────
        var badgeSolo = CreateBadge("Badge_SOLO", "SOLO", new Color(0.9f, 0.6f, 0.15f));
        var badgeNew  = CreateBadge("Badge_NEW",  "NEW",  new Color(0.3f, 0.8f, 0.35f));
        var badgePvp  = CreateBadge("Badge_PVP",  "PVP",  new Color(0.55f, 0.4f, 0.9f));

        // ─── Infinite Knight Run ───────────────────────────────
        var knightDef = CreateDefinition("InfiniteKnightRunDefinition",
            modeId:      "InfiniteKnightRun",
            displayName: "Infinite Knight Run",
            description: "Endless survival with a knight",
            badges:      new[] { badgeSolo },
            primaryBtn:  "Play",
            disabled:    true,
            disabledText:"Under Construction",
            pluginType:  "InfiniteKnightRunPlugin",
            tutKey:      "",
            hasTutorial: false,
            iconFile:    "infinite_knight_run_icon");

        // ─── Seed Chess ────────────────────────────────────────
        var seedDef = CreateDefinition("SeedChessDefinition",
            modeId:      "SeedChess",
            displayName: "Seed Chess",
            description: "Grow your pieces from seeds over time",
            badges:      new[] { badgeNew },
            primaryBtn:  "Play",
            disabled:    false,
            disabledText:"",
            pluginType:  "SeedChessPlugin",
            tutKey:      "SeedTutorialDone",
            hasTutorial: true,
            iconFile:    "seed_chess_icon",
            rulesTitle:  "Seed Chess Rules",
            rulesText:   "The board starts with only two Kings.\n\nSelect your King, then tap a seed button to plant a piece seed on an adjacent empty square.\n\nGrowth turns:\n  Pawn: 1  |  Knight: 3  |  Bishop: 3\n  Rook: 5  |  Queen: 9\n\nSeeds are passable and cannot be captured.\nWhen a seed's timer reaches 0 it hatches into the real piece.\n\nCheckmate or stalemate ends the game.");

        // ─── Bluffy Chess ──────────────────────────────────────
        var bluffyDef = CreateDefinition("BluffyChessDefinition",
            modeId:      "BluffyChess",
            displayName: "Bluffy Chess",
            description: "Hidden identities and bluff calls",
            badges:      new[] { badgePvp },
            primaryBtn:  "Play Online",
            disabled:    false,
            disabledText:"",
            pluginType:  "BluffyChessPlugin",
            tutKey:      "BluffyTutorialDone",
            hasTutorial: true,
            iconFile:    "bluffy_chess_icon",
            rulesTitle:  "Bluffy Chess Rules",
            rulesText:   "Big pieces are masked — your opponent can't see your real pieces!\n\nArrange your back rank before the game starts.\nBig pieces can move in ANY direction (Queen + Knight combined).\n\nAfter a big piece moves, opponent can:\n  • Accept — the move stands\n  • Call Bluff — if the move is illegal with the real type:\n      Caught! → the piece dies\n      Wrong call → you sacrifice a piece & mover rearranges");

        // ─── Registry ──────────────────────────────────────────
        var registry = ScriptableObject.CreateInstance<GameModeRegistry>();
        registry.modes = new[] { knightDef, seedDef, bluffyDef };
        AssetDatabase.CreateAsset(registry, $"{folder}/GameModeRegistry.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Game mode assets created in Resources/GameModes/.");
    }

    private static BadgeDefinition CreateBadge(string fileName, string label, Color color)
    {
        var badge = ScriptableObject.CreateInstance<BadgeDefinition>();
        badge.label = label;
        badge.color = color;
        AssetDatabase.CreateAsset(badge, $"Assets/Resources/GameModes/{fileName}.asset");
        return badge;
    }

    private static GameModeDefinition CreateDefinition(string fileName,
        string modeId, string displayName, string description,
        BadgeDefinition[] badges, string primaryBtn,
        bool disabled, string disabledText, string pluginType,
        string tutKey, bool hasTutorial, string iconFile,
        string rulesTitle = "", string rulesText = "")
    {
        var def = ScriptableObject.CreateInstance<GameModeDefinition>();
        def.modeId = modeId;
        def.displayName = displayName;
        def.description = description;
        def.badges = badges;
        def.primaryButtonText = primaryBtn;
        def.isDisabled = disabled;
        def.disabledText = disabledText;
        def.pluginTypeName = pluginType;
        def.tutorialPrefsKey = tutKey;
        def.hasTutorial = hasTutorial;
        def.rulesTitle = rulesTitle;
        def.rulesText = rulesText;

        // Try to load icon sprite from Art folder
        string artPath = $"Assets/Art/Icons/GameIcons/{iconFile}.png";
        def.icon = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);

        AssetDatabase.CreateAsset(def, $"Assets/Resources/GameModes/{fileName}.asset");
        return def;
    }
}
#endif
