using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LootAnchorMode { PartyLeader, SelectedEntity, FirstAlivePlayer }

public class LootWindowUI : MonoBehaviour
{
    public static LootWindowUI Instance;
    [Header("External References")] [SerializeField] private GameObject windowRoot; [SerializeField] private LootWindowGridAutoBuilder windowBuilder; [SerializeField] private PartyInventory partyInventory; [SerializeField] private PartyAnchorService partyAnchorService;
    [Header("Window")] [SerializeField] private Button closeButton;
    [Header("Panels")] [SerializeField] private Transform selectorContentRoot; [SerializeField] private Transform statsContentRoot; [SerializeField] private Transform equippedContentRoot; [SerializeField] private Transform inventoryContentRoot; [SerializeField] private Transform groundLootContentRoot;
    [Header("Prefab")] [SerializeField] private ItemButtonUI itemButtonPrefab;
    [Header("Info")] [SerializeField] private TMP_Text titleText; [SerializeField] private TMP_Text hintText;
    [Header("Input")] [SerializeField] private KeyCode toggleLootKey = KeyCode.E; [SerializeField] private KeyCode closeLootKey = KeyCode.Escape;
    [Header("Detection")] [SerializeField] private float detectionRadius = 0.35f;
    [Header("Anchor")] [SerializeField] private LootAnchorMode lootAnchorMode = LootAnchorMode.PartyLeader;
    [Header("Selector Style")] [SerializeField] private Vector2 selectorButtonSize = new Vector2(52f, 74f); [SerializeField] private Color selectorNormalColor = new Color(0.18f, 0.18f, 0.18f, 1f); [SerializeField] private Color selectorSelectedColor = new Color(0.25f, 0.45f, 0.90f, 1f); [SerializeField] private Color selectorTextColor = Color.white; [SerializeField] private int selectorFontSize = 14;
    [Header("Stats Style")] [SerializeField] private int statsHeaderFontSize = 14; [SerializeField] private int statsLineFontSize = 12; [SerializeField] private float statsHeaderSpacing = 8f; [SerializeField] private float statsRowSpacing = 4f; [SerializeField] private Color statsTextColor = Color.white; [SerializeField] private Color statsSecondaryTextColor = new Color(0.78f, 0.78f, 0.78f, 1f); [SerializeField] private Color positiveBonusColor = new Color(0.45f, 0.90f, 0.45f, 1f); [SerializeField] private Color negativeBonusColor = new Color(0.95f, 0.45f, 0.45f, 1f); [SerializeField] private Color neutralBonusColor = new Color(0.65f, 0.65f, 0.65f, 1f); [SerializeField] private Color plusButtonEnabledColor = new Color(0.22f, 0.55f, 0.22f, 1f); [SerializeField] private Color plusButtonDisabledColor = new Color(0.22f, 0.22f, 0.22f, 0.85f);
    [Header("Stats Layout")] [SerializeField] private float statsVerticalSpacing = 3f; [SerializeField] private float statsMetaLineHeight = 18f; [SerializeField] private float statsRowHeight = 22f; [SerializeField] private float statsLabelWidth = 28f; [SerializeField] private float statsValueWidth = 40f; [SerializeField] private float statsBonusWidth = 40f; [SerializeField] private float statsButtonWidth = 18f;
    private Entity currentEntity; private bool isOpen; private readonly List<GameObject> spawnedUI = new List<GameObject>(); private readonly List<Entity> cachedPlayers = new List<Entity>();
    public bool IsOpen => isOpen; public Entity CurrentSelectedEntity => currentEntity; public event Action<Entity> OnSelectedEntityChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; ResolveExternalReferences(); AutoFindReferences(); BindCloseButton(); if (windowRoot != null) windowRoot.SetActive(false);
    }

    private void OnDisable() { HideTooltip(); }
    private void OnDestroy() { HideTooltip(); }

    private void Update()
    {
        if (Input.GetKeyDown(toggleLootKey)) { if (isOpen) CloseWindow(); else TryOpenForFirstPlayer(); return; }
        if (Input.GetKeyDown(closeLootKey) && isOpen) { CloseWindow(); return; }
        if (!isOpen) return;
        if (currentEntity == null || currentEntity.IsDead)
        {
            Entity fallback = FindFirstAlivePlayer();
            if (fallback != null) SelectPlayer(fallback); else CloseWindow();
        }
    }

    public void ConfigureReferences(GameObject newWindowRoot, Button newCloseButton, Transform newSelectorContentRoot, Transform newStatsContentRoot, Transform newEquippedContentRoot, Transform newInventoryContentRoot, Transform newGroundLootContentRoot, TMP_Text newTitleText, TMP_Text newHintText)
    {
        windowRoot = newWindowRoot; closeButton = newCloseButton; selectorContentRoot = newSelectorContentRoot; statsContentRoot = newStatsContentRoot; equippedContentRoot = newEquippedContentRoot; inventoryContentRoot = newInventoryContentRoot; groundLootContentRoot = newGroundLootContentRoot; titleText = newTitleText; hintText = newHintText; AutoFindReferences(); BindCloseButton();
    }

    public void SetItemButtonPrefab(ItemButtonUI prefab) { itemButtonPrefab = prefab; }

    public void OpenForCell(Entity entity, Vector2Int cell)
    {
        EnsureWindowReady();
        if (entity == null || partyInventory == null) return;
        if (itemButtonPrefab == null) { Debug.LogWarning("LootWindowUI: ItemButtonPrefab esta vazio."); return; }
        if (selectorContentRoot == null || statsContentRoot == null || equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null) { Debug.LogWarning("LootWindowUI: content roots ausentes."); return; }
        bool changed = currentEntity != entity; currentEntity = entity; isOpen = true; if (changed) OnSelectedEntityChanged?.Invoke(currentEntity); if (windowRoot != null) windowRoot.SetActive(true); RefreshUI();
    }

    public void CloseWindow()
    {
        HideTooltip(); isOpen = false; currentEntity = null; ClearSpawnedUI(); if (windowRoot != null) windowRoot.SetActive(false);
    }

    private void TryOpenForFirstPlayer()
    {
        Entity firstPlayer = partyAnchorService != null ? partyAnchorService.GetLeader() : null; if (firstPlayer == null) firstPlayer = FindFirstAlivePlayer(); if (firstPlayer != null) OpenForCell(firstPlayer, firstPlayer.GridPosition);
    }

    private void EnsureWindowReady()
    {
        ResolveExternalReferences();
        if (windowBuilder != null) { windowBuilder.SetLootWindowUI(this); if (!windowBuilder.IsBuilt) windowBuilder.Build(); }
        AutoFindReferences(); BindCloseButton();
    }

    private void ResolveExternalReferences()
    {
        if (windowBuilder == null) windowBuilder = FindFirstObjectByType<LootWindowGridAutoBuilder>();
        if (windowRoot == null && windowBuilder != null) windowRoot = windowBuilder.gameObject;
        if (partyInventory == null) partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyAnchorService == null) partyAnchorService = FindFirstObjectByType<PartyAnchorService>();
    }

    private Entity FindFirstAlivePlayer() { List<Entity> players = GetAvailablePlayers(); return players.Count > 0 ? players[0] : null; }

    private List<Entity> GetAvailablePlayers()
    {
        cachedPlayers.Clear();
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
        for (int i = 0; i < entities.Length; i++) if (entities[i] != null && entities[i].team == Team.Player && !entities[i].IsDead) cachedPlayers.Add(entities[i]);
        cachedPlayers.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return cachedPlayers;
    }

    private void SelectPlayer(Entity entity)
    {
        if (entity == null) return;
        bool changed = currentEntity != entity; currentEntity = entity; if (changed) OnSelectedEntityChanged?.Invoke(currentEntity); HideTooltip(); RefreshUI();
    }

    private void AutoFindReferences()
    {
        Transform searchRoot = windowRoot != null ? windowRoot.transform : null;
        if (closeButton == null) { Transform t = FindDeepChild(searchRoot, "CloseButton"); if (t != null) closeButton = t.GetComponent<Button>(); }
        if (selectorContentRoot == null) { Transform t = FindDeepChild(searchRoot, "SelectorContent"); if (t != null) selectorContentRoot = t; }
        if (statsContentRoot == null) { Transform t = FindDeepChild(searchRoot, "StatsContent"); if (t != null) statsContentRoot = t; }
        if (equippedContentRoot == null) { Transform t = FindDeepChild(searchRoot, "EquippedContent"); if (t != null) equippedContentRoot = t; }
        if (inventoryContentRoot == null) { Transform t = FindDeepChild(searchRoot, "InventoryContent"); if (t != null) inventoryContentRoot = t; }
        if (groundLootContentRoot == null) { Transform t = FindDeepChild(searchRoot, "GroundLootContent"); if (t != null) groundLootContentRoot = t; }
        if (titleText == null) { Transform t = FindDeepChild(searchRoot, "TitleText"); if (t != null) titleText = t.GetComponent<TMP_Text>(); }
        if (hintText == null) { Transform t = FindDeepChild(searchRoot, "HintText"); if (t != null) hintText = t.GetComponent<TMP_Text>(); }
    }

    private void BindCloseButton() { if (closeButton == null) return; closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(CloseWindow); }

    private void RefreshUI()
    {
        if (!isOpen || currentEntity == null || partyInventory == null) return;
        EnsureWindowReady();
        if (itemButtonPrefab == null) return;
        if (selectorContentRoot == null || statsContentRoot == null || equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null) return;
        ApplyStatsLayoutSettings();
        ClearSpawnedUI();
        if (titleText != null) titleText.text = $"Party Inventory - Equipando: {currentEntity.name}";
        if (hintText != null) hintText.text = "Mochila compartilhada | selecao troca equipamento e stats | E abre/fecha";
        BuildSelectorSection(); BuildStatsSection(); BuildEquippedSection(); BuildInventorySection(); BuildGroundLootSection();
    }

    private void BuildSelectorSection()
    {
        List<Entity> players = GetAvailablePlayers();
        for (int i = 0; i < players.Count; i++)
        {
            Entity player = players[i];
            GameObject buttonGO = CreateSelectorButton(player, i + 1, player == currentEntity);
            buttonGO.transform.SetParent(selectorContentRoot, false);
            spawnedUI.Add(buttonGO);
        }
    }

    private void BuildStatsSection()
    {
        if (currentEntity == null) return;
        CharacterStats stats = currentEntity.GetStatsComponent();
        if (stats == null) return;
        StatBlock baseStats = stats.BaseStats ?? new StatBlock();
        StatBlock levelBonus = stats.LevelBonus ?? new StatBlock();
        StatBlock pointBonus = stats.PointBonus ?? new StatBlock();
        StatBlock itemBonus = stats.ItemBonus ?? new StatBlock();
        CreateStatsMetaText(currentEntity.name, statsHeaderFontSize, FontStyles.Bold, statsTextColor);
        CreateStatsMetaText($"Level {currentEntity.Level}", statsLineFontSize, FontStyles.Normal, statsSecondaryTextColor);
        CreateStatsMetaText($"XP {currentEntity.CurrentXP} / {stats.GetXPToNextLevel()}", statsLineFontSize, FontStyles.Normal, statsSecondaryTextColor);
        CreateStatsMetaText($"Pontos livres: {currentEntity.UnspentStatPoints}", statsLineFontSize, FontStyles.Bold, currentEntity.UnspentStatPoints > 0 ? positiveBonusColor : statsSecondaryTextColor);
        CreateStatsSpacer(statsHeaderSpacing);
        CreateIntegerStatRow("HP", baseStats.hp + levelBonus.hp + pointBonus.hp, itemBonus.hp, () => currentEntity != null && currentEntity.SpendPointOnHP(1));
        CreateIntegerStatRow("ATK", baseStats.atk + levelBonus.atk + pointBonus.atk, itemBonus.atk, () => currentEntity != null && currentEntity.SpendPointOnATK(1));
        CreateIntegerStatRow("DEF", baseStats.def + levelBonus.def + pointBonus.def, itemBonus.def, () => currentEntity != null && currentEntity.SpendPointOnDEF(1));
        CreateIntegerStatRow("AP", baseStats.ap + levelBonus.ap + pointBonus.ap, itemBonus.ap, () => currentEntity != null && currentEntity.SpendPointOnAP(1));
        CreateFloatStatRow("CRIT", baseStats.crit + levelBonus.crit + pointBonus.crit, itemBonus.crit, () => currentEntity != null && currentEntity.SpendPointOnCRIT(1f, 1));
    }

    private GameObject CreateSelectorButton(Entity player, int displayIndex, bool selected)
    {
        GameObject root = new GameObject($"PlayerSelector_{displayIndex}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rootRect = root.GetComponent<RectTransform>(); rootRect.sizeDelta = selectorButtonSize;
        LayoutElement layout = root.GetComponent<LayoutElement>(); layout.minWidth = selectorButtonSize.x; layout.preferredWidth = selectorButtonSize.x; layout.minHeight = selectorButtonSize.y; layout.preferredHeight = selectorButtonSize.y; layout.flexibleWidth = 0f; layout.flexibleHeight = 0f;
        Image bg = root.GetComponent<Image>(); bg.color = selected ? selectorSelectedColor : selectorNormalColor;
        Button button = root.GetComponent<Button>(); button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => SelectPlayer(player));
        GameObject iconGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image)); iconGO.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconGO.GetComponent<RectTransform>(); iconRect.anchorMin = new Vector2(0.5f, 1f); iconRect.anchorMax = new Vector2(0.5f, 1f); iconRect.pivot = new Vector2(0.5f, 1f); iconRect.sizeDelta = new Vector2(34f, 34f); iconRect.anchoredPosition = new Vector2(0f, -6f);
        Image icon = iconGO.GetComponent<Image>(); icon.raycastTarget = false; icon.preserveAspect = true; icon.sprite = GetEntityPortrait(player); icon.color = icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        GameObject labelGO = new GameObject("Label", typeof(RectTransform)); labelGO.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>(); labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 0f); labelRect.pivot = new Vector2(0.5f, 0f); labelRect.offsetMin = new Vector2(4f, 4f); labelRect.offsetMax = new Vector2(-4f, 22f);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>(); label.text = displayIndex.ToString(); label.fontSize = selectorFontSize; label.color = selectorTextColor; label.alignment = TextAlignmentOptions.Center; label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
        return root;
    }

    private void CreateStatsMetaText(string value, int fontSize, FontStyles fontStyle, Color color)
    {
        GameObject root = new GameObject("StatsMetaText", typeof(RectTransform), typeof(LayoutElement));
        root.transform.SetParent(statsContentRoot, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0f, statsMetaLineHeight);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = statsMetaLineHeight;
        layout.preferredHeight = statsMetaLineHeight;
        layout.flexibleWidth = 0f;

        CreateStretchText(root.transform, value, fontSize, fontStyle, color, TextAlignmentOptions.Left);
        spawnedUI.Add(root);
    }

    private void CreateStatsSpacer(float height)
    {
        GameObject spacer = new GameObject("StatsSpacer", typeof(RectTransform), typeof(LayoutElement)); spacer.transform.SetParent(statsContentRoot, false);
        LayoutElement layout = spacer.GetComponent<LayoutElement>(); layout.minHeight = height; layout.preferredHeight = height;
        spawnedUI.Add(spacer);
    }

    private void CreateIntegerStatRow(string label, int baseTotal, int itemBonus, Func<bool> spendAction) { CreateStatRow(label, baseTotal.ToString(), FormatIntegerBonus(itemBonus), itemBonus, spendAction); }
    private void CreateFloatStatRow(string label, float baseTotal, float itemBonus, Func<bool> spendAction) { CreateStatRow(label, $"{baseTotal:0.#}", FormatFloatBonus(itemBonus), itemBonus, spendAction); }

    private void CreateStatRow(string label, string baseValue, string bonusValue, float rawBonus, Func<bool> spendAction)
    {
        GameObject row = new GameObject($"StatRow_{label}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)); row.transform.SetParent(statsContentRoot, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, statsRowHeight);
        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>(); rowLayout.childAlignment = TextAnchor.MiddleLeft; rowLayout.childControlWidth = false; rowLayout.childControlHeight = false; rowLayout.childForceExpandWidth = false; rowLayout.childForceExpandHeight = false; rowLayout.spacing = statsRowSpacing; rowLayout.padding = new RectOffset(0, 0, 0, 0);
        LayoutElement rowElement = row.GetComponent<LayoutElement>(); rowElement.minHeight = statsRowHeight; rowElement.preferredHeight = statsRowHeight; rowElement.flexibleWidth = 0f;
        CreateStatCell(row.transform, label, statsLabelWidth, statsTextColor, FontStyles.Bold);
        CreateStatCell(row.transform, baseValue, statsValueWidth, statsTextColor, FontStyles.Normal);
        CreateStatCell(row.transform, bonusValue, statsBonusWidth, GetBonusColor(rawBonus), FontStyles.Normal);
        CreatePlusButton(row.transform, spendAction);
        spawnedUI.Add(row);
    }

    private void CreateStatCell(Transform parent, string value, float width, Color color, FontStyles style)
    {
        GameObject cell = new GameObject("Cell", typeof(RectTransform), typeof(LayoutElement));
        cell.transform.SetParent(parent, false);

        RectTransform cellRect = cell.GetComponent<RectTransform>();
        cellRect.sizeDelta = new Vector2(width, statsRowHeight);

        LayoutElement layout = cell.GetComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = statsRowHeight;
        layout.preferredHeight = statsRowHeight;
        layout.flexibleHeight = 0f;

        CreateStretchText(cell.transform, value, statsLineFontSize, style, color, TextAlignmentOptions.Left);
    }

    private void CreatePlusButton(Transform parent, Func<bool> spendAction)
    {
        GameObject buttonGO = new GameObject("PlusButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)); buttonGO.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(statsButtonWidth, statsButtonWidth);
        LayoutElement layout = buttonGO.GetComponent<LayoutElement>(); layout.minWidth = statsButtonWidth; layout.preferredWidth = statsButtonWidth; layout.flexibleWidth = 0f; layout.minHeight = statsButtonWidth; layout.preferredHeight = statsButtonWidth; layout.flexibleHeight = 0f;
        Image image = buttonGO.GetComponent<Image>(); Button button = buttonGO.GetComponent<Button>();
        bool canSpend = currentEntity != null && currentEntity.UnspentStatPoints > 0;
        image.color = canSpend ? plusButtonEnabledColor : plusButtonDisabledColor; button.interactable = canSpend;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => { if (spendAction != null && spendAction.Invoke()) HandleStatsChanged(); });
        ColorBlock colors = button.colors; colors.normalColor = image.color; colors.highlightedColor = canSpend ? Color.Lerp(image.color, Color.white, 0.15f) : image.color; colors.pressedColor = canSpend ? Color.Lerp(image.color, Color.black, 0.15f) : image.color; colors.disabledColor = plusButtonDisabledColor; colors.selectedColor = colors.highlightedColor; button.colors = colors;
        CreateStretchText(buttonGO.transform, "+", statsLineFontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    private void ApplyStatsLayoutSettings()
    {
        if (statsContentRoot == null)
            return;

        VerticalLayoutGroup statsLayout = statsContentRoot.GetComponent<VerticalLayoutGroup>();
        if (statsLayout != null)
        {
            statsLayout.spacing = statsVerticalSpacing;
            statsLayout.padding = new RectOffset(0, 0, 0, 0);
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = false;
            statsLayout.childForceExpandWidth = false;
            statsLayout.childForceExpandHeight = false;
        }
    }

    private void CreateStretchText(Transform parent, string value, int fontSize, FontStyles fontStyle, Color color, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(parent, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    private string FormatIntegerBonus(int value) { if (value > 0) return $"+{value}"; if (value < 0) return value.ToString(); return "+0"; }
    private string FormatFloatBonus(float value) { if (value > 0f) return $"+{value:0.#}"; if (value < 0f) return value.ToString("0.#"); return "+0"; }
    private Color GetBonusColor(float value) { if (value > 0f) return positiveBonusColor; if (value < 0f) return negativeBonusColor; return neutralBonusColor; }
    private void HandleStatsChanged() { HideTooltip(); RefreshUI(); }

    private Sprite GetEntityPortrait(Entity entity)
    {
        if (entity == null) return null;
        SpriteRenderer spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private void BuildEquippedSection()
    {
        CreateEquippedButton(EquipmentSlotType.Weapon); CreateEquippedButton(EquipmentSlotType.Armor); CreateEquippedButton(EquipmentSlotType.Accessory);
    }

    private void CreateEquippedButton(EquipmentSlotType slotType)
    {
        InventoryItemEntry equipped = partyInventory.GetEquippedEntry(currentEntity, slotType);
        ItemButtonUI button = Instantiate(itemButtonPrefab, equippedContentRoot); button.ConfigureAsEquippedSlot(slotType);
        button.Setup(equipped, equipped == null || equipped.IsEmpty ? null : () => { bool moved = partyInventory.UnequipToInventory(currentEntity, slotType); if (moved) RefreshUI(); }, null, equipped != null && !equipped.IsEmpty, sourceButton => HandleDropOnEquippedSlot(slotType, sourceButton), null);
        spawnedUI.Add(button.gameObject);
    }

    private void BuildInventorySection()
    {
        IReadOnlyList<InventoryItemEntry> items = partyInventory.Items;
        for (int i = 0; i < items.Count; i++)
        {
            int index = i; InventoryItemEntry entry = items[index]; InventoryItemEntry equippedCompare = null; if (entry != null && !entry.IsEmpty) equippedCompare = partyInventory.GetEquippedEntry(currentEntity, entry.SlotType);
            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryContentRoot); button.ConfigureAsInventorySlot(index);
            bool hasItem = entry != null && !entry.IsEmpty;
            button.Setup(hasItem ? entry : null, hasItem ? () => { bool equipped = partyInventory.TryEquipFromInventory(currentEntity, index); if (equipped) RefreshUI(); } : null, hasItem ? () => { bool equipped = partyInventory.TryEquipFromInventory(currentEntity, index); if (equipped) RefreshUI(); } : null, hasItem, sourceButton => HandleDropOnInventorySlot(index, sourceButton), equippedCompare);
            spawnedUI.Add(button.gameObject);
        }
    }

    private void BuildGroundLootSection()
    {
        Vector2Int anchorCell = GetPartyAnchorCell(); List<GroundItem> items = GetGroundItemsInCell(anchorCell); int maxGroundSlots = 20;
        for (int i = 0; i < maxGroundSlots; i++)
        {
            ItemButtonUI button = Instantiate(itemButtonPrefab, groundLootContentRoot);
            if (i >= items.Count || items[i] == null) { button.ClearContext(); button.Setup(null, null, null, false, null, null); spawnedUI.Add(button.gameObject); continue; }
            GroundItem groundItem = items[i]; InventoryItemEntry entry = groundItem.ToInventoryEntry(); InventoryItemEntry equippedCompare = null; if (entry != null && !entry.IsEmpty) equippedCompare = partyInventory.GetEquippedEntry(currentEntity, entry.SlotType);
            button.ConfigureAsGroundSlot(groundItem);
            button.Setup(entry, () => { bool moved = groundItem.TrySendToPartyInventory(partyInventory); if (moved) RefreshUI(); }, () => { bool equipped = groundItem.TryEquipDirectToParty(currentEntity, partyInventory); if (equipped) RefreshUI(); }, true, null, equippedCompare);
            spawnedUI.Add(button.gameObject);
        }
    }

    private Vector2Int GetPartyAnchorCell()
    {
        if (lootAnchorMode == LootAnchorMode.SelectedEntity && currentEntity != null) return currentEntity.GridPosition;
        if (lootAnchorMode == LootAnchorMode.PartyLeader && partyAnchorService != null)
        {
            Entity leader = partyAnchorService.GetLeader();
            if (leader != null) return leader.GridPosition;
        }
        List<Entity> players = GetAvailablePlayers();
        if (players.Count > 0) return players[0].GridPosition;
        return currentEntity != null ? currentEntity.GridPosition : Vector2Int.zero;
    }

    private void HandleDropOnInventorySlot(int targetIndex, ItemButtonUI sourceButton)
    {
        if (partyInventory == null || sourceButton == null) return;
        bool changed = false;
        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory: changed = partyInventory.MoveItem(sourceButton.InventoryIndex, targetIndex); break;
            case ItemButtonSlotKind.Equipped: changed = partyInventory.UnequipToInventorySlot(currentEntity, sourceButton.EquippedSlotType, targetIndex); break;
            case ItemButtonSlotKind.Ground: if (sourceButton.GroundItemRef != null) changed = sourceButton.GroundItemRef.TrySendToPartyInventorySlot(partyInventory, targetIndex); break;
        }
        if (changed) RefreshUI();
    }

    private void HandleDropOnEquippedSlot(EquipmentSlotType targetSlotType, ItemButtonUI sourceButton)
    {
        if (partyInventory == null || currentEntity == null || sourceButton == null) return;
        bool changed = false;
        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory: changed = partyInventory.TryEquipFromInventoryToSlot(currentEntity, sourceButton.InventoryIndex, targetSlotType); break;
            case ItemButtonSlotKind.Ground: if (sourceButton.GroundItemRef != null) changed = sourceButton.GroundItemRef.TryEquipDirectToPartySlot(currentEntity, partyInventory, targetSlotType); break;
        }
        if (changed) RefreshUI();
    }

    private List<GroundItem> GetGroundItemsInCell(Vector2Int cell)
    {
        List<GroundItem> result = new List<GroundItem>();
        if (GridManager.Instance == null) return result;
        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);
        if (hits == null) return result;
        for (int i = 0; i < hits.Length; i++) { GroundItem item = hits[i].GetComponent<GroundItem>(); if (item != null) result.Add(item); }
        return result;
    }

    private void ClearSpawnedUI()
    {
        for (int i = 0; i < spawnedUI.Count; i++) if (spawnedUI[i] != null) Destroy(spawnedUI[i]);
        spawnedUI.Clear();
    }

    private void HideTooltip() { if (ItemTooltipUI.Instance != null) ItemTooltipUI.Instance.Hide(); }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null) return null;
        if (parent.name == targetName) return parent;
        for (int i = 0; i < parent.childCount; i++) { Transform result = FindDeepChild(parent.GetChild(i), targetName); if (result != null) return result; }
        return null;
    }
}
