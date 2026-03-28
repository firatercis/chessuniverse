using UnityEngine;

/// <summary>
/// A small reusable label that can be attached to any game mode card.
/// Create assets via Assets → Create → Chess → Badge.
///
/// Examples:  NEW (green), PVP (purple), SOLO (amber), BETA (blue)
/// </summary>
[CreateAssetMenu(fileName = "NewBadge", menuName = "Chess/Badge")]
public class BadgeDefinition : ScriptableObject
{
    [Tooltip("Short text displayed inside the badge pill (e.g. NEW, PVP, SOLO).")]
    public string label;

    [Tooltip("Background colour of the badge pill.")]
    public Color color = Color.green;
}
