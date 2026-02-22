using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class GameSummary
{
    public string gameId;
    public string mode;
    public string playMode;
    public string result;
    public long   startedAt;
    public int    actionCount;
    public string playerName;
}

public class ReplayAction
{
    public string type;
    public int    fx, fy, tx, ty;
    public string piece;
    public string seedType;
    public string promotion;
    public string positions;
    public string color;
    public int    x1, y1, x2, y2;
    public long   ts;
}

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    private string firebaseUrl;
    private string currentGameId;
    private int    actionCount;

    private void Awake()
    {
        Instance = this;
        var s = Resources.Load<NetworkSettings>("NetworkSettings");
        firebaseUrl = s != null ? s.firebaseProjectUrl : "";
    }

    // ─── Game lifecycle ───

    public void StartGame(GameMode mode, PlayMode playMode)
    {
        if (string.IsNullOrEmpty(firebaseUrl)) return;
        // Online: only host logs to avoid duplicate entries
        if (playMode == PlayMode.Online && NetworkManager.Instance != null && !NetworkManager.Instance.IsHost) return;

        string playerName = PlayerPrefs.GetString("PlayerName", "Anonymous");
        currentGameId = $"g{Now()}_{UnityEngine.Random.Range(100, 999)}";
        actionCount   = 0;
        string json   = $"{{\"mode\":\"{mode}\",\"playMode\":\"{playMode}\",\"playerName\":\"{EscapeJson(playerName)}\",\"startedAt\":{Now()},\"result\":\"ongoing\",\"actionCount\":0}}";
        StartCoroutine(Put($"{firebaseUrl}/games/{currentGameId}.json", json));
    }

    public void RegisterPlayer(string displayName)
    {
        if (string.IsNullOrEmpty(firebaseUrl)) return;
        string deviceId = SystemInfo.deviceUniqueIdentifier.Replace("-", "");
        string safeId   = deviceId.Substring(0, Mathf.Min(16, deviceId.Length));
        string json     = $"{{\"name\":\"{EscapeJson(displayName)}\",\"registeredAt\":{Now()}}}";
        StartCoroutine(Put($"{firebaseUrl}/players/{safeId}.json", json));
    }

    public void EndGame(string result)
    {
        if (string.IsNullOrEmpty(currentGameId)) return;
        StartCoroutine(Patch($"{firebaseUrl}/games/{currentGameId}.json",
            $"{{\"result\":\"{result}\",\"endedAt\":{Now()}}}"));
        currentGameId = null;
    }

    // ─── Action logging ───

    public void LogMove(int fx, int fy, int tx, int ty, PieceType piece, PieceType? promo = null)
    {
        string note  = $"{Col(fx)}{fy + 1}{Col(tx)}{ty + 1}";
        string promoStr = promo.HasValue ? $",\"promotion\":\"{promo.Value}\"" : "";
        Push($"{{\"type\":\"move\",\"fx\":{fx},\"fy\":{fy},\"tx\":{tx},\"ty\":{ty},\"piece\":\"{piece}\",\"notation\":\"{note}\"{promoStr},\"ts\":{Now()}}}");
    }

    public void LogSeedPlant(int x, int y, PieceType seedType)
        => Push($"{{\"type\":\"seedPlant\",\"fx\":{x},\"fy\":{y},\"seedType\":\"{seedType}\",\"ts\":{Now()}}}");

    public void LogBluffySetup(PieceColor color, string positions)
        => Push($"{{\"type\":\"bluffySetup\",\"color\":\"{color}\",\"positions\":\"{positions}\",\"ts\":{Now()}}}");

    public void LogBluffCall()   => Push($"{{\"type\":\"bluffCall\",\"ts\":{Now()}}}");
    public void LogBluffAccept() => Push($"{{\"type\":\"bluffAccept\",\"ts\":{Now()}}}");

    public void LogSacrifice(int x, int y)
        => Push($"{{\"type\":\"sacrifice\",\"fx\":{x},\"fy\":{y},\"ts\":{Now()}}}");

    public void LogRearrangeSwap(int x1, int y1, int x2, int y2)
        => Push($"{{\"type\":\"rearrangeSwap\",\"x1\":{x1},\"y1\":{y1},\"x2\":{x2},\"y2\":{y2},\"ts\":{Now()}}}");

    // ─── Admin fetch ───

    public void FetchRecentGames(Action<List<GameSummary>> onDone)
        => StartCoroutine(FetchGamesCoroutine(onDone));

    private IEnumerator FetchGamesCoroutine(Action<List<GameSummary>> onDone)
    {
        using var req = UnityWebRequest.Get($"{firebaseUrl}/games.json?limitToLast=40");
        yield return req.SendWebRequest();
        var list = new List<GameSummary>();
        if (req.result == UnityWebRequest.Result.Success)
            list = ParseGameList(req.downloadHandler.text);
        list.Sort((a, b) => b.startedAt.CompareTo(a.startedAt));
        onDone?.Invoke(list);
    }

    public void FetchGameReplay(string gameId, Action<string, List<ReplayAction>> onDone)
        => StartCoroutine(FetchReplayCoroutine(gameId, onDone));

    private IEnumerator FetchReplayCoroutine(string gameId, Action<string, List<ReplayAction>> onDone)
    {
        using var req = UnityWebRequest.Get($"{firebaseUrl}/games/{gameId}.json");
        yield return req.SendWebRequest();
        string mode    = "";
        var    actions = new List<ReplayAction>();
        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            mode    = Str(json, "mode");
            actions = ParseReplayActions(json);
        }
        onDone?.Invoke(mode, actions);
    }

    // ─── JSON helpers ───

    private List<GameSummary> ParseGameList(string json)
    {
        var list = new List<GameSummary>();
        if (string.IsNullOrEmpty(json) || json.Trim() == "null") return list;
        int i = 0;
        while (true)
        {
            int ks = json.IndexOf('"', i); if (ks < 0) break;
            int ke = json.IndexOf('"', ks + 1); if (ke < 0) break;
            string key = json.Substring(ks + 1, ke - ks - 1);
            int vs = json.IndexOf('{', ke); if (vs < 0) break;
            int ve = ObjEnd(json, vs);
            string obj = json.Substring(vs, ve - vs + 1);
            list.Add(new GameSummary
            {
                gameId      = key,
                mode        = Str(obj, "mode"),
                playMode    = Str(obj, "playMode"),
                result      = Str(obj, "result"),
                startedAt   = Lng(obj, "startedAt"),
                actionCount = (int)Lng(obj, "actionCount"),
                playerName  = Str(obj, "playerName")
            });
            i = ve + 1;
        }
        return list;
    }

    private List<ReplayAction> ParseReplayActions(string gameJson)
    {
        var list = new List<ReplayAction>();
        if (string.IsNullOrEmpty(gameJson) || gameJson.Trim() == "null") return list;

        int actIdx = gameJson.IndexOf("\"actions\""); if (actIdx < 0) return list;
        int actStart = gameJson.IndexOf('{', actIdx); if (actStart < 0) return list;
        int actEnd = ObjEnd(gameJson, actStart);
        string actJson = gameJson.Substring(actStart, actEnd - actStart + 1);

        var raw = new SortedDictionary<int, string>();
        int i = 0;
        while (true)
        {
            int ks = actJson.IndexOf('"', i); if (ks < 0) break;
            int ke = actJson.IndexOf('"', ks + 1); if (ke < 0) break;
            string key = actJson.Substring(ks + 1, ke - ks - 1);
            int vs = actJson.IndexOf('{', ke); if (vs < 0) break;
            int ve = ObjEnd(actJson, vs);
            if (int.TryParse(key, out int idx))
                raw[idx] = actJson.Substring(vs, ve - vs + 1);
            i = ve + 1;
        }

        foreach (var kvp in raw)
        {
            string a = kvp.Value;
            list.Add(new ReplayAction
            {
                type      = Str(a, "type"),
                fx        = (int)Lng(a, "fx"),   fy = (int)Lng(a, "fy"),
                tx        = (int)Lng(a, "tx"),   ty = (int)Lng(a, "ty"),
                piece     = Str(a, "piece"),
                seedType  = Str(a, "seedType"),
                promotion = Str(a, "promotion"),
                positions = Str(a, "positions"),
                color     = Str(a, "color"),
                x1        = (int)Lng(a, "x1"),   y1 = (int)Lng(a, "y1"),
                x2        = (int)Lng(a, "x2"),   y2 = (int)Lng(a, "y2"),
                ts        = Lng(a, "ts")
            });
        }
        return list;
    }

    private static int ObjEnd(string s, int start)
    {
        int depth = 0;
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}' && --depth == 0) return i;
        }
        return s.Length - 1;
    }

    private static string Str(string json, string key)
    {
        string search = $"\"{key}\":\"";
        int i = json.IndexOf(search); if (i < 0) return "";
        int s = i + search.Length;
        int e = json.IndexOf('"', s);
        return e > s ? json.Substring(s, e - s) : "";
    }

    private static long Lng(string json, string key)
    {
        string search = $"\"{key}\":";
        int i = json.IndexOf(search); if (i < 0) return 0;
        int s = i + search.Length;
        while (s < json.Length && json[s] == ' ') s++;
        if (s >= json.Length || json[s] == '"') return 0;
        int e = s;
        while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '-')) e++;
        return long.TryParse(json.Substring(s, e - s), out long v) ? v : 0;
    }

    // ─── HTTP ───

    private void Push(string json)
    {
        if (string.IsNullOrEmpty(currentGameId)) return;
        int idx = actionCount++;
        StartCoroutine(Put($"{firebaseUrl}/games/{currentGameId}/actionCount.json", actionCount.ToString()));
        StartCoroutine(Put($"{firebaseUrl}/games/{currentGameId}/actions/{idx}.json", json));
    }

    private static string Col(int x)        => ((char)('a' + x)).ToString();
    private static long   Now()             => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private IEnumerator Put(string url, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        using var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }

    private IEnumerator Patch(string url, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        using var req = new UnityWebRequest(url, "PATCH");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }
}
