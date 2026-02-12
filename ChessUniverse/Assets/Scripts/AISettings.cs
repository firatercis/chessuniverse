using UnityEngine;

[CreateAssetMenu(fileName = "AISettings", menuName = "Chess/AI Settings")]
public class AISettings : ScriptableObject
{
    [Header("Classic Chess")]
    public int classicSearchDepth = 4;

    [Header("Seed Chess")]
    public int seedSearchDepth = 3;
    public bool simulateSeedHatching = false;
    public float seedDiscountBase = 0.85f;

    [Header("General")]
    public float aiDelay = 0.5f;
}
