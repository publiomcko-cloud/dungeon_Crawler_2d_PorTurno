using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class LootWindowGridAutoBuilder : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform windowRoot;

    [Header("Controller")]
    [SerializeField] private LootWindowUI lootWindowUI;

    [Header("Prefab")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Window")]
    [SerializeField] private Vector2 windowSize = new Vector2(760f, 430f);
    [SerializeField] private Color windowColor = new Color(0.08f, 0.08f, 0.08f, 0.96f);

    [Header("Panels")]
    [SerializeField] private Color panelColor = new Color(0.14f, 0.14f, 0.14f, 0.96f);

    [Header("Layout")]
    [SerializeField] private float topBarHeight = 56f;
    [SerializeField] private float outerPadding = 10f;
    [SerializeField] private float panelSpacing = 10f;

    [Header("Panel Widths")]
    [SerializeField] private float selectorPanelWidth = 84f;
    [SerializeField] private float equippedPanelWidth = 90f;
    [SerializeField] private float inventoryPanelWidth = 250f;
    [SerializeField] private float groundPanelWidth = 250f;

    [Header("Slots")]
    [SerializeField] private Vector2 slotSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 gridSpacing = new Vector2(4f, 4f);

    [Header("Text")]
    [SerializeField] private int titleFontSize = 18;
    [SerializeField] private int hintFontSize = 10;
    [SerializeField] private int headerFontSize = 13;

    [Header("Build")]
    [SerializeField] private bool rebuildOnStart = true;

    private void Start()
    {
        if (rebuildOnStart)
            Build();
    }

    [ContextMenu("Build Loot Grid Layout")]
    public void Build()
    {
        if (windowRoot == null)
            windowRoot = GetComponent<RectTransform>();

        if (lootWindowUI == null)
            lootWindowUI = GetComponent<LootWindowUI>();

        if (windowRoot == null)
            return;

        EnsureRootVisual();
        ClearChildren(windowRoot);

        windowRoot.anchorMin = new Vector2(0.5f, 0.5f);
        windowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        windowRoot.pivot = new Vector2(0.5f, 0.5f);
        windowRoot.anchoredPosition = Vector2.zero;
        windowRoot.sizeDelta = windowSize;

        TextMeshProUGUI titleText = CreateText(
            "TitleText",
            windowRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(12f, -6f),
            new Vector2(-48f, -26f),
            "Inventory",
            titleFontSize,
            TextAlignmentOptions.Left
        );

        TextMeshProUGUI hintText = CreateText(
            "HintText",
            windowRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(12f, -28f),
            new Vector2(-12f, -50f),
            "Party | E abre/fecha | Esc fecha",
            hintFontSize,
            TextAlignmentOptions.Left
        );

        Button closeButton = CreateButton(
            "CloseButton",
            windowRoot,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-34f, -6f),
            new Vector2(-6f, -30f),
            "X",
            14
        );

        Rect contentArea = CalculateContentArea();

        float x = contentArea.xMin;

        RectTransform selectorPanel = CreatePanel(
            "SelectorPanel",
            windowRoot,
            x,
            contentArea.center.y,
            selectorPanelWidth,
            contentArea.height
        );
        x += selectorPanelWidth + panelSpacing;

        RectTransform equippedPanel = CreatePanel(
            "EquippedPanel",
            windowRoot,
            x,
            contentArea.center.y,
            equippedPanelWidth,
            contentArea.height
        );
        x += equippedPanelWidth + panelSpacing;

        RectTransform inventoryPanel = CreatePanel(
            "InventoryPanel",
            windowRoot,
            x,
            contentArea.center.y,
            inventoryPanelWidth,
            contentArea.height
        );
        x += inventoryPanelWidth + panelSpacing;

        RectTransform groundPanel = CreatePanel(
            "GroundPanel",
            windowRoot,
            x,
            contentArea.center.y,
            groundPanelWidth,
            contentArea.height
        );

        CreatePanelHeader("SelectorHeader", selectorPanel, "Party");
        CreatePanelHeader("EquippedHeader", equippedPanel, "Equipped");
        CreatePanelHeader("InventoryHeader", inventoryPanel, "Inventory");
        CreatePanelHeader("GroundHeader", groundPanel, "Ground");

        RectTransform selectorContent = CreateContentRoot(
            "SelectorContent",
            selectorPanel,
            new Vector2(6f, 6f),
            new Vector2(-6f, -24f)
        );

        VerticalLayoutGroup selectorLayout = EnsureComponent<VerticalLayoutGroup>(selectorContent.gameObject);
        selectorLayout.childAlignment = TextAnchor.UpperCenter;
        selectorLayout.childControlWidth = true;
        selectorLayout.childControlHeight = false;
        selectorLayout.childForceExpandWidth = true;
        selectorLayout.childForceExpandHeight = false;
        selectorLayout.spacing = 6f;
        selectorLayout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter selectorFitter = EnsureComponent<ContentSizeFitter>(selectorContent.gameObject);
        selectorFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        selectorFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform equippedContent = CreateContentRoot(
            "EquippedContent",
            equippedPanel,
            new Vector2(8f, 8f),
            new Vector2(-8f, -26f)
        );

        VerticalLayoutGroup equippedLayout = EnsureComponent<VerticalLayoutGroup>(equippedContent.gameObject);
        equippedLayout.childAlignment = TextAnchor.UpperCenter;
        equippedLayout.childControlWidth = false;
        equippedLayout.childControlHeight = false;
        equippedLayout.childForceExpandWidth = false;
        equippedLayout.childForceExpandHeight = false;
        equippedLayout.spacing = 6f;
        equippedLayout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter equippedFitter = EnsureComponent<ContentSizeFitter>(equippedContent.gameObject);
        equippedFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        equippedFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform inventoryContent = CreateContentRoot(
            "InventoryContent",
            inventoryPanel,
            new Vector2(8f, 8f),
            new Vector2(-8f, -26f)
        );

        GridLayoutGroup inventoryGrid = EnsureComponent<GridLayoutGroup>(inventoryContent.gameObject);
        inventoryGrid.cellSize = slotSize;
        inventoryGrid.spacing = gridSpacing;
        inventoryGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        inventoryGrid.constraintCount = 4;
        inventoryGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        inventoryGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        inventoryGrid.childAlignment = TextAnchor.UpperLeft;

        RectTransform groundContent = CreateContentRoot(
            "GroundLootContent",
            groundPanel,
            new Vector2(8f, 8f),
            new Vector2(-8f, -26f)
        );

        GridLayoutGroup groundGrid = EnsureComponent<GridLayoutGroup>(groundContent.gameObject);
        groundGrid.cellSize = slotSize;
        groundGrid.spacing = gridSpacing;
        groundGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        groundGrid.constraintCount = 4;
        groundGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        groundGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        groundGrid.childAlignment = TextAnchor.UpperLeft;

        WireController(
            closeButton,
            selectorContent,
            equippedContent,
            inventoryContent,
            groundContent,
            titleText,
            hintText
        );
    }

    private Rect CalculateContentArea()
    {
        float width = windowSize.x - outerPadding * 2f;
        float height = windowSize.y - topBarHeight - outerPadding;
        float xMin = -windowSize.x * 0.5f + outerPadding;
        float yMin = -windowSize.y * 0.5f + outerPadding;

        return new Rect(xMin, yMin, width, height);
    }

    private void CreatePanelHeader(string objectName, RectTransform parent, string textValue)
    {
        CreateText(
            objectName,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(4f, -4f),
            new Vector2(-4f, -22f),
            textValue,
            headerFontSize,
            TextAlignmentOptions.Center
        );
    }

    private void WireController(
        Button closeButton,
        Transform selectorContent,
        Transform equippedContent,
        Transform inventoryContent,
        Transform groundContent,
        TMP_Text titleText,
        TMP_Text hintText)
    {
        if (lootWindowUI == null)
            lootWindowUI = FindFirstObjectByType<LootWindowUI>();

        if (lootWindowUI == null)
        {
            Debug.LogWarning("LootWindowGridAutoBuilder: não encontrou LootWindowUI na cena.");
            return;
        }

        lootWindowUI.ConfigureReferences(
            windowRoot.gameObject,
            closeButton,
            selectorContent,
            equippedContent,
            inventoryContent,
            groundContent,
            titleText,
            hintText
        );

        if (itemButtonPrefab != null)
            lootWindowUI.SetItemButtonPrefab(itemButtonPrefab);
        else
            Debug.LogWarning("LootWindowGridAutoBuilder: ItemButtonPrefab não está preenchido.");
    }

    private void EnsureRootVisual()
    {
        Image image = EnsureComponent<Image>(windowRoot.gameObject);
        image.color = windowColor;
    }

    private RectTransform CreatePanel(
        string objectName,
        RectTransform parent,
        float leftX,
        float centerY,
        float width,
        float height)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(leftX, centerY);
        rect.sizeDelta = new Vector2(width, height);

        Image image = go.GetComponent<Image>();
        image.color = panelColor;

        return rect;
    }

    private RectTransform CreateContentRoot(
        string objectName,
        RectTransform parent,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        return rect;
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string textValue,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(go);
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;

        return text;
    }

    private Button CreateButton(
        string objectName,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string label,
        int fontSize)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(textGO);
        text.text = label;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }
}