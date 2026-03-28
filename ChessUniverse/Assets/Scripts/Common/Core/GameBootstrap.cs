using UnityEngine;

/// <summary>
/// Bootstraps all game systems at startup via [RuntimeInitializeOnLoadMethod].
/// No GameObject needs to be placed in the scene — this runs automatically.
///
/// Creation order matters: dependencies must exist before dependents.
///   1. Common services  (UI, board, validator, AI, network, logging)
///   2. Game-mode managers (SeedManager, BluffyManager — still created here
///      for backward compatibility; future refactor may move creation into plugins)
///   3. GameModeManager   (discovers & instantiates plugins from the registry)
///   4. GameManager       (orchestrates gameplay, queries the active plugin)
/// </summary>
public static class GameBootstrap
{
    /// <summary>
    /// Legacy mode flag. Still used in many places during the transition to
    /// the plugin system. Will eventually be replaced by
    /// GameModeManager.ActivePlugin.ModeId checks.
    /// </summary>
    public static GameMode CurrentMode = GameMode.Classic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Application.targetFrameRate = 60;

        // ─── Plugin System (must be before UIManager — menu reads the registry) ──
        CreateSingleton<GameModeManager>("GameModeManager");

        // ─── Common Services ───────────────────────────────────
        CreateSingleton<MoveValidator>("MoveValidator");
        CreateSingleton<ChessBoard>("ChessBoard");
        CreateSingleton<ChessAI>("ChessAI");
        CreateSingleton<NetworkManager>("NetworkManager");
        CreateSingleton<GameLogger>("GameLogger");

        // ─── Game-Mode Managers (legacy singletons) ────────────
        // These will eventually be created by their respective plugins.
        CreateSingleton<SeedManager>("SeedManager");
        CreateSingleton<BluffyManager>("BluffyManager");
        CreateSingleton<BluffyAI>("BluffyAI");

        // ─── UI (reads GameModeManager.LoadedDefinitions for menu cards) ──
        CreateSingleton<UIManager>("UIManager");

        // ─── Game Orchestrator (must be last) ──────────────────
        CreateSingleton<GameManager>("GameManager");
    }

    /// <summary>Helper: creates a DontDestroyOnLoad singleton GameObject.</summary>
    private static T CreateSingleton<T>(string name) where T : Component
    {
        var obj = new GameObject(name);
        var comp = obj.AddComponent<T>();
        Object.DontDestroyOnLoad(obj);
        return comp;
    }
}
