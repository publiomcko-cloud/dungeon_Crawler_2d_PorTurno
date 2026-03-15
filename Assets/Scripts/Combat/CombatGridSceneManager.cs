using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatGridSceneManager : MonoBehaviour
{
    [Serializable]
    private sealed class CombatPrefabOverride
    {
        public string entityName;
        public GameObject prefab;
    }

    [Header("Validation")]
    [SerializeField] private bool enableInspectorWarnings = true;

    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnRoot;

    [Header("Default Prefabs")]
    [SerializeField] private GameObject defaultPlayerCombatPrefab;
    [SerializeField] private GameObject defaultEnemyCombatPrefab;

    [Header("Optional Prefab Overrides")]
    [SerializeField] private List<CombatPrefabOverride> prefabOverrides = new List<CombatPrefabOverride>();

    [Header("Formation")]
    [SerializeField] private Vector2Int playerOriginCell = new Vector2Int(-2, 2);
    [SerializeField] private Vector2Int enemyOriginCell = new Vector2Int(2, 2);
    [SerializeField] private int formationRowsPerColumn = 4;
    [SerializeField] private int rowSpacing = 1;
    [SerializeField] private int columnSpacing = 1;

    [Header("Flow")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool clearSessionAfterBuild = false;

    public bool HasBuiltCombatants { get; private set; }

    public event Action OnCombatantsBuilt;

    private void Awake()
    {
        ValidateInspectorConfiguration();
    }

    private void Start()
    {
        if (!buildOnStart)
            return;

        BuildCombatScene();
    }

    public void BuildCombatScene()
    {
        if (HasBuiltCombatants)
            return;

        if (!TryResolveReferences())
            return;

        CombatSessionData.CombatSessionSnapshot session = CombatSessionData.CurrentSession;
        if (session == null)
        {
            Debug.LogWarning("CombatGridSceneManager: no active CombatSessionData found.");
            return;
        }

        List<CombatSessionData.CombatParticipantSnapshot> participants = session.Attackers
            .Concat(session.Defenders)
            .OrderBy(participant => participant.Team)
            .ThenBy(participant => participant.EntityName)
            .ToList();

        List<CombatSessionData.CombatParticipantSnapshot> players = participants
            .Where(participant => participant.Team == Team.Player)
            .ToList();

        List<CombatSessionData.CombatParticipantSnapshot> enemies = participants
            .Where(participant => participant.Team == Team.Enemy)
            .ToList();

        SpawnSide(players, playerOriginCell, false);
        SpawnSide(enemies, enemyOriginCell, true);

        StartCoroutine(FinalizeBuildAfterSpawnRoutine());

        if (clearSessionAfterBuild)
            CombatSessionData.ClearSession();
    }

    private IEnumerator FinalizeBuildAfterSpawnRoutine()
    {
        yield return null;

        HasBuiltCombatants = true;
        OnCombatantsBuilt?.Invoke();
    }

    private bool TryResolveReferences()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogWarning("CombatGridSceneManager: GridManager reference is missing in CombatGrid scene.");
            return false;
        }

        if (formationRowsPerColumn < 1)
            formationRowsPerColumn = 1;

        if (rowSpacing < 1)
            rowSpacing = 1;

        if (columnSpacing < 1)
            columnSpacing = 1;

        return true;
    }

    private void SpawnSide(
        List<CombatSessionData.CombatParticipantSnapshot> participants,
        Vector2Int originCell,
        bool mirrorHorizontally)
    {
        for (int i = 0; i < participants.Count; i++)
        {
            CombatSessionData.CombatParticipantSnapshot snapshot = participants[i];
            Vector2Int spawnCell = GetFormationCell(originCell, i, mirrorHorizontally);

            if (gridManager.IsCellBlocked(spawnCell))
            {
                Debug.LogWarning($"CombatGridSceneManager: blocked combat spawn cell {spawnCell} for '{snapshot.EntityName}'.");
                continue;
            }

            GameObject prefab = ResolvePrefab(snapshot);
            if (prefab == null)
            {
                Debug.LogWarning($"CombatGridSceneManager: no prefab configured for '{snapshot.EntityName}' ({snapshot.Team}).");
                continue;
            }

            Vector3 spawnPosition = gridManager.GetCellCenterWorld(spawnCell);
            Transform parent = spawnRoot != null ? spawnRoot : null;
            GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, parent);

            ConfigureSpawnedEntity(instance, snapshot, spawnCell, i);
        }
    }

    private Vector2Int GetFormationCell(Vector2Int originCell, int index, bool mirrorHorizontally)
    {
        int row = index % formationRowsPerColumn;
        int column = index / formationRowsPerColumn;

        int horizontalStep = column * columnSpacing;
        if (mirrorHorizontally)
            horizontalStep *= -1;

        int verticalStep = row * rowSpacing;

        return new Vector2Int(
            originCell.x + horizontalStep,
            originCell.y - verticalStep);
    }

    private GameObject ResolvePrefab(CombatSessionData.CombatParticipantSnapshot snapshot)
    {
        for (int i = 0; i < prefabOverrides.Count; i++)
        {
            CombatPrefabOverride prefabOverride = prefabOverrides[i];
            if (prefabOverride == null || prefabOverride.prefab == null)
                continue;

            if (string.Equals(
                prefabOverride.entityName,
                snapshot.EntityName,
                StringComparison.OrdinalIgnoreCase))
            {
                return prefabOverride.prefab;
            }
        }

        return snapshot.Team == Team.Player
            ? defaultPlayerCombatPrefab
            : defaultEnemyCombatPrefab;
    }

    private void ConfigureSpawnedEntity(
        GameObject instance,
        CombatSessionData.CombatParticipantSnapshot snapshot,
        Vector2Int spawnCell,
        int spawnIndex)
    {
        Entity entity = instance.GetComponent<Entity>();
        CharacterStats stats = instance.GetComponent<CharacterStats>();

        if (entity == null || stats == null)
        {
            Debug.LogWarning(
                $"CombatGridSceneManager: prefab '{instance.name}' needs Entity and CharacterStats components.");
            Destroy(instance);
            return;
        }

        entity.team = snapshot.Team;
        entity.name = $"{snapshot.EntityName}_Combat_{spawnIndex}";
        entity.SetGridPosition(spawnCell);
        entity.SetVisualTarget(gridManager.GetCellCenterWorld(spawnCell), true);

        CombatEntityRuntime runtime = instance.GetComponent<CombatEntityRuntime>();
        if (runtime == null)
            runtime = instance.AddComponent<CombatEntityRuntime>();

        runtime.Setup(
            snapshot.CombatantId,
            snapshot.CharacterId,
            snapshot.EntityName,
            snapshot.ExplorationCell,
            spawnIndex,
            true);

        CharacterIdentity identity = instance.GetComponent<CharacterIdentity>();
        if (identity == null)
            identity = instance.AddComponent<CharacterIdentity>();

        identity.SetCharacterId(snapshot.CharacterId);

        if (instance.GetComponent<CombatTurnOutline>() == null)
            instance.AddComponent<CombatTurnOutline>();

        ApplyEntityState(entity, snapshot);
    }

    private void ApplyEntityState(Entity entity, CombatSessionData.EntityStateSnapshot snapshot)
    {
        if (entity == null || snapshot == null)
            return;

        CharacterStats stats = entity.GetStatsComponent();
        EquipmentSlots equipmentSlots = entity.GetEquipmentSlots();

        if (stats == null)
            return;

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

    private void ValidateInspectorConfiguration()
    {
        if (!enableInspectorWarnings)
            return;

        if (gridManager == null)
            Debug.LogWarning("CombatGridSceneManager: 'Grid Manager' nao esta preenchido.", this);

        if (defaultPlayerCombatPrefab == null)
            Debug.LogWarning("CombatGridSceneManager: 'Default Player Combat Prefab' nao esta preenchido.", this);

        if (defaultEnemyCombatPrefab == null)
            Debug.LogWarning("CombatGridSceneManager: 'Default Enemy Combat Prefab' nao esta preenchido.", this);

        ValidateCombatPrefab(defaultPlayerCombatPrefab, "Default Player Combat Prefab");
        ValidateCombatPrefab(defaultEnemyCombatPrefab, "Default Enemy Combat Prefab");

        for (int i = 0; i < prefabOverrides.Count; i++)
        {
            CombatPrefabOverride prefabOverride = prefabOverrides[i];
            if (prefabOverride == null)
            {
                Debug.LogWarning($"CombatGridSceneManager: prefab override {i} esta nulo.", this);
                continue;
            }

            if (string.IsNullOrWhiteSpace(prefabOverride.entityName))
                Debug.LogWarning($"CombatGridSceneManager: prefab override {i} esta sem entityName.", this);

            if (prefabOverride.prefab == null)
            {
                Debug.LogWarning($"CombatGridSceneManager: prefab override '{prefabOverride.entityName}' esta sem prefab.", this);
                continue;
            }

            ValidateCombatPrefab(prefabOverride.prefab, $"Prefab Override '{prefabOverride.entityName}'");
        }
    }

    private void ValidateCombatPrefab(GameObject prefab, string label)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponent<Entity>() == null)
            Debug.LogWarning($"CombatGridSceneManager: {label} '{prefab.name}' nao possui Entity.", this);

        if (prefab.GetComponent<CharacterStats>() == null)
            Debug.LogWarning($"CombatGridSceneManager: {label} '{prefab.name}' nao possui CharacterStats.", this);
    }
}
