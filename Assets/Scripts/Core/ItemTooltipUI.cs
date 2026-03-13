using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance;

    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;

    [Header("Follow")]
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    [Header("Auto Size")]
    [SerializeField] private float minWidth = 180f;
    [SerializeField] private float maxWidth = 360f;
    [SerializeField] private float topPadding = 10f;
    [SerializeField] private float bottomPadding = 10f;
    [SerializeField] private float leftPadding = 10f;
    [SerializeField] private float rightPadding = 10f;
    [SerializeField] private float spacingBetweenNameAndStats = 8f;

    [Header("Visual")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.96f);
    [SerializeField] private bool tintBackgroundByRarity = true;
    [SerializeField] private float rarityBackgroundTint = 0.18f;

    [Header("Delta Colors")]
    [SerializeField] private string positiveHex = "#57D66B";
    [SerializeField] private string negativeHex = "#FF6B6B";
    [SerializeField] private string neutralHex = "#CFCFCF";

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

        UpdatePosition();
    }

    public void Show(InventoryItemEntry entry)
    {
        Show(entry, null);
    }

    public void Show(InventoryItemEntry entry, InventoryItemEntry compareEntry)
    {
        if (entry == null || entry.IsEmpty)
        {
            Hide();
            return;
        }

        SetupVisuals();
        ApplyRarityVisual(entry);

        if (nameText != null)
            nameText.text = BuildNameLine(entry);

        if (statsText != null)
            statsText.text = BuildFullTooltipText(entry, compareEntry);

        root.SetActive(true);

        ResizeToText();
        UpdatePosition();
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
            backgroundImage.color = backgroundColor;
        }

        if (nameText != null)
        {
            nameText.raycastTarget = false;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;
        }

        if (statsText != null)
        {
            statsText.raycastTarget = false;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.textWrappingMode = TextWrappingModes.Normal;
            statsText.enableWordWrapping = true;
            statsText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void ApplyRarityVisual(InventoryItemEntry entry)
    {
        Color rarityColor = GetRarityColor(entry.Rarity);

        if (nameText != null)
            nameText.color = rarityColor;

        if (statsText != null)
            statsText.color = Color.white;

        if (backgroundImage != null)
        {
            if (tintBackgroundByRarity)
                backgroundImage.color = Color.Lerp(backgroundColor, rarityColor, rarityBackgroundTint);
            else
                backgroundImage.color = backgroundColor;

            backgroundImage.color = new Color(
                backgroundImage.color.r,
                backgroundImage.color.g,
                backgroundImage.color.b,
                backgroundColor.a
            );
        }
    }

    private string BuildNameLine(InventoryItemEntry entry)
    {
        return $"{entry.ItemName} [{entry.Rarity}]";
    }

    private string BuildFullTooltipText(InventoryItemEntry entry, InventoryItemEntry compareEntry)
    {
        string text = "";

        text += $"Slot: {entry.SlotType}\n";
        text += $"Req Lv: {entry.RequiredLevel}\n";

        if (!string.IsNullOrWhiteSpace(entry.Description))
            text += $"Desc: {entry.Description}\n";

        if (entry.Value > 0)
            text += $"Value: {entry.Value}\n";

        text += "\nItem\n";
        text += BuildStatsText(entry.StatBonus);

        bool canCompare =
            compareEntry != null &&
            !compareEntry.IsEmpty &&
            compareEntry.SlotType == entry.SlotType &&
            compareEntry.ItemName != entry.ItemName;

        if (canCompare)
        {
            text += "\n\nEquipped\n";
            text += BuildStatsText(compareEntry.StatBonus);

            text += "\n\nDelta\n";
            text += BuildDeltaText(entry.StatBonus, compareEntry.StatBonus);
        }

        return text.TrimEnd();
    }

    private string BuildStatsText(StatBlock stats)
    {
        if (stats == null)
            return "No bonus";

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

    private string BuildDeltaText(StatBlock candidate, StatBlock equipped)
    {
        StatBlock a = candidate ?? new StatBlock();
        StatBlock b = equipped ?? new StatBlock();

        int hp = a.hp - b.hp;
        int atk = a.atk - b.atk;
        int def = a.def - b.def;
        int ap = a.ap - b.ap;
        float crit = a.crit - b.crit;

        string text = "";

        if (hp != 0) text += $"HP: {FormatSignedColored(hp)}\n";
        if (atk != 0) text += $"ATK: {FormatSignedColored(atk)}\n";
        if (def != 0) text += $"DEF: {FormatSignedColored(def)}\n";
        if (ap != 0) text += $"AP: {FormatSignedColored(ap)}\n";
        if (Mathf.Abs(crit) > 0.001f) text += $"CRIT: {FormatSignedColored(crit)}%\n";

        if (string.IsNullOrWhiteSpace(text))
            text = $"<color={neutralHex}>No change</color>";

        return text.TrimEnd();
    }

    private string FormatSignedColored(int value)
    {
        string color = value > 0 ? positiveHex : negativeHex;
        string sign = value > 0 ? "+" : "";
        return $"<color={color}>{sign}{value}</color>";
    }

    private string FormatSignedColored(float value)
    {
        string color = value > 0f ? positiveHex : negativeHex;
        string sign = value > 0f ? "+" : "";
        return $"<color={color}>{sign}{value:0.#}</color>";
    }

    private void ResizeToText()
    {
        if (panelRect == null || nameText == null || statsText == null)
            return;

        RectTransform nameRect = nameText.rectTransform;
        RectTransform statsRect = statsText.rectTransform;

        float innerMaxWidth = Mathf.Max(60f, maxWidth - leftPadding - rightPadding);

        nameText.enableWordWrapping = false;
        statsText.enableWordWrapping = true;

        nameText.ForceMeshUpdate();
        statsText.ForceMeshUpdate();

        Vector2 namePreferred = nameText.GetPreferredValues(nameText.text, innerMaxWidth, 0f);
        Vector2 statsPreferred = statsText.GetPreferredValues(statsText.text, innerMaxWidth, 0f);

        float contentWidth = Mathf.Max(namePreferred.x, statsPreferred.x);
        float finalWidth = Mathf.Clamp(contentWidth + leftPadding + rightPadding, minWidth, maxWidth);

        float finalInnerWidth = finalWidth - leftPadding - rightPadding;

        namePreferred = nameText.GetPreferredValues(nameText.text, finalInnerWidth, 0f);
        statsPreferred = statsText.GetPreferredValues(statsText.text, finalInnerWidth, 0f);

        float finalHeight =
            topPadding +
            namePreferred.y +
            spacingBetweenNameAndStats +
            statsPreferred.y +
            bottomPadding;

        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
        panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(leftPadding, -topPadding);
        nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalInnerWidth);
        nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, namePreferred.y);

        statsRect.anchorMin = new Vector2(0f, 1f);
        statsRect.anchorMax = new Vector2(1f, 1f);
        statsRect.pivot = new Vector2(0f, 1f);
        statsRect.anchoredPosition = new Vector2(leftPadding, -(topPadding + namePreferred.y + spacingBetweenNameAndStats));
        statsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalInnerWidth);
        statsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, statsPreferred.y);
    }

    private void UpdatePosition()
    {
        Vector2 localPoint;
        RectTransform canvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;

        if (canvasRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out localPoint))
        {
            Vector2 desired = localPoint + offset;

            float halfWidth = panelRect.rect.width * 0.5f;
            float halfHeight = panelRect.rect.height * 0.5f;

            float minX = -canvasRect.rect.width * 0.5f + halfWidth;
            float maxX = canvasRect.rect.width * 0.5f - halfWidth;
            float minY = -canvasRect.rect.height * 0.5f + halfHeight;
            float maxY = canvasRect.rect.height * 0.5f - halfHeight;

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);

            panelRect.anchoredPosition = desired;
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return new Color(0.85f, 0.85f, 0.85f, 1f);
            case ItemRarity.Uncommon:
                return new Color(0.35f, 0.9f, 0.35f, 1f);
            case ItemRarity.Rare:
                return new Color(0.35f, 0.6f, 1f, 1f);
            case ItemRarity.Epic:
                return new Color(0.8f, 0.4f, 1f, 1f);
            case ItemRarity.Legendary:
                return new Color(1f, 0.72f, 0.2f, 1f);
            default:
                return Color.white;
        }
    }
}