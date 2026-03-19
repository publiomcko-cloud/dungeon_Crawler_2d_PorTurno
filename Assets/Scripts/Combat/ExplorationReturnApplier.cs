using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExplorationReturnApplier : MonoBehaviour
{
    [Header("Validation")]
    [SerializeField] private bool enableInspectorWarnings = true;

    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private LootWindowUI lootWindowUI;
    [SerializeField] private PartyAnchorService partyAnchorService;
    [SerializeField] private PartyInventory partyInventory;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject playerPartyMemberPrefab;
    [SerializeField] private PlayerCharacterPrefabLibrary playerPrefabLibrary;

    [Header("Loot")]
    [SerializeField] private GameObject groundItemPrefab;
    [SerializeField] private bool autoOpenLootWindowAfterVictory = true;

    private bool applied;

    private void Awake()
    {
        ValidateInspectorConfiguration();
    }

    private void Start()
    {
        if (applied || !CombatExplorationReturnData.HasPendingReturn)
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

        if (playerPrefabLibrary == null)
            playerPrefabLibrary = FindFirstObjectByType<PlayerCharacterPrefabLibrary>();

        yield return null;

        CombatExplorationReturnData.ExplorationReturnSnapshot pending = CombatExplorationReturnData.PendingReturn;
        if (pending == null)
            yield break;

        RestorePartyInventory(pending);
        ApplyRewardMoney(pending);
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
        if (partyInventory != null && pending != null)
            partyInventory.RestoreItemsSnapshot(pending.PartyInventoryItems);
    }

    private void ApplyRewardMoney(CombatExplorationReturnData.ExplorationReturnSnapshot pending)
    {
        if (pending != null && pending.RewardMoney > 0 && PartyCurrency.Instance != null)
            PartyCurrency.Instance.AddMoney(pending.RewardMoney);
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

        for (int i = 0; i < pending.PreservedEnemies.Count; i++)
        {
            CombatExplorationReturnData.EnemyReturnSnapshot snapshot = pending.PreservedEnemies[i];
            if (snapshot == null)
                continue;

            GameObject enemyPrefab = enemySpawner != null
                ? enemySpawner.ResolveEnemyPrefab(snapshot.EnemyPrefabId)
                : null;

            if (enemyPrefab == null)
                enemyPrefab = enemySpawner != null ? enemySpawner.FallbackEnemyPrefab : null;

            if (enemyPrefab == null)
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

            if (enemySpawner != null)
                enemySpawner.ApplySpawnMetadata(entity, enemyPrefab);

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

        Dictionary<string, CombatExplorationReturnData.PlayerReturnSnapshot> survivorsByCharacterId =
            new Dictionary<string, CombatExplorationReturnData.PlayerReturnSnapshot>();

        for (int i = 0; i < pending.PlayerSurvivors.Count; i++)
        {
            CombatExplorationReturnData.PlayerReturnSnapshot snapshot = pending.PlayerSurvivors[i];
            if (snapshot != null && !string.IsNullOrWhiteSpace(snapshot.CharacterId) && !survivorsByCharacterId.ContainsKey(snapshot.CharacterId))
                survivorsByCharacterId.Add(snapshot.CharacterId, snapshot);
        }

        Dictionary<string, Entity> playersByCharacterId = new Dictionary<string, Entity>();
        for (int i = 0; i < scenePlayers.Count; i++)
        {
            Entity player = scenePlayers[i];
            if (player == null)
                continue;

            string characterId = CharacterIdentity.ResolveFromEntity(player);
            if (!playersByCharacterId.ContainsKey(characterId))
                playersByCharacterId.Add(characterId, player);
        }

        for (int i = 0; i < pending.PlayerSurvivors.Count; i++)
        {
            CombatExplorationReturnData.PlayerReturnSnapshot snapshot = pending.PlayerSurvivors[i];
            if (snapshot == null)
                continue;

            Entity player;
            if (!playersByCharacterId.TryGetValue(snapshot.CharacterId, out player) || player == null)
            {
                player = CreatePlayerFromSnapshot(snapshot, scenePlayers, pending.ReturnCell);
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

            string characterId = CharacterIdentity.ResolveFromEntity(player);
            if (!survivorsByCharacterId.ContainsKey(characterId))
            {
                if (gridManager != null)
                    gridManager.RemoveEntity(player);

                Destroy(player.gameObject);
            }
        }

        if (partyAnchorService != null)
        {
            Entity leader = result.FirstOrDefault(entity =>
                entity != null &&
                entity.team == Team.Player &&
                CharacterIdentity.ResolveFromEntity(entity) == pending.LeaderCharacterId);

            if (leader == null)
                leader = result.FirstOrDefault(entity => entity != null && entity.team == Team.Player);

            partyAnchorService.SetExplicitLeader(leader);
            partyAnchorService.RefreshLeader();
        }

        return result;
    }

    private GameObject ResolvePlayerTemplate(string characterId, List<Entity> scenePlayers)
    {
        if (playerPrefabLibrary != null)
        {
            GameObject resolvedPrefab = playerPrefabLibrary.ResolvePrefab(characterId);
            if (resolvedPrefab != null)
                return resolvedPrefab;
        }

        if (playerPartyMemberPrefab != null)
            return playerPartyMemberPrefab;

        for (int i = 0; i < scenePlayers.Count; i++)
        {
            if (scenePlayers[i] != null)
                return scenePlayers[i].gameObject;
        }

        return null;
    }

    private Entity CreatePlayerFromSnapshot(
        CombatExplorationReturnData.PlayerReturnSnapshot snapshot,
        List<Entity> scenePlayers,
        Vector2Int targetCell)
    {
        if (snapshot == null)
            return null;

        GameObject playerTemplate = ResolvePlayerTemplate(snapshot.CharacterId, scenePlayers);
        if (playerTemplate == null)
            return null;

        Vector3 spawnPosition = gridManager != null
            ? gridManager.GetCellCenterWorld(targetCell)
            : new Vector3(targetCell.x + 0.5f, targetCell.y + 0.5f, 0f);

        GameObject instance = Instantiate(playerTemplate, spawnPosition, Quaternion.identity);
        instance.name = snapshot.OriginalEntityName;

        Entity entity = instance.GetComponent<Entity>();
        if (entity == null)
        {
            Destroy(instance);
            return null;
        }

        entity.team = Team.Player;
        CharacterIdentity identity = instance.GetComponent<CharacterIdentity>();
        if (identity == null)
            identity = instance.AddComponent<CharacterIdentity>();

        identity.SetCharacterId(snapshot.CharacterId);
        return entity;
    }

    private void ApplyEntitySnapshot(Entity entity, CombatExplorationReturnData.EntityReturnSnapshot snapshot, Vector2Int targetCell)
    {
        if (entity == null || snapshot == null)
            return;

        entity.name = snapshot.EntityName;
        entity.SetMoneyReward(snapshot.MoneyReward);
        entity.SetQuestEnemyId(snapshot.QuestEnemyId);
        entity.SetEnemyPrefabId(snapshot.EnemyPrefabId);

        CharacterIdentity identity = entity.GetComponent<CharacterIdentity>();
        if (identity == null)
            identity = entity.gameObject.AddComponent<CharacterIdentity>();

        identity.SetCharacterId(snapshot.CharacterId);

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
            entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            entity.EquipGeneratedItem(entry.GeneratedItem);
    }

    private void SpawnVictoryLoot(CombatExplorationReturnData.ExplorationReturnSnapshot pending)
    {
        if (groundItemPrefab == null || pending == null || pending.LootEntries == null || pending.LootEntries.Count == 0)
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

    private void TryOpenLootWindow(CombatExplorationReturnData.ExplorationReturnSnapshot pending, List<Entity> survivors)
    {
        if (lootWindowUI == null)
            return;

        Entity target = null;
        if (partyAnchorService != null)
            target = partyAnchorService.GetLeader();

        if (target == null && survivors.Count > 0)
            target = survivors[0];

        if (target != null)
            lootWindowUI.OpenForCell(target, pending.LootCell);
    }

    private void ValidateInspectorConfiguration()
    {
        if (!enableInspectorWarnings)
            return;

        WarnIfMissing(gridManager, "Grid Manager");
        WarnIfMissing(lootWindowUI, "Loot Window UI");
        WarnIfMissing(partyAnchorService, "Party Anchor Service");
        WarnIfMissing(partyInventory, "Party Inventory");
        WarnIfMissing(enemySpawner, "Enemy Spawner");
        WarnIfMissing(groundItemPrefab, "Ground Item Prefab");
    }

    private void WarnIfMissing(Object value, string label)
    {
        if (value == null)
            Debug.LogWarning($"ExplorationReturnApplier: '{label}' nao esta preenchido.", this);
    }
}
