using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplorationSceneBootstrap : MonoBehaviour
{
    [Header("Validation")]
    [SerializeField] private bool enableInspectorWarnings = true;

    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PartyAnchorService partyAnchorService;
    [SerializeField] private PartyInventory partyInventory;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject groundItemPrefab;
    [SerializeField] private GameObject playerPartyMemberPrefab;
    [SerializeField] private PlayerCharacterPrefabLibrary playerPrefabLibrary;

    private bool applied;

    private void Awake()
    {
        ValidateInspectorConfiguration();
    }

    private void Start()
    {
        if (applied || CombatExplorationReturnData.HasPendingReturn)
            return;

        StartCoroutine(BootstrapRoutine());
    }

    private IEnumerator BootstrapRoutine()
    {
        applied = true;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (partyAnchorService == null)
            partyAnchorService = PartyAnchorService.Instance != null ? PartyAnchorService.Instance : FindFirstObjectByType<PartyAnchorService>();

        if (partyInventory == null)
            partyInventory = FindFirstObjectByType<PartyInventory>();

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (playerPrefabLibrary == null)
            playerPrefabLibrary = FindFirstObjectByType<PlayerCharacterPrefabLibrary>();

        yield return null;

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!ExplorationScenePersistenceData.HasPendingTransitionToScene(activeSceneName))
            yield break;

        ExplorationScenePersistenceData.PendingSceneTransition pending = ExplorationScenePersistenceData.CurrentTransition;
        if (pending == null)
            yield break;

        ExplorationScenePersistenceData.SceneStateSnapshot sceneState =
            ExplorationScenePersistenceData.GetSavedSceneState(activeSceneName);

        if (sceneState != null)
            RestoreSceneState(sceneState);

        RestorePartyInventory(pending);

        Vector2Int arrivalCell = ResolveArrivalCell(pending.TargetPortalId);
        ApplyPartyState(pending, arrivalCell);

        ExplorationScenePersistenceData.ClearPendingTransition();
    }

    private void RestoreSceneState(ExplorationScenePersistenceData.SceneStateSnapshot sceneState)
    {
        RemoveAllEnemiesInScene();
        RemoveAllGroundItemsInScene();
        RestoreEnemies(sceneState);
        RestoreGroundItems(sceneState);
    }

    private void RestorePartyInventory(ExplorationScenePersistenceData.PendingSceneTransition pending)
    {
        if (partyInventory != null && pending != null)
            partyInventory.RestoreItemsSnapshot(pending.PartyInventoryItems);
    }

    private void ApplyPartyState(ExplorationScenePersistenceData.PendingSceneTransition pending, Vector2Int arrivalCell)
    {
        if (pending == null)
            return;

        List<Entity> scenePlayers = FindObjectsByType<Entity>(FindObjectsSortMode.None)
            .Where(entity => entity != null && entity.team == Team.Player)
            .OrderBy(entity => entity.name)
            .ToList();

        Dictionary<string, ExplorationScenePersistenceData.PartyMemberSnapshot> snapshotsByCharacterId =
            new Dictionary<string, ExplorationScenePersistenceData.PartyMemberSnapshot>();

        for (int i = 0; i < pending.PartyMembers.Count; i++)
        {
            ExplorationScenePersistenceData.PartyMemberSnapshot snapshot = pending.PartyMembers[i];
            if (snapshot != null && !string.IsNullOrWhiteSpace(snapshot.CharacterId) && !snapshotsByCharacterId.ContainsKey(snapshot.CharacterId))
                snapshotsByCharacterId.Add(snapshot.CharacterId, snapshot);
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

        List<Entity> resolvedPlayers = new List<Entity>();

        foreach (ExplorationScenePersistenceData.PartyMemberSnapshot snapshot in pending.PartyMembers)
        {
            if (snapshot == null)
                continue;

            Entity player;
            if (!playersByCharacterId.TryGetValue(snapshot.CharacterId, out player) || player == null)
            {
                player = CreatePlayerFromSnapshot(snapshot, scenePlayers, arrivalCell);
                if (player == null)
                    continue;
            }

            ApplyEntitySnapshot(player, snapshot, arrivalCell);
            resolvedPlayers.Add(player);
        }

        for (int i = 0; i < scenePlayers.Count; i++)
        {
            Entity player = scenePlayers[i];
            if (player == null || resolvedPlayers.Contains(player))
                continue;

            string characterId = CharacterIdentity.ResolveFromEntity(player);
            if (!snapshotsByCharacterId.ContainsKey(characterId))
            {
                if (gridManager != null)
                    gridManager.RemoveEntity(player);

                Destroy(player.gameObject);
            }
        }

        if (partyAnchorService != null)
        {
            Entity leader = resolvedPlayers.FirstOrDefault(entity =>
                entity != null &&
                entity.team == Team.Player &&
                CharacterIdentity.ResolveFromEntity(entity) == pending.LeaderCharacterId);

            partyAnchorService.SetExplicitLeader(leader);
            partyAnchorService.RefreshLeader();
        }
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
        ExplorationScenePersistenceData.PartyMemberSnapshot snapshot,
        List<Entity> scenePlayers,
        Vector2Int arrivalCell)
    {
        if (snapshot == null)
            return null;

        GameObject playerTemplate = ResolvePlayerTemplate(snapshot.CharacterId, scenePlayers);
        if (playerTemplate == null)
        {
            Debug.LogWarning($"ExplorationSceneBootstrap: no player prefab/template available to restore '{snapshot.CharacterId}'.");
            return null;
        }

        Vector3 spawnPosition = gridManager != null
            ? gridManager.GetCellCenterWorld(arrivalCell)
            : new Vector3(arrivalCell.x + 0.5f, arrivalCell.y + 0.5f, 0f);

        GameObject instance = Instantiate(playerTemplate, spawnPosition, Quaternion.identity);
        instance.name = snapshot.EntityName;

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

    private Vector2Int ResolveArrivalCell(string targetPortalId)
    {
        ScenePortal[] portals = FindObjectsByType<ScenePortal>(FindObjectsSortMode.None);
        for (int i = 0; i < portals.Length; i++)
        {
            ScenePortal portal = portals[i];
            if (portal != null && portal.PortalId == targetPortalId)
                return portal.GetArrivalCell();
        }

        return Vector2Int.zero;
    }

    private void RestoreEnemies(ExplorationScenePersistenceData.SceneStateSnapshot sceneState)
    {
        if (sceneState == null || sceneState.Enemies == null || sceneState.Enemies.Count == 0)
            return;

        for (int i = 0; i < sceneState.Enemies.Count; i++)
        {
            ExplorationScenePersistenceData.EnemyStateSnapshot snapshot = sceneState.Enemies[i];
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

    private void RestoreGroundItems(ExplorationScenePersistenceData.SceneStateSnapshot sceneState)
    {
        if (sceneState == null || sceneState.GroundItems == null || sceneState.GroundItems.Count == 0 || groundItemPrefab == null)
            return;

        for (int i = 0; i < sceneState.GroundItems.Count; i++)
        {
            ExplorationScenePersistenceData.GroundItemSnapshot snapshot = sceneState.GroundItems[i];
            if (snapshot == null || snapshot.ItemEntry == null || snapshot.ItemEntry.IsEmpty)
                continue;

            Vector3 spawnPosition = gridManager != null
                ? gridManager.GetCellCenterWorld(snapshot.Cell)
                : new Vector3(snapshot.Cell.x + 0.5f, snapshot.Cell.y + 0.5f, 0f);

            GameObject instance = Instantiate(groundItemPrefab, spawnPosition, Quaternion.identity);
            GroundItem groundItem = instance.GetComponent<GroundItem>();
            if (groundItem == null)
            {
                Destroy(instance);
                continue;
            }

            if (snapshot.ItemEntry.IsStaticItem)
                groundItem.SetupStatic(snapshot.ItemEntry.StaticItem);
            else if (snapshot.ItemEntry.IsGeneratedItem)
                groundItem.SetupGenerated(snapshot.ItemEntry.GeneratedItem);
        }
    }

    private void ApplyEntitySnapshot(
        Entity entity,
        ExplorationScenePersistenceData.EntityStateSnapshot snapshot,
        Vector2Int targetCell)
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

    private void RemoveAllGroundItemsInScene()
    {
        GroundItem[] items = FindObjectsByType<GroundItem>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                Destroy(items[i].gameObject);
        }
    }

    private void ValidateInspectorConfiguration()
    {
        if (!enableInspectorWarnings)
            return;

        WarnIfMissing(gridManager, "Grid Manager");
        WarnIfMissing(partyAnchorService, "Party Anchor Service");
        WarnIfMissing(partyInventory, "Party Inventory");
        WarnIfMissing(enemySpawner, "Enemy Spawner");
        WarnIfMissing(groundItemPrefab, "Ground Item Prefab");

        if (playerPrefabLibrary == null && playerPartyMemberPrefab == null)
        {
            Debug.LogWarning(
                "ExplorationSceneBootstrap: preencha 'Player Character Prefab Library' ou 'Player Party Member Prefab'.",
                this);
        }
    }

    private void WarnIfMissing(Object value, string label)
    {
        if (value == null)
            Debug.LogWarning($"ExplorationSceneBootstrap: '{label}' nao esta preenchido.", this);
    }
}
