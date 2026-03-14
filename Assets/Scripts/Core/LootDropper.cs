using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class LootDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject groundItemPrefab;
    [SerializeField] private List<LootDropEntry> lootTable = new List<LootDropEntry>();

    private Entity entity;
    private bool dropped = false;

    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    private void OnEnable()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (entity != null)
            entity.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (entity != null)
            entity.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (dropped)
            return;

        dropped = true;

        if (ShouldSuppressWorldDrop())
            return;

        TryDropLoot();
    }

    private bool ShouldSuppressWorldDrop()
    {
        CombatEntityRuntime combatRuntime = GetComponent<CombatEntityRuntime>();
        return combatRuntime != null && combatRuntime.SuppressWorldDropOnDeath;
    }

    private void TryDropLoot()
    {
        if (groundItemPrefab == null)
            return;

        if (lootTable == null || lootTable.Count == 0)
            return;

        List<LootDropEntry> validDrops = new List<LootDropEntry>();

        for (int i = 0; i < lootTable.Count; i++)
        {
            if (lootTable[i] == null)
                continue;

            if (!lootTable[i].HasValidItemSource())
                continue;

            if (lootTable[i].RollDrop())
                validDrops.Add(lootTable[i]);
        }

        if (validDrops.Count == 0)
            return;

        LootDropEntry chosen = validDrops[Random.Range(0, validDrops.Count)];
        SpawnGroundItem(chosen);
    }

    private void SpawnGroundItem(LootDropEntry entry)
    {
        if (entry == null)
            return;

        Vector2Int cell = entity != null ? entity.GridPosition : Vector2Int.zero;
        Vector3 worldPos = GridManager.Instance != null
            ? GridManager.Instance.GetCellCenterWorld(cell)
            : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        GameObject instance = Instantiate(groundItemPrefab, worldPos, Quaternion.identity);

        GroundItem groundItem = instance.GetComponent<GroundItem>();
        if (groundItem == null)
        {
            Destroy(instance);
            return;
        }

        if (entry.staticItem != null)
        {
            groundItem.SetupStatic(entry.staticItem);
            return;
        }

        if (entry.generatedProfile != null)
        {
            GeneratedItemInstance generated = ItemGenerator.Generate(entry.generatedProfile);
            groundItem.SetupGenerated(generated);
        }
    }

    public List<LootDropEntry> GetLootTableSnapshot()
    {
        List<LootDropEntry> snapshot = new List<LootDropEntry>();

        if (lootTable == null)
            return snapshot;

        for (int i = 0; i < lootTable.Count; i++)
        {
            LootDropEntry entry = lootTable[i];
            if (entry == null)
                continue;

            snapshot.Add(entry.Clone());
        }

        return snapshot;
    }
}
