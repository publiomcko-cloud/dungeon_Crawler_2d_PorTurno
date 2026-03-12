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
            {
                CloseWindow();
            }
            else
            {
                TryOpenForFirstPlayer();
            }

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
        {
            CloseWindow();
        }
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
            hintText.text = "E abre/fecha | Esc fecha | Click chão -> mochila | Shift+Click chão -> equipar | Click mochila -> equipar | Click equipado -> mochila";

        BuildEquippedSection();
        BuildInventorySection();
        BuildGroundLootSection();
    }

    private void BuildEquippedSection()
    {
        CreateEquippedButton(EquipmentSlotType.Weapon, "Weapon");
        CreateEquippedButton(EquipmentSlotType.Armor, "Armor");
        CreateEquippedButton(EquipmentSlotType.Accessory, "Accessory");
    }

    private void CreateEquippedButton(EquipmentSlotType slotType, string slotLabel)
    {
        InventoryItemEntry equipped = currentInventory.GetEquippedEntry(slotType);

        string text = equipped == null || equipped.IsEmpty
            ? $"{slotLabel}: Empty"
            : $"{slotLabel}: {equipped.ItemName}";

        ItemButtonUI button = Instantiate(itemButtonPrefab, equippedContentRoot);

        button.Setup(
            text,
            equipped == null || equipped.IsEmpty
                ? null
                : () =>
                {
                    currentInventory.UnequipToInventory(slotType);
                    RefreshUI();
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

            string text = entry == null || entry.IsEmpty
                ? $"Slot {index}: Empty"
                : $"Slot {index}: {entry.ItemName}";

            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryContentRoot);

            if (entry == null || entry.IsEmpty)
            {
                button.Setup(text, null, null);
            }
            else
            {
                button.Setup(
                    text,
                    () =>
                    {
                        currentInventory.TryEquipFromInventory(index);
                        RefreshUI();
                    },
                    () =>
                    {
                        currentInventory.TryEquipFromInventory(index);
                        RefreshUI();
                    }
                );
            }

            spawnedUI.Add(button.gameObject);
        }
    }

    private void BuildGroundLootSection()
    {
        List<GroundItem> items = GetGroundItemsInCell(currentEntity.GridPosition);

        if (items.Count == 0)
        {
            ItemButtonUI emptyButton = Instantiate(itemButtonPrefab, groundLootContentRoot);
            emptyButton.Setup("Ground: Empty", null, null);
            spawnedUI.Add(emptyButton.gameObject);
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            GroundItem groundItem = items[i];
            if (groundItem == null)
                continue;

            string text = $"Ground: {groundItem.GetDisplayName()}";

            ItemButtonUI button = Instantiate(itemButtonPrefab, groundLootContentRoot);

            button.Setup(
                text,
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
                }
            );

            spawnedUI.Add(button.gameObject);
        }
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