using System;
using UnityEngine;

public class EquipmentSlots : MonoBehaviour
{
    [Header("Static Equipped Items")]
    [SerializeField] private ItemData weapon;
    [SerializeField] private ItemData armor;
    [SerializeField] private ItemData accessory;

    [Header("Generated Equipped Items")]
    [SerializeField] private GeneratedItemInstance generatedWeapon;
    [SerializeField] private GeneratedItemInstance generatedArmor;
    [SerializeField] private GeneratedItemInstance generatedAccessory;

    public event Action OnEquipmentChanged;

    public ItemData Weapon => weapon;
    public ItemData Armor => armor;
    public ItemData Accessory => accessory;

    public GeneratedItemInstance GeneratedWeapon => generatedWeapon;
    public GeneratedItemInstance GeneratedArmor => generatedArmor;
    public GeneratedItemInstance GeneratedAccessory => generatedAccessory;

    public ItemData GetItemInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return weapon;
            case EquipmentSlotType.Armor: return armor;
            case EquipmentSlotType.Accessory: return accessory;
            default: return null;
        }
    }

    public GeneratedItemInstance GetGeneratedItemInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return generatedWeapon;
            case EquipmentSlotType.Armor: return generatedArmor;
            case EquipmentSlotType.Accessory: return generatedAccessory;
            default: return null;
        }
    }

    public bool Equip(ItemData item, int ownerLevel)
    {
        if (item == null)
            return false;

        if (ownerLevel < item.requiredLevel)
            return false;

        ClearSlot(item.slotType);

        switch (item.slotType)
        {
            case EquipmentSlotType.Weapon:
                weapon = item;
                break;

            case EquipmentSlotType.Armor:
                armor = item;
                break;

            case EquipmentSlotType.Accessory:
                accessory = item;
                break;

            default:
                return false;
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool EquipGenerated(GeneratedItemInstance item, int ownerLevel)
    {
        if (item == null)
            return false;

        if (ownerLevel < item.requiredLevel)
            return false;

        ClearSlot(item.slotType);

        switch (item.slotType)
        {
            case EquipmentSlotType.Weapon:
                generatedWeapon = item.Clone();
                break;

            case EquipmentSlotType.Armor:
                generatedArmor = item.Clone();
                break;

            case EquipmentSlotType.Accessory:
                generatedAccessory = item.Clone();
                break;

            default:
                return false;
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public void Unequip(EquipmentSlotType slotType)
    {
        bool changed = HasAnythingInSlot(slotType);

        ClearSlot(slotType);

        if (changed)
            OnEquipmentChanged?.Invoke();
    }

    public void UnequipAll()
    {
        bool changed =
            weapon != null || armor != null || accessory != null ||
            generatedWeapon != null || generatedArmor != null || generatedAccessory != null;

        weapon = null;
        armor = null;
        accessory = null;

        generatedWeapon = null;
        generatedArmor = null;
        generatedAccessory = null;

        if (changed)
            OnEquipmentChanged?.Invoke();
    }

    public StatBlock GetTotalItemBonus()
    {
        StatBlock total = new StatBlock
        {
            hp = 0,
            atk = 0,
            def = 0,
            ap = 0,
            crit = 0f
        };

        AddItemBonus(ref total, weapon);
        AddItemBonus(ref total, armor);
        AddItemBonus(ref total, accessory);

        AddGeneratedItemBonus(ref total, generatedWeapon);
        AddGeneratedItemBonus(ref total, generatedArmor);
        AddGeneratedItemBonus(ref total, generatedAccessory);

        return total;
    }

    private void AddItemBonus(ref StatBlock total, ItemData item)
    {
        if (item == null || item.statBonus == null)
            return;

        total.hp += item.statBonus.hp;
        total.atk += item.statBonus.atk;
        total.def += item.statBonus.def;
        total.ap += item.statBonus.ap;
        total.crit += item.statBonus.crit;
    }

    private void AddGeneratedItemBonus(ref StatBlock total, GeneratedItemInstance item)
    {
        if (item == null || item.statBonus == null)
            return;

        total.hp += item.statBonus.hp;
        total.atk += item.statBonus.atk;
        total.def += item.statBonus.def;
        total.ap += item.statBonus.ap;
        total.crit += item.statBonus.crit;
    }

    private bool HasAnythingInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                return weapon != null || generatedWeapon != null;

            case EquipmentSlotType.Armor:
                return armor != null || generatedArmor != null;

            case EquipmentSlotType.Accessory:
                return accessory != null || generatedAccessory != null;

            default:
                return false;
        }
    }

    private void ClearSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                weapon = null;
                generatedWeapon = null;
                break;

            case EquipmentSlotType.Armor:
                armor = null;
                generatedArmor = null;
                break;

            case EquipmentSlotType.Accessory:
                accessory = null;
                generatedAccessory = null;
                break;
        }
    }
}