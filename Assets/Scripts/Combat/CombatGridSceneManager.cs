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
        if (buildOnStart)
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

        SpawnSide(participants.Where(participant => participant.Team == Team.Player).ToList(), playerOriginCell, false);
        SpawnSide(participants.Where(participant => participant.Team == Team.Enemy).ToList(), enemyOriginCell, true);

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
            return false;

        formationRowsPerColumn = Mathf.Max(1, formationRowsPerColumn);
        rowSpacing = Mathf.Max(1, rowSpacing);
        columnSpacing = Mathf.Max(1, columnSpacing);
        return true;
    }

    private void SpawnSide(List<CombatSessionData.CombatParticipantSnapshot> participants, Vector2Int originCell, bool mirrorHorizontally)
    {
        for (int i = 0; i < participants.Count; i++)
        {
            CombatSessionData.CombatParticipantSnapshot snapshot = participants[i];
            Vector2Int spawnCell = GetFormationCell(originCell, i, mirrorHorizontally);

            if (gridManager.IsCellBlocked(spawnCell))
                continue;

            GameObject prefab = ResolvePrefab(snapshot);
            if (prefab == null)
                continue;

            Vector3 spawnPosition = gridManager.GetCellCenterWorld(spawnCell);
            GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, spawnRoot != null ? spawnRoot : null);
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
        return new Vector2Int(originCell.x + horizontalStep, originCell.y - verticalStep);
    }

    private GameObject ResolvePrefab(CombatSessionData.CombatParticipantSnapshot snapshot)
    {
        for (int i = 0; i < prefabOverrides.Count; i++)
        {
            CombatPrefabOverride prefabOverride = prefabOverrides[i];
            if (prefabOverride == null || prefabOverride.prefab == null)
                continue;

            if (string.Equals(prefabOverride.entityName, snapshot.EntityName, StringComparison.OrdinalIgnoreCase))
                return prefabOverride.prefab;
        }

        return snapshot.Team == Team.Player ? defaultPlayerCombatPrefab : defaultEnemyCombatPrefab;
    }

    private void ConfigureSpawnedEntity(GameObject instance, CombatSessionData.CombatParticipantSnapshot snapshot, Vector2Int spawnCell, int spawnIndex)
    {
        Entity entity = instance.GetComponent<Entity>();
        CharacterStats stats = instance.GetComponent<CharacterStats>();
        if (entity == null || stats == null)
        {
            Destroy(instance);
            return;
        }

        entity.team = snapshot.Team;
        entity.name = $"{snapshot.EntityName}_Combat_{spawnIndex}";
        entity.SetGridPosition(spawnCell);
        entity.SetVisualTarget(gridManager.GetCellCenterWorld(spawnCell), true);
        entity.SetMoneyReward(snapshot.MoneyReward);
        entity.SetQuestEnemyId(snapshot.QuestEnemyId);
        entity.SetEnemyPrefabId(snapshot.EnemyPrefabId);

        CombatEntityRuntime runtime = instance.GetComponent<CombatEntityRuntime>();
        if (runtime == null)
            runtime = instance.AddComponent<CombatEntityRuntime>();

        runtime.Setup(snapshot.CombatantId, snapshot.CharacterId, snapshot.EntityName, snapshot.ExplorationCell, spawnIndex, true);

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
            entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
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
    }
}
