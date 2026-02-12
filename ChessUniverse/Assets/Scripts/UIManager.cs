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
    private GameObject seedButtonsPanel;

    private void Awake()
    {
        Instance = this;
        CreateUI();
    }

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("UICanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
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

        // Seed Chess panels
        CreateMainMenuPanel(canvasObj);
        CreateSeedButtonsPanel(canvasObj);
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

        // Classic Chess button
        CreateButton(mainMenuPanel, "ClassicBtn", "Classic Chess", new Vector2(0, 20),
            () => GameManager.Instance.StartGame(GameMode.Classic), 300);

        // Seed Chess button
        CreateButton(mainMenuPanel, "SeedBtn", "Seed Chess", new Vector2(0, -50),
            () => GameManager.Instance.StartGame(GameMode.SeedChess), 300);

        mainMenuPanel.SetActive(false);
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

    private void CreateButton(GameObject parent, string name, string label, Vector2 position,
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
        mainMenuPanel.SetActive(true);
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
}
