using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central manager that discovers, instantiates, and provides access to
/// game-mode plugins. Created by GameBootstrap at startup.
///
/// Usage:
///   GameModeManager.ActivePlugin   — the plugin for the current game
///   GameModeManager.Registry       — the ordered list of all definitions
///   GameModeManager.GetPlugin(id)  — look up a plugin by modeId
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    /// <summary>Currently active plugin (set when a game starts).</summary>
    public IGameModePlugin ActivePlugin { get; private set; }

    /// <summary>The loaded registry asset (defines menu order).</summary>
    public GameModeRegistry Registry { get; private set; }

    /// <summary>All successfully loaded definitions, in menu order.</summary>
    public List<GameModeDefinition> LoadedDefinitions { get; private set; } = new();

    // Internal map: modeId → plugin instance
    private readonly Dictionary<string, IGameModePlugin> plugins = new();

    private void Awake()
    {
        Instance = this;
        LoadRegistry();
    }

    // ─── Registry Loading ──────────────────────────────────────

    /// <summary>
    /// Loads GameModeRegistry from Resources, then instantiates each
    /// plugin whose type can be resolved. Modes with missing types
    /// (deleted folders) are silently skipped.
    /// </summary>
    private void LoadRegistry()
    {
        Registry = Resources.Load<GameModeRegistry>("GameModes/GameModeRegistry");
        if (Registry == null)
        {
            Debug.LogWarning("GameModeManager: GameModeRegistry not found in Resources/GameModes/.");
            return;
        }

        foreach (var def in Registry.modes)
        {
            if (def == null) continue;

            // Always add the definition (even if plugin type is missing)
            // so the menu can show disabled/placeholder cards.
            LoadedDefinitions.Add(def);

            if (string.IsNullOrEmpty(def.pluginTypeName)) continue;

            // Try to find the C# type by name. If the assembly (folder) was
            // deleted, GetType returns null and we skip gracefully.
            Type pluginType = Type.GetType(def.pluginTypeName);
            if (pluginType == null)
            {
                Debug.Log($"GameModeManager: Plugin type '{def.pluginTypeName}' not found — skipping {def.modeId}.");
                continue;
            }

            try
            {
                var plugin = (IGameModePlugin)Activator.CreateInstance(pluginType);
                plugin.Definition = def;
                plugin.Initialize();
                plugins[def.modeId] = plugin;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameModeManager: Failed to create plugin '{def.pluginTypeName}': {ex.Message}");
            }
        }
    }

    // ─── Plugin Access ─────────────────────────────────────────

    /// <summary>Returns the plugin for the given modeId, or null.</summary>
    public IGameModePlugin GetPlugin(string modeId)
    {
        plugins.TryGetValue(modeId, out var p);
        return p;
    }

    /// <summary>Returns true if a plugin was loaded for the given mode.</summary>
    public bool HasPlugin(string modeId) => plugins.ContainsKey(modeId);

    // ─── Activation ────────────────────────────────────────────

    /// <summary>
    /// Sets the active plugin for the current game session.
    /// Called by GameManager.StartGame().
    /// </summary>
    public void SetActivePlugin(string modeId)
    {
        ActivePlugin = GetPlugin(modeId);
    }

    /// <summary>Clears the active plugin (called on game end / menu return).</summary>
    public void ClearActivePlugin()
    {
        ActivePlugin?.OnGameEnd();
        ActivePlugin = null;
    }
}
