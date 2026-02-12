using UnityEngine;

/// <summary>
/// Automatically initializes all game systems when the game starts.
/// No need to attach to any GameObject - runs via RuntimeInitializeOnLoadMethod.
/// </summary>
public static class GameBootstrap
{
    public static GameMode CurrentMode = GameMode.Classic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Create UI Manager
        GameObject uiObj = new GameObject("UIManager");
        uiObj.AddComponent<UIManager>();
        Object.DontDestroyOnLoad(uiObj);

        // Create Move Validator
        GameObject validatorObj = new GameObject("MoveValidator");
        validatorObj.AddComponent<MoveValidator>();
        Object.DontDestroyOnLoad(validatorObj);

        // Create Chess Board
        GameObject boardObj = new GameObject("ChessBoard");
        boardObj.AddComponent<ChessBoard>();
        Object.DontDestroyOnLoad(boardObj);

        // Create Seed Manager
        GameObject seedObj = new GameObject("SeedManager");
        seedObj.AddComponent<SeedManager>();
        Object.DontDestroyOnLoad(seedObj);

        // Create Chess AI
        GameObject aiObj = new GameObject("ChessAI");
        aiObj.AddComponent<ChessAI>();
        Object.DontDestroyOnLoad(aiObj);

        // Create Game Manager
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
        Object.DontDestroyOnLoad(gmObj);
    }
}
