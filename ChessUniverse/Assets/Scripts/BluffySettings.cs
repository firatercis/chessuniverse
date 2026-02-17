using UnityEngine;

[CreateAssetMenu(fileName = "BluffySettings", menuName = "Chess/BluffySettings")]
public class BluffySettings : ScriptableObject
{
    [Header("Debug")]
    [Tooltip("Hold this key to peek at real pieces behind masks")]
    public KeyCode peekKey = KeyCode.Tab;

    [Header("AI Timing")]
    public float aiMoveDelay = 0.6f;
    public float bluffCallDelay = 0.8f;

    [Header("AI Bluff Calling")]
    [Tooltip("AI won't consider calling bluff unless P(bluff) exceeds this")]
    [Range(0f, 1f)]
    public float minBluffProbToCall = 0.25f;

    [Tooltip("Capped King value for belief EV calculation (prevents always-call due to King's huge value)")]
    public int beliefKingValue = 1000;

    [Header("AI Bluff Making")]
    [Tooltip("Base probability AI picks a bluff move over a safe move of similar value")]
    [Range(0f, 1f)]
    public float bluffMoveChance = 0.2f;

    [Tooltip("Valuable pieces (Rook+) bluff less. This multiplier is applied to bluffMoveChance.")]
    [Range(0f, 1f)]
    public float valuablePieceBluffReduction = 0.4f;

    [Header("AI Risk & Tactics")]
    [Tooltip("How likely AI thinks opponent will call bluff on its moves")]
    [Range(0f, 1f)]
    public float expectedCatchRate = 0.25f;

    [Tooltip("Score range for random move selection among top moves (higher = more varied play)")]
    public float moveRandomness = 30f;

    [Tooltip("Bonus evaluation points subtracted for capture moves (AI minimizes, so negative = prefer)")]
    public float captureBonus = 50f;

    [Tooltip("Overall aggression multiplier. Higher values make AI prefer aggressive/risky play.")]
    [Range(0.5f, 2f)]
    public float aggressionMultiplier = 1.0f;

    [Header("Animation")]
    [Tooltip("Piece sliding speed in world units per second")]
    public float pieceAnimSpeed = 12f;

    [Tooltip("Delay before showing pass-device panel so piece animation can finish")]
    public float passDeviceDelay = 0.35f;
}
