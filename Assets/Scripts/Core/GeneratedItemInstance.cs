using System;
using UnityEngine;

[Serializable]
public class GeneratedItemInstance
{
    public string itemName;
    public string description;
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
            slotType = slotType,
            rarity = rarity,
            requiredLevel = requiredLevel,
            value = value,
            statBonus = statBonus != null ? statBonus.Clone() : new StatBlock { hp = 0, atk = 0, def = 0, ap = 0, crit = 0f }
        };
    }
}