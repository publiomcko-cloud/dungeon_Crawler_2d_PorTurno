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

    public GeneratedItemInstance GeneratedWeapon => IsValidGeneratedItem(generatedWeapon) ? generatedWeapon : null;
    public GeneratedItemInstance GeneratedArmor => IsValidGeneratedItem(generatedArmor) ? generatedArmor : null;
    public GeneratedItemInstance GeneratedAccessory => IsValidGeneratedItem(generatedAccessory) ? generatedAccessory : null;

    private void Awake()
    {
        NormalizeGeneratedItems();
    }

    private void OnValidate()
    {
        NormalizeGeneratedItems();
    }

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
            case EquipmentSlotType.Weapon: return GeneratedWeapon;
            case EquipmentSlotType.Armor: return GeneratedArmor;
            case EquipmentSlotType.Accessory: return GeneratedAccessory;
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

        GeneratedItemInstance clone = item.Clone();

        switch (item.slotType)
        {
            case EquipmentSlotType.Weapon:
                generatedWeapon = clone;
                break;

            case EquipmentSlotType.Armor:
                generatedArmor = clone;
                break;

            case EquipmentSlotType.Accessory:
                generatedAccessory = clone;
                break;

            default:
                return false;
        }

        NormalizeGeneratedItems();
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
            IsValidGeneratedItem(generatedWeapon) ||
            IsValidGeneratedItem(generatedArmor) ||
            IsValidGeneratedItem(generatedAccessory);

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
        NormalizeGeneratedItems();

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
        if (!IsValidGeneratedItem(item))
            return;

        if (item.statBonus == null)
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
                return weapon != null || IsValidGeneratedItem(generatedWeapon);

            case EquipmentSlotType.Armor:
                return armor != null || IsValidGeneratedItem(generatedArmor);

            case EquipmentSlotType.Accessory:
                return accessory != null || IsValidGeneratedItem(generatedAccessory);

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

    private void NormalizeGeneratedItems()
    {
        if (!IsValidGeneratedItem(generatedWeapon))
            generatedWeapon = null;

        if (!IsValidGeneratedItem(generatedArmor))
            generatedArmor = null;

        if (!IsValidGeneratedItem(generatedAccessory))
            generatedAccessory = null;
    }

    private bool IsValidGeneratedItem(GeneratedItemInstance item)
    {
        if (item == null)
            return false;

        bool hasName = !string.IsNullOrWhiteSpace(item.itemName);
        bool hasDescription = !string.IsNullOrWhiteSpace(item.description);
        bool hasLevel = item.requiredLevel > 0;
        bool hasValue = item.value > 0;
        bool hasStats =
            item.statBonus != null &&
            (item.statBonus.hp != 0 ||
             item.statBonus.atk != 0 ||
             item.statBonus.def != 0 ||
             item.statBonus.ap != 0 ||
             Mathf.Abs(item.statBonus.crit) > 0.001f);

        return hasName || hasDescription || hasLevel || hasValue || hasStats;
    }
}