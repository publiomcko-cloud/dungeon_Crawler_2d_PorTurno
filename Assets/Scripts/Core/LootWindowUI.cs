using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LootAnchorMode
{
    PartyLeader,
    SelectedEntity,
    FirstAlivePlayer
}

public class LootWindowUI : MonoBehaviour
{
    public static LootWindowUI Instance;

    [Header("External References")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private LootWindowGridAutoBuilder windowBuilder;
    [SerializeField] private PartyInventory partyInventory;
    [SerializeField] private PartyAnchorService partyAnchorService;

    [Header("Window")]
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private Transform selectorContentRoot;
    [SerializeField] private Transform equippedContentRoot;
    [SerializeField] private Transform inventoryContentRoot;
    [SerializeField] private Transform groundLootContentRoot;

    [Header("Prefab")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;

    [Header("Input")]
    [SerializeField] private KeyCode toggleLootKey = KeyCode.E;
    [SerializeField] private KeyCode closeLootKey = KeyCode.Escape;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 0.35f;

    [Header("Anchor")]
    [SerializeField] private LootAnchorMode lootAnchorMode = LootAnchorMode.PartyLeader;

    [Header("Selector Style")]
    [SerializeField] private Vector2 selectorButtonSize = new Vector2(52f, 74f);
    [SerializeField] private Color selectorNormalColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color selectorSelectedColor = new Color(0.25f, 0.45f, 0.90f, 1f);
    [SerializeField] private Color selectorTextColor = Color.white;
    [SerializeField] private int selectorFontSize = 14;

    private Entity currentEntity;
    private bool isOpen = false;

    private readonly List<GameObject> spawnedUI = new List<GameObject>();
    private readonly List<Entity> cachedPlayers = new List<Entity>();

    public bool IsOpen => isOpen;
    public Entity CurrentSelectedEntity => currentEntity;

    public event System.Action<Entity> OnSelectedEntityChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveExternalReferences();
        AutoFindReferences();
        BindCloseButton();

        if (windowRoot != null)
            windowRoot.SetActive(false);
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
        if (Input.GetKeyDown(toggleLootKey))
        {
            if (isOpen)
                CloseWindow();
            else
                TryOpenForFirstPlayer();

            return;
        }

        if (Input.GetKeyDown(closeLootKey) && isOpen)
        {
            CloseWindow();
            return;
        }

        if (!isOpen)
            return;

        if (currentEntity == null || currentEntity.IsDead)
        {
            Entity fallback = FindFirstAlivePlayer();
            if (fallback != null)
                SelectPlayer(fallback);
            else
                CloseWindow();
        }
    }

    public void ConfigureReferences(
        GameObject newWindowRoot,
        Button newCloseButton,
        Transform newSelectorContentRoot,
        Transform newEquippedContentRoot,
        Transform newInventoryContentRoot,
        Transform newGroundLootContentRoot,
        TMP_Text newTitleText,
        TMP_Text newHintText)
    {
        windowRoot = newWindowRoot;
        closeButton = newCloseButton;
        selectorContentRoot = newSelectorContentRoot;
        equippedContentRoot = newEquippedContentRoot;
        inventoryContentRoot = newInventoryContentRoot;
        groundLootContentRoot = newGroundLootContentRoot;
        titleText = newTitleText;
        hintText = newHintText;

        AutoFindReferences();
        BindCloseButton();
    }

    public void SetItemButtonPrefab(ItemButtonUI prefab)
    {
        itemButtonPrefab = prefab;
    }

    public void OpenForCell(Entity entity, Vector2Int cell)
    {
        EnsureWindowReady();

        if (entity == null || partyInventory == null)
            return;

        if (itemButtonPrefab == null)
        {
            Debug.LogWarning("LootWindowUI: ItemButtonPrefab está vazio.");
            return;
        }

        if (selectorContentRoot == null || equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
        {
            Debug.LogWarning("LootWindowUI: content roots ausentes.");
            return;
        }

        bool changed = currentEntity != entity;
        currentEntity = entity;
        isOpen = true;

        if (changed)
            OnSelectedEntityChanged?.Invoke(currentEntity);

        if (windowRoot != null)
            windowRoot.SetActive(true);

        RefreshUI();
    }

    public void CloseWindow()
    {
        HideTooltip();

        isOpen = false;
        currentEntity = null;

        ClearSpawnedUI();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void TryOpenForFirstPlayer()
    {
        Entity firstPlayer = null;

        if (partyAnchorService != null)
            firstPlayer = partyAnchorService.GetLeader();

        if (firstPlayer == null)
            firstPlayer = FindFirstAlivePlayer();

        if (firstPlayer == null)
            return;

        OpenForCell(firstPlayer, firstPlayer.GridPosition);
    }

    private void EnsureWindowReady()
    {
        ResolveExternalReferences();

        if (windowBuilder != null)
        {
            windowBuilder.SetLootWindowUI(this);

            if (!windowBuilder.IsBuilt)
                windowBuilder.Build();
        }

        AutoFindReferences();
        BindCloseButton();
    }

    private void ResolveExternalReferences()
    {
        if (windowBuilder == null)
            windowBuilder = FindFirstObjectByType<LootWindowGridAutoBuilder>();

        if (windowRoot == null && windowBuilder != null)
            windowRoot = windowBuilder.gameObject;

        if (partyInventory == null)
            partyInventory = FindFirstObjectByType<PartyInventory>();

        if (partyAnchorService == null)
            partyAnchorService = FindFirstObjectByType<PartyAnchorService>();
    }

    private Entity FindFirstAlivePlayer()
    {
        List<Entity> players = GetAvailablePlayers();
        if (players.Count == 0)
            return null;

        return players[0];
    }

    private List<Entity> GetAvailablePlayers()
    {
        cachedPlayers.Clear();

        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            if (entity == null)
                continue;

            if (entity.team != Team.Player)
                continue;

            if (entity.IsDead)
                continue;

            cachedPlayers.Add(entity);
        }

        cachedPlayers.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return cachedPlayers;
    }

    private void SelectPlayer(Entity entity)
    {
        if (entity == null)
            return;

        bool changed = currentEntity != entity;
        currentEntity = entity;

        if (changed)
            OnSelectedEntityChanged?.Invoke(currentEntity);

        HideTooltip();
        RefreshUI();
    }

    private void AutoFindReferences()
    {
        Transform searchRoot = windowRoot != null ? windowRoot.transform : null;

        if (closeButton == null)
        {
            Transform t = FindDeepChild(searchRoot, "CloseButton");
            if (t != null)
                closeButton = t.GetComponent<Button>();
        }

        if (selectorContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "SelectorContent");
            if (t != null)
                selectorContentRoot = t;
        }

        if (equippedContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "EquippedContent");
            if (t != null)
                equippedContentRoot = t;
        }

        if (inventoryContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "InventoryContent");
            if (t != null)
                inventoryContentRoot = t;
        }

        if (groundLootContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "GroundLootContent");
            if (t != null)
                groundLootContentRoot = t;
        }

        if (titleText == null)
        {
            Transform t = FindDeepChild(searchRoot, "TitleText");
            if (t != null)
                titleText = t.GetComponent<TMP_Text>();
        }

        if (hintText == null)
        {
            Transform t = FindDeepChild(searchRoot, "HintText");
            if (t != null)
                hintText = t.GetComponent<TMP_Text>();
        }
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseWindow);
    }

    private void RefreshUI()
    {
        if (!isOpen || currentEntity == null || partyInventory == null)
            return;

        EnsureWindowReady();

        if (itemButtonPrefab == null)
            return;

        if (selectorContentRoot == null || equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
            return;

        ClearSpawnedUI();

        if (titleText != null)
            titleText.text = $"Party Inventory - Equipando: {currentEntity.name}";

        if (hintText != null)
            hintText.text = "Party shared bag | seletor troca só equipamentos | chão e mochila são únicos";

        BuildSelectorSection();
        BuildEquippedSection();
        BuildInventorySection();
        BuildGroundLootSection();
    }

    private void BuildSelectorSection()
    {
        List<Entity> players = GetAvailablePlayers();

        for (int i = 0; i < players.Count; i++)
        {
            Entity player = players[i];
            int displayIndex = i + 1;

            GameObject buttonGO = CreateSelectorButton(player, displayIndex, player == currentEntity);
            buttonGO.transform.SetParent(selectorContentRoot, false);
            spawnedUI.Add(buttonGO);
        }
    }

    private GameObject CreateSelectorButton(Entity player, int displayIndex, bool selected)
    {
        GameObject root = new GameObject(
            $"PlayerSelector_{displayIndex}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = selectorButtonSize;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minWidth = selectorButtonSize.x;
        layout.preferredWidth = selectorButtonSize.x;
        layout.minHeight = selectorButtonSize.y;
        layout.preferredHeight = selectorButtonSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image bg = root.GetComponent<Image>();
        bg.color = selected ? selectorSelectedColor : selectorNormalColor;

        Button button = root.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            SelectPlayer(player);
        });

        GameObject iconGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(root.transform, false);

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        iconRect.anchoredPosition = new Vector2(0f, -6f);

        Image icon = iconGO.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = GetEntityPortrait(player);
        icon.color = icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(root.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, 22f);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = displayIndex.ToString();
        label.fontSize = selectorFontSize;
        label.color = selectorTextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        return root;
    }

    private Sprite GetEntityPortrait(Entity entity)
    {
        if (entity == null)
            return null;

        SpriteRenderer spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            return null;

        return spriteRenderer.sprite;
    }

    private void BuildEquippedSection()
    {
        CreateEquippedButton(EquipmentSlotType.Weapon);
        CreateEquippedButton(EquipmentSlotType.Armor);
        CreateEquippedButton(EquipmentSlotType.Accessory);
    }

    private void CreateEquippedButton(EquipmentSlotType slotType)
    {
        InventoryItemEntry equipped = partyInventory.GetEquippedEntry(currentEntity, slotType);

        ItemButtonUI button = Instantiate(itemButtonPrefab, equippedContentRoot);
        button.ConfigureAsEquippedSlot(slotType);

        button.Setup(
            equipped,
            equipped == null || equipped.IsEmpty
                ? null
                : () =>
                {
                    bool moved = partyInventory.UnequipToInventory(currentEntity, slotType);
                    if (moved)
                        RefreshUI();
                },
            null,
            equipped != null && !equipped.IsEmpty,
            (sourceButton) =>
            {
                HandleDropOnEquippedSlot(slotType, sourceButton);
            },
            null
        );

        spawnedUI.Add(button.gameObject);
    }

    private void BuildInventorySection()
    {
        IReadOnlyList<InventoryItemEntry> items = partyInventory.Items;

        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            InventoryItemEntry entry = items[index];
            InventoryItemEntry equippedCompare = null;

            if (entry != null && !entry.IsEmpty)
                equippedCompare = partyInventory.GetEquippedEntry(currentEntity, entry.SlotType);

            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryContentRoot);
            button.ConfigureAsInventorySlot(index);

            bool hasItem = entry != null && !entry.IsEmpty;

            button.Setup(
                hasItem ? entry : null,
                hasItem
                    ? () =>
                    {
                        bool equipped = partyInventory.TryEquipFromInventory(currentEntity, index);
                        if (equipped)
                            RefreshUI();
                    }
                    : null,
                hasItem
                    ? () =>
                    {
                        bool equipped = partyInventory.TryEquipFromInventory(currentEntity, index);
                        if (equipped)
                            RefreshUI();
                    }
                    : null,
                hasItem,
                (sourceButton) =>
                {
                    HandleDropOnInventorySlot(index, sourceButton);
                },
                equippedCompare
            );

            spawnedUI.Add(button.gameObject);
        }
    }

    private void BuildGroundLootSection()
    {
        Vector2Int anchorCell = GetPartyAnchorCell();
        List<GroundItem> items = GetGroundItemsInCell(anchorCell);
        int maxGroundSlots = 20;

        for (int i = 0; i < maxGroundSlots; i++)
        {
            ItemButtonUI button = Instantiate(itemButtonPrefab, groundLootContentRoot);

            if (i >= items.Count || items[i] == null)
            {
                button.ClearContext();
                button.Setup(null, null, null, false, null, null);
                spawnedUI.Add(button.gameObject);
                continue;
            }

            GroundItem groundItem = items[i];
            InventoryItemEntry entry = groundItem.ToInventoryEntry();
            InventoryItemEntry equippedCompare = null;

            if (entry != null && !entry.IsEmpty)
                equippedCompare = partyInventory.GetEquippedEntry(currentEntity, entry.SlotType);

            button.ConfigureAsGroundSlot(groundItem);

            button.Setup(
                entry,
                () =>
                {
                    bool moved = groundItem.TrySendToPartyInventory(partyInventory);
                    if (moved)
                        RefreshUI();
                },
                () =>
                {
                    bool equipped = groundItem.TryEquipDirectToParty(currentEntity, partyInventory);
                    if (equipped)
                        RefreshUI();
                },
                true,
                null,
                equippedCompare
            );

            spawnedUI.Add(button.gameObject);
        }
    }

    private Vector2Int GetPartyAnchorCell()
    {
        if (lootAnchorMode == LootAnchorMode.SelectedEntity && currentEntity != null)
            return currentEntity.GridPosition;

        if (lootAnchorMode == LootAnchorMode.PartyLeader && partyAnchorService != null)
        {
            Entity leader = partyAnchorService.GetLeader();
            if (leader != null)
                return leader.GridPosition;
        }

        List<Entity> players = GetAvailablePlayers();
        if (players.Count > 0)
            return players[0].GridPosition;

        return currentEntity != null ? currentEntity.GridPosition : Vector2Int.zero;
    }

    private void HandleDropOnInventorySlot(int targetIndex, ItemButtonUI sourceButton)
    {
        if (partyInventory == null || sourceButton == null)
            return;

        bool changed = false;

        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory:
                changed = partyInventory.MoveItem(sourceButton.InventoryIndex, targetIndex);
                break;

            case ItemButtonSlotKind.Equipped:
                changed = partyInventory.UnequipToInventorySlot(currentEntity, sourceButton.EquippedSlotType, targetIndex);
                break;

            case ItemButtonSlotKind.Ground:
                if (sourceButton.GroundItemRef != null)
                    changed = sourceButton.GroundItemRef.TrySendToPartyInventorySlot(partyInventory, targetIndex);
                break;
        }

        if (changed)
            RefreshUI();
    }

    private void HandleDropOnEquippedSlot(EquipmentSlotType targetSlotType, ItemButtonUI sourceButton)
    {
        if (partyInventory == null || currentEntity == null || sourceButton == null)
            return;

        bool changed = false;

        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory:
                changed = partyInventory.TryEquipFromInventoryToSlot(currentEntity, sourceButton.InventoryIndex, targetSlotType);
                break;

            case ItemButtonSlotKind.Ground:
                if (sourceButton.GroundItemRef != null)
                    changed = sourceButton.GroundItemRef.TryEquipDirectToPartySlot(currentEntity, partyInventory, targetSlotType);
                break;
        }

        if (changed)
            RefreshUI();
    }

    private List<GroundItem> GetGroundItemsInCell(Vector2Int cell)
    {
        List<GroundItem> result = new List<GroundItem>();

        if (GridManager.Instance == null)
            return result;

        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);

        if (hits == null)
            return result;

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem item = hits[i].GetComponent<GroundItem>();
            if (item != null)
                result.Add(item);
        }

        return result;
    }

    private void ClearSpawnedUI()
    {
        for (int i = 0; i < spawnedUI.Count; i++)
        {
            if (spawnedUI[i] != null)
                Destroy(spawnedUI[i]);
        }

        spawnedUI.Clear();
    }

    private void HideTooltip()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}