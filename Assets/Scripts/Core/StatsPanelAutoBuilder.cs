using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class StatsPanelAutoBuilder : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform panelRoot;

    [Header("Controller")]
    [SerializeField] private StatsPanelUI statsPanelUI;

    [Header("Style")]
    [SerializeField] private Vector2 panelSize = new Vector2(320f, 420f);
    [SerializeField] private Vector2 panelPosition = new Vector2(180f, -220f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Vector2 rowSize = new Vector2(280f, 36f);
    [SerializeField] private Vector2 buttonSize = new Vector2(80f, 32f);
    [SerializeField] private float spacing = 8f;
    [SerializeField] private bool rebuildOnStart = true;

    private void Start()
    {
        if (rebuildOnStart)
            Build();
    }

    [ContextMenu("Build UI Layout")]
    public void Build()
    {
        if (panelRoot == null)
            panelRoot = GetComponent<RectTransform>();

        if (panelRoot == null)
            return;

        EnsurePanelVisual();
        ClearChildren(panelRoot);

        VerticalLayoutGroup rootLayout = EnsureComponent<VerticalLayoutGroup>(panelRoot.gameObject);
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.spacing = spacing;
        rootLayout.padding = new RectOffset(12, 12, 12, 12);

        ContentSizeFitter rootFitter = EnsureComponent<ContentSizeFitter>(panelRoot.gameObject);
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panelRoot.anchorMin = new Vector2(0f, 1f);
        panelRoot.anchorMax = new Vector2(0f, 1f);
        panelRoot.pivot = new Vector2(0.5f, 1f);
        panelRoot.anchoredPosition = panelPosition;
        panelRoot.sizeDelta = panelSize;

        TextMeshProUGUI nameText = CreateText(panelRoot, "NameText", "Character");
        TextMeshProUGUI levelText = CreateText(panelRoot, "LevelText", "Level: 1");
        TextMeshProUGUI xpText = CreateText(panelRoot, "XPText", "XP: 0 / 10");
        TextMeshProUGUI pointsText = CreateText(panelRoot, "PointsText", "Points: 0");

        StatRowRefs hp = CreateStatRow("HP", panelRoot);
        StatRowRefs atk = CreateStatRow("ATK", panelRoot);
        StatRowRefs def = CreateStatRow("DEF", panelRoot);
        StatRowRefs ap = CreateStatRow("AP", panelRoot);
        StatRowRefs crit = CreateStatRow("CRIT", panelRoot);

        WireController(
            nameText, levelText, xpText, pointsText,
            hp, atk, def, ap, crit
        );
    }

    private void WireController(
        TextMeshProUGUI nameText,
        TextMeshProUGUI levelText,
        TextMeshProUGUI xpText,
        TextMeshProUGUI pointsText,
        StatRowRefs hp,
        StatRowRefs atk,
        StatRowRefs def,
        StatRowRefs ap,
        StatRowRefs crit)
    {
        if (statsPanelUI == null)
            statsPanelUI = FindFirstObjectByType<StatsPanelUI>();

        if (statsPanelUI == null)
        {
            Debug.LogWarning("StatsPanelAutoBuilder: não encontrou StatsPanelUI na cena.");
            return;
        }

        statsPanelUI.ConfigureReferences(
            panelRoot.gameObject,
            nameText, levelText, xpText, pointsText,
            hp.valueText, atk.valueText, def.valueText, ap.valueText, crit.valueText,
            hp.button, atk.button, def.button, ap.button, crit.button
        );
    }

    private void EnsurePanelVisual()
    {
        Image image = EnsureComponent<Image>(panelRoot.gameObject);
        image.color = panelColor;
    }

    private StatRowRefs CreateStatRow(string statName, RectTransform parent)
    {
        GameObject row = new GameObject($"{statName}Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = rowSize;

        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(row);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 2, 2);

        ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(row);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI valueText = CreateText(rowRect, $"{statName}ValueText", $"{statName}: 0");
        Button button = CreateButton(rowRect, $"{statName}Button", $"+ {statName}");

        return new StatRowRefs
        {
            valueText = valueText,
            button = button
        };
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string objectName, string textValue)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = rowSize;

        LayoutElement layout = EnsureComponent<LayoutElement>(go);
        layout.minHeight = 30f;
        layout.preferredHeight = 34f;
        layout.flexibleWidth = 1f;

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(go);
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return text;
    }

    private Button CreateButton(RectTransform parent, string objectName, string label)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = buttonSize;

        LayoutElement layout = EnsureComponent<LayoutElement>(go);
        layout.minWidth = buttonSize.x;
        layout.preferredWidth = buttonSize.x;
        layout.minHeight = buttonSize.y;
        layout.preferredHeight = buttonSize.y;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = EnsureComponent<TextMeshProUGUI>(textGO);
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    private class StatRowRefs
    {
        public TextMeshProUGUI valueText;
        public Button button;
    }
}