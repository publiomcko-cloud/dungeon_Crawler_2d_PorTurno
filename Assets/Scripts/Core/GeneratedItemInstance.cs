using System;
using UnityEngine;

[Serializable]
public class GeneratedItemInstance
{
    public string itemName;
    public string description;
    public Sprite icon;
    public EquipmentSlotType slotType;
    public ItemRarity rarity;
    public int requiredLevel;
    public int value;
    public StatBlock statBonus;

    public GeneratedItemInstance Clone()
    {
        return new GeneratedItemInstance
        {
            itemName = itemName,
            description = description,
            icon = icon,
            slotType = slotType,
            rarity = rarity,
            requiredLevel = requiredLevel,
            value = value,
            statBonus = statBonus != null ? statBonus.Clone() : new StatBlock()
        };
    }
}