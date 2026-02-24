using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private Canvas canvas;
    private TextMeshProUGUI turnText;
    private GameObject gameOverPanel;
    private TextMeshProUGUI gameOverText;
    private GameObject promotionPanel;
    private GameObject mainMenuPanel;
    private GameObject playerModePanel;
    private GameMode pendingGameMode;
    private GameObject seedButtonsPanel;

    // Bluffy panels
    private GameObject setupPanel;
    private TextMeshProUGUI setupText;
    private GameObject passDevicePanel;
    private TextMeshProUGUI passDeviceText;
    private GameObject bluffPanel;
    private GameObject sacrificePanel;
    private TextMeshProUGUI sacrificeText;
    private GameObject rearrangePanel;
    private TextMeshProUGUI rearrangeText;
    private Button singlePlayerButton;
    private GameObject infoPanel;
    private TextMeshProUGUI infoText;
    private System.Action infoDismissAction;

    // Rules dialog
    private GameObject rulesPanel;
    private TextMeshProUGUI rulesText;

    // Orientation
    private CanvasScaler canvasScaler;
    private bool lastPortrait;

    // Player profile panel
    private GameObject profilePanel;
    private TMP_InputField profileNameInput;

    // Admin panel
    private const string AdminPassword = "chess2025";
    private GameObject adminPasswordPanel;
    private TMP_InputField adminPasswordInput;
    private TextMeshProUGUI adminPasswordError;
    private GameObject adminGameListPanel;
    private GameObject adminGameListContent;
    private TextMeshProUGUI adminStatusText;

    // Replay controls
    private GameObject replayControlsPanel;
    private TextMeshProUGUI replayProgressText;
    private Coroutine replayAutoCoroutine;

    // Splash text (dramatic bluff call)
    private GameObject splashPanel;
    private TextMeshProUGUI splashText;
    private CanvasGroup splashCanvasGroup;

    // Online panels
    private GameObject lobbyPanel;
    private GameObject hostWaitPanel;
    private TextMeshProUGUI hostWaitCodeText;
    private GameObject joinPanel;
    private TMP_InputField joinCodeInput;
    private TextMeshProUGUI joinErrorText;
    private GameObject connectionStatusObj;
    private TextMeshProUGUI connectionStatusText;

    private void Awake()
    {
        Instance = this;
        CreateUI();
    }

    private void Update()
    {
        bool isPortrait = Screen.height > Screen.width;
        if (isPortrait == lastPortrait) return;
        lastPortrait = isPortrait;
        canvasScaler.matchWidthOrHeight = isPortrait ? 1f : 0f;
        GameManager.Instance?.UpdateCameraSize();
    }

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("UICanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler = canvasObj.GetComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        lastPortrait = Screen.height > Screen.width;
        canvasScaler.matchWidthOrHeight = lastPortrait ? 1f : 0f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem (required for UI button clicks)
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Turn indicator
        CreateTurnIndicator(canvasObj);

        // Game over panel
        CreateGameOverPanel(canvasObj);

        // Promotion panel
        CreatePromotionPanel(canvasObj);

        // Restart button
        CreateRestartButton(canvasObj);

        // File/Rank labels
        CreateBoardLabels(canvasObj);

        // Menus and panels
        CreateMainMenuPanel(canvasObj);
        CreatePlayerModePanel(canvasObj);
        CreateSeedButtonsPanel(canvasObj);

        // Splash text
        CreateSplashPanel(canvasObj);

        // Online panels
        CreateLobbyPanel(canvasObj);
        CreateHostWaitPanel(canvasObj);
        CreateJoinPanel(canvasObj);
        CreateConnectionStatus(canvasObj);

        // Bluffy panels
        CreateSetupPanel(canvasObj);
        CreatePassDevicePanel(canvasObj);
        CreateBluffPanel(canvasObj);
        CreateSacrificePanel(canvasObj);
        CreateRearrangePanel(canvasObj);
        CreateInfoPanel(canvasObj);
        CreateRulesPanel(canvasObj);

        // Profile + Admin + Replay (on top of everything)
        CreateProfilePanel(canvasObj);
        CreateAdminPasswordPanel(canvasObj);
        CreateAdminGameListPanel(canvasObj);
        CreateReplayControlsPanel(canvasObj);
    }

    private void CreateTurnIndicator(GameObject parent)
    {
        GameObject turnObj = new GameObject("TurnText");
        turnObj.transform.SetParent(parent.transform, false);

        turnText = turnObj.AddComponent<TextMeshProUGUI>();
        turnText.text = "White's Turn";
        turnText.fontSize = 36;
        turnText.alignment = TextAlignmentOptions.Center;
        turnText.color = Color.white;
        turnText.fontStyle = FontStyles.Bold;

        var rect = turnText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(500, 60);

        // Background
        GameObject bgObj = new GameObject("TurnBG");
        bgObj.transform.SetParent(turnObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);
        var bgRect = bgImg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(40, 20);
        bgRect.anchoredPosition = Vector2.zero;
    }

    private void CreateGameOverPanel(GameObject parent)
    {
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(parent.transform, false);

        var panelImg = gameOverPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.85f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 250);

        // Game over text
        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(gameOverPanel.transform, false);
        gameOverText = textObj.AddComponent<TextMeshProUGUI>();
        gameOverText.fontSize = 42;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.white;
        gameOverText.fontStyle = FontStyles.Bold;
        var textRect = gameOverText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0.4f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(-20, -20);

        // Play again button
        CreateButton(gameOverPanel, "PlayAgainBtn", "Play Again", new Vector2(0, -70), () => GameManager.Instance.RestartGame());

        gameOverPanel.SetActive(false);
    }

    private void CreatePromotionPanel(GameObject parent)
    {
        promotionPanel = new GameObject("PromotionPanel");
        promotionPanel.transform.SetParent(parent.transform, false);

        var panelImg = promotionPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.9f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 200);

        // Title
        GameObject titleObj = new GameObject("PromTitle");
        titleObj.transform.SetParent(promotionPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Promote Pawn To:";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0, 0.65f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -10);

        // Promotion buttons
        float startX = -170;
        string[] labels = { "Queen", "Rook", "Bishop", "Knight" };
        PieceType[] types = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };

        for (int i = 0; i < 4; i++)
        {
            PieceType t = types[i];
            CreateButton(promotionPanel, $"Prom_{labels[i]}", labels[i],
                new Vector2(startX + i * 115, -40), () => GameManager.Instance.OnPromotionSelected(t), 100);
        }

        promotionPanel.SetActive(false);
    }

    private void CreateRestartButton(GameObject parent)
    {
        GameObject btnArea = new GameObject("RestartArea");
        btnArea.transform.SetParent(parent.transform, false);
        var areaRect = btnArea.AddComponent<RectTransform>();
        areaRect.anchorMin = new Vector2(0.5f, 0);
        areaRect.anchorMax = new Vector2(0.5f, 0);
        areaRect.pivot = new Vector2(0.5f, 0);
        areaRect.anchoredPosition = new Vector2(0, 20);
        areaRect.sizeDelta = new Vector2(200, 50);

        CreateButton(btnArea, "RestartBtn", "Restart", Vector2.zero, () => GameManager.Instance.RestartGame(), 180);
    }

    private void CreateBoardLabels(GameObject parent)
    {
        // We'll create world-space labels instead for the board
        string files = "abcdefgh";
        for (int i = 0; i < 8; i++)
        {
            // File labels (bottom)
            CreateWorldLabel(files[i].ToString(), new Vector3(i, -0.65f, 0), 4f);
            // Rank labels (left)
            CreateWorldLabel((i + 1).ToString(), new Vector3(-0.65f, i, 0), 4f);
        }
    }

    private void CreateMainMenuPanel(GameObject parent)
    {
        mainMenuPanel = new GameObject("MainMenuPanel");
        mainMenuPanel.transform.SetParent(parent.transform, false);

        var panelImg = mainMenuPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("MenuTitle");
        titleObj.transform.SetParent(mainMenuPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Chess Universe";
        titleText.fontSize = 56;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 80);
        titleRect.anchoredPosition = new Vector2(0, 120);

        // Player name subtitle
        GameObject nameSubObj = new GameObject("PlayerNameSub");
        nameSubObj.transform.SetParent(mainMenuPanel.transform, false);
        var nameSub = nameSubObj.AddComponent<TextMeshProUGUI>();
        nameSub.fontSize = 22;
        nameSub.alignment = TextAlignmentOptions.Center;
        nameSub.color = new Color(0.6f, 0.75f, 0.6f);
        var nameSubRect = nameSub.rectTransform;
        nameSubRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameSubRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameSubRect.sizeDelta = new Vector2(500, 30);
        nameSubRect.anchoredPosition = new Vector2(0, 78);
        // Name is populated dynamically in ShowMainMenu via UpdatePlayerNameDisplay
        nameSub.text = $"Playing as: {PlayerPrefs.GetString("PlayerName", "")}";

        // Classic Chess button
        CreateButton(mainMenuPanel, "ClassicBtn", "Classic Chess", new Vector2(0, 20),
            () => { HideMainMenu(); ShowPlayerModePanel(GameMode.Classic); }, 300);

        // Seed Chess button + info
        CreateButton(mainMenuPanel, "SeedBtn", "Seed Chess", new Vector2(0, -50),
            () => { HideMainMenu(); ShowPlayerModePanel(GameMode.SeedChess); }, 300);
        CreateInfoIcon(mainMenuPanel, new Vector2(180, -50), () => ShowRulesDialog(GameMode.SeedChess));

        // Bluffy Chess button + info
        CreateButton(mainMenuPanel, "BluffyBtn", "Bluffy Chess", new Vector2(0, -120),
            () => { HideMainMenu(); ShowPlayerModePanel(GameMode.BluffyChess); }, 300);
        CreateInfoIcon(mainMenuPanel, new Vector2(180, -120), () => ShowRulesDialog(GameMode.BluffyChess));

        // Change name button (small, bottom-right area)
        var changeBtn = CreateButton(mainMenuPanel, "ChangeNameBtn", "Change Name", new Vector2(0, -200),
            () => { HideMainMenu(); ShowProfilePanel(); }, 180);
        changeBtn.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);

        // Admin gear button (bottom-left, subtle)
        GameObject adminBtnObj = new GameObject("AdminBtn");
        adminBtnObj.transform.SetParent(mainMenuPanel.transform, false);
        var adminBtnImg = adminBtnObj.AddComponent<Image>();
        adminBtnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        var adminBtnRect = adminBtnImg.rectTransform;
        adminBtnRect.anchorMin = new Vector2(0, 0);
        adminBtnRect.anchorMax = new Vector2(0, 0);
        adminBtnRect.pivot = new Vector2(0, 0);
        adminBtnRect.anchoredPosition = new Vector2(20, 20);
        adminBtnRect.sizeDelta = new Vector2(44, 44);
        var adminBtn = adminBtnObj.AddComponent<Button>();
        adminBtn.targetGraphic = adminBtnImg;
        adminBtn.onClick.AddListener(() => ShowAdminPasswordPanel());
        GameObject adminIconObj = new GameObject("AdminIcon");
        adminIconObj.transform.SetParent(adminBtnObj.transform, false);
        var adminIconText = adminIconObj.AddComponent<TextMeshProUGUI>();
        adminIconText.text = "⚙";
        adminIconText.fontSize = 22;
        adminIconText.alignment = TextAlignmentOptions.Center;
        adminIconText.color = new Color(0.6f, 0.6f, 0.6f);
        var adminIconRect = adminIconText.rectTransform;
        adminIconRect.anchorMin = Vector2.zero;
        adminIconRect.anchorMax = Vector2.one;
        adminIconRect.offsetMin = Vector2.zero;
        adminIconRect.offsetMax = Vector2.zero;

        mainMenuPanel.SetActive(false);
    }

    private void CreatePlayerModePanel(GameObject parent)
    {
        playerModePanel = new GameObject("PlayerModePanel");
        playerModePanel.transform.SetParent(parent.transform, false);

        var panelImg = playerModePanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("ModeTitle");
        titleObj.transform.SetParent(playerModePanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Select Mode";
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 70);
        titleRect.anchoredPosition = new Vector2(0, 100);

        // Single Player button
        var spBtnObj = CreateButton(playerModePanel, "SinglePlayerBtn", "Single Player", new Vector2(0, 10),
            () => {
                HidePlayerModePanel();
                GameManager.Instance.StartGame(pendingGameMode, PlayMode.SinglePlayer);
            }, 300);
        singlePlayerButton = spBtnObj.GetComponent<Button>();

        // Two Players button
        CreateButton(playerModePanel, "TwoPlayersBtn", "Two Players", new Vector2(0, -60),
            () => {
                HidePlayerModePanel();
                GameManager.Instance.StartGame(pendingGameMode, PlayMode.Local);
            }, 300);

        // Online button
        CreateButton(playerModePanel, "OnlineBtn", "Online", new Vector2(0, -130),
            () => {
                HidePlayerModePanel();
                ShowLobbyPanel();
            }, 300);

        // Back button
        CreateButton(playerModePanel, "BackBtn", "Back", new Vector2(0, -200),
            () => {
                HidePlayerModePanel();
                ShowMainMenu();
            }, 200);

        playerModePanel.SetActive(false);
    }

    public void ShowPlayerModePanel(GameMode mode)
    {
        pendingGameMode = mode;
        // Single player available for all modes
        singlePlayerButton.interactable = true;
        playerModePanel.SetActive(true);
    }

    public void HidePlayerModePanel()
    {
        playerModePanel.SetActive(false);
    }

    private void CreateSeedButtonsPanel(GameObject parent)
    {
        seedButtonsPanel = new GameObject("SeedButtonsPanel");
        seedButtonsPanel.transform.SetParent(parent.transform, false);

        var panelImg = seedButtonsPanel.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.13f, 0.08f, 0.85f);

        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-10, 0);
        panelRect.sizeDelta = new Vector2(130, 310);

        // Title
        GameObject titleObj = new GameObject("SeedTitle");
        titleObj.transform.SetParent(seedButtonsPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Plant";
        titleText.fontSize = 22;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.2f);
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(120, 30);
        titleRect.anchoredPosition = new Vector2(0, 130);

        // Piece buttons stacked vertically
        string[] labels = { "Pawn (1)", "Knight (3)", "Bishop (3)", "Rook (5)", "Queen (9)" };
        PieceType[] types = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };

        for (int i = 0; i < 5; i++)
        {
            PieceType t = types[i];
            float yPos = 80 - i * 55;
            CreateSeedButton(seedButtonsPanel, $"Seed_{types[i]}", labels[i],
                new Vector2(0, yPos), () => GameManager.Instance.OnSeedButtonClick(t));
        }

        seedButtonsPanel.SetActive(false);
    }

    private void CreateSeedButton(GameObject parent, string name, string label, Vector2 position,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.7f, 0.6f, 0.15f);

        var btnRect = btnImg.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(115, 45);
        btnRect.anchoredPosition = position;

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = btnImg;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.85f, 0.75f, 0.2f);
        colors.pressedColor = new Color(0.5f, 0.4f, 0.1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 19;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.fontStyle = FontStyles.Bold;
        var textRect = btnText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateWorldLabel(string text, Vector3 position, float fontSize)
    {
        GameObject obj = new GameObject($"Label_{text}");
        obj.transform.position = position;

        var tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.7f, 0.7f, 0.7f);
        tmp.rectTransform.sizeDelta = new Vector2(1, 1);
        tmp.sortingOrder = 5;
    }

    private GameObject CreateButton(GameObject parent, string name, string label, Vector2 position,
        UnityEngine.Events.UnityAction onClick, float width = 200)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.55f, 0.3f);

        var btnRect = btnImg.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(width, 45);
        btnRect.anchoredPosition = position;

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = btnImg;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.7f, 0.4f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.2f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 22;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        var textRect = btnText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    public void UpdateTurnText(PieceColor color, bool inCheck = false)
    {
        string colorName = color == PieceColor.White ? "White" : "Black";
        string modePrefix = GameBootstrap.CurrentMode == GameMode.SeedChess ? "[Seed] " : "";
        turnText.text = inCheck ? $"{modePrefix}{colorName}'s Turn - CHECK!" : $"{modePrefix}{colorName}'s Turn";
        turnText.color = inCheck ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }

    public void ShowGameOver(PieceColor winner)
    {
        string winnerName = winner == PieceColor.White ? "White" : "Black";
        gameOverText.text = $"Checkmate!\n{winnerName} Wins!";
        gameOverPanel.SetActive(true);
    }

    public void ShowStalemate()
    {
        gameOverText.text = "Stalemate!\nDraw!";
        gameOverPanel.SetActive(true);
    }

    public void HideGameOver()
    {
        gameOverPanel.SetActive(false);
    }

    public void ShowPromotionPanel(PieceColor color)
    {
        promotionPanel.SetActive(true);
    }

    public void HidePromotionPanel()
    {
        promotionPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        // Always show the main menu first so the board is never a blank screen
        var nameSub = mainMenuPanel.transform.Find("PlayerNameSub")?.GetComponent<TextMeshProUGUI>();
        if (nameSub != null)
            nameSub.text = $"Playing as: {PlayerPrefs.GetString("PlayerName", "")}";
        mainMenuPanel.SetActive(true);

        // Show profile name-entry panel on top if no name has been set yet
        if (!PlayerPrefs.HasKey("PlayerName"))
            ShowProfilePanel();
    }

    public void HideMainMenu()
    {
        mainMenuPanel.SetActive(false);
    }

    public void ShowSeedButtons()
    {
        seedButtonsPanel.SetActive(true);
    }

    public void HideSeedButtons()
    {
        seedButtonsPanel.SetActive(false);
    }

    // ─── Rules Dialog ───

    private void CreateInfoIcon(GameObject parent, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("InfoIcon");
        btnObj.transform.SetParent(parent.transform, false);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.35f, 0.55f, 0.8f);

        var btnRect = btnImg.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(40, 40);
        btnRect.anchoredPosition = position;

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = btnImg;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.45f, 0.65f, 0.9f);
        colors.pressedColor = new Color(0.25f, 0.4f, 0.6f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "?";
        btnText.fontSize = 26;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.fontStyle = FontStyles.Bold;
        var textRect = btnText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void CreateRulesPanel(GameObject parent)
    {
        rulesPanel = new GameObject("RulesPanel");
        rulesPanel.transform.SetParent(parent.transform, false);

        var panelImg = rulesPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.14f, 0.97f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700, 560);

        // Title
        GameObject titleObj = new GameObject("RulesTitle");
        titleObj.transform.SetParent(rulesPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 32;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.4f, 0.8f, 1f);
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -15);
        titleRect.sizeDelta = new Vector2(0, 45);

        // Body text
        GameObject textObj = new GameObject("RulesBody");
        textObj.transform.SetParent(rulesPanel.transform, false);
        rulesText = textObj.AddComponent<TextMeshProUGUI>();
        rulesText.fontSize = 20;
        rulesText.alignment = TextAlignmentOptions.TopLeft;
        rulesText.color = new Color(0.9f, 0.9f, 0.9f);
        rulesText.lineSpacing = 4;
        var textRect = rulesText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(30, 70);
        textRect.offsetMax = new Vector2(-30, -65);

        // Close button
        CreateButton(rulesPanel, "CloseRulesBtn", "Close", new Vector2(0, -235),
            () => rulesPanel.SetActive(false), 160);

        rulesPanel.SetActive(false);
    }

    private void ShowRulesDialog(GameMode mode)
    {
        var titleText = rulesPanel.transform.Find("RulesTitle").GetComponent<TextMeshProUGUI>();

        if (mode == GameMode.SeedChess)
        {
            titleText.text = "Seed Chess Rules";
            rulesText.text =
                "All standard chess rules apply, plus:\n\n" +
                "<b>Planting:</b> The King can plant seeds on adjacent\n" +
                "empty squares instead of moving.\n\n" +
                "<b>Growth:</b> Seeds grow into pieces after a set\n" +
                "number of your turns:\n" +
                "  Pawn = 1, Knight = 3, Bishop = 3, Rook = 5, Queen = 9\n\n" +
                "<b>Passable:</b> Seeds don't block movement.\n" +
                "Any piece can move through or land on them.\n\n" +
                "<b>Restriction:</b> You cannot plant while in check.";
        }
        else if (mode == GameMode.BluffyChess)
        {
            titleText.text = "Bluffy Chess Rules";
            rulesText.text =
                "<b>Setup:</b> Each player secretly arranges their\n" +
                "back rank. Opponents see colored masks instead.\n\n" +
                "<b>Movement:</b> All non-pawn pieces can move like\n" +
                "any piece (Queen + Knight combined).\n\n" +
                "<b>Bluff Call:</b> After a non-pawn move, the opponent\n" +
                "can call \"Bluff!\" to challenge the move.\n\n" +
                "<b>Caught Bluffing:</b> If the move was illegal for the\n" +
                "real piece type, that piece is removed.\n\n" +
                "<b>Wrong Call:</b> If the move was legal, the caller\n" +
                "must sacrifice one of their non-pawn pieces.\n" +
                "The mover may then swap their piece with another.\n\n" +
                "<b>Win Condition:</b> Capture the opponent's King.";
        }

        rulesPanel.SetActive(true);
    }

    // ─── Splash Text ───

    private void CreateSplashPanel(GameObject parent)
    {
        splashPanel = new GameObject("SplashPanel");
        splashPanel.transform.SetParent(parent.transform, false);

        splashCanvasGroup = splashPanel.AddComponent<CanvasGroup>();
        splashCanvasGroup.alpha = 0f;
        splashCanvasGroup.blocksRaycasts = false;
        splashCanvasGroup.interactable = false;

        var panelRect = splashPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dramatic text
        GameObject textObj = new GameObject("SplashText");
        textObj.transform.SetParent(splashPanel.transform, false);
        splashText = textObj.AddComponent<TextMeshProUGUI>();
        splashText.fontSize = 100;
        splashText.alignment = TextAlignmentOptions.Center;
        splashText.color = new Color(1f, 0.15f, 0.15f);
        splashText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        splashText.enableWordWrapping = false;
        splashText.outlineWidth = 0.3f;
        splashText.outlineColor = new Color32(0, 0, 0, 200);
        var textRect = splashText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(900, 150);
        textRect.anchoredPosition = Vector2.zero;

        splashPanel.SetActive(false);
    }

    private static readonly Color SplashRed = new Color(1f, 0.15f, 0.15f);

    public void ShowSplashText(string text, float duration, System.Action onComplete = null, Color? color = null)
    {
        StartCoroutine(SplashTextCoroutine(text, duration, onComplete, color ?? SplashRed));
    }

    private System.Collections.IEnumerator SplashTextCoroutine(string text, float duration, System.Action onComplete, Color color)
    {
        splashText.text = text;
        splashText.color = color;
        splashPanel.SetActive(true);

        // Scale-up punch entrance
        float fadeIn = 0.15f;
        float startScale = 1.8f;
        float elapsed = 0f;

        splashText.transform.localScale = Vector3.one * startScale;
        splashCanvasGroup.alpha = 0f;

        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeIn;
            splashCanvasGroup.alpha = t;
            float s = Mathf.Lerp(startScale, 1f, t * t); // ease-out
            splashText.transform.localScale = Vector3.one * s;
            yield return null;
        }
        splashCanvasGroup.alpha = 1f;
        splashText.transform.localScale = Vector3.one;

        // Hold
        yield return new WaitForSeconds(duration);

        // Fade out
        float fadeOut = 0.25f;
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            splashCanvasGroup.alpha = 1f - (elapsed / fadeOut);
            yield return null;
        }

        splashCanvasGroup.alpha = 0f;
        splashPanel.SetActive(false);

        onComplete?.Invoke();
    }

    // ─── Online Panels ───

    private void CreateLobbyPanel(GameObject parent)
    {
        lobbyPanel = new GameObject("LobbyPanel");
        lobbyPanel.transform.SetParent(parent.transform, false);

        var panelImg = lobbyPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("LobbyTitle");
        titleObj.transform.SetParent(lobbyPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Online";
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 70);
        titleRect.anchoredPosition = new Vector2(0, 100);

        CreateButton(lobbyPanel, "HostGameBtn", "Host Game", new Vector2(0, 10),
            () => {
                HideLobbyPanel();
                OnHostGameClicked();
            }, 300);

        CreateButton(lobbyPanel, "JoinGameBtn", "Join Game", new Vector2(0, -60),
            () => {
                HideLobbyPanel();
                ShowJoinPanel();
            }, 300);

        CreateButton(lobbyPanel, "LobbyBackBtn", "Back", new Vector2(0, -130),
            () => {
                HideLobbyPanel();
                ShowPlayerModePanel(pendingGameMode);
            }, 200);

        lobbyPanel.SetActive(false);
    }

    private void CreateHostWaitPanel(GameObject parent)
    {
        hostWaitPanel = new GameObject("HostWaitPanel");
        hostWaitPanel.transform.SetParent(parent.transform, false);

        var panelImg = hostWaitPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Room code display
        GameObject codeObj = new GameObject("RoomCodeText");
        codeObj.transform.SetParent(hostWaitPanel.transform, false);
        hostWaitCodeText = codeObj.AddComponent<TextMeshProUGUI>();
        hostWaitCodeText.fontSize = 56;
        hostWaitCodeText.alignment = TextAlignmentOptions.Center;
        hostWaitCodeText.color = new Color(0.4f, 0.8f, 1f);
        hostWaitCodeText.fontStyle = FontStyles.Bold;
        var codeRect = hostWaitCodeText.rectTransform;
        codeRect.anchorMin = new Vector2(0.5f, 0.5f);
        codeRect.anchorMax = new Vector2(0.5f, 0.5f);
        codeRect.sizeDelta = new Vector2(600, 80);
        codeRect.anchoredPosition = new Vector2(0, 40);

        // Waiting text
        GameObject waitObj = new GameObject("WaitText");
        waitObj.transform.SetParent(hostWaitPanel.transform, false);
        var waitText = waitObj.AddComponent<TextMeshProUGUI>();
        waitText.text = "Waiting for opponent...";
        waitText.fontSize = 28;
        waitText.alignment = TextAlignmentOptions.Center;
        waitText.color = Color.white;
        var waitRect = waitText.rectTransform;
        waitRect.anchorMin = new Vector2(0.5f, 0.5f);
        waitRect.anchorMax = new Vector2(0.5f, 0.5f);
        waitRect.sizeDelta = new Vector2(600, 50);
        waitRect.anchoredPosition = new Vector2(0, -20);

        CreateButton(hostWaitPanel, "CancelHostBtn", "Cancel", new Vector2(0, -90),
            () => {
                NetworkManager.Instance.LeaveRoom();
                HideHostWaitPanel();
                ShowLobbyPanel();
            }, 200);

        hostWaitPanel.SetActive(false);
    }

    private void CreateJoinPanel(GameObject parent)
    {
        joinPanel = new GameObject("JoinPanel");
        joinPanel.transform.SetParent(parent.transform, false);

        var panelImg = joinPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("JoinTitle");
        titleObj.transform.SetParent(joinPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Enter Room Code";
        titleText.fontSize = 36;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 50);
        titleRect.anchoredPosition = new Vector2(0, 80);

        // Input field
        GameObject inputObj = new GameObject("CodeInput");
        inputObj.transform.SetParent(joinPanel.transform, false);
        var inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(0.2f, 0.2f, 0.25f);
        var inputRect = inputImg.rectTransform;
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(300, 50);
        inputRect.anchoredPosition = new Vector2(0, 20);

        // Text area for input
        GameObject textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        var textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textAreaObj.transform, false);
        var inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 32;
        inputText.alignment = TextAlignmentOptions.Center;
        inputText.color = Color.white;
        var inputTextRect = inputText.rectTransform;
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textAreaObj.transform, false);
        var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "ABCDEF";
        placeholder.fontSize = 32;
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholder.fontStyle = FontStyles.Italic;
        var phRect = placeholder.rectTransform;
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        joinCodeInput = inputObj.AddComponent<TMP_InputField>();
        joinCodeInput.textComponent = inputText;
        joinCodeInput.placeholder = placeholder;
        joinCodeInput.characterLimit = 6;
        joinCodeInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        joinCodeInput.textViewport = textAreaRect;

        // Error text
        GameObject errorObj = new GameObject("JoinError");
        errorObj.transform.SetParent(joinPanel.transform, false);
        joinErrorText = errorObj.AddComponent<TextMeshProUGUI>();
        joinErrorText.fontSize = 22;
        joinErrorText.alignment = TextAlignmentOptions.Center;
        joinErrorText.color = new Color(1f, 0.4f, 0.4f);
        var errorRect = joinErrorText.rectTransform;
        errorRect.anchorMin = new Vector2(0.5f, 0.5f);
        errorRect.anchorMax = new Vector2(0.5f, 0.5f);
        errorRect.sizeDelta = new Vector2(400, 30);
        errorRect.anchoredPosition = new Vector2(0, -20);

        CreateButton(joinPanel, "JoinBtn", "Join", new Vector2(-70, -70),
            () => OnJoinClicked(), 140);

        CreateButton(joinPanel, "JoinBackBtn", "Back", new Vector2(70, -70),
            () => {
                HideJoinPanel();
                ShowLobbyPanel();
            }, 140);

        joinPanel.SetActive(false);
    }

    private void CreateConnectionStatus(GameObject parent)
    {
        connectionStatusObj = new GameObject("ConnectionStatus");
        connectionStatusObj.transform.SetParent(parent.transform, false);

        connectionStatusText = connectionStatusObj.AddComponent<TextMeshProUGUI>();
        connectionStatusText.fontSize = 20;
        connectionStatusText.alignment = TextAlignmentOptions.Right;
        connectionStatusText.color = new Color(1f, 0.8f, 0.2f);
        var rect = connectionStatusText.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-10, -10);
        rect.sizeDelta = new Vector2(250, 30);

        connectionStatusObj.SetActive(false);
    }

    // Online panel actions

    private void OnHostGameClicked()
    {
        NetworkManager.Instance.OnOpponentJoined += OnOpponentJoinedHandler;
        NetworkManager.Instance.CreateRoom(pendingGameMode,
            (code) => {
                hostWaitCodeText.text = $"Room: {code}";
                ShowHostWaitPanel();
            },
            (error) => {
                ShowInfoPanel(error);
                ShowLobbyPanel();
            });
    }

    private void OnOpponentJoinedHandler()
    {
        NetworkManager.Instance.OnOpponentJoined -= OnOpponentJoinedHandler;
        HideHostWaitPanel();
        GameManager.Instance.StartGame(pendingGameMode, PlayMode.Online);
    }

    private void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (code.Length != 6)
        {
            joinErrorText.text = "Code must be 6 characters";
            return;
        }

        joinErrorText.text = "";
        NetworkManager.Instance.JoinRoom(code,
            (mode) => {
                HideJoinPanel();
                pendingGameMode = mode;
                GameManager.Instance.StartGame(mode, PlayMode.Online);
            },
            (error) => {
                joinErrorText.text = error;
            });
    }

    // Online panel show/hide

    public void ShowLobbyPanel() { lobbyPanel.SetActive(true); }
    public void HideLobbyPanel() { lobbyPanel.SetActive(false); }
    public void ShowHostWaitPanel() { hostWaitPanel.SetActive(true); }
    public void HideHostWaitPanel() { hostWaitPanel.SetActive(false); }
    public void ShowJoinPanel()
    {
        joinCodeInput.text = "";
        joinErrorText.text = "";
        joinPanel.SetActive(true);
    }
    public void HideJoinPanel() { joinPanel.SetActive(false); }

    public void ShowConnectionStatus(string status)
    {
        connectionStatusText.text = status;
        connectionStatusObj.SetActive(true);
    }

    public void HideConnectionStatus()
    {
        connectionStatusObj.SetActive(false);
    }

    public void HideOnlinePanels()
    {
        HideLobbyPanel();
        HideHostWaitPanel();
        HideJoinPanel();
        HideConnectionStatus();
    }

    // ─── Bluffy Panels ───

    private void CreateSetupPanel(GameObject parent)
    {
        setupPanel = new GameObject("SetupPanel");
        setupPanel.transform.SetParent(parent.transform, false);

        var panelImg = setupPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -90);
        panelRect.sizeDelta = new Vector2(500, 120);

        GameObject textObj = new GameObject("SetupText");
        textObj.transform.SetParent(setupPanel.transform, false);
        setupText = textObj.AddComponent<TextMeshProUGUI>();
        setupText.fontSize = 24;
        setupText.alignment = TextAlignmentOptions.Center;
        setupText.color = Color.white;
        var textRect = setupText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, -5);

        CreateButton(setupPanel, "ConfirmSetupBtn", "Confirm", new Vector2(0, -35),
            () => BluffyManager.Instance.ConfirmSetup(), 180);

        setupPanel.SetActive(false);
    }

    private void CreatePassDevicePanel(GameObject parent)
    {
        passDevicePanel = new GameObject("PassDevicePanel");
        passDevicePanel.transform.SetParent(parent.transform, false);

        var panelImg = passDevicePanel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("PassText");
        textObj.transform.SetParent(passDevicePanel.transform, false);
        passDeviceText = textObj.AddComponent<TextMeshProUGUI>();
        passDeviceText.fontSize = 42;
        passDeviceText.alignment = TextAlignmentOptions.Center;
        passDeviceText.color = Color.white;
        passDeviceText.fontStyle = FontStyles.Bold;
        var textRect = passDeviceText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(700, 120);
        textRect.anchoredPosition = new Vector2(0, 50);

        CreateButton(passDevicePanel, "ReadyBtn", "Ready", new Vector2(0, -60),
            () => BluffyManager.Instance.OnPassDeviceReady(), 250);

        passDevicePanel.SetActive(false);
    }

    private void CreateBluffPanel(GameObject parent)
    {
        bluffPanel = new GameObject("BluffPanel");
        bluffPanel.transform.SetParent(parent.transform, false);

        var panelImg = bluffPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0, 80);
        panelRect.sizeDelta = new Vector2(400, 120);

        GameObject textObj = new GameObject("BluffText");
        textObj.transform.SetParent(bluffPanel.transform, false);
        var bluffText = textObj.AddComponent<TextMeshProUGUI>();
        bluffText.text = "Opponent moved a big piece!";
        bluffText.fontSize = 22;
        bluffText.alignment = TextAlignmentOptions.Center;
        bluffText.color = Color.white;
        var textRect = bluffText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0.55f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, -5);

        // Bluff button (red)
        var bluffBtn = CreateButton(bluffPanel, "CallBluffBtn", "Bluff!", new Vector2(-80, -25),
            () => GameManager.Instance.OnBluffCalled(), 140);
        bluffBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

        // Accept button (green)
        CreateButton(bluffPanel, "AcceptBtn", "Accept", new Vector2(80, -25),
            () => GameManager.Instance.OnMoveAccepted(), 140);

        bluffPanel.SetActive(false);
    }

    private void CreateSacrificePanel(GameObject parent)
    {
        sacrificePanel = new GameObject("SacrificePanel");
        sacrificePanel.transform.SetParent(parent.transform, false);

        var panelImg = sacrificePanel.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.08f, 0.08f, 0.9f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -90);
        panelRect.sizeDelta = new Vector2(500, 70);

        GameObject textObj = new GameObject("SacrificeText");
        textObj.transform.SetParent(sacrificePanel.transform, false);
        sacrificeText = textObj.AddComponent<TextMeshProUGUI>();
        sacrificeText.fontSize = 24;
        sacrificeText.alignment = TextAlignmentOptions.Center;
        sacrificeText.color = new Color(1f, 0.4f, 0.4f);
        sacrificeText.fontStyle = FontStyles.Bold;
        var textRect = sacrificeText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        sacrificePanel.SetActive(false);
    }

    private void CreateRearrangePanel(GameObject parent)
    {
        rearrangePanel = new GameObject("RearrangePanel");
        rearrangePanel.transform.SetParent(parent.transform, false);

        var panelImg = rearrangePanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -90);
        panelRect.sizeDelta = new Vector2(500, 120);

        GameObject textObj = new GameObject("RearrangeText");
        textObj.transform.SetParent(rearrangePanel.transform, false);
        rearrangeText = textObj.AddComponent<TextMeshProUGUI>();
        rearrangeText.fontSize = 24;
        rearrangeText.alignment = TextAlignmentOptions.Center;
        rearrangeText.color = new Color(0.5f, 0.8f, 1f);
        rearrangeText.fontStyle = FontStyles.Bold;
        var textRect = rearrangeText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, -5);

        CreateButton(rearrangePanel, "FinishRearrangeBtn", "Done", new Vector2(0, -35),
            () => BluffyManager.Instance.FinishRearrange(), 180);

        rearrangePanel.SetActive(false);
    }

    // Bluffy panel show/hide methods

    public void ShowSetupPanel(PieceColor color)
    {
        string colorName = color == PieceColor.White ? "White" : "Black";
        setupText.text = $"{colorName}: Arrange your back rank\nClick two big pieces to swap them";
        setupPanel.SetActive(true);
    }

    public void HideSetupPanel()
    {
        setupPanel.SetActive(false);
    }

    public void ShowPassDevicePanel(PieceColor targetColor)
    {
        string colorName = targetColor == PieceColor.White ? "White" : "Black";
        passDeviceText.text = $"Pass the device to {colorName}";
        passDevicePanel.SetActive(true);
    }

    public void HidePassDevicePanel()
    {
        passDevicePanel.SetActive(false);
    }

    public void ShowBluffPanel()
    {
        bluffPanel.SetActive(true);
    }

    public void HideBluffPanel()
    {
        bluffPanel.SetActive(false);
    }

    public void ShowSacrificePanel(PieceColor color)
    {
        string colorName = color == PieceColor.White ? "White" : "Black";
        sacrificeText.text = $"{colorName}: Select a big piece to sacrifice";
        sacrificePanel.SetActive(true);
    }

    public void HideSacrificePanel()
    {
        sacrificePanel.SetActive(false);
    }

    public void ShowRearrangePanel(PieceColor color)
    {
        string colorName = color == PieceColor.White ? "White" : "Black";
        rearrangeText.text = $"{colorName}: Swap your moved piece\nwith another big piece, or Done";
        rearrangePanel.SetActive(true);
    }

    public void HideRearrangePanel()
    {
        rearrangePanel.SetActive(false);
    }

    public void UpdateBluffyTurnText(PieceColor color)
    {
        string colorName = color == PieceColor.White ? "White" : "Black";
        turnText.text = $"[Bluffy] {colorName}'s Turn";
        turnText.color = Color.white;
    }

    public void ShowBluffyGameOver(PieceColor winner)
    {
        string winnerName = winner == PieceColor.White ? "White" : "Black";
        gameOverText.text = $"King Captured!\n{winnerName} Wins!";
        gameOverPanel.SetActive(true);
    }

    // ─── Info Panel ───

    private void CreateInfoPanel(GameObject parent)
    {
        infoPanel = new GameObject("InfoPanel");
        infoPanel.transform.SetParent(parent.transform, false);

        var panelImg = infoPanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(550, 200);

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(infoPanel.transform, false);
        infoText = textObj.AddComponent<TextMeshProUGUI>();
        infoText.fontSize = 28;
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.color = Color.white;
        infoText.fontStyle = FontStyles.Bold;
        var textRect = infoText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0.4f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(-20, -15);

        CreateButton(infoPanel, "InfoOkBtn", "OK", new Vector2(0, -60),
            () => {
                infoPanel.SetActive(false);
                infoDismissAction?.Invoke();
                infoDismissAction = null;
            }, 160);

        infoPanel.SetActive(false);
    }

    public void ShowInfoPanel(string message, System.Action onDismiss = null)
    {
        infoText.text = message;
        infoDismissAction = onDismiss;
        infoPanel.SetActive(true);
    }

    public void HideInfoPanel()
    {
        infoPanel.SetActive(false);
        infoDismissAction = null;
    }

    public void HideBluffyPanels()
    {
        HideSetupPanel();
        HidePassDevicePanel();
        HideBluffPanel();
        HideSacrificePanel();
        HideRearrangePanel();
        HideInfoPanel();
        HideOnlinePanels();
    }

    // ─── Player Profile Panel ───

    private void CreateProfilePanel(GameObject parent)
    {
        profilePanel = new GameObject("ProfilePanel");
        profilePanel.transform.SetParent(parent.transform, false);

        var panelImg = profilePanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new GameObject("ProfileTitle");
        titleObj.transform.SetParent(profilePanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Chess Universe";
        titleText.fontSize = 52;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 70);
        titleRect.anchoredPosition = new Vector2(0, 130);

        // Subtitle
        GameObject subObj = new GameObject("ProfileSub");
        subObj.transform.SetParent(profilePanel.transform, false);
        var subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = "What's your name?";
        subText.fontSize = 28;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = new Color(0.75f, 0.85f, 0.75f);
        var subRect = subText.rectTransform;
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.sizeDelta = new Vector2(500, 40);
        subRect.anchoredPosition = new Vector2(0, 60);

        // Input field
        GameObject inputObj = new GameObject("NameInput");
        inputObj.transform.SetParent(profilePanel.transform, false);
        var inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(0.18f, 0.18f, 0.24f);
        var inputRect = inputImg.rectTransform;
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(360, 56);
        inputRect.anchoredPosition = new Vector2(0, -10);

        GameObject textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        var textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(12, 6);
        textAreaRect.offsetMax = new Vector2(-12, -6);

        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textAreaObj.transform, false);
        var inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 30;
        inputText.alignment = TextAlignmentOptions.Center;
        inputText.color = Color.white;
        var inputTextRect = inputText.rectTransform;
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;

        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(textAreaObj.transform, false);
        var phText = phObj.AddComponent<TextMeshProUGUI>();
        phText.text = "Enter your name...";
        phText.fontSize = 26;
        phText.alignment = TextAlignmentOptions.Center;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        phText.fontStyle = FontStyles.Italic;
        var phRect = phText.rectTransform;
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        profileNameInput = inputObj.AddComponent<TMP_InputField>();
        profileNameInput.textComponent = inputText;
        profileNameInput.placeholder = phText;
        profileNameInput.characterLimit = 20;
        profileNameInput.textViewport = textAreaRect;
        profileNameInput.onSubmit.AddListener((_) => OnProfileConfirm());

        // Pre-fill existing name if changing
        profileNameInput.text = PlayerPrefs.GetString("PlayerName", "");

        // Confirm button
        var confirmBtn = CreateButton(profilePanel, "ProfileConfirmBtn", "Let's Play!", new Vector2(0, -85),
            () => OnProfileConfirm(), 220);
        confirmBtn.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.25f);

        profilePanel.SetActive(false);
    }

    private void ShowProfilePanel()
    {
        profileNameInput.text = PlayerPrefs.GetString("PlayerName", "");
        profilePanel.SetActive(true);
        // Do NOT call ActivateInputField here — on WebGL it can trigger the
        // mobile keyboard or cause a JS exception before the user interacts.
    }

    private void OnProfileConfirm()
    {
        string name = profileNameInput.text.Trim();
        if (name.Length < 1)
        {
            profileNameInput.text = "";
            return;
        }
        if (name.Length > 20) name = name.Substring(0, 20);
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
        GameLogger.Instance?.RegisterPlayer(name);
        profilePanel.SetActive(false);
        ShowMainMenu();
    }

    // ─── Admin Password Panel ───

    private void CreateAdminPasswordPanel(GameObject parent)
    {
        adminPasswordPanel = new GameObject("AdminPasswordPanel");
        adminPasswordPanel.transform.SetParent(parent.transform, false);

        var panelImg = adminPasswordPanel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.1f, 0.97f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(480, 280);

        // Title
        GameObject titleObj = new GameObject("AdminTitle");
        titleObj.transform.SetParent(adminPasswordPanel.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Admin Access";
        titleText.fontSize = 30;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.9f, 0.7f, 0.2f);
        titleText.fontStyle = FontStyles.Bold;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(420, 44);
        titleRect.anchoredPosition = new Vector2(0, 90);

        // Password input
        GameObject inputObj = new GameObject("PasswordInput");
        inputObj.transform.SetParent(adminPasswordPanel.transform, false);
        var inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(0.15f, 0.15f, 0.2f);
        var inputRect = inputImg.rectTransform;
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(340, 50);
        inputRect.anchoredPosition = new Vector2(0, 20);

        GameObject taObj = new GameObject("Text Area");
        taObj.transform.SetParent(inputObj.transform, false);
        var taRect = taObj.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 5);
        taRect.offsetMax = new Vector2(-10, -5);

        GameObject pwTextObj = new GameObject("Text");
        pwTextObj.transform.SetParent(taObj.transform, false);
        var pwText = pwTextObj.AddComponent<TextMeshProUGUI>();
        pwText.fontSize = 28;
        pwText.alignment = TextAlignmentOptions.Center;
        pwText.color = Color.white;
        var pwTextRect = pwText.rectTransform;
        pwTextRect.anchorMin = Vector2.zero;
        pwTextRect.anchorMax = Vector2.one;
        pwTextRect.offsetMin = Vector2.zero;
        pwTextRect.offsetMax = Vector2.zero;

        GameObject pwPhObj = new GameObject("Placeholder");
        pwPhObj.transform.SetParent(taObj.transform, false);
        var pwPh = pwPhObj.AddComponent<TextMeshProUGUI>();
        pwPh.text = "Password";
        pwPh.fontSize = 24;
        pwPh.alignment = TextAlignmentOptions.Center;
        pwPh.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
        pwPh.fontStyle = FontStyles.Italic;
        var pwPhRect = pwPh.rectTransform;
        pwPhRect.anchorMin = Vector2.zero;
        pwPhRect.anchorMax = Vector2.one;
        pwPhRect.offsetMin = Vector2.zero;
        pwPhRect.offsetMax = Vector2.zero;

        adminPasswordInput = inputObj.AddComponent<TMP_InputField>();
        adminPasswordInput.textComponent = pwText;
        adminPasswordInput.placeholder = pwPh;
        adminPasswordInput.contentType = TMP_InputField.ContentType.Password;
        adminPasswordInput.textViewport = taRect;
        adminPasswordInput.onSubmit.AddListener((_) => OnAdminPasswordSubmit());

        // Error text
        GameObject errObj = new GameObject("AdminPwError");
        errObj.transform.SetParent(adminPasswordPanel.transform, false);
        adminPasswordError = errObj.AddComponent<TextMeshProUGUI>();
        adminPasswordError.fontSize = 20;
        adminPasswordError.alignment = TextAlignmentOptions.Center;
        adminPasswordError.color = new Color(1f, 0.35f, 0.35f);
        var errRect = adminPasswordError.rectTransform;
        errRect.anchorMin = new Vector2(0.5f, 0.5f);
        errRect.anchorMax = new Vector2(0.5f, 0.5f);
        errRect.sizeDelta = new Vector2(380, 28);
        errRect.anchoredPosition = new Vector2(0, -20);

        // Buttons
        var enterBtn = CreateButton(adminPasswordPanel, "AdminEnterBtn", "Enter", new Vector2(-70, -75),
            () => OnAdminPasswordSubmit(), 130);
        enterBtn.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f);

        CreateButton(adminPasswordPanel, "AdminCancelBtn", "Cancel", new Vector2(70, -75),
            () => adminPasswordPanel.SetActive(false), 130);

        adminPasswordPanel.SetActive(false);
    }

    private void ShowAdminPasswordPanel()
    {
        adminPasswordInput.text = "";
        adminPasswordError.text = "";
        adminPasswordPanel.SetActive(true);
        // ActivateInputField skipped — unreliable on WebGL without a user gesture.
    }

    private void OnAdminPasswordSubmit()
    {
        if (adminPasswordInput.text == AdminPassword)
        {
            adminPasswordPanel.SetActive(false);
            ShowAdminGameList();
        }
        else
        {
            adminPasswordError.text = "Incorrect password";
            adminPasswordInput.text = "";
        }
    }

    // ─── Admin Game List Panel ───

    private void CreateAdminGameListPanel(GameObject parent)
    {
        adminGameListPanel = new GameObject("AdminGameListPanel");
        adminGameListPanel.transform.SetParent(parent.transform, false);

        var panelImg = adminGameListPanel.AddComponent<Image>();
        panelImg.color = new Color(0.07f, 0.07f, 0.1f, 0.97f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Header
        GameObject headerObj = new GameObject("AdminHeader");
        headerObj.transform.SetParent(adminGameListPanel.transform, false);
        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "Recent Games";
        headerText.fontSize = 36;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = new Color(0.9f, 0.7f, 0.2f);
        headerText.fontStyle = FontStyles.Bold;
        var headerRect = headerText.rectTransform;
        headerRect.anchorMin = new Vector2(0.5f, 1f);
        headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0, -20);
        headerRect.sizeDelta = new Vector2(700, 50);

        // Status text (Loading... / error / empty — shown over the scroll area)
        GameObject loadObj = new GameObject("AdminStatusText");
        loadObj.transform.SetParent(adminGameListPanel.transform, false);
        adminStatusText = loadObj.AddComponent<TextMeshProUGUI>();
        adminStatusText.text = "Loading...";
        adminStatusText.fontSize = 24;
        adminStatusText.alignment = TextAlignmentOptions.Center;
        adminStatusText.color = new Color(0.6f, 0.6f, 0.6f);
        adminStatusText.enableWordWrapping = true;
        var loadRect = adminStatusText.rectTransform;
        loadRect.anchorMin = new Vector2(0.1f, 0.15f);
        loadRect.anchorMax = new Vector2(0.9f, 0.85f);
        loadRect.offsetMin = Vector2.zero;
        loadRect.offsetMax = Vector2.zero;

        // ScrollRect
        GameObject scrollObj = new GameObject("GameScrollView");
        scrollObj.transform.SetParent(adminGameListPanel.transform, false);
        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0.05f, 0.05f, 0.08f);
        var scrollRect = scrollObj.AddComponent<ScrollRect>();
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.1f, 0.1f);
        scrollRt.anchorMax = new Vector2(0.9f, 0.88f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        // Viewport
        GameObject vpObj = new GameObject("Viewport");
        vpObj.transform.SetParent(scrollObj.transform, false);
        vpObj.AddComponent<Image>().color = Color.clear;
        vpObj.AddComponent<Mask>().showMaskGraphic = false;
        var vpRect = vpObj.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;

        // Content
        adminGameListContent = new GameObject("Content");
        adminGameListContent.transform.SetParent(vpObj.transform, false);
        var contentRt = adminGameListContent.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        var vlg = adminGameListContent.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        var csf = adminGameListContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = vpRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 25;

        // Close button
        CreateButton(adminGameListPanel, "AdminCloseBtn", "Close", new Vector2(0, 30),
            () => adminGameListPanel.SetActive(false), 180);

        adminGameListPanel.SetActive(false);
    }

    private void ShowAdminGameList()
    {
        adminGameListPanel.SetActive(true);
        foreach (Transform child in adminGameListContent.transform)
            Destroy(child.gameObject);
        adminStatusText.text = "Loading...";
        adminStatusText.color = new Color(0.6f, 0.6f, 0.6f);
        adminStatusText.gameObject.SetActive(true);

        GameLogger.Instance?.FetchRecentGames((games, fetchError) =>
        {
            if (!adminGameListPanel.activeSelf) return;
            foreach (Transform child in adminGameListContent.transform)
                Destroy(child.gameObject);

            if (fetchError != null)
            {
                adminStatusText.text = fetchError;
                adminStatusText.color = new Color(1f, 0.45f, 0.45f);
                adminStatusText.gameObject.SetActive(true);
                return;
            }

            if (games.Count == 0)
            {
                adminStatusText.text = "No games recorded yet.";
                adminStatusText.color = new Color(0.6f, 0.6f, 0.6f);
                adminStatusText.gameObject.SetActive(true);
                return;
            }

            adminStatusText.gameObject.SetActive(false);

            foreach (var game in games)
            {
                string gameId    = game.gameId;
                string timeAgo   = FormatTimeAgo(game.startedAt);
                string modeShort = game.mode == "SeedChess" ? "Seed" :
                                   game.mode == "BluffyChess" ? "Bluffy" : "Classic";
                string resultStr = game.result == "ongoing" ? "Ongoing" :
                                   game.result.Replace("_wins", " Wins").Replace("white", "White").Replace("black", "Black");
                string player    = string.IsNullOrEmpty(game.playerName) ? "?" : game.playerName;

                // Row container
                GameObject rowObj = new GameObject($"Row_{gameId}");
                rowObj.transform.SetParent(adminGameListContent.transform, false);
                var rowImg = rowObj.AddComponent<Image>();
                rowImg.color = new Color(0.12f, 0.12f, 0.17f);
                var rowRt = rowImg.rectTransform;
                rowRt.sizeDelta = new Vector2(0, 52);
                var rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(12, 12, 0, 0);
                rowLayout.spacing = 10;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childControlHeight = true;

                // Info text
                GameObject infoObj = new GameObject("RowInfo");
                infoObj.transform.SetParent(rowObj.transform, false);
                var infoText = infoObj.AddComponent<TextMeshProUGUI>();
                infoText.text = $"<b>{modeShort}</b>  {resultStr}  <color=#999>{timeAgo}</color>  <color=#8cf>{player}</color>";
                infoText.fontSize = 19;
                infoText.alignment = TextAlignmentOptions.MidlineLeft;
                infoText.color = Color.white;
                var infoRt = infoText.rectTransform;
                infoRt.sizeDelta = new Vector2(600, 0);
                var infoLE = infoObj.AddComponent<LayoutElement>();
                infoLE.flexibleWidth = 1;
                infoLE.minHeight = 40;

                // Watch button
                GameObject watchBtnObj = new GameObject("WatchBtn");
                watchBtnObj.transform.SetParent(rowObj.transform, false);
                var watchImg = watchBtnObj.AddComponent<Image>();
                watchImg.color = new Color(0.25f, 0.45f, 0.7f);
                var watchBtn = watchBtnObj.AddComponent<Button>();
                watchBtn.targetGraphic = watchImg;
                var watchColors = watchBtn.colors;
                watchColors.highlightedColor = new Color(0.35f, 0.55f, 0.85f);
                watchColors.pressedColor = new Color(0.15f, 0.3f, 0.5f);
                watchBtn.colors = watchColors;
                var watchLE = watchBtnObj.AddComponent<LayoutElement>();
                watchLE.preferredWidth = 100;
                watchLE.minHeight = 36;
                watchBtn.onClick.AddListener(() => OnWatchGameClicked(gameId, game.mode));
                GameObject watchTextObj = new GameObject("WatchText");
                watchTextObj.transform.SetParent(watchBtnObj.transform, false);
                var watchText = watchTextObj.AddComponent<TextMeshProUGUI>();
                watchText.text = "Watch";
                watchText.fontSize = 18;
                watchText.alignment = TextAlignmentOptions.Center;
                watchText.color = Color.white;
                var watchTextRt = watchText.rectTransform;
                watchTextRt.anchorMin = Vector2.zero;
                watchTextRt.anchorMax = Vector2.one;
                watchTextRt.offsetMin = Vector2.zero;
                watchTextRt.offsetMax = Vector2.zero;
            }
        });
    }

    private void OnWatchGameClicked(string gameId, string mode)
    {
        adminGameListPanel.SetActive(false);
        GameLogger.Instance?.FetchGameReplay(gameId, (fetchedMode, actions) =>
        {
            if (actions.Count == 0)
            {
                ShowInfoPanel("No actions recorded for this game.");
                adminGameListPanel.SetActive(true);
                return;
            }
            string replayMode = string.IsNullOrEmpty(fetchedMode) ? mode : fetchedMode;
            GameManager.Instance.StartReplay(actions, replayMode);
        });
    }

    private static string FormatTimeAgo(long unixMs)
    {
        long nowMs  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long deltaS = (nowMs - unixMs) / 1000;
        if (deltaS < 60)  return $"{deltaS}s ago";
        if (deltaS < 3600) return $"{deltaS / 60}m ago";
        if (deltaS < 86400) return $"{deltaS / 3600}h ago";
        return $"{deltaS / 86400}d ago";
    }

    // ─── Replay Controls ───

    private void CreateReplayControlsPanel(GameObject parent)
    {
        replayControlsPanel = new GameObject("ReplayControlsPanel");
        replayControlsPanel.transform.SetParent(parent.transform, false);

        var panelImg = replayControlsPanel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0, 10);
        panelRect.sizeDelta = new Vector2(680, 60);

        // Progress text
        GameObject progObj = new GameObject("ReplayProgress");
        progObj.transform.SetParent(replayControlsPanel.transform, false);
        replayProgressText = progObj.AddComponent<TextMeshProUGUI>();
        replayProgressText.text = "Move 0 / 0";
        replayProgressText.fontSize = 20;
        replayProgressText.alignment = TextAlignmentOptions.Center;
        replayProgressText.color = new Color(0.85f, 0.85f, 0.85f);
        var progRect = replayProgressText.rectTransform;
        progRect.anchorMin = new Vector2(0.5f, 0.5f);
        progRect.anchorMax = new Vector2(0.5f, 0.5f);
        progRect.sizeDelta = new Vector2(120, 40);
        progRect.anchoredPosition = new Vector2(-230, 0);

        // Back button
        var backBtn = CreateButton(replayControlsPanel, "ReplayBackBtn", "◀ Back",
            new Vector2(-130, 0), () => GameManager.Instance.ReplayStepBackward(), 110);
        backBtn.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.45f);

        // Next button
        var nextBtn = CreateButton(replayControlsPanel, "ReplayNextBtn", "Next ▶",
            new Vector2(-10, 0), () => GameManager.Instance.ReplayStepForward(), 110);
        nextBtn.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.3f);

        // Auto button
        var autoBtn = CreateButton(replayControlsPanel, "ReplayAutoBtn", "⏭ Auto",
            new Vector2(110, 0), () => ToggleReplayAuto(), 110);
        autoBtn.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.1f);

        // Stop button
        var stopBtn = CreateButton(replayControlsPanel, "ReplayStopBtn", "✕ Stop",
            new Vector2(240, 0), () => GameManager.Instance.StopReplay(), 100);
        stopBtn.GetComponent<Image>().color = new Color(0.55f, 0.15f, 0.15f);

        replayControlsPanel.SetActive(false);
    }

    public void ShowReplayControls(int index, int total)
    {
        replayProgressText.text = $"Move {index} / {total}";
        replayControlsPanel.SetActive(true);
    }

    public void HideReplayControls()
    {
        if (replayAutoCoroutine != null)
        {
            StopCoroutine(replayAutoCoroutine);
            replayAutoCoroutine = null;
        }
        replayControlsPanel.SetActive(false);
    }

    public void UpdateReplayProgress(int index, int total)
    {
        replayProgressText.text = $"Move {index} / {total}";
    }

    private void ToggleReplayAuto()
    {
        if (replayAutoCoroutine != null)
        {
            StopCoroutine(replayAutoCoroutine);
            replayAutoCoroutine = null;
            var autoBtn = replayControlsPanel.transform.Find("ReplayAutoBtn");
            if (autoBtn != null) autoBtn.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.1f);
        }
        else
        {
            replayAutoCoroutine = StartCoroutine(AutoPlayCoroutine());
        }
    }

    private System.Collections.IEnumerator AutoPlayCoroutine()
    {
        var autoBtn = replayControlsPanel.transform.Find("ReplayAutoBtn");
        if (autoBtn != null) autoBtn.GetComponent<Image>().color = new Color(0.6f, 0.55f, 0.15f);

        while (true)
        {
            yield return new WaitForSeconds(0.8f);
            string prevText = replayProgressText.text;
            GameManager.Instance.ReplayStepForward();
            // If text didn't advance, we're at the end
            if (replayProgressText.text == prevText)
                break;
        }

        replayAutoCoroutine = null;
        if (autoBtn != null) autoBtn.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.1f);
    }
}
