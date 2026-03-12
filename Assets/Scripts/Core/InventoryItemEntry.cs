using System;
using UnityEngine;

[Serializable]
public class InventoryItemEntry
{
    [SerializeField] private ItemData staticItem;
    [SerializeField] private GeneratedItemInstance generatedItem;

    public ItemData StaticItem => staticItem;
    public GeneratedItemInstance GeneratedItem => generatedItem;

    public bool IsEmpty => staticItem == null && generatedItem == null;
    public bool IsStaticItem => staticItem != null;
    public bool IsGeneratedItem => generatedItem != null;

    public EquipmentSlotType SlotType
    {
        get
        {
            if (staticItem != null)
                return staticItem.slotType;

            if (generatedItem != null)
                return generatedItem.slotType;

            return EquipmentSlotType.Weapon;
        }
    }

    public string ItemName
    {
        get
        {
            if (staticItem != null)
                return staticItem.itemName;

            if (generatedItem != null)
                return generatedItem.itemName;

            return "";
        }
    }

    public Sprite Icon
    {
        get
        {
            if (staticItem != null)
                return staticItem.icon;

            if (generatedItem != null)
                return generatedItem.icon;

            return null;
        }
    }

    public int RequiredLevel
    {
        get
        {
            if (staticItem != null)
                return staticItem.requiredLevel;

            if (generatedItem != null)
                return generatedItem.requiredLevel;

            return 1;
        }
    }

    public StatBlock StatBonus
    {
        get
        {
            if (staticItem != null && staticItem.statBonus != null)
                return staticItem.statBonus;

            if (generatedItem != null && generatedItem.statBonus != null)
                return generatedItem.statBonus;

            return new StatBlock();
        }
    }

    public static InventoryItemEntry FromStatic(ItemData item)
    {
        if (item == null)
            return null;

        return new InventoryItemEntry
        {
            staticItem = item,
            generatedItem = null
        };
    }

    public static InventoryItemEntry FromGenerated(GeneratedItemInstance item)
    {
        if (item == null)
            return null;

        return new InventoryItemEntry
        {
            staticItem = null,
            generatedItem = item.Clone()
        };
    }

    public InventoryItemEntry Clone()
    {
        if (IsStaticItem)
            return FromStatic(staticItem);

        if (IsGeneratedItem)
            return FromGenerated(generatedItem);

        return new InventoryItemEntry();
    }

    public void Clear()
    {
        staticItem = null;
        generatedItem = null;
    }
}