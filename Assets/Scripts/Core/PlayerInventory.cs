using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
[RequireComponent(typeof(EquipmentSlots))]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<InventoryItemEntry> items = new List<InventoryItemEntry>();

    public event Action OnInventoryChanged;

    private Entity entity;
    private EquipmentSlots equipmentSlots;

    public int InventorySize => inventorySize;
    public IReadOnlyList<InventoryItemEntry> Items => items;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        equipmentSlots = GetComponent<EquipmentSlots>();
        EnsureSize();
    }

    public InventoryItemEntry GetItem(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return items[index];
    }

    public bool HasEmptySlot()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return true;
        }

        return false;
    }

    public int GetFirstEmptySlotIndex()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return i;
        }

        return -1;
    }

    public bool AddStaticItem(ItemData item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromStatic(item));
    }

    public bool AddGeneratedItem(GeneratedItemInstance item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromGenerated(item));
    }

    public bool AddEntry(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        EnsureSize();

        int emptyIndex = GetFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        items[emptyIndex] = entry.Clone();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (!IsValidIndex(index))
            return false;

        if (items[index] == null || items[index].IsEmpty)
            return false;

        items[index] = new InventoryItemEntry();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool MoveItem(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            return false;

        if (fromIndex == toIndex)
            return false;

        EnsureSize();

        InventoryItemEntry from = items[fromIndex];
        InventoryItemEntry to = items[toIndex];

        items[toIndex] = from;
        items[fromIndex] = to;

        NormalizeSlot(fromIndex);
        NormalizeSlot(toIndex);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipFromInventory(int index)
    {
        if (!IsValidIndex(index))
            return false;

        InventoryItemEntry entry = items[index];

        if (entry == null || entry.IsEmpty)
            return false;

        if (entity == null)
            entity = GetComponent<Entity>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (entity == null || equipmentSlots == null)
            return false;

        if (entity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(entry.SlotType);

        if (equippedEntry != null && !equippedEntry.IsEmpty)
        {
            int emptyIndex = GetFirstEmptySlotIndex();
            if (emptyIndex < 0 || emptyIndex == index)
            {
                int swapIndex = index;
                items[swapIndex] = equippedEntry.Clone();
            }
            else
            {
                items[emptyIndex] = equippedEntry.Clone();
                NormalizeSlot(emptyIndex);
                items[index] = new InventoryItemEntry();
            }
        }
        else
        {
            items[index] = new InventoryItemEntry();
        }

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = entity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
        {
            items[index] = entry.Clone();
            OnInventoryChanged?.Invoke();
            return false;
        }

        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventory(EquipmentSlotType slotType)
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (equipmentSlots == null)
            return false;

        if (!HasEmptySlot())
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        bool added = AddEntry(equippedEntry);
        if (!added)
            return false;

        entity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventoryItemEntry GetEquippedEntry(EquipmentSlotType slotType)
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (equipmentSlots == null)
            return null;

        ItemData staticItem = equipmentSlots.GetItemInSlot(slotType);
        if (staticItem != null)
            return InventoryItemEntry.FromStatic(staticItem);

        GeneratedItemInstance generatedItem = equipmentSlots.GetGeneratedItemInSlot(slotType);
        if (generatedItem != null)
            return InventoryItemEntry.FromGenerated(generatedItem);

        return null;
    }

    public bool TryEquipEntryDirectly(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        if (entity == null)
            entity = GetComponent<Entity>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (entity == null || equipmentSlots == null)
            return false;

        if (entity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(entry.SlotType);

        if (equippedEntry != null && !equippedEntry.IsEmpty && !HasEmptySlot())
            return false;

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = entity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        if (equippedEntry != null && !equippedEntry.IsEmpty)
            AddEntry(equippedEntry);

        OnInventoryChanged?.Invoke();
        return true;
    }

    private void EnsureSize()
    {
        if (inventorySize < 1)
            inventorySize = 1;

        if (items == null)
            items = new List<InventoryItemEntry>();

        while (items.Count < inventorySize)
            items.Add(new InventoryItemEntry());

        while (items.Count > inventorySize)
            items.RemoveAt(items.Count - 1);

        for (int i = 0; i < items.Count; i++)
            NormalizeSlot(i);
    }

    private void NormalizeSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (items[index] == null)
            items[index] = new InventoryItemEntry();
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < items.Count;
    }
}