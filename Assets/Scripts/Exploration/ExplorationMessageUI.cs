using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplorationMessageUI : MonoBehaviour
{
    private static Sprite runtimeWhiteSprite;

    public static ExplorationMessageUI Instance;

    [Header("Optional References")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Style")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Vector2 panelSize = new Vector2(520f, 64f);
    [SerializeField] private int canvasSortingOrder = 540;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float defaultDuration = 2.2f;

    private float hideAtTime = -1f;

    public static ExplorationMessageUI GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        ExplorationMessageUI existing = FindFirstObjectByType<ExplorationMessageUI>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("UI_ExplorationMessage");
        return go.AddComponent<ExplorationMessageUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
        HideImmediate();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf)
            return;

        if (hideAtTime > 0f && Time.unscaledTime >= hideAtTime)
            HideImmediate();

        if (canvasGroup != null && hideAtTime > 0f)
        {
            float remaining = hideAtTime - Time.unscaledTime;
            if (remaining <= fadeDuration)
                canvasGroup.alpha = Mathf.Clamp01(remaining / Mathf.Max(0.01f, fadeDuration));
        }
    }

    public void ShowMessage(string message, float duration = -1f)
    {
        EnsureUi();

        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();

        if (root != null)
            root.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        float finalDuration = duration > 0f ? duration : defaultDuration;
        hideAtTime = Time.unscaledTime + finalDuration;
    }

    public void HideImmediate()
    {
        hideAtTime = -1f;

        if (root != null)
            root.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void EnsureUi()
    {
        EnsureCanvas();

        if (root != null)
            return;

        root = BuildUi(uiCanvas.transform);
    }

    private void EnsureCanvas()
    {
        if (uiCanvas == null)
        {
            GameObject existing = GameObject.Find("ExplorationMessageCanvas");
            if (existing != null)
                uiCanvas = existing.GetComponent<Canvas>();
        }

        if (uiCanvas == null)
        {
            GameObject canvasGo = new GameObject("ExplorationMessageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiCanvas = canvasGo.GetComponent<Canvas>();
        }

        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.overrideSorting = true;
        uiCanvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = uiCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = uiCanvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
    }

    private GameObject BuildUi(Transform parent)
    {
        GameObject panel = new GameObject("ExplorationMessagePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 0.8f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = panelSize;

        Image image = panel.GetComponent<Image>();
        image.sprite = GetRuntimeWhiteSprite();
        image.type = Image.Type.Simple;
        image.color = backgroundColor;

        canvasGroup = panel.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject textGo = new GameObject("MessageText", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        messageText = textGo.AddComponent<TextMeshProUGUI>();
        messageText.fontSize = 22;
        messageText.color = textColor;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.raycastTarget = false;
        messageText.text = string.Empty;

        return panel;
    }

    private static Sprite GetRuntimeWhiteSprite()
    {
        if (runtimeWhiteSprite != null)
            return runtimeWhiteSprite;

        Texture2D texture = Texture2D.whiteTexture;
        runtimeWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        runtimeWhiteSprite.name = "ExplorationMessageUI_WhiteSprite";
        return runtimeWhiteSprite;
    }
}
