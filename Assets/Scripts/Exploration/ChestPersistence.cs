using System;
using System.Collections.Generic;

[Serializable]
public class ChestStoredState
{
    public bool IsOpened;
    public int PendingMoney;
    public List<InventoryItemEntry> Items = new List<InventoryItemEntry>();

    public ChestStoredState Clone()
    {
        ChestStoredState clone = new ChestStoredState
        {
            IsOpened = IsOpened,
            PendingMoney = PendingMoney,
            Items = new List<InventoryItemEntry>()
        };

        for (int i = 0; i < Items.Count; i++)
        {
            InventoryItemEntry entry = Items[i];
            clone.Items.Add(entry != null ? entry.Clone() : new InventoryItemEntry());
        }

        return clone;
    }
}

public static class ChestPersistence
{
    private static readonly HashSet<string> openedChestKeys = new HashSet<string>();
    private static readonly Dictionary<string, ChestStoredState> chestStates = new Dictionary<string, ChestStoredState>();

    public static bool IsChestOpened(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (openedChestKeys.Contains(key))
            return true;

        if (chestStates.TryGetValue(key, out ChestStoredState state))
            return state != null && state.IsOpened;

        return false;
    }

    public static ChestStoredState GetOrCreateState(string key, Func<ChestStoredState> factory)
    {
        if (string.IsNullOrWhiteSpace(key))
            return factory != null ? factory.Invoke() : new ChestStoredState();

        if (chestStates.TryGetValue(key, out ChestStoredState existing) && existing != null)
            return existing;

        ChestStoredState created = factory != null ? factory.Invoke() : new ChestStoredState();
        chestStates[key] = created ?? new ChestStoredState();
        return chestStates[key];
    }

    public static void MarkChestOpened(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        openedChestKeys.Add(key);

        if (chestStates.TryGetValue(key, out ChestStoredState state) && state != null)
            state.IsOpened = true;
    }

    public static void Clear()
    {
        openedChestKeys.Clear();
        chestStates.Clear();
    }
}
