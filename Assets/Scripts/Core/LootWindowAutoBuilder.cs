using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class LootWindowAutoBuilder : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform windowRoot;

    [Header("Controller")]
    [SerializeField] private LootWindowUI lootWindowUI;

    [Header("Prefab")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Style")]
    [SerializeField] private Vector2 windowSize = new Vector2(1100f, 620f);
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private int titleFontSize = 30;
    [SerializeField] private int hintFontSize = 18;
    [SerializeField] private int headerFontSize = 22;
    [SerializeField] private bool rebuildOnStart = true;

    private void Start()
    {
        if (rebuildOnStart)
            Build();
    }

    [ContextMenu("Build Loot Window Layout")]
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
            new Vector2(-80f, -50f),
            "Loot",
            titleFontSize,
            TextAlignmentOptions.Left
        );

        TextMeshProUGUI hintText = CreateText(
            "HintText",
            windowRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(20f, -55f),
            new Vector2(-20f, -90f),
            "Click chão -> mochila | Shift+Click chão -> equipar | Click mochila -> equipar | Click equipado -> mochila",
            hintFontSize,
            TextAlignmentOptions.Left
        );

        Button closeButton = CreateButton(
            "CloseButton",
            windowRoot,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-60f, -15f),
            new Vector2(-15f, -60f),
            "X"
        );

        RectTransform equippedPanel = CreatePanel(
            "EquippedPanel",
            windowRoot,
            new Vector2(0f, 0f),
            new Vector2(0.28f, 1f),
            new Vector2(20f, 20f),
            new Vector2(-10f, -110f)
        );

        RectTransform inventoryPanel = CreatePanel(
            "InventoryPanel",
            windowRoot,
            new Vector2(0.28f, 0f),
            new Vector2(0.72f, 1f),
            new Vector2(10f, 20f),
            new Vector2(-10f, -110f)
        );

        RectTransform groundPanel = CreatePanel(
            "GroundLootPanel",
            windowRoot,
            new Vector2(0.72f, 0f),
            new Vector2(1f, 1f),
            new Vector2(10f, 20f),
            new Vector2(-20f, -110f)
        );

        CreateSectionHeader("EquippedHeader", equippedPanel, "Equipped");
        CreateSectionHeader("InventoryHeader", inventoryPanel, "Inventory");
        CreateSectionHeader("GroundHeader", groundPanel, "Ground Loot");

        RectTransform equippedContent = CreateContentRoot("EquippedContent", equippedPanel);
        RectTransform inventoryContent = CreateContentRoot("InventoryContent", inventoryPanel);
        RectTransform groundContent = CreateContentRoot("GroundLootContent", groundPanel);

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
        image.color = backgroundColor;
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
        image.color = new Color(0.14f, 0.14f, 0.14f, 0.95f);

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

    private RectTransform CreateContentRoot(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(10f, 10f);
        rect.offsetMax = new Vector2(-10f, -50f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(go);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;
        layout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(go);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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