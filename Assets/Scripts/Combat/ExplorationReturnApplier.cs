using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExplorationReturnApplier : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private LootWindowUI lootWindowUI;
    [SerializeField] private PartyAnchorService partyAnchorService;
    [SerializeField] private PartyInventory partyInventory;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject playerPartyMemberPrefab;

    [Header("Loot")]
    [SerializeField] private GameObject groundItemPrefab;
    [SerializeField] private bool autoOpenLootWindowAfterVictory = true;

    private bool applied;

    private void Start()
    {
        if (applied)
            return;

        if (!CombatExplorationReturnData.HasPendingReturn)
            return;

        StartCoroutine(ApplyReturnRoutine());
    }

    private IEnumerator ApplyReturnRoutine()
    {
        applied = true;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (lootWindowUI == null)
            lootWindowUI = LootWindowUI.Instance != null ? LootWindowUI.Instance : FindFirstObjectByType<LootWindowUI>();

        if (partyAnchorService == null)
            partyAnchorService = PartyAnchorService.Instance != null ? PartyAnchorService.Instance : FindFirstObjectByType<PartyAnchorService>();

        if (partyInventory == null)
            partyInventory = FindFirstObjectByType<PartyInventory>();

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        yield return null;

        CombatExplorationReturnData.ExplorationReturnSnapshot pending = CombatExplorationReturnData.PendingReturn;
        if (pending == null)
            yield break;

        RestorePartyInventory(pending);
        RemoveAllEnemiesInScene();
        RestorePreservedEnemies(pending);
        List<Entity> survivors = ApplyPlayerSurvivors(pending);
        SpawnVictoryLoot(pending);

        yield return null;

        if (autoOpenLootWindowAfterVictory)
            TryOpenLootWindow(pending, survivors);

        CombatExplorationReturnData.Clear();
    }

    private void RestorePartyInventory(CombatExplorationReturnData.ExplorationReturnSnapshot pending)
    {
        if (partyInventory == null || pending == null)
            return;

        partyInventory.RestoreItemsSnapshot(pending.PartyInventoryItems);
    }

    private void RemoveAllEnemiesInScene()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entity == null || entity.team != Team.Enemy)
                continue;

            if (gridManager != null)
                gridManager.RemoveEntity(entity);

            Destroy(entity.gameObject);
        }
    }

    private void RestorePreservedEnemies(CombatExplorationReturnData.ExplorationReturnSnapshot pending)
    {
        if (pending == null || pending.PreservedEnemies == null || pending.PreservedEnemies.Count == 0)
            return;

        GameObject enemyPrefab = enemySpawner != null ? enemySpawner.enemyPrefab : null;
        if (enemyPrefab == null)
            return;

        for (int i = 0; i < pending.PreservedEnemies.Count; i++)
        {
            CombatExplorationReturnData.EnemyReturnSnapshot snapshot = pending.PreservedEnemies[i];
            if (snapshot == null)
                continue;

            Vector3 spawnPosition = gridManager != null
                ? gridManager.GetCellCenterWorld(snapshot.Cell)
                : new Vector3(snapshot.Cell.x + 0.5f, snapshot.Cell.y + 0.5f, 0f);

            GameObject instance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Entity entity = instance.GetComponent<Entity>();

            if (entity == null)
            {
                Destroy(instance);
                continue;
            }

            entity.name = snapshot.EntityName;
            entity.team = Team.Enemy;

            ApplyEntitySnapshot(entity, snapshot, snapshot.Cell);
        }
    }

    private List<Entity> ApplyPlayerSurvivors(CombatExplorationReturnData.ExplorationReturnSnapshot pending)
    {
        List<Entity> result = new List<Entity>();
        List<Entity> scenePlayers = FindObjectsByType<Entity>(FindObjectsSortMode.None)
            .Where(entity => entity != null && entity.team == Team.Player)
            .OrderBy(entity => entity.name)
            .ToList();

        Dictionary<string, CombatExplorationReturnData.PlayerReturnSnapshot> survivorsByName =
            pending.PlayerSurvivors.ToDictionary(snapshot => snapshot.OriginalEntityName, snapshot => snapshot);

        Dictionary<string, Entity> playersByName = new Dictionary<string, Entity>();
        for (int i = 0; i < scenePlayers.Count; i++)
        {
            Entity player = scenePlayers[i];
            if (player == null)
                continue;

            if (!playersByName.ContainsKey(player.name))
                playersByName.Add(player.name, player);
        }

        GameObject playerTemplate = ResolvePlayerTemplate(scenePlayers);

        for (int i = 0; i < pending.PlayerSurvivors.Count; i++)
        {
            CombatExplorationReturnData.PlayerReturnSnapshot snapshot = pending.PlayerSurvivors[i];
            if (snapshot == null)
                continue;

            Entity player;
            if (!playersByName.TryGetValue(snapshot.OriginalEntityName, out player) || player == null)
            {
                player = CreatePlayerFromSnapshot(snapshot, playerTemplate, pending.ReturnCell);
                if (player == null)
                    continue;
            }

            ApplyEntitySnapshot(player, snapshot, pending.ReturnCell);
            result.Add(player);
        }

        for (int i = 0; i < scenePlayers.Count; i++)
        {
            Entity player = scenePlayers[i];
            if (player == null || result.Contains(player))
                continue;

            if (!survivorsByName.ContainsKey(player.name))
            {
                if (gridManager != null)
                    gridManager.RemoveEntity(player);

                Destroy(player.gameObject);
            }
        }

        if (partyAnchorService != null)
        {
            Entity leader = result.FirstOrDefault(entity => entity != null && entity.team == Team.Player);
            partyAnchorService.SetExplicitLeader(leader);
            partyAnchorService.RefreshLeader();
        }

        return result;
    }

    private GameObject ResolvePlayerTemplate(List<Entity> scenePlayers)
    {
        if (playerPartyMemberPrefab != null)
            return playerPartyMemberPrefab;

        for (int i = 0; i < scenePlayers.Count; i++)
        {
            Entity player = scenePlayers[i];
            if (player != null)
                return player.gameObject;
        }

        return null;
    }

    private Entity CreatePlayerFromSnapshot(
        CombatExplorationReturnData.PlayerReturnSnapshot snapshot,
        GameObject playerTemplate,
        Vector2Int targetCell)
    {
        if (snapshot == null)
            return null;

        if (playerTemplate == null)
        {
            Debug.LogWarning(
                $"ExplorationReturnApplier: no player prefab/template available to restore '{snapshot.OriginalEntityName}'.");
            return null;
        }

        Vector3 spawnPosition = gridManager != null
            ? gridManager.GetCellCenterWorld(targetCell)
            : new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

        GameObject instance = Instantiate(playerTemplate, spawnPosition, Quaternion.identity);
        instance.name = snapshot.OriginalEntityName;

        Entity entity = instance.GetComponent<Entity>();
        if (entity == null)
        {
            Debug.LogWarning(
                $"ExplorationReturnApplier: player template '{playerTemplate.name}' needs an Entity component.");
            Destroy(instance);
            return null;
        }

        entity.team = Team.Player;
        return entity;
    }

    private void ApplyEntitySnapshot(Entity entity, CombatExplorationReturnData.EntityReturnSnapshot snapshot, Vector2Int targetCell)
    {
        if (entity == null || snapshot == null)
            return;

        CharacterStats stats = entity.GetStatsComponent();
        EquipmentSlots equipmentSlots = entity.GetEquipmentSlots();

        if (stats != null)
        {
            stats.Initialize();

            if (equipmentSlots != null)
                equipmentSlots.UnequipAll();

            stats.SetBaseStats(snapshot.BaseStats != null ? snapshot.BaseStats : new StatBlock(), false);
            stats.SetPointBonus(snapshot.PointBonus != null ? snapshot.PointBonus : new StatBlock(), false);
            stats.SetProgressionData(snapshot.Level, snapshot.CurrentXP, snapshot.UnspentStatPoints);

            ApplyEquipmentSnapshot(entity, snapshot.EquippedWeapon);
            ApplyEquipmentSnapshot(entity, snapshot.EquippedArmor);
            ApplyEquipmentSnapshot(entity, snapshot.EquippedAccessory);

            stats.SetCurrentHPToMax();

            int missingHP = Mathf.Max(0, entity.maxHP - snapshot.CurrentHP);
            if (missingHP > 0)
                stats.ReceiveRawDamage(missingHP);
        }

        if (gridManager != null)
        {
            gridManager.RemoveEntity(entity);
            gridManager.RegisterEntity(entity, targetCell);
        }
        else
        {
            Vector3 worldPosition = new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);
            entity.SetGridPosition(targetCell);
            entity.transform.position = worldPosition;
            entity.SetVisualTarget(worldPosition, true);
        }
    }

    private void ApplyEquipmentSnapshot(Entity entity, InventoryItemEntry entry)
    {
        if (entity == null || entry == null || entry.IsEmpty)
            return;

        if (entry.IsStaticItem)
        {
            entity.EquipItem(entry.StaticItem);
            return;
        }

        if (entry.IsGeneratedItem)
            entity.EquipGeneratedItem(entry.GeneratedItem);
    }

    private void SpawnVictoryLoot(CombatExplorationReturnData.ExplorationReturnSnapshot pending)
    {
        if (groundItemPrefab == null)
            return;

        if (pending == null || pending.LootEntries == null || pending.LootEntries.Count == 0)
            return;

        Vector3 basePosition = gridManager != null
            ? gridManager.GetCellCenterWorld(pending.LootCell)
            : new Vector3(pending.LootCell.x + 0.5f, pending.LootCell.y + 0.5f, 0f);

        for (int i = 0; i < pending.LootEntries.Count; i++)
        {
            InventoryItemEntry lootEntry = pending.LootEntries[i];
            if (lootEntry == null || lootEntry.IsEmpty)
                continue;

            Vector3 spawnPosition = basePosition + new Vector3((i % 2) * 0.08f, (i / 2) * 0.08f, 0f);
            GameObject instance = Instantiate(groundItemPrefab, spawnPosition, Quaternion.identity);
            GroundItem groundItem = instance.GetComponent<GroundItem>();

            if (groundItem == null)
            {
                Destroy(instance);
                continue;
            }

            if (lootEntry.IsStaticItem)
                groundItem.SetupStatic(lootEntry.StaticItem);
            else if (lootEntry.IsGeneratedItem)
                groundItem.SetupGenerated(lootEntry.GeneratedItem);
        }
    }

    private void TryOpenLootWindow(
        CombatExplorationReturnData.ExplorationReturnSnapshot pending,
        List<Entity> survivors)
    {
        if (lootWindowUI == null)
            return;

        Entity target = null;

        if (partyAnchorService != null)
            target = partyAnchorService.GetLeader();

        if (target == null && survivors.Count > 0)
            target = survivors[0];

        if (target == null)
            return;

        lootWindowUI.OpenForCell(target, pending.LootCell);
    }
}
