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
    [SerializeField] private Vector2 windowSize = new Vector2(1080f, 720f);
    [SerializeField] private Color windowColor = new Color(0.08f, 0.08f, 0.08f, 0.94f);

    [Header("Panels")]
    [SerializeField] private Color panelColor = new Color(0.14f, 0.14f, 0.14f, 0.96f);

    [Header("Slots")]
    [SerializeField] private Vector2 slotSize = new Vector2(92f, 92f);
    [SerializeField] private Vector2 equipmentSlotSize = new Vector2(110f, 110f);
    [SerializeField] private Vector2 gridSpacing = new Vector2(8f, 8f);

    [Header("Text")]
    [SerializeField] private int titleFontSize = 30;
    [SerializeField] private int hintFontSize = 18;
    [SerializeField] private int headerFontSize = 22;

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
            new Vector2(20f, -10f),
            new Vector2(-80f, -52f),
            "Inventory",
            titleFontSize,
            TextAlignmentOptions.Left
        );

        TextMeshProUGUI hintText = CreateText(
            "HintText",
            windowRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(20f, -56f),
            new Vector2(-20f, -92f),
            "E abre/fecha | Esc fecha | Click chão -> mochila | Shift+Click chão -> equipar | Click mochila -> equipar | Click equipado -> mochila",
            hintFontSize,
            TextAlignmentOptions.Left
        );

        Button closeButton = CreateButton(
            "CloseButton",
            windowRoot,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-60f, -14f),
            new Vector2(-14f, -58f),
            "X"
        );

        RectTransform equippedPanel = CreatePanel(
            "EquippedPanel",
            windowRoot,
            new Vector2(0f, 0f),
            new Vector2(0.22f, 1f),
            new Vector2(20f, 20f),
            new Vector2(-10f, -110f)
        );

        RectTransform inventoryPanel = CreatePanel(
            "InventoryPanel",
            windowRoot,
            new Vector2(0.22f, 0f),
            new Vector2(0.61f, 1f),
            new Vector2(10f, 20f),
            new Vector2(-10f, -110f)
        );

        RectTransform groundPanel = CreatePanel(
            "GroundLootPanel",
            windowRoot,
            new Vector2(0.61f, 0f),
            new Vector2(1f, 1f),
            new Vector2(10f, 20f),
            new Vector2(-20f, -110f)
        );

        CreateSectionHeader("EquippedHeader", equippedPanel, "Equipped");
        CreateSectionHeader("InventoryHeader", inventoryPanel, "Inventory");
        CreateSectionHeader("GroundHeader", groundPanel, "Ground Loot");

        RectTransform equippedContent = CreateEquipmentContent("EquippedContent", equippedPanel);
        RectTransform inventoryContent = CreateGridContent("InventoryContent", inventoryPanel, 4);
        RectTransform groundContent = CreateGridContent("GroundLootContent", groundPanel, 4);

        if (lootWindowUI != null)
        {
            lootWindowUI.ConfigureReferences(
                windowRoot.gameObject,
                closeButton,
                equippedContent,
                inventoryContent,
                groundContent,
                titleText,
                hintText
            );

            lootWindowUI.SetItemButtonPrefab(itemButtonPrefab);
        }
    }

    private void EnsureRootVisual()
    {
        Image image = GetOrAdd<Image>(windowRoot.gameObject);
        image.color = windowColor;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = go.GetComponent<Image>();
        image.color = panelColor;

        return rect;
    }

    private void CreateSectionHeader(string name, RectTransform parent, string text)
    {
        CreateText(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(10f, -10f),
            new Vector2(-10f, -40f),
            text,
            headerFontSize,
            TextAlignmentOptions.Left
        );
    }

    private RectTransform CreateEquipmentContent(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(12f, 12f);
        rect.offsetMax = new Vector2(-12f, -50f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(go);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(go);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    private RectTransform CreateGridContent(string name, RectTransform parent, int constraintCount)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(12f, 12f);
        rect.offsetMax = new Vector2(-12f, -50f);

        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(go);
        grid.cellSize = slotSize;
        grid.spacing = gridSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = constraintCount;

        return rect;
    }

    private TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string content,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(go);
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        return tmp;
    }

    private Button CreateButton(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(textGO);
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }
}