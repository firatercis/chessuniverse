using UnityEngine;

public enum MarketCategory { PieceStyle, BoardTheme }

[CreateAssetMenu(menuName = "Chess/Market Item")]
public class MarketItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    public string description;
    public Sprite icon;
    public int price;
    public MarketCategory category;
    public bool isDefault;

    // Piece style packs
    public Sprite[] pieceSprites;

    // Board themes
    public Color lightSquareColor = new Color(0.941f, 0.851f, 0.710f);
    public Color darkSquareColor  = new Color(0.710f, 0.533f, 0.388f);
}
