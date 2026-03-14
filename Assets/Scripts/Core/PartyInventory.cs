using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyInventory : MonoBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<InventoryItemEntry> items = new List<InventoryItemEntry>();

    public event Action OnInventoryChanged;

    public int InventorySize => inventorySize;
    public IReadOnlyList<InventoryItemEntry> Items => items;

    private void Awake()
    {
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

    public InventoryItemEntry GetEquippedEntry(Entity targetEntity, EquipmentSlotType slotType)
    {
        if (targetEntity == null)
            return null;

        EquipmentSlots equipmentSlots = targetEntity.GetComponent<EquipmentSlots>();
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

    public bool TryEquipFromInventory(Entity targetEntity, int index)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (targetEntity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(targetEntity, entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = targetEntity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = targetEntity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        items[index] = new InventoryItemEntry();

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = targetEntity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = targetEntity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                items[index] = entry.Clone();
                NormalizeSlot(index);

                if (!restored)
                    Debug.LogWarning("PartyInventory: falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipFromInventoryToSlot(Entity targetEntity, int index, EquipmentSlotType targetSlotType)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipFromInventory(targetEntity, index);
    }

    public bool UnequipToInventory(Entity targetEntity, EquipmentSlotType slotType)
    {
        if (targetEntity == null)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(targetEntity, slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        if (!AddEntry(equippedEntry))
            return false;

        targetEntity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventorySlot(Entity targetEntity, EquipmentSlotType slotType, int inventoryIndex)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(inventoryIndex))
            return false;

        EnsureSize();

        if (items[inventoryIndex] != null && !items[inventoryIndex].IsEmpty)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(targetEntity, slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        items[inventoryIndex] = equippedEntry.Clone();
        NormalizeSlot(inventoryIndex);

        targetEntity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectly(Entity targetEntity, InventoryItemEntry entry)
    {
        if (targetEntity == null || entry == null || entry.IsEmpty)
            return false;

        if (targetEntity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(targetEntity, entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = targetEntity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = targetEntity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = targetEntity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = targetEntity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                if (!restored)
                    Debug.LogWarning("PartyInventory: falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectlyToSlot(Entity targetEntity, InventoryItemEntry entry, EquipmentSlotType targetSlotType)
    {
        if (targetEntity == null || entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipEntryDirectly(targetEntity, entry);
    }

    public List<InventoryItemEntry> GetItemsSnapshot()
    {
        EnsureSize();

        List<InventoryItemEntry> snapshot = new List<InventoryItemEntry>(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemEntry entry = items[i];
            snapshot.Add(entry != null ? entry.Clone() : new InventoryItemEntry());
        }

        return snapshot;
    }

    public void RestoreItemsSnapshot(List<InventoryItemEntry> snapshot)
    {
        EnsureSize();

        items.Clear();

        if (snapshot != null)
        {
            for (int i = 0; i < snapshot.Count; i++)
            {
                InventoryItemEntry entry = snapshot[i];
                items.Add(entry != null ? entry.Clone() : new InventoryItemEntry());
            }
        }

        EnsureSize();
        OnInventoryChanged?.Invoke();
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
