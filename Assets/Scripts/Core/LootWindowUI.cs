using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootWindowUI : MonoBehaviour
{
    public static LootWindowUI Instance;

    [Header("Window")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
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

    private Entity currentEntity;
    private PlayerInventory currentInventory;
    private bool isOpen = false;

    private readonly List<GameObject> spawnedUI = new List<GameObject>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoFindReferences();
        BindCloseButton();

        if (windowRoot != null)
            windowRoot.SetActive(false);
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

        if (currentEntity == null || currentInventory == null || currentEntity.IsDead)
            CloseWindow();
    }

    public void ConfigureReferences(
        GameObject newWindowRoot,
        Button newCloseButton,
        Transform newEquippedContentRoot,
        Transform newInventoryContentRoot,
        Transform newGroundLootContentRoot,
        TMP_Text newTitleText,
        TMP_Text newHintText)
    {
        windowRoot = newWindowRoot;
        closeButton = newCloseButton;
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

    public void OpenForCell(Entity entity, PlayerInventory inventory, Vector2Int cell)
    {
        AutoFindReferences();

        if (entity == null || inventory == null)
            return;

        if (itemButtonPrefab == null)
            return;

        if (equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
            return;

        currentEntity = entity;
        currentInventory = inventory;
        isOpen = true;

        if (windowRoot != null)
            windowRoot.SetActive(true);

        RefreshUI();
    }

    public void CloseWindow()
    {
        isOpen = false;
        currentEntity = null;
        currentInventory = null;

        ClearSpawnedUI();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void TryOpenForFirstPlayer()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            if (entity == null || entity.IsDead || entity.team != Team.Player)
                continue;

            PlayerInventory inventory = entity.GetComponent<PlayerInventory>();
            if (inventory == null)
                continue;

            OpenForCell(entity, inventory, entity.GridPosition);
            return;
        }
    }

    private void AutoFindReferences()
    {
        if (closeButton == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "CloseButton");
            if (t != null)
                closeButton = t.GetComponent<Button>();
        }

        if (equippedContentRoot == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "EquippedContent");
            if (t != null)
                equippedContentRoot = t;
        }

        if (inventoryContentRoot == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "InventoryContent");
            if (t != null)
                inventoryContentRoot = t;
        }

        if (groundLootContentRoot == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "GroundLootContent");
            if (t != null)
                groundLootContentRoot = t;
        }

        if (titleText == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "TitleText");
            if (t != null)
                titleText = t.GetComponent<TMP_Text>();
        }

        if (hintText == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "HintText");
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
        if (!isOpen || currentEntity == null || currentInventory == null)
            return;

        AutoFindReferences();

        if (itemButtonPrefab == null)
            return;

        if (equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
            return;

        ClearSpawnedUI();

        if (titleText != null)
            titleText.text = $"Inventory - {currentEntity.name}";

        if (hintText != null)
            hintText.text = "Click chão -> mochila | Shift+Click chão -> equipar | Click mochila -> equipar | Click equipado -> mochila | Drag ativo";

        BuildEquippedSection();
        BuildInventorySection();
        BuildGroundLootSection();
    }

    private void BuildEquippedSection()
    {
        CreateEquippedButton(EquipmentSlotType.Weapon);
        CreateEquippedButton(EquipmentSlotType.Armor);
        CreateEquippedButton(EquipmentSlotType.Accessory);
    }

    private void CreateEquippedButton(EquipmentSlotType slotType)
    {
        InventoryItemEntry equipped = currentInventory.GetEquippedEntry(slotType);

        ItemButtonUI button = Instantiate(itemButtonPrefab, equippedContentRoot);
        button.ConfigureAsEquippedSlot(slotType);

        button.Setup(
            equipped,
            equipped == null || equipped.IsEmpty
                ? null
                : () =>
                {
                    bool moved = currentInventory.UnequipToInventory(slotType);
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
        IReadOnlyList<InventoryItemEntry> items = currentInventory.Items;

        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            InventoryItemEntry entry = items[index];
            InventoryItemEntry equippedCompare = null;

            if (entry != null && !entry.IsEmpty)
                equippedCompare = currentInventory.GetEquippedEntry(entry.SlotType);

            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryContentRoot);
            button.ConfigureAsInventorySlot(index);

            bool hasItem = entry != null && !entry.IsEmpty;

            button.Setup(
                hasItem ? entry : null,
                hasItem
                    ? () =>
                    {
                        bool equipped = currentInventory.TryEquipFromInventory(index);
                        if (equipped)
                            RefreshUI();
                    }
                    : null,
                hasItem
                    ? () =>
                    {
                        bool equipped = currentInventory.TryEquipFromInventory(index);
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
        List<GroundItem> items = GetGroundItemsInCell(currentEntity.GridPosition);
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
                equippedCompare = currentInventory.GetEquippedEntry(entry.SlotType);

            button.ConfigureAsGroundSlot(groundItem);

            button.Setup(
                entry,
                () =>
                {
                    bool moved = groundItem.TrySendToInventory(currentInventory);
                    if (moved)
                        RefreshUI();
                },
                () =>
                {
                    bool equipped = groundItem.TryEquipDirect(currentEntity, currentInventory);
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

    private void HandleDropOnInventorySlot(int targetIndex, ItemButtonUI sourceButton)
    {
        if (currentInventory == null || sourceButton == null)
            return;

        bool changed = false;

        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory:
                changed = currentInventory.MoveItem(sourceButton.InventoryIndex, targetIndex);
                break;

            case ItemButtonSlotKind.Equipped:
                changed = currentInventory.UnequipToInventorySlot(sourceButton.EquippedSlotType, targetIndex);
                break;

            case ItemButtonSlotKind.Ground:
                if (sourceButton.GroundItemRef != null)
                    changed = sourceButton.GroundItemRef.TrySendToInventorySlot(currentInventory, targetIndex);
                break;
        }

        if (changed)
            RefreshUI();
    }

    private void HandleDropOnEquippedSlot(EquipmentSlotType targetSlotType, ItemButtonUI sourceButton)
    {
        if (currentInventory == null || currentEntity == null || sourceButton == null)
            return;

        bool changed = false;

        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory:
                changed = currentInventory.TryEquipFromInventoryToSlot(sourceButton.InventoryIndex, targetSlotType);
                break;

            case ItemButtonSlotKind.Ground:
                if (sourceButton.GroundItemRef != null)
                    changed = sourceButton.GroundItemRef.TryEquipDirectToSlot(currentEntity, currentInventory, targetSlotType);
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