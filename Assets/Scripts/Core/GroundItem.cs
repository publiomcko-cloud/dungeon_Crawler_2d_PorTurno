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
        RefreshVisual();
    }

    public void SetupGenerated(GeneratedItemInstance item)
    {
        generatedItem = item != null ? item.Clone() : null;
        staticItem = null;
        RefreshVisual();
    }

    public bool TryAutoEquip(Entity entity)
    {
        if (entity == null)
            return false;

        bool equipped = false;

        if (staticItem != null)
            equipped = entity.EquipItem(staticItem);
        else if (generatedItem != null)
            equipped = entity.EquipGeneratedItem(generatedItem);

        if (equipped)
            Destroy(gameObject);

        return equipped;
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