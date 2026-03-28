using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// Lightweight player stats — stored in PlayerPrefs and synced to Firebase.
/// </summary>
public static class PlayerStats
{
    private const string KeyGamesPlayed   = "Stats_GamesPlayed";
    private const string KeyWins          = "Stats_Wins";
    private const string KeyLosses        = "Stats_Losses";
    private const string KeyCurrentStreak = "Stats_CurrentStreak";
    private const string KeyBestStreak    = "Stats_BestStreak";
    private const string KeyLastLoginDate = "Stats_LastLoginDate";

    public static int GamesPlayed   => PlayerPrefs.GetInt(KeyGamesPlayed, 0);
    public static int Wins          => PlayerPrefs.GetInt(KeyWins, 0);
    public static int Losses        => PlayerPrefs.GetInt(KeyLosses, 0);
    public static int CurrentStreak => PlayerPrefs.GetInt(KeyCurrentStreak, 0);
    public static int BestStreak    => PlayerPrefs.GetInt(KeyBestStreak, 0);
    public static string LastLoginDate => PlayerPrefs.GetString(KeyLastLoginDate, "");

    public static void RecordWin()
    {
        PlayerPrefs.SetInt(KeyGamesPlayed, GamesPlayed + 1);
        PlayerPrefs.SetInt(KeyWins, Wins + 1);
        int streak = CurrentStreak + 1;
        PlayerPrefs.SetInt(KeyCurrentStreak, streak);
        if (streak > BestStreak)
            PlayerPrefs.SetInt(KeyBestStreak, streak);
        PlayerPrefs.Save();
    }

    public static void RecordLoss()
    {
        PlayerPrefs.SetInt(KeyGamesPlayed, GamesPlayed + 1);
        PlayerPrefs.SetInt(KeyLosses, Losses + 1);
        PlayerPrefs.SetInt(KeyCurrentStreak, 0);
        PlayerPrefs.Save();
    }

    public static void RecordDraw()
    {
        PlayerPrefs.SetInt(KeyGamesPlayed, GamesPlayed + 1);
        PlayerPrefs.SetInt(KeyCurrentStreak, 0);
        PlayerPrefs.Save();
    }

    /// <summary>Returns true and awards gold if this is the first login today.</summary>
    public static bool CheckDailyLogin()
    {
        string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (LastLoginDate == today) return false;
        PlayerPrefs.SetString(KeyLastLoginDate, today);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Sync stats + leaderboard entry to Firebase.</summary>
    public static void SyncToFirebase(MonoBehaviour runner)
    {
        runner.StartCoroutine(SyncCoroutine());
    }

    private static IEnumerator SyncCoroutine()
    {
        var settings = Resources.Load<NetworkSettings>("NetworkSettings");
        if (settings == null || string.IsNullOrEmpty(settings.firebaseProjectUrl)) yield break;

        string deviceId = SystemInfo.deviceUniqueIdentifier.Replace("-", "");
        string safeId = deviceId.Substring(0, Mathf.Min(16, deviceId.Length));
        string displayName = PlayerPrefs.GetString("PlayerName", "Anonymous");
        string url = settings.firebaseProjectUrl;

        // Update player stats
        string statsJson = $"{{\"gamesPlayed\":{GamesPlayed},\"wins\":{Wins},\"losses\":{Losses},\"currentStreak\":{CurrentStreak},\"bestStreak\":{BestStreak}}}";
        yield return PatchRequest($"{url}/players/{safeId}/stats.json", statsJson);

        // Update leaderboard entry
        string leaderJson = $"{{\"name\":\"{EscapeJson(displayName)}\",\"wins\":{Wins},\"bestStreak\":{BestStreak},\"gamesPlayed\":{GamesPlayed},\"lastUpdated\":{Now()}}}";
        yield return PatchRequest($"{url}/leaderboard/{safeId}.json", leaderJson);
    }

    public static void FetchLeaderboard(MonoBehaviour runner, System.Action<LeaderboardEntry[]> onDone)
    {
        runner.StartCoroutine(FetchLeaderboardCoroutine(onDone));
    }

    private static IEnumerator FetchLeaderboardCoroutine(System.Action<LeaderboardEntry[]> onDone)
    {
        var settings = Resources.Load<NetworkSettings>("NetworkSettings");
        if (settings == null || string.IsNullOrEmpty(settings.firebaseProjectUrl))
        {
            onDone?.Invoke(new LeaderboardEntry[0]);
            yield break;
        }

        string url = $"{settings.firebaseProjectUrl}/leaderboard.json?orderBy=\"wins\"&limitToLast=50";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success || req.downloadHandler.text == "null")
        {
            onDone?.Invoke(new LeaderboardEntry[0]);
            yield break;
        }

        var entries = ParseLeaderboard(req.downloadHandler.text);
        // Sort descending by wins
        System.Array.Sort(entries, (a, b) => b.wins != a.wins ? b.wins.CompareTo(a.wins) : b.bestStreak.CompareTo(a.bestStreak));
        onDone?.Invoke(entries);
    }

    private static LeaderboardEntry[] ParseLeaderboard(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Trim() == "null")
            return new LeaderboardEntry[0];

        var list = new System.Collections.Generic.List<LeaderboardEntry>();
        string deviceId = SystemInfo.deviceUniqueIdentifier.Replace("-", "");
        string myId = deviceId.Substring(0, Mathf.Min(16, deviceId.Length));
        int i = 0;
        while (true)
        {
            int ks = json.IndexOf('"', i); if (ks < 0) break;
            int ke = json.IndexOf('"', ks + 1); if (ke < 0) break;
            string key = json.Substring(ks + 1, ke - ks - 1);
            int vs = json.IndexOf('{', ke); if (vs < 0) break;
            int ve = ObjEnd(json, vs);
            string obj = json.Substring(vs, ve - vs + 1);
            list.Add(new LeaderboardEntry
            {
                deviceId   = key,
                name       = Str(obj, "name"),
                wins       = (int)Lng(obj, "wins"),
                bestStreak = (int)Lng(obj, "bestStreak"),
                gamesPlayed = (int)Lng(obj, "gamesPlayed"),
                isMe       = key == myId
            });
            i = ve + 1;
        }
        return list.ToArray();
    }

    // ─── JSON helpers (same pattern as GameLogger) ───

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

    private static string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static long Now() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static IEnumerator PatchRequest(string url, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        using var req = new UnityWebRequest(url, "PATCH");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }
}

public class LeaderboardEntry
{
    public string deviceId;
    public string name;
    public int wins;
    public int bestStreak;
    public int gamesPlayed;
    public bool isMe;
}
