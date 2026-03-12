using UnityEngine;

[CreateAssetMenu(fileName = "NewItemGenerationProfile", menuName = "RPG/Item Generation Profile")]
public class ItemGenerationProfile : ScriptableObject
{
    [Header("Identity")]
    public string generatedNamePrefix = "Generated";

    [Header("Base")]
    public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
    public ItemRarity rarity = ItemRarity.Common;
    public int requiredLevel = 1;
    public int value = 10;

    [Header("Stat Ranges")]
    public Vector2Int hpRange = new Vector2Int(0, 0);
    public Vector2Int atkRange = new Vector2Int(0, 0);
    public Vector2Int defRange = new Vector2Int(0, 0);
    public Vector2Int apRange = new Vector2Int(0, 0);
    public Vector2 critRange = new Vector2(0f, 0f);

    public StatBlock RollStats()
    {
        return new StatBlock
        {
            hp = Random.Range(Mathf.Min(hpRange.x, hpRange.y), Mathf.Max(hpRange.x, hpRange.y) + 1),
            atk = Random.Range(Mathf.Min(atkRange.x, atkRange.y), Mathf.Max(atkRange.x, atkRange.y) + 1),
            def = Random.Range(Mathf.Min(defRange.x, defRange.y), Mathf.Max(defRange.x, defRange.y) + 1),
            ap = Random.Range(Mathf.Min(apRange.x, apRange.y), Mathf.Max(apRange.x, apRange.y) + 1),
            crit = Random.Range(Mathf.Min(critRange.x, critRange.y), Mathf.Max(critRange.x, critRange.y))
        };
    }
}