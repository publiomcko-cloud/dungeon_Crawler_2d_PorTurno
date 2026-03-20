using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChestInteractionUI : MonoBehaviour
{
    private static Sprite runtimeWhiteSprite;

    public static ChestInteractionUI Instance;

    [Header("Optional References")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text chestHeaderText;
    [SerializeField] private TMP_Text inventoryHeaderText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text closeButtonText;
    [SerializeField] private RectTransform chestPanelRoot;
    [SerializeField] private RectTransform inventoryPanelRoot;
    [SerializeField] private Transform chestGridRoot;
    [SerializeField] private Transform inventoryGridRoot;
    [SerializeField] private ScrollRect chestScrollRect;
    [SerializeField] private ScrollRect inventoryScrollRect;

    [Header("Prefabs")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Window")]
    [SerializeField] private Vector2 windowScreenOccupancy = new Vector2(0.8f, 0.8f);
    [SerializeField] private Color windowBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.97f);
    [SerializeField] private Color panelColor = new Color(0.14f, 0.14f, 0.14f, 0.96f);
    [SerializeField] private int canvasSortingOrder = 520;
    [SerializeField] private float scrollbarWidth = 14f;

    [Header("Slot Layout")]
    [SerializeField] private Vector2 slotSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 gridSpacing = new Vector2(6f, 6f);
    [SerializeField] private int chestColumns = 4;
    [SerializeField] private int inventoryColumns = 4;

    [Header("Input")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private readonly List<GameObject> spawnedButtons = new List<GameObject>();

    private ChestActor currentChest;
    private Entity currentInteractor;

    public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

    public static ChestInteractionUI GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        ChestInteractionUI existing = FindFirstObjectByType<ChestInteractionUI>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("UI_ChestInteraction");
        return go.AddComponent<ChestInteractionUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureWindow();
        Close();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(closeKey))
            Close();
    }

    public void Open(ChestActor chest, Entity interactor, string initialStatus)
    {
        if (chest == null)
            return;

        EnsureWindow();
        currentChest = chest;
        currentInteractor = interactor;

        if (windowRoot != null)
            windowRoot.SetActive(true);

        Refresh(initialStatus);
    }

    public void Close()
    {
        currentChest = null;
        currentInteractor = null;
        ClearButtons();
        HideTooltip();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void Refresh(string statusMessage = "")
    {
        if (currentChest == null)
            return;

        EnsureItemButtonPrefab();
        ClearButtons();
        HideTooltip();

        if (titleText != null)
            titleText.text = "Bau";

        if (hintText != null)
            hintText.text = "Clique no item do bau para levar. Clique no item da mochila para guardar.";

        if (statusText != null)
            statusText.text = statusMessage ?? string.Empty;

        if (chestHeaderText != null)
            chestHeaderText.text = "Bau";

        if (inventoryHeaderText != null)
            inventoryHeaderText.text = "Mochila da Party";

        if (closeButtonText != null)
            closeButtonText.text = "Fechar";

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (itemButtonPrefab == null)
        {
            if (statusText != null)
                statusText.text = "ItemButtonPrefab nao encontrado. Verifique o LootWindowGridAutoBuilder.";
            return;
        }

        BuildChestGrid();
        BuildInventoryGrid();

        if (chestScrollRect != null)
            chestScrollRect.verticalNormalizedPosition = 1f;

        if (inventoryScrollRect != null)
            inventoryScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildChestGrid()
    {
        if (chestGridRoot == null || itemButtonPrefab == null || currentChest == null)
            return;

        int itemCount = Mathf.Max(1, currentChest.GetStoredItemCount());
        for (int i = 0; i < itemCount; i++)
        {
            int chestIndex = i;
            InventoryItemEntry entry = currentChest.GetStoredItemAt(chestIndex);
            InventoryItemEntry compareEntry = GetCompareEntry(entry);

            ItemButtonUI button = Instantiate(itemButtonPrefab, chestGridRoot);
            button.ClearContext();
            button.Setup(
                entry != null && !entry.IsEmpty ? entry : null,
                entry != null && !entry.IsEmpty ? () => HandleTakeFromChest(chestIndex) : null,
                null,
                false,
                null,
                compareEntry);

            spawnedButtons.Add(button.gameObject);
        }
    }

    private void BuildInventoryGrid()
    {
        if (inventoryGridRoot == null || itemButtonPrefab == null)
            return;

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
            return;

        for (int i = 0; i < partyInventory.Items.Count; i++)
        {
            int inventoryIndex = i;
            InventoryItemEntry entry = partyInventory.GetItem(inventoryIndex);
            InventoryItemEntry compareEntry = GetCompareEntry(entry);

            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryGridRoot);
            button.ConfigureAsInventorySlot(inventoryIndex);
            button.Setup(
                entry != null && !entry.IsEmpty ? entry : null,
                entry != null && !entry.IsEmpty ? () => HandleStoreInChest(inventoryIndex) : null,
                null,
                false,
                null,
                compareEntry);

            spawnedButtons.Add(button.gameObject);
        }
    }

    private void HandleTakeFromChest(int chestIndex)
    {
        if (currentChest == null)
            return;

        currentChest.TryMoveStoredItemToParty(chestIndex, out string message);
        Refresh(message);
    }

    private void HandleStoreInChest(int inventoryIndex)
    {
        if (currentChest == null)
            return;

        currentChest.TryMovePartyItemToChest(inventoryIndex, out string message);
        Refresh(message);
    }

    private InventoryItemEntry GetCompareEntry(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty || currentInteractor == null)
            return null;

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
            return null;

        return partyInventory.GetEquippedEntry(currentInteractor, entry.SlotType);
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i]);
        }

        spawnedButtons.Clear();
    }

    private void HideTooltip()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private void EnsureWindow()
    {
        EnsureCanvas();
        EnsureItemButtonPrefab();

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        if (windowRoot != null)
            return;

        windowRoot = BuildWindow(uiCanvas.transform);
    }

    private void EnsureCanvas()
    {
        if (uiCanvas == null)
        {
            GameObject existing = GameObject.Find("ChestInteractionCanvas");
            if (existing != null)
                uiCanvas = existing.GetComponent<Canvas>();
        }

        if (uiCanvas == null)
        {
            GameObject canvasGo = new GameObject("ChestInteractionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

    private void EnsureItemButtonPrefab()
    {
        if (itemButtonPrefab != null)
            return;

        LootWindowGridAutoBuilder builder = FindFirstObjectByType<LootWindowGridAutoBuilder>();
        if (builder != null)
        {
            itemButtonPrefab = builder.ItemButtonPrefab;
            if (itemButtonPrefab != null)
                return;
        }

        LootWindowGridAutoBuilder[] allBuilders = Resources.FindObjectsOfTypeAll<LootWindowGridAutoBuilder>();
        for (int i = 0; i < allBuilders.Length; i++)
        {
            LootWindowGridAutoBuilder candidate = allBuilders[i];
            if (candidate == null)
                continue;

            GameObject candidateObject = candidate.gameObject;
            if (candidateObject == null || !candidateObject.scene.IsValid())
                continue;

            if (candidate.ItemButtonPrefab == null)
                continue;

            itemButtonPrefab = candidate.ItemButtonPrefab;
            return;
        }

        ItemButtonUI[] allButtons = Resources.FindObjectsOfTypeAll<ItemButtonUI>();
        for (int i = 0; i < allButtons.Length; i++)
        {
            ItemButtonUI candidate = allButtons[i];
            if (candidate == null)
                continue;

            GameObject candidateObject = candidate.gameObject;
            if (candidateObject == null || !candidateObject.scene.IsValid())
                continue;

            itemButtonPrefab = candidate;
            return;
        }
    }

    private GameObject BuildWindow(Transform parent)
    {
        GameObject root = new GameObject("ChestInteractionWindow", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        float widthRatio = Mathf.Clamp01(windowScreenOccupancy.x);
        float heightRatio = Mathf.Clamp01(windowScreenOccupancy.y);
        float horizontalMargin = (1f - widthRatio) * 0.5f;
        float verticalMargin = (1f - heightRatio) * 0.5f;

        rootRect.anchorMin = new Vector2(horizontalMargin, verticalMargin);
        rootRect.anchorMax = new Vector2(1f - horizontalMargin, 1f - verticalMargin);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        ApplyImageStyle(root.GetComponent<Image>(), windowBackgroundColor);

        titleText = CreateText("TitleText", root.transform, new Vector2(18f, -18f), new Vector2(260f, 28f), 24, TextAlignmentOptions.Left);
        hintText = CreateText("HintText", root.transform, new Vector2(18f, -46f), new Vector2(780f, 22f), 14, TextAlignmentOptions.Left);
        statusText = CreateText("StatusText", root.transform, new Vector2(18f, -76f), new Vector2(900f, 22f), 14, TextAlignmentOptions.Left);
        closeButton = CreateButton("CloseButton", root.transform, new Vector2(-18f, -18f), new Vector2(112f, 32f), out closeButtonText, "Fechar");

        chestPanelRoot = CreatePanel("ChestPanel", root.transform, 0f, 0.49f);
        inventoryPanelRoot = CreatePanel("InventoryPanel", root.transform, 0.51f, 1f);

        chestHeaderText = CreatePanelHeader("ChestHeader", chestPanelRoot, "Bau");
        inventoryHeaderText = CreatePanelHeader("InventoryHeader", inventoryPanelRoot, "Mochila da Party");

        chestScrollRect = CreatePanelScrollRect("ChestScrollRect", chestPanelRoot, out RectTransform chestViewport);
        inventoryScrollRect = CreatePanelScrollRect("InventoryScrollRect", inventoryPanelRoot, out RectTransform inventoryViewport);

        chestGridRoot = CreateScrollableGridRoot("ChestGridRoot", chestViewport, chestColumns, chestScrollRect);
        inventoryGridRoot = CreateScrollableGridRoot("InventoryGridRoot", inventoryViewport, inventoryColumns, inventoryScrollRect);

        return root;
    }

    private RectTransform CreatePanel(string objectName, Transform parent, float anchorMinX, float anchorMaxX)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorMinX, 0f);
        rect.anchorMax = new Vector2(anchorMaxX, 1f);
        rect.offsetMin = new Vector2(18f, 18f);
        rect.offsetMax = new Vector2(-18f, -112f);

        ApplyImageStyle(go.GetComponent<Image>(), panelColor);
        return rect;
    }

    private TMP_Text CreatePanelHeader(string objectName, RectTransform parent, string value)
    {
        TMP_Text text = CreateText(objectName, parent, new Vector2(10f, -8f), new Vector2(280f, 24f), 16, TextAlignmentOptions.Left);
        text.text = value;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private ScrollRect CreatePanelScrollRect(string objectName, RectTransform parent, out RectTransform viewportRect)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = new Vector2(8f, 8f);
        rootRect.offsetMax = new Vector2(-8f, -34f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(root.transform, false);

        viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-scrollbarWidth - 4f, 0f);

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        Mask viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        Scrollbar scrollbar = CreateScrollbar(root.transform);

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        return scrollRect;
    }

    private Scrollbar CreateScrollbar(Transform parent)
    {
        GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(parent, false);

        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, 0f);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Image scrollbarImage = scrollbarObject.GetComponent<Image>();
        ApplyImageStyle(scrollbarImage, new Color(0.18f, 0.18f, 0.18f, 1f));

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);

        RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2f, 2f);
        slidingAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(slidingArea.transform, false);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Image handleImage = handle.GetComponent<Image>();
        ApplyImageStyle(handleImage, new Color(0.45f, 0.45f, 0.45f, 1f));

        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.2f;
        scrollbar.value = 1f;

        return scrollbar;
    }

    private Transform CreateScrollableGridRoot(string objectName, RectTransform viewport, int columns, ScrollRect scrollRect)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(viewport, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(4f, -4f);
        rect.sizeDelta = new Vector2(Mathf.Max(1f, viewport.rect.width - 8f), 0f);

        GridLayoutGroup grid = go.GetComponent<GridLayoutGroup>();
        grid.cellSize = slotSize;
        grid.spacing = gridSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (scrollRect != null)
            scrollRect.content = rect;

        return rect;
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, out TMP_Text labelText, string label)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        ApplyImageStyle(image, new Color(0.20f, 0.35f, 0.70f, 1f));

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        labelText = text;
        return button;
    }

    private void ApplyImageStyle(Image image, Color color)
    {
        if (image == null)
            return;

        image.sprite = GetRuntimeWhiteSprite();
        image.type = Image.Type.Simple;
        image.color = color;
    }

    private static Sprite GetRuntimeWhiteSprite()
    {
        if (runtimeWhiteSprite != null)
            return runtimeWhiteSprite;

        Texture2D texture = Texture2D.whiteTexture;
        runtimeWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        runtimeWhiteSprite.name = "ChestInteractionUI_WhiteSprite";
        return runtimeWhiteSprite;
    }
}
