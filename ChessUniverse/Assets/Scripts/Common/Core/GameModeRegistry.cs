using UnityEngine;

/// <summary>
/// Master list that controls which game modes appear in the main menu and in
/// what order. Lives at Resources/GameModes/GameModeRegistry.asset.
///
/// To add a new game mode:
///   1. Create a GameModeDefinition asset for it.
///   2. Drag it into this registry's array.
///   3. Implement IGameModePlugin and set the pluginTypeName.
///
/// To remove a game mode:
///   1. Delete its Scripts and Resources.
///   2. Remove it from this array (or leave it — GameModeManager skips
///      entries whose plugin type can't be found).
/// </summary>
[CreateAssetMenu(fileName = "GameModeRegistry", menuName = "Chess/Game Mode Registry")]
public class GameModeRegistry : ScriptableObject
{
    [Tooltip("Ordered list of game modes shown in the main menu. " +
             "Drag to reorder. Modes whose plugin type is missing are silently skipped.")]
    public GameModeDefinition[] modes;
}
