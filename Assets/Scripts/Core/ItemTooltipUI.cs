using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
            root = gameObject;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponent<Image>();

        SetupVisuals();
        Hide();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf)
            return;

        Vector2 localPoint;
        RectTransform canvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;

        if (canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                out localPoint))
        {
            panelRect.anchoredPosition = localPoint + offset;
            panelRect.SetAsLastSibling();
        }
    }

    public void Show(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
        {
            Hide();
            return;
        }

        if (nameText != null)
            nameText.text = entry.ItemName;

        if (statsText != null)
            statsText.text = BuildStatsText(entry.StatBonus);

        SetupVisuals();
        root.SetActive(true);
        panelRect.SetAsLastSibling();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void SetupVisuals()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        }

        if (nameText != null)
        {
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.margin = new Vector4(10f, 10f, 10f, 0f);
        }

        if (statsText != null)
        {
            statsText.color = Color.white;
            statsText.raycastTarget = false;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.textWrappingMode = TextWrappingModes.Normal;
            statsText.margin = new Vector4(10f, 30f, 10f, 10f);
        }
    }

    private string BuildStatsText(StatBlock stats)
    {
        if (stats == null)
            return "";

        string text = "";

        if (stats.hp != 0) text += $"HP: {stats.hp}\n";
        if (stats.atk != 0) text += $"ATK: {stats.atk}\n";
        if (stats.def != 0) text += $"DEF: {stats.def}\n";
        if (stats.ap != 0) text += $"AP: {stats.ap}\n";
        if (Mathf.Abs(stats.crit) > 0.001f) text += $"CRIT: {stats.crit:0.#}%\n";

        if (string.IsNullOrWhiteSpace(text))
            text = "No bonus";

        return text.TrimEnd();
    }
}