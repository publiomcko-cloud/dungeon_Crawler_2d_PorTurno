using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class NpcTextStyleSettings
{
    public float left = 18f;
    public float bottom = 18f;
    public float width = 200f;
    public float height = 24f;
    public int fontSize = 16;
    public FontStyles fontStyle = FontStyles.Normal;
    public Color color = Color.white;
    public TextAlignmentOptions alignment = TextAlignmentOptions.Left;
}

[Serializable]
public class NpcButtonStyleSettings
{
    public float left = 18f;
    public float bottom = 6f;
    public float width = 112f;
    public float height = 32f;
    public Color backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    public Color textColor = Color.white;
    public int fontSize = 16;
}

[Serializable]
public class NpcPanelStyleSettings
{
    public float left = 18f;
    public float bottom = 18f;
    public float width = 200f;
    public float height = 120f;
    public Color backgroundColor = new Color(0.14f, 0.14f, 0.14f, 0.96f);
}

[Serializable]
public class NpcGridStyleSettings
{
    public float contentLeft = 10f;
    public float contentBottom = 10f;
    public float contentRight = 10f;
    public float contentTop = 10f;
    public float cellWidth = 64f;
    public float cellHeight = 64f;
    public float horizontalSpacing = 6f;
    public float verticalSpacing = 6f;
    public int columns = 4;
}

public class NpcInteractionUI : MonoBehaviour
{
    private enum PendingMerchantAction
    {
        None,
        Buy,
        Sell
    }

    private static Sprite runtimeWhiteSprite;

    public static NpcInteractionUI Instance;

    [Header("Optional References")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text merchantHeaderText;
    [SerializeField] private TMP_Text playerHeaderText;
    [SerializeField] private RectTransform merchantPanelRoot;
    [SerializeField] private RectTransform playerPanelRoot;
    [SerializeField] private Transform merchantGridRoot;
    [SerializeField] private Transform playerGridRoot;
    [SerializeField] private TMP_Text questInfoText;
    [SerializeField] private RectTransform actionBarRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text cancelButtonText;
    [SerializeField] private Button previousQuestButton;
    [SerializeField] private TMP_Text previousQuestButtonText;
    [SerializeField] private Button nextQuestButton;
    [SerializeField] private TMP_Text nextQuestButtonText;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TMP_Text confirmationMessageText;
    [SerializeField] private Button confirmationConfirmButton;
    [SerializeField] private TMP_Text confirmationConfirmButtonText;
    [SerializeField] private Button confirmationCancelButton;
    [SerializeField] private TMP_Text confirmationCancelButtonText;

    [Header("Prefabs")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Window Style")]
    [SerializeField] private Vector2 windowSize = new Vector2(760f, 430f);
    [SerializeField] private Vector2 windowScreenOccupancy = new Vector2(0.8f, 0.8f);
    [SerializeField] private Color windowBackgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.97f);
    [SerializeField] private int canvasSortingOrder = 500;

    [Header("Recommended Text Layout")]
    [SerializeField] private NpcTextStyleSettings titleStyle = new NpcTextStyleSettings { left = 18f, bottom = 392f, width = 724f, height = 24f, fontSize = 22, fontStyle = FontStyles.Bold, alignment = TextAlignmentOptions.Left };
    [SerializeField] private NpcTextStyleSettings moneyStyle = new NpcTextStyleSettings { left = 18f, bottom = 368f, width = 724f, height = 20f, fontSize = 15, alignment = TextAlignmentOptions.Left };
    [SerializeField] private NpcTextStyleSettings bodyStyle = new NpcTextStyleSettings { left = 18f, bottom = 322f, width = 724f, height = 40f, fontSize = 15, alignment = TextAlignmentOptions.TopLeft };
    [SerializeField] private NpcTextStyleSettings merchantHeaderStyle = new NpcTextStyleSettings { left = 18f, bottom = 292f, width = 332f, height = 20f, fontSize = 16, fontStyle = FontStyles.Bold, alignment = TextAlignmentOptions.Left };
    [SerializeField] private NpcTextStyleSettings playerHeaderStyle = new NpcTextStyleSettings { left = 392f, bottom = 292f, width = 350f, height = 20f, fontSize = 16, fontStyle = FontStyles.Bold, alignment = TextAlignmentOptions.Left };
    [SerializeField] private NpcTextStyleSettings questInfoStyle = new NpcTextStyleSettings { left = 18f, bottom = 104f, width = 724f, height = 184f, fontSize = 16, alignment = TextAlignmentOptions.TopLeft };
    [SerializeField] private NpcTextStyleSettings statusStyle = new NpcTextStyleSettings { left = 18f, bottom = 52f, width = 724f, height = 22f, fontSize = 14, alignment = TextAlignmentOptions.Left };
    [SerializeField] private NpcTextStyleSettings confirmationMessageStyle = new NpcTextStyleSettings { left = 16f, bottom = 74f, width = 328f, height = 56f, fontSize = 17, alignment = TextAlignmentOptions.Center };

    [Header("Recommended Panel Layout")]
    [SerializeField] private NpcPanelStyleSettings merchantPanelStyle = new NpcPanelStyleSettings { left = 18f, bottom = 104f, width = 332f, height = 184f };
    [SerializeField] private NpcPanelStyleSettings playerPanelStyle = new NpcPanelStyleSettings { left = 392f, bottom = 104f, width = 350f, height = 184f };
    [SerializeField] private NpcPanelStyleSettings actionBarStyle = new NpcPanelStyleSettings { left = 18f, bottom = 8f, width = 724f, height = 40f, backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f) };
    [SerializeField] private Color confirmationOverlayColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private NpcPanelStyleSettings confirmationPanelStyle = new NpcPanelStyleSettings { left = 200f, bottom = 140f, width = 360f, height = 150f, backgroundColor = new Color(0.10f, 0.10f, 0.10f, 0.98f) };

    [Header("Recommended Button Layout")]
    [SerializeField] private NpcButtonStyleSettings confirmButtonStyle = new NpcButtonStyleSettings { left = 470f, bottom = 4f, width = 112f, height = 32f, backgroundColor = new Color(0.70f, 0.20f, 0.20f, 1f) };
    [SerializeField] private NpcButtonStyleSettings cancelButtonStyle = new NpcButtonStyleSettings { left = 596f, bottom = 4f, width = 112f, height = 32f, backgroundColor = new Color(0.20f, 0.35f, 0.70f, 1f) };
    [SerializeField] private NpcButtonStyleSettings previousQuestButtonStyle = new NpcButtonStyleSettings { left = 8f, bottom = 4f, width = 40f, height = 32f };
    [SerializeField] private NpcButtonStyleSettings nextQuestButtonStyle = new NpcButtonStyleSettings { left = 56f, bottom = 4f, width = 40f, height = 32f };
    [SerializeField] private NpcButtonStyleSettings confirmationConfirmButtonStyle = new NpcButtonStyleSettings { left = 44f, bottom = 18f, width = 112f, height = 32f, backgroundColor = new Color(0.70f, 0.20f, 0.20f, 1f) };
    [SerializeField] private NpcButtonStyleSettings confirmationCancelButtonStyle = new NpcButtonStyleSettings { left = 204f, bottom = 18f, width = 112f, height = 32f, backgroundColor = new Color(0.20f, 0.35f, 0.70f, 1f) };

    [Header("Recommended Slot Layout")]
    [SerializeField] private NpcGridStyleSettings merchantGridStyle = new NpcGridStyleSettings();
    [SerializeField] private NpcGridStyleSettings playerGridStyle = new NpcGridStyleSettings();

    [Header("Input")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private readonly List<GameObject> spawnedSlotButtons = new List<GameObject>();

    private NpcActor currentNpc;
    private Entity currentInteractor;
    private PendingMerchantAction pendingMerchantAction;
    private int pendingMerchantIndex = -1;
    private int pendingInventoryIndex = -1;
    private int selectedQuestIndex;

    public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

    public static NpcInteractionUI GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        NpcInteractionUI existing = FindFirstObjectByType<NpcInteractionUI>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("UI_NpcInteraction");
        return go.AddComponent<NpcInteractionUI>();
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

    public void Open(NpcActor npc, Entity interactor)
    {
        if (npc == null)
            return;

        EnsureWindow();
        currentNpc = npc;
        currentInteractor = interactor;
        selectedQuestIndex = 0;

        if (windowRoot != null)
            windowRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        currentNpc = null;
        currentInteractor = null;
        ClearPendingMerchantAction();
        ClearSpawnedButtons();
        HideTooltip();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (currentNpc == null)
            return;

        EnsureItemButtonPrefab();
        ClearPendingMerchantAction();
        ClearSpawnedButtons();
        ClearStatus();
        HideTooltip();

        if (titleText != null)
            titleText.text = currentNpc.DisplayName;

        RefreshMoneyText();

        switch (currentNpc.Type)
        {
            case NpcType.Recruit:
                RefreshRecruitMode();
                break;
            case NpcType.Merchant:
                RefreshMerchantMode();
                break;
            case NpcType.Quest:
                RefreshQuestMode();
                break;
        }
    }

    private void RefreshRecruitMode()
    {
        SetMerchantAreaVisible(false);
        SetQuestAreaVisible(false);
        SetButtonVisible(confirmButton, true);
        SetButtonVisible(cancelButton, true);

        if (bodyText != null)
            bodyText.text = $"{currentNpc.GreetingText}\n\nCusto: {currentNpc.RecruitmentCost}";

        if (confirmButtonText != null)
            confirmButtonText.text = currentNpc.ConfirmButtonLabel;

        if (cancelButtonText != null)
            cancelButtonText.text = currentNpc.CancelButtonLabel;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleRecruitConfirm);
            confirmButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Close);
            cancelButton.interactable = true;
        }

        SetQuestNavigationInteractable(false, false);
        BringActionButtonsToFront();
    }

    private void RefreshMerchantMode()
    {
        ClearSpawnedButtons();
        SetMerchantAreaVisible(true);
        SetQuestAreaVisible(false);
        SetButtonVisible(confirmButton, false);
        SetButtonVisible(cancelButton, true);

        if (bodyText != null)
            bodyText.text = currentNpc.MerchantGreetingText;

        if (merchantHeaderText != null)
            merchantHeaderText.text = "Loja";

        if (playerHeaderText != null)
            playerHeaderText.text = "Mochila da Party";

        if (cancelButtonText != null)
            cancelButtonText.text = "Fechar";

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Close);
            cancelButton.interactable = true;
        }

        SetQuestNavigationInteractable(false, false);
        BuildMerchantStockGrid();
        BuildPlayerInventoryGrid();
        BringActionButtonsToFront();
    }

    private void RefreshQuestMode()
    {
        SetMerchantAreaVisible(false);
        SetQuestAreaVisible(true);
        SetButtonVisible(cancelButton, true);

        if (bodyText != null)
            bodyText.text = currentNpc.QuestGreetingText;

        if (cancelButtonText != null)
            cancelButtonText.text = "Fechar";

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Close);
            cancelButton.interactable = true;
        }

        BindQuestNavigationButtons();

        QuestTracker tracker = QuestTracker.Instance;
        if (tracker != null && tracker.ActiveQuest != null)
        {
            SetButtonVisible(confirmButton, true);

            if (questInfoText != null)
                questInfoText.text = BuildActiveQuestText(tracker.ActiveQuest, tracker.ActiveQuestProgress);

            if (tracker.IsActiveQuestComplete)
            {
                if (confirmButtonText != null)
                    confirmButtonText.text = "Entregar";

                if (confirmButton != null)
                {
                    confirmButton.interactable = true;
                    confirmButton.onClick.RemoveAllListeners();
                    confirmButton.onClick.AddListener(HandleClaimQuest);
                }
            }
            else
            {
                if (confirmButtonText != null)
                    confirmButtonText.text = "Em andamento";

                if (confirmButton != null)
                {
                    confirmButton.interactable = false;
                    confirmButton.onClick.RemoveAllListeners();
                }
            }

            SetQuestNavigationInteractable(false, false);
            BringActionButtonsToFront();
            return;
        }

        int availableQuestCount = currentNpc.GetAvailableQuestCount();
        if (availableQuestCount <= 0)
        {
            if (questInfoText != null)
                questInfoText.text = "Nenhuma quest disponivel no momento.";

            SetButtonVisible(confirmButton, false);
            SetQuestNavigationInteractable(false, false);
            BringActionButtonsToFront();
            return;
        }

        selectedQuestIndex = ClampSelection(selectedQuestIndex, availableQuestCount);
        QuestDefinition selectedQuest = currentNpc.GetAvailableQuestAt(selectedQuestIndex);

        if (questInfoText != null)
            questInfoText.text = BuildQuestOfferText(selectedQuest, selectedQuestIndex, availableQuestCount);

        SetButtonVisible(confirmButton, true);

        if (confirmButtonText != null)
            confirmButtonText.text = "Aceitar";

        if (confirmButton != null)
        {
            confirmButton.interactable = selectedQuest != null;
            confirmButton.onClick.RemoveAllListeners();
            if (selectedQuest != null)
                confirmButton.onClick.AddListener(() => HandleAcceptQuest(selectedQuest));
        }

        bool allowNavigation = availableQuestCount > 1;
        SetQuestNavigationInteractable(allowNavigation, allowNavigation);
        BringActionButtonsToFront();
    }

    private void HandleRecruitConfirm()
    {
        if (currentNpc == null)
            return;

        bool recruited = currentNpc.TryRecruit(currentInteractor, out string message);
        ShowStatus(message);
        RefreshMoneyText();

        if (recruited)
            Close();
        else
            RefreshRecruitMode();
    }

    private void HandleAcceptQuest(QuestDefinition questDefinition)
    {
        if (currentNpc == null || questDefinition == null)
            return;

        bool accepted = currentNpc.TryAcceptQuest(questDefinition, out string message);
        ShowStatus(message);

        if (accepted)
            RefreshQuestMode();
    }

    private void HandleClaimQuest()
    {
        if (currentNpc == null)
            return;

        bool claimed = currentNpc.TryClaimQuestReward(out string message);
        ShowStatus(message);
        RefreshMoneyText();

        if (claimed)
            RefreshQuestMode();
    }

    private void BuildMerchantStockGrid()
    {
        if (merchantGridRoot == null || itemButtonPrefab == null || currentNpc == null)
            return;

        int stockCount = currentNpc.GetMerchantStockCount();
        for (int i = 0; i < stockCount; i++)
        {
            int stockIndex = i;
            InventoryItemEntry entry = currentNpc.GetMerchantStockEntry(stockIndex);
            InventoryItemEntry compareEntry = GetCompareEntry(entry);

            ItemButtonUI button = Instantiate(itemButtonPrefab, merchantGridRoot);
            button.ClearContext();
            button.Setup(
                entry,
                entry != null && !entry.IsEmpty ? () => PromptBuy(stockIndex, entry) : null,
                null,
                false,
                null,
                compareEntry);

            spawnedSlotButtons.Add(button.gameObject);
        }
    }

    private void BuildPlayerInventoryGrid()
    {
        if (playerGridRoot == null || itemButtonPrefab == null)
            return;

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
            return;

        for (int i = 0; i < partyInventory.Items.Count; i++)
        {
            int inventoryIndex = i;
            InventoryItemEntry entry = partyInventory.GetItem(inventoryIndex);
            InventoryItemEntry compareEntry = GetCompareEntry(entry);

            ItemButtonUI button = Instantiate(itemButtonPrefab, playerGridRoot);
            button.ConfigureAsInventorySlot(inventoryIndex);
            button.Setup(
                entry != null && !entry.IsEmpty ? entry : null,
                entry != null && !entry.IsEmpty ? () => PromptSell(inventoryIndex, entry) : null,
                null,
                false,
                null,
                compareEntry);

            spawnedSlotButtons.Add(button.gameObject);
        }
    }

    private void PromptBuy(int merchantIndex, InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return;

        pendingMerchantAction = PendingMerchantAction.Buy;
        pendingMerchantIndex = merchantIndex;
        pendingInventoryIndex = -1;
        ShowConfirmation($"Comprar {entry.ItemName} por {Mathf.Max(0, entry.Value)}?", "Confirmar", ExecutePendingMerchantAction);
    }

    private void PromptSell(int inventoryIndex, InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return;

        pendingMerchantAction = PendingMerchantAction.Sell;
        pendingMerchantIndex = -1;
        pendingInventoryIndex = inventoryIndex;
        ShowConfirmation($"Vender {entry.ItemName} por {Mathf.Max(0, entry.Value)}?", "Confirmar", ExecutePendingMerchantAction);
    }

    private void ExecutePendingMerchantAction()
    {
        if (currentNpc == null)
            return;

        bool success = false;
        string message = string.Empty;

        switch (pendingMerchantAction)
        {
            case PendingMerchantAction.Buy:
                success = currentNpc.TryBuyMerchantItem(pendingMerchantIndex, out message);
                break;
            case PendingMerchantAction.Sell:
                success = currentNpc.TrySellInventoryItem(pendingInventoryIndex, out message);
                break;
        }

        ShowStatus(message);
        RefreshMoneyText();
        ClearPendingMerchantAction();

        if (currentNpc.Type == NpcType.Merchant)
            RefreshMerchantMode();

        if (!success)
            return;
    }

    private void ShowConfirmation(string message, string confirmLabel, UnityEngine.Events.UnityAction confirmAction)
    {
        if (confirmationRoot == null)
            return;

        if (confirmationMessageText != null)
            confirmationMessageText.text = message;

        if (confirmationConfirmButtonText != null)
            confirmationConfirmButtonText.text = confirmLabel;

        if (confirmationCancelButtonText != null)
            confirmationCancelButtonText.text = "Cancelar";

        if (confirmationConfirmButton != null)
        {
            confirmationConfirmButton.onClick.RemoveAllListeners();
            confirmationConfirmButton.onClick.AddListener(confirmAction);
        }

        if (confirmationCancelButton != null)
        {
            confirmationCancelButton.onClick.RemoveAllListeners();
            confirmationCancelButton.onClick.AddListener(ClearPendingMerchantAction);
        }

        confirmationRoot.SetActive(true);
    }

    private void ClearPendingMerchantAction()
    {
        pendingMerchantAction = PendingMerchantAction.None;
        pendingMerchantIndex = -1;
        pendingInventoryIndex = -1;

        if (confirmationRoot != null)
            confirmationRoot.SetActive(false);
    }

    private string BuildQuestOfferText(QuestDefinition questDefinition, int index, int totalCount)
    {
        if (questDefinition == null)
            return "Quest invalida.";

        return
            $"Quest [{index + 1}/{totalCount}]\n" +
            $"{questDefinition.DisplayName}\n\n" +
            $"{questDefinition.Description}\n\n" +
            $"Nivel minimo do lider: {questDefinition.MinimumLeaderLevel}\n" +
            $"{questDefinition.BuildTargetSummary()}\n" +
            $"Quantidade: {questDefinition.RequiredKillCount}\n" +
            $"Recompensa: {questDefinition.RewardMoney}";
    }

    private string BuildActiveQuestText(QuestDefinition questDefinition, int progress)
    {
        if (questDefinition == null)
            return "Nenhuma quest ativa.";

        return
            $"Quest ativa\n" +
            $"{questDefinition.DisplayName}\n\n" +
            $"{questDefinition.Description}\n\n" +
            $"{questDefinition.BuildTargetSummary()}\n" +
            $"Progresso: {progress}/{questDefinition.RequiredKillCount}\n" +
            $"Recompensa: {questDefinition.RewardMoney}";
    }

    private void BindQuestNavigationButtons()
    {
        if (previousQuestButtonText != null)
            previousQuestButtonText.text = "<";

        if (nextQuestButtonText != null)
            nextQuestButtonText.text = ">";

        if (previousQuestButton != null)
        {
            previousQuestButton.onClick.RemoveAllListeners();
            previousQuestButton.onClick.AddListener(() =>
            {
                selectedQuestIndex -= 1;
                RefreshQuestMode();
            });
        }

        if (nextQuestButton != null)
        {
            nextQuestButton.onClick.RemoveAllListeners();
            nextQuestButton.onClick.AddListener(() =>
            {
                selectedQuestIndex += 1;
                RefreshQuestMode();
            });
        }
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

    private void RefreshMoneyText()
    {
        if (moneyText == null)
            return;

        moneyText.text = PartyCurrency.Instance != null
            ? $"Dinheiro: {PartyCurrency.Instance.CurrentMoney}"
            : "Dinheiro: ?";
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ClearStatus()
    {
        if (statusText != null)
            statusText.text = string.Empty;
    }

    private void ClearSpawnedButtons()
    {
        for (int i = 0; i < spawnedSlotButtons.Count; i++)
        {
            if (spawnedSlotButtons[i] != null)
                Destroy(spawnedSlotButtons[i]);
        }

        spawnedSlotButtons.Clear();
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
        EnsureEventSystem();

        if (windowRoot != null)
            return;

        windowRoot = BuildWindow(uiCanvas.transform);
    }

    private void EnsureCanvas()
    {
        if (uiCanvas == null)
        {
            GameObject canvasObject = GameObject.Find("NpcInteractionCanvas");
            if (canvasObject != null)
                uiCanvas = canvasObject.GetComponent<Canvas>();
        }

        if (uiCanvas == null)
        {
            GameObject go = new GameObject("NpcInteractionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiCanvas = go.GetComponent<Canvas>();
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

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void EnsureItemButtonPrefab()
    {
        if (itemButtonPrefab != null)
            return;

        LootWindowGridAutoBuilder builder = FindFirstObjectByType<LootWindowGridAutoBuilder>();
        if (builder != null)
            itemButtonPrefab = builder.ItemButtonPrefab;
    }

    private GameObject BuildWindow(Transform parent)
    {
        GameObject root = new GameObject("NpcInteractionWindow", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        float widthRatio = Mathf.Clamp01(windowScreenOccupancy.x);
        float heightRatio = Mathf.Clamp01(windowScreenOccupancy.y);
        float horizontalMargin = (1f - widthRatio) * 0.5f;
        float verticalMargin = (1f - heightRatio) * 0.5f;

        rootRect.anchorMin = new Vector2(horizontalMargin, verticalMargin);
        rootRect.anchorMax = new Vector2(1f - horizontalMargin, 1f - verticalMargin);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.sizeDelta = windowSize;

        ApplyImageStyle(root.GetComponent<Image>(), windowBackgroundColor);

        titleText = CreateText("TitleText", root.transform, titleStyle);
        moneyText = CreateText("MoneyText", root.transform, moneyStyle);
        bodyText = CreateText("BodyText", root.transform, bodyStyle);
        merchantHeaderText = CreateText("MerchantHeaderText", root.transform, merchantHeaderStyle);
        playerHeaderText = CreateText("PlayerHeaderText", root.transform, playerHeaderStyle);

        merchantPanelRoot = CreatePanel("MerchantPanel", root.transform, merchantPanelStyle);
        playerPanelRoot = CreatePanel("PlayerPanel", root.transform, playerPanelStyle);

        merchantGridRoot = CreateGridRoot("MerchantGridRoot", merchantPanelRoot, merchantGridStyle);
        playerGridRoot = CreateGridRoot("PlayerGridRoot", playerPanelRoot, playerGridStyle);

        questInfoText = CreateText("QuestInfoText", root.transform, questInfoStyle);
        statusText = CreateText("StatusText", root.transform, statusStyle);

        actionBarRoot = CreatePanel("ActionBar", root.transform, actionBarStyle);
        previousQuestButton = CreateButton("PreviousQuestButton", actionBarRoot, previousQuestButtonStyle, out previousQuestButtonText, "<");
        nextQuestButton = CreateButton("NextQuestButton", actionBarRoot, nextQuestButtonStyle, out nextQuestButtonText, ">");
        confirmButton = CreateButton("ConfirmButton", actionBarRoot, confirmButtonStyle, out confirmButtonText, "Confirmar");
        cancelButton = CreateButton("CancelButton", actionBarRoot, cancelButtonStyle, out cancelButtonText, "Cancelar");

        confirmationRoot = BuildConfirmationPanel(root.transform);
        confirmationRoot.SetActive(false);

        BringActionButtonsToFront();
        return root;
    }

    private GameObject BuildConfirmationPanel(Transform parent)
    {
        GameObject overlay = new GameObject("ConfirmationOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        ApplyImageStyle(overlay.GetComponent<Image>(), confirmationOverlayColor);

        GameObject panel = new GameObject("ConfirmationPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        ApplyBottomLeftRect(panelRect, confirmationPanelStyle.left, confirmationPanelStyle.bottom, confirmationPanelStyle.width, confirmationPanelStyle.height);
        ApplyImageStyle(panel.GetComponent<Image>(), confirmationPanelStyle.backgroundColor);

        confirmationMessageText = CreateText("ConfirmationMessageText", panel.transform, confirmationMessageStyle);
        confirmationConfirmButton = CreateButton("ConfirmationConfirmButton", panel.transform, confirmationConfirmButtonStyle, out confirmationConfirmButtonText, "Confirmar");
        confirmationCancelButton = CreateButton("ConfirmationCancelButton", panel.transform, confirmationCancelButtonStyle, out confirmationCancelButtonText, "Cancelar");

        return overlay;
    }

    private RectTransform CreatePanel(string objectName, Transform parent, NpcPanelStyleSettings style)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        ApplyBottomLeftRect(rect, style.left, style.bottom, style.width, style.height);
        ApplyImageStyle(go.GetComponent<Image>(), style.backgroundColor);
        return rect;
    }

    private Transform CreateGridRoot(string objectName, RectTransform parent, NpcGridStyleSettings style)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(GridLayoutGroup));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(style.contentLeft, style.contentBottom);
        rect.offsetMax = new Vector2(-style.contentRight, -style.contentTop);

        GridLayoutGroup grid = go.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(style.cellWidth, style.cellHeight);
        grid.spacing = new Vector2(style.horizontalSpacing, style.verticalSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, style.columns);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        return rect;
    }

    private TMP_Text CreateText(string objectName, Transform parent, NpcTextStyleSettings style)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        ApplyBottomLeftRect(rect, style.left, style.bottom, style.width, style.height);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = style.fontSize;
        text.fontStyle = style.fontStyle;
        text.color = style.color;
        text.alignment = style.alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, NpcButtonStyleSettings style, out TMP_Text labelText, string label)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        ApplyBottomLeftRect(rect, style.left, style.bottom, style.width, style.height);

        Image image = go.GetComponent<Image>();
        ApplyImageStyle(image, style.backgroundColor);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.9f);
        button.colors = colors;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = style.fontSize;
        labelTmp.color = style.textColor;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
        labelTmp.raycastTarget = false;

        labelText = labelTmp;
        return button;
    }

    private void ApplyBottomLeftRect(RectTransform rect, float left, float bottom, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(left, bottom);
        rect.sizeDelta = new Vector2(width, height);
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
        runtimeWhiteSprite.name = "NpcInteractionUI_WhiteSprite";
        return runtimeWhiteSprite;
    }

    private int ClampSelection(int index, int count)
    {
        if (count <= 0)
            return 0;
        if (index < 0)
            return count - 1;
        if (index >= count)
            return 0;
        return index;
    }

    private void SetMerchantAreaVisible(bool visible)
    {
        SetControlVisible(merchantHeaderText, visible);
        SetControlVisible(playerHeaderText, visible);
        SetControlVisible(merchantPanelRoot, visible);
        SetControlVisible(playerPanelRoot, visible);
        SetControlVisible(merchantGridRoot, visible);
        SetControlVisible(playerGridRoot, visible);
    }

    private void SetQuestAreaVisible(bool visible)
    {
        SetControlVisible(questInfoText, visible);
        SetControlVisible(previousQuestButton, visible);
        SetControlVisible(nextQuestButton, visible);
    }

    private void SetQuestNavigationInteractable(bool previousEnabled, bool nextEnabled)
    {
        if (previousQuestButton != null)
            previousQuestButton.interactable = previousEnabled;

        if (nextQuestButton != null)
            nextQuestButton.interactable = nextEnabled;
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void BringActionButtonsToFront()
    {
        if (actionBarRoot != null)
            actionBarRoot.SetAsLastSibling();

        if (previousQuestButton != null)
            previousQuestButton.transform.SetAsLastSibling();

        if (nextQuestButton != null)
            nextQuestButton.transform.SetAsLastSibling();

        if (confirmButton != null)
            confirmButton.transform.SetAsLastSibling();

        if (cancelButton != null)
            cancelButton.transform.SetAsLastSibling();
    }

    private void SetControlVisible(UnityEngine.Object target, bool visible)
    {
        if (target is Component component)
            component.gameObject.SetActive(visible);
        else if (target is GameObject gameObject)
            gameObject.SetActive(visible);
    }
}
