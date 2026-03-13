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

    public bool IsSlotEmpty(int index)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();
        return items[index] == null || items[index].IsEmpty;
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
        NormalizeSlot(emptyIndex);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool AddEntryToIndex(InventoryItemEntry entry, int index)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        if (items[index] != null && !items[index].IsEmpty)
            return false;

        items[index] = entry.Clone();
        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();

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

        EnsureSize();

        if (fromIndex == toIndex)
            return false;

        if (items[fromIndex] == null || items[fromIndex].IsEmpty)
            return false;

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

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        ResolveReferences();

        if (entity == null || equipmentSlots == null)
            return false;

        if (entity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = entity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        items[index] = new InventoryItemEntry();

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = entity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = entity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                items[index] = entry.Clone();
                NormalizeSlot(index);

                if (!restored)
                    Debug.LogWarning("Falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipFromInventoryToSlot(int index, EquipmentSlotType targetSlotType)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipFromInventory(index);
    }

    public bool UnequipToInventory(EquipmentSlotType slotType)
    {
        ResolveReferences();

        if (entity == null || equipmentSlots == null)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        if (!AddEntry(equippedEntry))
            return false;

        entity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventorySlot(EquipmentSlotType slotType, int inventoryIndex)
    {
        ResolveReferences();

        if (entity == null || equipmentSlots == null)
            return false;

        if (!IsValidIndex(inventoryIndex))
            return false;

        EnsureSize();

        if (items[inventoryIndex] != null && !items[inventoryIndex].IsEmpty)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        items[inventoryIndex] = equippedEntry.Clone();
        NormalizeSlot(inventoryIndex);

        entity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventoryItemEntry GetEquippedEntry(EquipmentSlotType slotType)
    {
        ResolveReferences();

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

        ResolveReferences();

        if (entity == null || equipmentSlots == null)
            return false;

        if (entity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = entity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = entity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = entity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                if (!restored)
                    Debug.LogWarning("Falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectlyToSlot(InventoryItemEntry entry, EquipmentSlotType targetSlotType)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipEntryDirectly(entry);
    }

    private void ResolveReferences()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();
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