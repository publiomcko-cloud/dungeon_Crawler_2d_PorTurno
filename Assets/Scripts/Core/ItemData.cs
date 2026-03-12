using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";
    [TextArea] public string description = "";

    [Header("Visual")]
    public Sprite icon;

    [Header("Classification")]
    public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Requirements")]
    public int requiredLevel = 1;

    [Header("Economy")]
    public int value = 10;

    [Header("Stat Bonus")]
    public StatBlock statBonus = new StatBlock();
}