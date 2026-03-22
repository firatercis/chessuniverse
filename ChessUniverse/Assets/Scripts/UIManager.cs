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
    private GameObject adminScrollView;
    private TextMeshProUGUI adminStatusText;

    // Replay controls
    private GameObject replayControlsPanel;
    private TextMeshProUGUI replayProgressText;
    private Coroutine replayAutoCoroutine;

    // Splash text (dramatic bluff call)
    private GameObject splashPanel;
    private TextMeshProUGUI splashText;
    private CanvasGroup splashCanvasGroup;

    // Currency display
    private GameObject currencyPanel;
    private TextMeshProUGUI currencyText;

    // Tutorial
    private GameObject tutorialOverlay;
    private GameObject tutorialMsgPanel;
    private TextMeshProUGUI tutorialMsgText;
    private GameObject tutorialOkBtn;
    private GameObject tutorialSkipBtn;
    private int savedSeedBtnSiblingIndex;
    private GameObject fingerObj;
    private Coroutine fingerPulseCoroutine;

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

        // Currency display
        CreateCurrencyDisplay(canvasObj);

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

        // Tutorial overlay
        CreateTutorialPanel(canvasObj);

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

    private void CreateCurrencyDisplay(GameObject parent)
    {
        currencyPanel = new GameObject("CurrencyPanel");
        currencyPanel.transform.SetParent(parent.transform, false);

        // Dark background pill
        var panelImg = currencyPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.10f, 0.08f, 0.9f);

        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20, -90);
        panelRect.sizeDelta = new Vector2(160, 50);

        // Golden pawn icon
        GameObject iconObj = new GameObject("GoldenPawnIcon");
        iconObj.transform.SetParent(currencyPanel.transform, false);
        var iconImg = iconObj.AddComponent<Image>();
        var goldenPawnTex = Resources.Load<Texture2D>("UI/goldenPawn");
        if (goldenPawnTex != null)
        {
            iconImg.sprite = Sprite.Create(goldenPawnTex,
                new Rect(0, 0, goldenPawnTex.width, goldenPawnTex.height),
                new Vector2(0.5f, 0.5f));
        }
        iconImg.preserveAspect = true;
        var iconRt = iconImg.rectTransform;
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(6, 0);
        iconRt.sizeDelta = new Vector2(40, 40);

        // Amount text
        GameObject textObj = new GameObject("CurrencyAmount");
        textObj.transform.SetParent(currencyPanel.transform, false);
        currencyText = textObj.AddComponent<TextMeshProUGUI>();
        currencyText.text = GetGold().ToString();
        currencyText.fontSize = 26;
        currencyText.alignment = TextAlignmentOptions.MidlineLeft;
        currencyText.color = new Color(1f, 0.85f, 0.25f);
        currencyText.fontStyle = FontStyles.Bold;
        var textRt = currencyText.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(50, 0);
        textRt.offsetMax = new Vector2(-8, 0);

        currencyPanel.SetActive(false);
    }

    // ─── Tutorial ───

    private void CreateTutorialPanel(GameObject parent)
    {
        // Full-screen dark overlay (blocks clicks)
        tutorialOverlay = new GameObject("TutorialOverlay");
        tutorialOverlay.transform.SetParent(parent.transform, false);
        var overlayImg = tutorialOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f);
        overlayImg.raycastTarget = true;
        var overlayRect = overlayImg.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Message panel (centered on overlay)
        tutorialMsgPanel = new GameObject("TutorialMsgPanel");
        tutorialMsgPanel.transform.SetParent(tutorialOverlay.transform, false);
        var msgImg = tutorialMsgPanel.AddComponent<Image>();
        msgImg.color = new Color(0.14f, 0.12f, 0.10f, 0.95f);
        var msgRect = msgImg.rectTransform;
        msgRect.anchorMin = new Vector2(0.5f, 0.5f);
        msgRect.anchorMax = new Vector2(0.5f, 0.5f);
        msgRect.sizeDelta = new Vector2(620, 220);

        // Message text
        var textObj = new GameObject("TutorialMsgText");
        textObj.transform.SetParent(tutorialMsgPanel.transform, false);
        tutorialMsgText = textObj.AddComponent<TextMeshProUGUI>();
        tutorialMsgText.fontSize = 26;
        tutorialMsgText.alignment = TextAlignmentOptions.Center;
        tutorialMsgText.color = Color.white;
        var textRect = tutorialMsgText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0.35f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(-20, -15);

        // OK button
        tutorialOkBtn = CreateButton(tutorialMsgPanel, "TutorialOkBtn", "OK", new Vector2(0, -75),
            () => GameManager.Instance.AdvanceTutorial(), 160);

        // Skip Tutorial button (child of canvas, always on top)
        tutorialSkipBtn = CreateButton(parent, "SkipTutorialBtn", "Skip Tutorial", Vector2.zero,
            () => GameManager.Instance.SkipTutorial(), 200);
        var skipRect = tutorialSkipBtn.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.pivot = new Vector2(1f, 0f);
        skipRect.anchoredPosition = new Vector2(-20, 20);
        var skipImg = tutorialSkipBtn.GetComponent<Image>();
        if (skipImg != null)
        {
            skipImg.color = new Color(0.4f, 0.35f, 0.3f);
            var skipColors = tutorialSkipBtn.GetComponent<Button>().colors;
            skipColors.highlightedColor = new Color(0.5f, 0.45f, 0.4f);
            tutorialSkipBtn.GetComponent<Button>().colors = skipColors;
        }

        tutorialOverlay.SetActive(false);
        tutorialSkipBtn.SetActive(false);

        // Finger pointer (child of canvas, always on top)
        fingerObj = new GameObject("FingerPointer");
        fingerObj.transform.SetParent(parent.transform, false);
        var fingerImgComp = fingerObj.AddComponent<Image>();
        var fingerTex = Resources.Load<Texture2D>("UI/finger");
        if (fingerTex != null)
            fingerImgComp.sprite = Sprite.Create(fingerTex,
                new Rect(0, 0, fingerTex.width, fingerTex.height),
                new Vector2(0.5f, 0.5f));
        fingerImgComp.preserveAspect = true;
        fingerImgComp.raycastTarget = false;
        fingerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
        fingerObj.SetActive(false);
    }

    public void ShowTutorialMessage(string message, string buttonText = "OK")
    {
        tutorialMsgText.text = message;
        tutorialOkBtn.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
        tutorialMsgPanel.SetActive(true);
        tutorialOkBtn.SetActive(true);
        tutorialOverlay.SetActive(true);
        tutorialSkipBtn.SetActive(true);
    }

    public void ShowTutorialOverlayOnly()
    {
        tutorialMsgPanel.SetActive(false);
        tutorialOverlay.SetActive(true);
        tutorialSkipBtn.SetActive(true);
    }

    public void HideTutorialOverlay()
    {
        tutorialOverlay.SetActive(false);
    }

    public void HideTutorialAll()
    {
        tutorialOverlay.SetActive(false);
        tutorialSkipBtn.SetActive(false);
        HideFinger();
        UnfocusSeedButtons();
        SetSeedButtonsInteractable(null);
    }

    private void ShowFingerOnUI(Transform target, float yOffset = -50f)
    {
        fingerObj.SetActive(true);
        float s = canvas.transform.localScale.x;
        fingerObj.transform.position = target.position + new Vector3(0, yOffset * s, 0);
        fingerObj.transform.SetAsLastSibling();
        StartFingerPulse();
    }

    public void ShowFingerOnPawnButton()
    {
        Transform btn = seedButtonsPanel.transform.Find("Seed_Pawn");
        if (btn != null)
            ShowFingerOnUI(btn);
    }

    public void ShowFingerOnBoardSquare(int x, int y)
    {
        fingerObj.SetActive(true);
        Vector3 worldPos = ChessBoard.Instance.VisualPos(x, y);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        float s = canvas.transform.localScale.x;
        fingerObj.transform.position = screenPos + new Vector3(0, -50 * s, 0);
        fingerObj.transform.SetAsLastSibling();
        StartFingerPulse();
    }

    public void ShowFingerOnBluffyConfirm()
    {
        Transform btn = setupPanel.transform.Find("ConfirmSetupBtn");
        if (btn != null)
            ShowFingerOnUI(btn);
    }

    public void HideFinger()
    {
        fingerObj.SetActive(false);
        if (fingerPulseCoroutine != null)
        {
            StopCoroutine(fingerPulseCoroutine);
            fingerPulseCoroutine = null;
        }
    }

    private void StartFingerPulse()
    {
        if (fingerPulseCoroutine != null)
            StopCoroutine(fingerPulseCoroutine);
        fingerPulseCoroutine = StartCoroutine(FingerPulseCoroutine());
    }

    private System.Collections.IEnumerator FingerPulseCoroutine()
    {
        var img = fingerObj.GetComponent<Image>();
        while (true)
        {
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                img.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0.25f, t / 0.5f));
                yield return null;
            }
            t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                img.color = new Color(1, 1, 1, Mathf.Lerp(0.25f, 1f, t / 0.5f));
                yield return null;
            }
        }
    }

    public void FocusSeedButtons()
    {
        savedSeedBtnSiblingIndex = seedButtonsPanel.transform.GetSiblingIndex();
        seedButtonsPanel.transform.SetSiblingIndex(tutorialOverlay.transform.GetSiblingIndex() + 1);
    }

    public void UnfocusSeedButtons()
    {
        if (savedSeedBtnSiblingIndex > 0)
            seedButtonsPanel.transform.SetSiblingIndex(savedSeedBtnSiblingIndex);
    }

    public void SetSeedButtonsInteractable(PieceType? onlyType)
    {
        foreach (Transform child in seedButtonsPanel.transform)
        {
            var btn = child.GetComponent<Button>();
            if (btn == null) continue;
            btn.interactable = onlyType == null || child.name == $"Seed_{onlyType.Value}";
        }
    }

    // ─── Currency ───

    public static int GetGold()
    {
        return PlayerPrefs.GetInt("GoldenPawns", 100);
    }

    public static void AddGold(int amount)
    {
        int current = GetGold();
        PlayerPrefs.SetInt("GoldenPawns", current + amount);
        PlayerPrefs.Save();
        if (Instance != null && Instance.currencyText != null)
            Instance.currencyText.text = GetGold().ToString();
    }

    public static void SetGold(int amount)
    {
        PlayerPrefs.SetInt("GoldenPawns", amount);
        PlayerPrefs.Save();
        if (Instance != null && Instance.currencyText != null)
            Instance.currencyText.text = GetGold().ToString();
    }

    public void ShowCurrencyDisplay()
    {
        currencyText.text = GetGold().ToString();
        currencyPanel.SetActive(true);
    }

    public void HideCurrencyDisplay()
    {
        currencyPanel.SetActive(false);
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

    private Sprite LoadIconSprite(string resourcePath)
    {
        var tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private void CreateMainMenuPanel(GameObject parent)
    {
        mainMenuPanel = new GameObject("MainMenuPanel");
        mainMenuPanel.transform.SetParent(parent.transform, false);

        var panelImg = mainMenuPanel.AddComponent<Image>();
        panelImg.color = new Color(0.09f, 0.09f, 0.12f, 1f);
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ─── Header ───
        var titleObj = new GameObject("MenuTitle");
        titleObj.transform.SetParent(mainMenuPanel.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CHESS MODES";
        titleTmp.fontSize = 38;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.characterSpacing = 5;
        var titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -25);
        titleRt.sizeDelta = new Vector2(0, 45);

        var subObj = new GameObject("MenuSubtitle");
        subObj.transform.SetParent(mainMenuPanel.transform, false);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Choose a mode and start playing";
        subTmp.fontSize = 18;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color(0.55f, 0.55f, 0.6f);
        var subRt = subTmp.rectTransform;
        subRt.anchorMin = new Vector2(0, 1);
        subRt.anchorMax = new Vector2(1, 1);
        subRt.pivot = new Vector2(0.5f, 1);
        subRt.anchoredPosition = new Vector2(0, -72);
        subRt.sizeDelta = new Vector2(0, 25);

        // Player name (top-right, clickable to change)
        var nameObj = new GameObject("PlayerNameSub");
        nameObj.transform.SetParent(mainMenuPanel.transform, false);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 16;
        nameTmp.alignment = TextAlignmentOptions.MidlineRight;
        nameTmp.color = new Color(0.5f, 0.65f, 0.5f);
        nameTmp.text = $"Playing as: {PlayerPrefs.GetString("PlayerName", "")}";
        var nameRt = nameTmp.rectTransform;
        nameRt.anchorMin = new Vector2(1, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(1, 1);
        nameRt.anchoredPosition = new Vector2(-15, -8);
        nameRt.sizeDelta = new Vector2(280, 22);
        var nameBtn = nameObj.AddComponent<Button>();
        nameBtn.transition = Selectable.Transition.None;
        nameBtn.onClick.AddListener(() => ShowProfilePanel());

        // ─── ScrollView for Cards ───
        var scrollObj = new GameObject("CardScrollView");
        scrollObj.transform.SetParent(mainMenuPanel.transform, false);
        var scrollComp = scrollObj.AddComponent<ScrollRect>();
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(0, 70);
        scrollRt.offsetMax = new Vector2(0, -100);
        scrollComp.horizontal = false;
        scrollComp.vertical = true;
        scrollComp.scrollSensitivity = 40;
        scrollComp.movementType = ScrollRect.MovementType.Elastic;

        var vpObj = new GameObject("Viewport");
        vpObj.transform.SetParent(scrollObj.transform, false);
        vpObj.AddComponent<RectMask2D>();
        var vpRt = vpObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        scrollComp.viewport = vpRt;

        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(vpObj.transform, false);
        var contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;

        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 18;
        vlg.padding = new RectOffset(25, 25, 8, 25);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollComp.content = contentRt;

        // ─── Card 1: Infinite Knight Run (disabled) ───
        CreateGameModeCard(contentObj.transform,
            "Infinite Knight Run", "Endless survival with a knight",
            "SOLO", new Color(0.9f, 0.6f, 0.15f),
            LoadIconSprite("UI/GameIcons/infinite_knight_run_icon"),
            true, "Play",
            null, null, null);

        // ─── Card 2: Seed Chess ───
        CreateGameModeCard(contentObj.transform,
            "Seed Chess", "Grow your pieces from seeds over time",
            "NEW", new Color(0.3f, 0.8f, 0.35f),
            LoadIconSprite("UI/GameIcons/seed_chess_icon"),
            false, "Play",
            () => {
                PlayerPrefs.SetInt("SeedTutorialDone", 0); PlayerPrefs.Save();
                HideMainMenu();
                GameManager.Instance.StartGame(GameMode.SeedChess, PlayMode.SinglePlayer);
            },
            () => { HideMainMenu(); GameManager.Instance.StartGame(GameMode.SeedChess, PlayMode.SinglePlayer); },
            () => { pendingGameMode = GameMode.SeedChess; HideMainMenu(); ShowLobbyPanel(); });

        // ─── Card 3: Bluffy Chess ───
        CreateGameModeCard(contentObj.transform,
            "Bluffy Chess", "Hidden identities and bluff calls",
            "PVP", new Color(0.55f, 0.4f, 0.9f),
            LoadIconSprite("UI/GameIcons/bluffy_chess_icon"),
            false, "Play Online",
            () => {
                PlayerPrefs.SetInt("BluffyTutorialDone", 0); PlayerPrefs.Save();
                HideMainMenu();
                GameManager.Instance.StartGame(GameMode.BluffyChess, PlayMode.SinglePlayer);
            },
            () => { HideMainMenu(); GameManager.Instance.StartGame(GameMode.BluffyChess, PlayMode.SinglePlayer); },
            () => { pendingGameMode = GameMode.BluffyChess; HideMainMenu(); ShowLobbyPanel(); });

        // ─── Admin gear (bottom-left) ───
        var adminBtnObj = new GameObject("AdminBtn");
        adminBtnObj.transform.SetParent(mainMenuPanel.transform, false);
        var adminBtnImg = adminBtnObj.AddComponent<Image>();
        adminBtnImg.color = new Color(0.16f, 0.16f, 0.20f, 0.8f);
        var adminBtnRect = adminBtnImg.rectTransform;
        adminBtnRect.anchorMin = Vector2.zero;
        adminBtnRect.anchorMax = Vector2.zero;
        adminBtnRect.pivot = Vector2.zero;
        adminBtnRect.anchoredPosition = new Vector2(15, 15);
        adminBtnRect.sizeDelta = new Vector2(44, 44);
        var adminBtn = adminBtnObj.AddComponent<Button>();
        adminBtn.targetGraphic = adminBtnImg;
        adminBtn.onClick.AddListener(() => ShowAdminPasswordPanel());
        var adminIconObj = new GameObject("AdminIcon");
        adminIconObj.transform.SetParent(adminBtnObj.transform, false);
        var adminTmp = adminIconObj.AddComponent<TextMeshProUGUI>();
        adminTmp.text = "\u2699";
        adminTmp.fontSize = 22;
        adminTmp.alignment = TextAlignmentOptions.Center;
        adminTmp.color = new Color(0.5f, 0.5f, 0.55f);
        var adminIconRt = adminTmp.rectTransform;
        adminIconRt.anchorMin = Vector2.zero;
        adminIconRt.anchorMax = Vector2.one;
        adminIconRt.offsetMin = Vector2.zero;
        adminIconRt.offsetMax = Vector2.zero;

        mainMenuPanel.SetActive(false);
    }

    private void CreateGameModeCard(Transform parent, string title, string description,
        string badge, Color badgeColor, Sprite icon, bool disabled, string primaryText,
        System.Action onTutorial, System.Action onVsAI, System.Action onPrimary)
    {
        var card = new GameObject($"Card_{title.Replace(" ", "")}");
        card.transform.SetParent(parent, false);

        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 190;
        le.minHeight = 190;

        var cardImg = card.AddComponent<Image>();
        cardImg.color = disabled ? new Color(0.13f, 0.13f, 0.16f) : new Color(0.15f, 0.15f, 0.19f);

        // ─── Icon ───
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(card.transform, false);
        var iconImg = iconObj.AddComponent<Image>();
        if (icon != null) iconImg.sprite = icon;
        iconImg.preserveAspect = true;
        if (disabled) iconImg.color = new Color(1, 1, 1, 0.35f);
        var iconRt = iconImg.rectTransform;
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(14, 10);
        iconRt.sizeDelta = new Vector2(72, 72);

        // ─── Title ───
        float textLeft = 100;
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 26;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = disabled ? new Color(0.5f, 0.5f, 0.55f) : Color.white;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.overflowMode = TextOverflowModes.Ellipsis;
        var titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0, 1);
        titleRt.anchoredPosition = new Vector2(textLeft, -18);
        titleRt.sizeDelta = new Vector2(-textLeft - 80, 34);

        // ─── Badge ───
        var badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(card.transform, false);
        var badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.color = disabled ? new Color(0.25f, 0.25f, 0.28f) : badgeColor;
        var badgeRt = badgeBg.rectTransform;
        badgeRt.anchorMin = new Vector2(1, 1);
        badgeRt.anchorMax = new Vector2(1, 1);
        badgeRt.pivot = new Vector2(1, 1);
        badgeRt.anchoredPosition = new Vector2(-14, -20);
        badgeRt.sizeDelta = new Vector2(52, 22);
        var badgeTextObj = new GameObject("BadgeText");
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        var badgeTmp = badgeTextObj.AddComponent<TextMeshProUGUI>();
        badgeTmp.text = badge;
        badgeTmp.fontSize = 11;
        badgeTmp.fontStyle = FontStyles.Bold;
        badgeTmp.alignment = TextAlignmentOptions.Center;
        badgeTmp.color = disabled ? new Color(0.55f, 0.55f, 0.6f) : Color.white;
        var badgeTextRt = badgeTmp.rectTransform;
        badgeTextRt.anchorMin = Vector2.zero;
        badgeTextRt.anchorMax = Vector2.one;
        badgeTextRt.offsetMin = Vector2.zero;
        badgeTextRt.offsetMax = Vector2.zero;

        // ─── Description ───
        var descObj = new GameObject("Description");
        descObj.transform.SetParent(card.transform, false);
        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = description;
        descTmp.fontSize = 17;
        descTmp.color = disabled ? new Color(0.4f, 0.4f, 0.43f) : new Color(0.6f, 0.6f, 0.65f);
        descTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var descRt = descTmp.rectTransform;
        descRt.anchorMin = new Vector2(0, 1);
        descRt.anchorMax = new Vector2(1, 1);
        descRt.pivot = new Vector2(0, 1);
        descRt.anchoredPosition = new Vector2(textLeft, -55);
        descRt.sizeDelta = new Vector2(-textLeft - 20, 24);

        // ─── Disabled overlay ───
        if (disabled)
        {
            var overlayObj = new GameObject("DisabledOverlay");
            overlayObj.transform.SetParent(card.transform, false);
            var overlayTmp = overlayObj.AddComponent<TextMeshProUGUI>();
            overlayTmp.text = "Under Construction";
            overlayTmp.fontSize = 16;
            overlayTmp.fontStyle = FontStyles.Italic;
            overlayTmp.color = new Color(0.85f, 0.6f, 0.15f, 0.7f);
            overlayTmp.alignment = TextAlignmentOptions.Center;
            var overlayRt = overlayTmp.rectTransform;
            overlayRt.anchorMin = new Vector2(0, 0);
            overlayRt.anchorMax = new Vector2(1, 0);
            overlayRt.pivot = new Vector2(0.5f, 0);
            overlayRt.anchoredPosition = new Vector2(0, 22);
            overlayRt.sizeDelta = new Vector2(0, 30);
            return;
        }

        // ─── Action Buttons Row ───
        float btnY = -145;
        float btnH = 36;
        Color secColor = new Color(0.22f, 0.22f, 0.28f);
        Color secHighlight = new Color(0.30f, 0.30f, 0.38f);
        Color priColor = new Color(0.22f, 0.52f, 0.28f);
        Color priHighlight = new Color(0.30f, 0.65f, 0.35f);

        // Tutorial (secondary)
        var tutBtn = new GameObject("TutorialBtn");
        tutBtn.transform.SetParent(card.transform, false);
        var tutImg = tutBtn.AddComponent<Image>();
        tutImg.color = secColor;
        var tutRt = tutImg.rectTransform;
        tutRt.anchorMin = new Vector2(0, 1);
        tutRt.anchorMax = new Vector2(0, 1);
        tutRt.pivot = new Vector2(0, 1);
        tutRt.anchoredPosition = new Vector2(textLeft, btnY);
        tutRt.sizeDelta = new Vector2(100, btnH);
        var tutBtnComp = tutBtn.AddComponent<Button>();
        tutBtnComp.targetGraphic = tutImg;
        var tutColors = tutBtnComp.colors;
        tutColors.highlightedColor = secHighlight;
        tutColors.pressedColor = new Color(0.18f, 0.18f, 0.22f);
        tutBtnComp.colors = tutColors;
        if (onTutorial != null) tutBtnComp.onClick.AddListener(() => onTutorial());
        var tutTextObj = new GameObject("Text");
        tutTextObj.transform.SetParent(tutBtn.transform, false);
        var tutTmp = tutTextObj.AddComponent<TextMeshProUGUI>();
        tutTmp.text = "Tutorial";
        tutTmp.fontSize = 16;
        tutTmp.alignment = TextAlignmentOptions.Center;
        tutTmp.color = new Color(0.75f, 0.75f, 0.8f);
        var tutTextRt = tutTmp.rectTransform;
        tutTextRt.anchorMin = Vector2.zero;
        tutTextRt.anchorMax = Vector2.one;
        tutTextRt.offsetMin = Vector2.zero;
        tutTextRt.offsetMax = Vector2.zero;

        // vs AI (secondary)
        var aiBtn = new GameObject("VsAIBtn");
        aiBtn.transform.SetParent(card.transform, false);
        var aiImg = aiBtn.AddComponent<Image>();
        aiImg.color = secColor;
        var aiRt = aiImg.rectTransform;
        aiRt.anchorMin = new Vector2(0, 1);
        aiRt.anchorMax = new Vector2(0, 1);
        aiRt.pivot = new Vector2(0, 1);
        aiRt.anchoredPosition = new Vector2(textLeft + 110, btnY);
        aiRt.sizeDelta = new Vector2(80, btnH);
        var aiBtnComp = aiBtn.AddComponent<Button>();
        aiBtnComp.targetGraphic = aiImg;
        var aiColors = aiBtnComp.colors;
        aiColors.highlightedColor = secHighlight;
        aiColors.pressedColor = new Color(0.18f, 0.18f, 0.22f);
        aiBtnComp.colors = aiColors;
        if (onVsAI != null) aiBtnComp.onClick.AddListener(() => onVsAI());
        var aiTextObj = new GameObject("Text");
        aiTextObj.transform.SetParent(aiBtn.transform, false);
        var aiTmp = aiTextObj.AddComponent<TextMeshProUGUI>();
        aiTmp.text = "vs AI";
        aiTmp.fontSize = 16;
        aiTmp.alignment = TextAlignmentOptions.Center;
        aiTmp.color = new Color(0.75f, 0.75f, 0.8f);
        var aiTextRt = aiTmp.rectTransform;
        aiTextRt.anchorMin = Vector2.zero;
        aiTextRt.anchorMax = Vector2.one;
        aiTextRt.offsetMin = Vector2.zero;
        aiTextRt.offsetMax = Vector2.zero;

        // Primary button (right-aligned)
        var priBtn = new GameObject("PrimaryBtn");
        priBtn.transform.SetParent(card.transform, false);
        var priImg = priBtn.AddComponent<Image>();
        priImg.color = priColor;
        var priRt = priImg.rectTransform;
        priRt.anchorMin = new Vector2(1, 1);
        priRt.anchorMax = new Vector2(1, 1);
        priRt.pivot = new Vector2(1, 1);
        priRt.anchoredPosition = new Vector2(-14, btnY);
        priRt.sizeDelta = new Vector2(130, btnH);
        var priBtnComp = priBtn.AddComponent<Button>();
        priBtnComp.targetGraphic = priImg;
        var priColors2 = priBtnComp.colors;
        priColors2.highlightedColor = priHighlight;
        priColors2.pressedColor = new Color(0.18f, 0.40f, 0.20f);
        priBtnComp.colors = priColors2;
        if (onPrimary != null) priBtnComp.onClick.AddListener(() => onPrimary());
        var priTextObj = new GameObject("Text");
        priTextObj.transform.SetParent(priBtn.transform, false);
        var priTmp = priTextObj.AddComponent<TextMeshProUGUI>();
        priTmp.text = primaryText;
        priTmp.fontSize = 17;
        priTmp.fontStyle = FontStyles.Bold;
        priTmp.alignment = TextAlignmentOptions.Center;
        priTmp.color = Color.white;
        var priTextRt = priTmp.rectTransform;
        priTextRt.anchorMin = Vector2.zero;
        priTextRt.anchorMax = Vector2.one;
        priTextRt.offsetMin = Vector2.zero;
        priTextRt.offsetMax = Vector2.zero;
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
        var spBtnObj = CreateButton(playerModePanel, "SinglePlayerBtn", "Practice vs Computer", new Vector2(0, 10),
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
        panelImg.color = new Color(0.15f, 0.13f, 0.08f, 0.88f);

        // Horizontal strip below turn text & currency, above board
        var panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -150);
        panelRect.sizeDelta = new Vector2(340, 56);

        // "Plant:" label on the left
        var labelObj = new GameObject("PlantLabel");
        labelObj.transform.SetParent(seedButtonsPanel.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Plant:";
        labelTmp.fontSize = 16;
        labelTmp.alignment = TextAlignmentOptions.MidlineRight;
        labelTmp.color = new Color(0.85f, 0.85f, 0.85f);
        var labelRt = labelTmp.rectTransform;
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(60, 46);
        labelRt.anchoredPosition = new Vector2(-140, 0);

        // 5 compact piece-symbol buttons in a row
        string[] turns   = { "1", "3", "3", "5", "9" };
        PieceType[] types = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
        Color lightGreen = new Color(0.5f, 0.9f, 0.3f);

        for (int i = 0; i < 5; i++)
        {
            PieceType t = types[i];
            float xPos = -80 + i * 55;

            GameObject btnObj = new GameObject($"Seed_{types[i]}");
            btnObj.transform.SetParent(seedButtonsPanel.transform, false);

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.35f, 0.15f);

            var btnRect = btnImg.rectTransform;
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(48, 46);
            btnRect.anchoredPosition = new Vector2(xPos, 0);

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImg;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.2f);
            colors.pressedColor = new Color(0.15f, 0.25f, 0.1f);
            button.colors = colors;
            button.onClick.AddListener(() => GameManager.Instance.OnSeedButtonClick(t));

            // Piece sprite icon (top area)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            var iconImg = iconObj.AddComponent<Image>();
            var pieceSprite = ChessPiece.GetPieceSprite(PieceColor.White, t);
            if (pieceSprite != null) iconImg.sprite = pieceSprite;
            iconImg.color = lightGreen;
            iconImg.preserveAspect = true;
            var iconRt = iconImg.rectTransform;
            iconRt.anchorMin = new Vector2(0.15f, 0.30f);
            iconRt.anchorMax = new Vector2(0.85f, 0.95f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            // Growth turns (small, bottom area)
            GameObject turnObj = new GameObject("Turns");
            turnObj.transform.SetParent(btnObj.transform, false);
            var turnTmp = turnObj.AddComponent<TextMeshProUGUI>();
            turnTmp.text = turns[i];
            turnTmp.fontSize = 13;
            turnTmp.alignment = TextAlignmentOptions.Center;
            turnTmp.color = new Color(0.7f, 0.95f, 0.5f);
            var turnRt = turnTmp.rectTransform;
            turnRt.anchorMin = new Vector2(0, 0);
            turnRt.anchorMax = new Vector2(1, 0.32f);
            turnRt.offsetMin = Vector2.zero;
            turnRt.offsetMax = Vector2.zero;
        }

        seedButtonsPanel.SetActive(false);
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
        ShowCurrencyDisplay();

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
        adminScrollView = new GameObject("GameScrollView");
        var scrollObj = adminScrollView;
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
        vpObj.AddComponent<RectMask2D>();
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
        vlg.childControlHeight = true;
        var csf = adminGameListContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = vpRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 25;

        // Close button (anchored to bottom left of center)
        var closeBtn = CreateButton(adminGameListPanel, "AdminCloseBtn", "Close", Vector2.zero,
            () => adminGameListPanel.SetActive(false), 150);
        var closeBtnRt = closeBtn.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRt.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRt.anchoredPosition = new Vector2(-85, 32);

        // Delete All button (red, bottom right of center)
        var deleteBtn = CreateButton(adminGameListPanel, "AdminDeleteAllBtn", "Delete All", Vector2.zero,
            () => OnDeleteAllGamesClicked(), 150);
        var deleteBtnRt = deleteBtn.GetComponent<RectTransform>();
        deleteBtnRt.anchorMin = new Vector2(0.5f, 0f);
        deleteBtnRt.anchorMax = new Vector2(0.5f, 0f);
        deleteBtnRt.anchoredPosition = new Vector2(85, 32);
        var deleteBtnImg = deleteBtn.GetComponent<Image>();
        deleteBtnImg.color = new Color(0.7f, 0.2f, 0.2f);
        var deleteBtnButton = deleteBtn.GetComponent<Button>();
        var dc = deleteBtnButton.colors;
        dc.highlightedColor = new Color(0.85f, 0.3f, 0.3f);
        dc.pressedColor = new Color(0.5f, 0.15f, 0.15f);
        deleteBtnButton.colors = dc;

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
        adminScrollView.SetActive(false);

        if (GameLogger.Instance == null)
        {
            adminStatusText.text = "GameLogger not available.";
            adminStatusText.color = new Color(1f, 0.45f, 0.45f);
            return;
        }

        GameLogger.Instance.FetchRecentGames((games, fetchError) =>
        {
            if (!adminGameListPanel.activeSelf) return;
            foreach (Transform child in adminGameListContent.transform)
                Destroy(child.gameObject);

            if (fetchError != null)
            {
                adminStatusText.text = fetchError;
                adminStatusText.color = new Color(1f, 0.45f, 0.45f);
                adminStatusText.gameObject.SetActive(true);
                adminScrollView.SetActive(false);
                return;
            }

            if (games.Count == 0)
            {
                adminStatusText.text = "No games recorded yet.";
                adminStatusText.color = new Color(0.6f, 0.6f, 0.6f);
                adminStatusText.gameObject.SetActive(true);
                adminScrollView.SetActive(false);
                return;
            }

            adminStatusText.gameObject.SetActive(false);
            adminScrollView.SetActive(true);

            // Table header
            CreateGameTableRow(adminGameListContent.transform,
                "Mode", "Player", "Result", "Time",
                null, null, new Color(0.9f, 0.75f, 0.25f), true);

            // Game rows
            foreach (var game in games)
            {
                string gameId    = game.gameId;
                string timeAgo   = FormatTimeAgo(game.startedAt);
                string modeShort = game.mode == "SeedChess" ? "Seed" :
                                   game.mode == "BluffyChess" ? "Bluffy" : "Classic";
                string resultStr = game.result == "ongoing" ? "Ongoing" :
                                   game.result.Replace("_wins", " Wins")
                                       .Replace("white", "White").Replace("black", "Black");
                string player    = string.IsNullOrEmpty(game.playerName) ? "?" : game.playerName;

                CreateGameTableRow(adminGameListContent.transform,
                    modeShort, player, resultStr, timeAgo,
                    gameId, game.mode, Color.white, false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                adminGameListContent.GetComponent<RectTransform>());
        });
    }

    private void OnDeleteAllGamesClicked()
    {
        ShowInfoPanel("Delete ALL games?\nThis cannot be undone.", () =>
        {
            if (GameLogger.Instance == null) return;
            adminStatusText.text = "Deleting...";
            adminStatusText.color = new Color(0.6f, 0.6f, 0.6f);
            adminStatusText.gameObject.SetActive(true);
            adminScrollView.SetActive(false);
            foreach (Transform child in adminGameListContent.transform)
                Destroy(child.gameObject);

            GameLogger.Instance.DeleteAllGames((success) =>
            {
                if (success)
                    ShowAdminGameList();
                else
                {
                    adminStatusText.text = "Failed to delete games.";
                    adminStatusText.color = new Color(1f, 0.45f, 0.45f);
                }
            });
        });
    }

    // ─── Admin Table Helpers ───

    private void CreateGameTableRow(Transform parent,
        string mode, string player, string result, string time,
        string gameId, string gameMode, Color textColor, bool isHeader)
    {
        float rowHeight = isHeader ? 34 : 44;

        var rowObj = new GameObject(isHeader ? "HeaderRow" : $"Row_{gameId}");
        rowObj.transform.SetParent(parent, false);

        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = isHeader ? new Color(0.14f, 0.14f, 0.2f) : new Color(0.1f, 0.1f, 0.15f);

        var le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = rowHeight;

        float fs = isHeader ? 15f : 17f;
        AddTableCell(rowObj.transform, mode,   0.02f, 0.17f, TextAlignmentOptions.MidlineLeft,   fs).color = textColor;
        AddTableCell(rowObj.transform, player, 0.17f, 0.37f, TextAlignmentOptions.MidlineLeft,   fs).color = textColor;
        AddTableCell(rowObj.transform, result, 0.37f, 0.60f, TextAlignmentOptions.MidlineLeft,   fs).color = textColor;
        AddTableCell(rowObj.transform, time,   0.60f, 0.78f, TextAlignmentOptions.Midline, fs).color = textColor;

        if (!isHeader && gameId != null)
        {
            // Watch button (anchor-based, no layout group needed)
            GameObject watchBtnObj = new GameObject("WatchBtn");
            watchBtnObj.transform.SetParent(rowObj.transform, false);
            var watchImg = watchBtnObj.AddComponent<Image>();
            watchImg.color = new Color(0.25f, 0.45f, 0.7f);
            var watchRect = watchImg.rectTransform;
            watchRect.anchorMin = new Vector2(0.80f, 0.12f);
            watchRect.anchorMax = new Vector2(0.97f, 0.88f);
            watchRect.offsetMin = Vector2.zero;
            watchRect.offsetMax = Vector2.zero;

            var watchBtn = watchBtnObj.AddComponent<Button>();
            watchBtn.targetGraphic = watchImg;
            var wc = watchBtn.colors;
            wc.highlightedColor = new Color(0.35f, 0.55f, 0.85f);
            wc.pressedColor = new Color(0.15f, 0.3f, 0.5f);
            watchBtn.colors = wc;
            string gid = gameId;
            string gm = gameMode;
            watchBtn.onClick.AddListener(() => OnWatchGameClicked(gid, gm));

            GameObject watchTextObj = new GameObject("WText");
            watchTextObj.transform.SetParent(watchBtnObj.transform, false);
            var watchText = watchTextObj.AddComponent<TextMeshProUGUI>();
            watchText.text = "Watch";
            watchText.fontSize = 15;
            watchText.alignment = TextAlignmentOptions.Center;
            watchText.color = Color.white;
            var wrt = watchText.rectTransform;
            wrt.anchorMin = Vector2.zero;
            wrt.anchorMax = Vector2.one;
            wrt.offsetMin = Vector2.zero;
            wrt.offsetMax = Vector2.zero;
        }
    }

    private static TextMeshProUGUI AddTableCell(Transform parent, string text,
        float anchorLeft, float anchorRight, TextAlignmentOptions align, float fontSize)
    {
        var obj = new GameObject("Cell");
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(anchorLeft, 0);
        rt.anchorMax = new Vector2(anchorRight, 1);
        rt.offsetMin = new Vector2(5, 2);
        rt.offsetMax = new Vector2(-2, -2);
        return tmp;
    }

    private void OnWatchGameClicked(string gameId, string mode)
    {
        adminGameListPanel.SetActive(false);
        GameLogger.Instance?.FetchGameReplay(gameId, (fetchedMode, result, actions) =>
        {
            if (actions.Count == 0)
            {
                ShowInfoPanel("No actions recorded for this game.");
                adminGameListPanel.SetActive(true);
                return;
            }
            string replayMode = string.IsNullOrEmpty(fetchedMode) ? mode : fetchedMode;
            GameManager.Instance.StartReplay(actions, replayMode, result);
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
