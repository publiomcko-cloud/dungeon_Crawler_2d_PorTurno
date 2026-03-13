using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GroundItem : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ItemData staticItem;
    [SerializeField] private GeneratedItemInstance generatedItem;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color staticItemColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private Color generatedItemColor = new Color(1f, 0.85f, 0.2f, 1f);

    private bool consumed = false;

    public bool HasStaticItem => staticItem != null;
    public bool HasGeneratedItem => generatedItem != null;

    public ItemData StaticItem => staticItem;
    public GeneratedItemInstance GeneratedItem => generatedItem;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        RefreshVisual();
    }

    public void SetupStatic(ItemData item)
    {
        staticItem = item;
        generatedItem = null;
        consumed = false;
        RefreshVisual();
    }

    public void SetupGenerated(GeneratedItemInstance item)
    {
        generatedItem = item != null ? item.Clone() : null;
        staticItem = null;
        consumed = false;
        RefreshVisual();
    }

    public InventoryItemEntry ToInventoryEntry()
    {
        if (staticItem != null)
            return InventoryItemEntry.FromStatic(staticItem);

        if (generatedItem != null)
            return InventoryItemEntry.FromGenerated(generatedItem);

        return null;
    }

    [System.Obsolete("Fluxo legado. Prefira métodos com PartyInventory.")]
    public bool TrySendToInventory(PlayerInventory inventory)
    {
        if (consumed || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool added = inventory.AddEntry(entry);
        if (!added)
            return false;

        Consume();
        return true;
    }

    [System.Obsolete("Fluxo legado. Prefira métodos com PartyInventory.")]
    public bool TrySendToInventorySlot(PlayerInventory inventory, int targetIndex)
    {
        if (consumed || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool added = inventory.AddEntryToIndex(entry, targetIndex);
        if (!added)
            return false;

        Consume();
        return true;
    }

    [System.Obsolete("Fluxo legado. Prefira métodos com PartyInventory.")]
    public bool TryEquipDirect(Entity entity, PlayerInventory inventory)
    {
        if (consumed || entity == null || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool equipped = inventory.TryEquipEntryDirectly(entry);
        if (!equipped)
            return false;

        Consume();
        return true;
    }

    [System.Obsolete("Fluxo legado. Prefira métodos com PartyInventory.")]
    public bool TryEquipDirectToSlot(Entity entity, PlayerInventory inventory, EquipmentSlotType targetSlotType)
    {
        if (consumed || entity == null || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool equipped = inventory.TryEquipEntryDirectlyToSlot(entry, targetSlotType);
        if (!equipped)
            return false;

        Consume();
        return true;
    }

    public bool TrySendToPartyInventory(PartyInventory inventory)
    {
        if (consumed || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool added = inventory.AddEntry(entry);
        if (!added)
            return false;

        Consume();
        return true;
    }

    public bool TrySendToPartyInventorySlot(PartyInventory inventory, int targetIndex)
    {
        if (consumed || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool added = inventory.AddEntryToIndex(entry, targetIndex);
        if (!added)
            return false;

        Consume();
        return true;
    }

    public bool TryEquipDirectToParty(Entity entity, PartyInventory inventory)
    {
        if (consumed || entity == null || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool equipped = inventory.TryEquipEntryDirectly(entity, entry);
        if (!equipped)
            return false;

        Consume();
        return true;
    }

    public bool TryEquipDirectToPartySlot(Entity entity, PartyInventory inventory, EquipmentSlotType targetSlotType)
    {
        if (consumed || entity == null || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool equipped = inventory.TryEquipEntryDirectlyToSlot(entity, entry, targetSlotType);
        if (!equipped)
            return false;

        Consume();
        return true;
    }

    private void Consume()
    {
        if (consumed)
            return;

        consumed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
            return;

        if (generatedItem != null)
            spriteRenderer.color = generatedItemColor;
        else if (staticItem != null)
            spriteRenderer.color = staticItemColor;
    }
}