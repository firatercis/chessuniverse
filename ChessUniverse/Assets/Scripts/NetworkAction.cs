[System.Serializable]
public class NetworkAction
{
    public string type;
    public int fromX, fromY, toX, toY;
    public string promotion;
    public string seedType;
    public string positions; // Bluffy setup JSON
    public int x1, y1, x2, y2; // rearrange swap coords

    public static NetworkAction Move(int fromX, int fromY, int toX, int toY, string promotion = null)
    {
        return new NetworkAction { type = "move", fromX = fromX, fromY = fromY, toX = toX, toY = toY, promotion = promotion };
    }

    public static NetworkAction SeedPlant(int x, int y, string seedType)
    {
        return new NetworkAction { type = "seedPlant", fromX = x, fromY = y, seedType = seedType };
    }

    public static NetworkAction BluffySetup(string positionsJson)
    {
        return new NetworkAction { type = "bluffySetup", positions = positionsJson };
    }

    public static NetworkAction BluffCall()
    {
        return new NetworkAction { type = "bluffCall" };
    }

    public static NetworkAction BluffAccept()
    {
        return new NetworkAction { type = "bluffAccept" };
    }

    public static NetworkAction Sacrifice(int x, int y)
    {
        return new NetworkAction { type = "sacrifice", fromX = x, fromY = y };
    }

    public static NetworkAction RearrangeSwap(int x1, int y1, int x2, int y2)
    {
        return new NetworkAction { type = "rearrangeSwap", x1 = x1, y1 = y1, x2 = x2, y2 = y2 };
    }

    public static NetworkAction RearrangeSkip()
    {
        return new NetworkAction { type = "rearrangeSkip" };
    }

    public static NetworkAction Resign()
    {
        return new NetworkAction { type = "resign" };
    }

    public string ToJson()
    {
        return UnityEngine.JsonUtility.ToJson(this);
    }

    public static NetworkAction FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<NetworkAction>(json);
    }
}
