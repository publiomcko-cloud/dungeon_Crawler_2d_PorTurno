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

    public string GetDisplayName()
    {
        if (staticItem != null)
            return staticItem.itemName;

        if (generatedItem != null)
            return generatedItem.itemName;

        return "Empty";
    }

    public InventoryItemEntry ToInventoryEntry()
    {
        if (staticItem != null)
            return InventoryItemEntry.FromStatic(staticItem);

        if (generatedItem != null)
            return InventoryItemEntry.FromGenerated(generatedItem);

        return null;
    }

    public bool TrySendToInventory(PlayerInventory inventory)
    {
        if (consumed || inventory == null)
            return false;

        bool added = false;

        if (staticItem != null)
            added = inventory.AddStaticItem(staticItem);
        else if (generatedItem != null)
            added = inventory.AddGeneratedItem(generatedItem);

        if (!added)
            return false;

        Consume();
        return true;
    }

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