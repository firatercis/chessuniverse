using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public bool IsOnline { get; private set; }
    public bool IsHost { get; private set; }
    public PieceColor MyColor => IsHost ? PieceColor.White : PieceColor.Black;

    private string roomCode;
    private string firebaseUrl;
    private int lastProcessedAction = -1;
    private int lastPushedAction = -1; // track our own pushed actions to skip them
    private Coroutine pollCoroutine;
    private NetworkSettings settings;

    // Connection status
    private float lastSuccessfulPoll;
    private bool isReconnecting;

    // Bluffy setup tracking
    private bool mySetupSent;
    private bool opponentSetupReceived;
    private string opponentSetupPositions;

    // Callback for when opponent joins
    public event Action OnOpponentJoined;
    public event Action<string> OnConnectionStatusChanged;

    private void Awake()
    {
        Instance = this;
        settings = Resources.Load<NetworkSettings>("NetworkSettings");
        if (settings == null)
            settings = ScriptableObject.CreateInstance<NetworkSettings>();
        firebaseUrl = settings.firebaseProjectUrl;
    }

    // ─── Room Management ───

    public void CreateRoom(GameMode mode, Action<string> onCreated, Action<string> onError)
    {
        StartCoroutine(CreateRoomCoroutine(mode, onCreated, onError));
    }

    private IEnumerator CreateRoomCoroutine(GameMode mode, Action<string> onCreated, Action<string> onError)
    {
        roomCode = GenerateRoomCode();
        string roomUrl = $"{firebaseUrl}/rooms/{roomCode}";

        string modeStr = mode.ToString();
        string json = $"{{\"gameMode\":\"{modeStr}\",\"hostJoined\":true,\"guestJoined\":false,\"actionCount\":0}}";

        yield return PutRequest($"{roomUrl}.json", json, (success, response) =>
        {
            if (success)
            {
                IsHost = true;
                IsOnline = true;
                lastProcessedAction = -1;
                lastPushedAction = -1;
                mySetupSent = false;
                opponentSetupReceived = false;
                onCreated?.Invoke(roomCode);
                pollCoroutine = StartCoroutine(WaitForGuestCoroutine());
            }
            else
            {
                onError?.Invoke("Failed to create room");
            }
        });
    }

    private IEnumerator WaitForGuestCoroutine()
    {
        while (IsOnline)
        {
            yield return new WaitForSeconds(settings.pollIntervalSeconds);

            bool done = false;
            yield return GetRequest($"{firebaseUrl}/rooms/{roomCode}/guestJoined.json", (success, response) =>
            {
                if (success && response.Trim() == "true")
                {
                    done = true;
                    OnOpponentJoined?.Invoke();
                }
            });

            if (done)
            {
                // Start game polling
                lastSuccessfulPoll = Time.time;
                pollCoroutine = StartCoroutine(PollLoop());
                yield break;
            }
        }
    }

    public void JoinRoom(string code, Action<GameMode> onJoined, Action<string> onError)
    {
        StartCoroutine(JoinRoomCoroutine(code, onJoined, onError));
    }

    private IEnumerator JoinRoomCoroutine(string code, Action<GameMode> onJoined, Action<string> onError)
    {
        roomCode = code.ToUpper();
        string roomUrl = $"{firebaseUrl}/rooms/{roomCode}";

        // Check if room exists
        yield return GetRequest($"{roomUrl}.json", (success, response) =>
        {
            if (!success || response.Trim() == "null")
            {
                onError?.Invoke("Room not found");
                return;
            }

            // Parse gameMode from response
            var roomData = JsonUtility.FromJson<RoomData>(response);
            if (roomData == null)
            {
                onError?.Invoke("Invalid room data");
                return;
            }

            if (roomData.guestJoined)
            {
                onError?.Invoke("Room is full");
                return;
            }

            IsHost = false;
            IsOnline = true;
            lastProcessedAction = -1;
            lastPushedAction = -1;
            mySetupSent = false;
            opponentSetupReceived = false;

            GameMode mode;
            if (!Enum.TryParse(roomData.gameMode, out mode))
                mode = GameMode.Classic;

            // Mark guest joined
            StartCoroutine(PatchRequest($"{roomUrl}.json", "{\"guestJoined\":true}", (s, r) =>
            {
                if (s)
                {
                    lastSuccessfulPoll = Time.time;
                    pollCoroutine = StartCoroutine(PollLoop());
                    onJoined?.Invoke(mode);
                }
                else
                {
                    onError?.Invoke("Failed to join room");
                }
            }));
        });
    }

    public void LeaveRoom()
    {
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }

        if (IsOnline && !string.IsNullOrEmpty(roomCode))
        {
            // Fire and forget: delete room
            StartCoroutine(DeleteRequest($"{firebaseUrl}/rooms/{roomCode}.json"));
        }

        IsOnline = false;
        IsHost = false;
        roomCode = null;
        lastProcessedAction = -1;
        lastPushedAction = -1;
        mySetupSent = false;
        opponentSetupReceived = false;
    }

    // ─── Action Sync ───

    public void PushAction(NetworkAction action)
    {
        if (!IsOnline) return;
        StartCoroutine(PushActionCoroutine(action));
    }

    private IEnumerator PushActionCoroutine(NetworkAction action)
    {
        string roomUrl = $"{firebaseUrl}/rooms/{roomCode}";

        // Get current actionCount
        int currentCount = -1;
        yield return GetRequest($"{roomUrl}/actionCount.json", (success, response) =>
        {
            if (success)
            {
                int.TryParse(response.Trim(), out currentCount);
            }
        });

        if (currentCount < 0) yield break;

        // Write the action
        string actionJson = action.ToJson();
        int actionIndex = currentCount;
        yield return PutRequest($"{roomUrl}/actions/{actionIndex}.json", actionJson, (success, response) =>
        {
            if (success)
                lastPushedAction = actionIndex;
            else
                Debug.LogError("Failed to push action");
        });

        // Increment actionCount
        yield return PutRequest($"{roomUrl}/actionCount.json", (currentCount + 1).ToString(), (success, response) =>
        {
            if (!success)
                Debug.LogError("Failed to update actionCount");
        });
    }

    // ─── Polling ───

    private IEnumerator PollLoop()
    {
        while (IsOnline)
        {
            yield return new WaitForSeconds(settings.pollIntervalSeconds);

            string roomUrl = $"{firebaseUrl}/rooms/{roomCode}";
            bool gotResponse = false;

            yield return GetRequest($"{roomUrl}/actionCount.json", (success, response) =>
            {
                if (success)
                {
                    gotResponse = true;
                    lastSuccessfulPoll = Time.time;

                    if (isReconnecting)
                    {
                        isReconnecting = false;
                        OnConnectionStatusChanged?.Invoke("Connected");
                    }

                    int serverCount;
                    if (int.TryParse(response.Trim(), out serverCount))
                    {
                        if (serverCount > lastProcessedAction + 1)
                        {
                            // Fetch new actions
                            StartCoroutine(FetchNewActions(lastProcessedAction + 1, serverCount));
                        }
                    }
                }
            });

            if (!gotResponse)
            {
                float elapsed = Time.time - lastSuccessfulPoll;
                if (elapsed > settings.connectionTimeoutSeconds && !isReconnecting)
                {
                    isReconnecting = true;
                    OnConnectionStatusChanged?.Invoke("Reconnecting...");
                }
            }
        }
    }

    private IEnumerator FetchNewActions(int fromIndex, int toCount)
    {
        string roomUrl = $"{firebaseUrl}/rooms/{roomCode}";

        for (int i = fromIndex; i < toCount; i++)
        {
            int idx = i; // capture for closure

            // Skip our own actions (already applied locally)
            if (idx <= lastPushedAction)
            {
                lastProcessedAction = idx;
                continue;
            }

            yield return GetRequest($"{roomUrl}/actions/{idx}.json", (success, response) =>
            {
                if (success && response.Trim() != "null")
                {
                    var action = NetworkAction.FromJson(response);
                    if (action != null)
                    {
                        lastProcessedAction = idx;
                        ProcessAction(action);
                    }
                }
            });
        }
    }

    // ─── Action Processing ───

    private void ProcessAction(NetworkAction action)
    {
        switch (action.type)
        {
            case "move":
                ProcessMoveAction(action);
                break;
            case "seedPlant":
                ProcessSeedPlantAction(action);
                break;
            case "bluffySetup":
                ProcessBluffySetupAction(action);
                break;
            case "bluffCall":
                ProcessBluffCallAction();
                break;
            case "bluffAccept":
                ProcessBluffAcceptAction();
                break;
            case "sacrifice":
                ProcessSacrificeAction(action);
                break;
            case "rearrangeSwap":
                ProcessRearrangeSwapAction(action);
                break;
            case "rearrangeSkip":
                ProcessRearrangeSkipAction();
                break;
            case "resign":
                ProcessResignAction();
                break;
        }
    }

    private void ProcessMoveAction(NetworkAction action)
    {
        // Only process opponent's moves
        PieceType? promo = null;
        if (!string.IsNullOrEmpty(action.promotion))
        {
            Enum.TryParse(action.promotion, out PieceType pt);
            promo = pt;
        }

        GameManager.Instance.ApplyRemoteMove(
            action.fromX, action.fromY,
            action.toX, action.toY,
            promo);
    }

    private void ProcessSeedPlantAction(NetworkAction action)
    {
        Enum.TryParse(action.seedType, out PieceType st);
        GameManager.Instance.ApplyRemoteSeedPlant(action.fromX, action.fromY, st);
    }

    private void ProcessBluffySetupAction(NetworkAction action)
    {
        opponentSetupReceived = true;
        opponentSetupPositions = action.positions;

        // If both setups done, start game
        if (mySetupSent && opponentSetupReceived)
            BluffyManager.Instance.OnBothSetupsReady(opponentSetupPositions, IsHost);
    }

    private void ProcessBluffCallAction()
    {
        GameManager.Instance.SetRemoteActionFlag(true);
        GameManager.Instance.OnBluffCalled();
        GameManager.Instance.SetRemoteActionFlag(false);
    }

    private void ProcessBluffAcceptAction()
    {
        GameManager.Instance.SetRemoteActionFlag(true);
        GameManager.Instance.OnMoveAccepted();
        GameManager.Instance.SetRemoteActionFlag(false);
    }

    private void ProcessSacrificeAction(NetworkAction action)
    {
        BluffyManager.Instance.ApplyRemoteSacrifice(action.fromX, action.fromY);
    }

    private void ProcessRearrangeSwapAction(NetworkAction action)
    {
        BluffyManager.Instance.ApplyRemoteRearrangeSwap(action.x1, action.y1, action.x2, action.y2);
    }

    private void ProcessRearrangeSkipAction()
    {
        BluffyManager.Instance.ApplyRemoteRearrangeSkip();
    }

    private void ProcessResignAction()
    {
        // Opponent resigned, we win
        PieceColor winner = MyColor;
        GameManager.Instance.gameState = GameState.Checkmate;

        if (GameBootstrap.CurrentMode == GameMode.BluffyChess)
        {
            BluffyManager.Instance.currentPhase = BluffyPhase.GameOver;
            UIManager.Instance.ShowBluffyGameOver(winner);
        }
        else
        {
            UIManager.Instance.ShowGameOver(winner);
        }
    }

    // ─── Bluffy Setup Helpers ───

    public void SendMySetup(string positionsJson)
    {
        mySetupSent = true;
        PushAction(NetworkAction.BluffySetup(positionsJson));

        if (mySetupSent && opponentSetupReceived)
            BluffyManager.Instance.OnBothSetupsReady(opponentSetupPositions, IsHost);
    }

    public bool IsMyTurn()
    {
        return GameManager.Instance.currentTurn == MyColor;
    }

    // ─── Firebase REST Helpers ───

    private IEnumerator PutRequest(string url, string json, Action<bool, string> callback)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest(url, "PUT");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string response = success ? request.downloadHandler.text : request.error;
        callback?.Invoke(success, response);
    }

    private IEnumerator GetRequest(string url, Action<bool, string> callback)
    {
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string response = success ? request.downloadHandler.text : request.error;
        callback?.Invoke(success, response);
    }

    private IEnumerator PatchRequest(string url, string json, Action<bool, string> callback)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using var request = new UnityWebRequest(url, "PATCH");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string response = success ? request.downloadHandler.text : request.error;
        callback?.Invoke(success, response);
    }

    private IEnumerator DeleteRequest(string url)
    {
        using var request = UnityWebRequest.Delete(url);
        yield return request.SendWebRequest();
    }

    // ─── Utilities ───

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(code);
    }

    [Serializable]
    private class RoomData
    {
        public string gameMode;
        public bool hostJoined;
        public bool guestJoined;
        public int actionCount;
    }
}
