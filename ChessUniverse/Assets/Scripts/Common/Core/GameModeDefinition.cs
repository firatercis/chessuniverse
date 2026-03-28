using UnityEngine;

/// <summary>
/// Describes a single game mode — its identity, menu appearance, play options,
/// and rules text. One asset per game mode lives under Resources/GameModes/.
/// The UIManager reads these to build the main-menu cards at runtime.
/// </summary>
[CreateAssetMenu(fileName = "NewGameModeDefinition", menuName = "Chess/Game Mode Definition")]
public class GameModeDefinition : ScriptableObject
{
    // ─── Identity ───────────────────────────────────────────────

    [Tooltip("Unique key used in code (e.g. \"SeedChess\"). Must match the GameMode enum value.")]
    public string modeId;

    [Tooltip("Shown on the menu card title.")]
    public string displayName;

    [Tooltip("One-liner shown below the title on the menu card.")]
    public string description;

    // ─── Menu Card Appearance ───────────────────────────────────

    [Tooltip("72 x 72 icon displayed on the left side of the card.")]
    public Sprite icon;

    [Tooltip("Badges shown in the top-right corner of the card. Drag BadgeDefinition assets here.")]
    public BadgeDefinition[] badges;

    [Tooltip("Label for the primary (green) action button on the card.")]
    public string primaryButtonText = "Play";

    [Tooltip("When true the card is greyed out and shows disabledText instead of action buttons.")]
    public bool isDisabled;

    [Tooltip("Text shown on a disabled card (e.g. \"Under Construction\").")]
    public string disabledText = "Under Construction";

    // ─── Supported Play Modes ───────────────────────────────────

    public bool supportsSinglePlayer = true;
    public bool supportsLocal = true;
    public bool supportsOnline = true;

    // ─── Rules Dialog ───────────────────────────────────────────

    [TextArea(2, 4)]
    public string rulesTitle;

    [TextArea(5, 20)]
    public string rulesText;

    // ─── Tutorial ───────────────────────────────────────────────

    [Tooltip("PlayerPrefs key that tracks whether the tutorial has been completed.")]
    public string tutorialPrefsKey;

    public bool hasTutorial;

    // ─── Board Setup ────────────────────────────────────────────

    [Tooltip("Optional custom starting position. Null = use default for the mode.")]
    public BoardSetup startingPosition;

    // ─── Plugin Binding ─────────────────────────────────────────

    [Tooltip("Fully-qualified C# type name of the IGameModePlugin implementation. " +
             "GameModeManager uses reflection to instantiate it. If the type is missing " +
             "(e.g. the game-mode folder was deleted) the mode is silently skipped.")]
    public string pluginTypeName;
}
