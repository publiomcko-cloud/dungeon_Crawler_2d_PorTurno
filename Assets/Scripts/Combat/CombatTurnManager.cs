using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatTurnManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private CombatGridSceneManager combatGridSceneManager;

    [Header("Turn Flow")]
    [SerializeField] private Team startingTeam = Team.Player;
    [SerializeField] private float delayBetweenEnemyActions = 0.3f;
    [SerializeField] private float delayAfterAttack = 0.2f;

    [Header("Active Highlight")]
    [SerializeField] private Color activeOutlineColor = new Color(1f, 0.9f, 0.15f, 0.95f);
    [SerializeField] private float activeOutlineOffset = 0.03f;
    [SerializeField] private int activeOutlineSortingOrderOffset = -1;

    [Header("Enemy AI")]
    [SerializeField] private bool enemyRequiresLineOfSight;

    public Team CurrentTurnTeam { get; private set; }
    public Entity CurrentActiveEntity { get; private set; }
    public int RoundIndex { get; private set; } = 1;
    public bool IsResolvingAction { get; private set; }
    public bool IsCombatFinished { get; private set; }

    private readonly HashSet<Entity> actedThisPhase = new HashSet<Entity>();
    private Coroutine enemyTurnRoutine;
    private bool hasStartedCombatLoop;
    private bool returnTriggered;
    private Entity highlightedEntity;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (combatGridSceneManager == null)
            combatGridSceneManager = FindFirstObjectByType<CombatGridSceneManager>();
    }

    private void OnEnable()
    {
        if (combatGridSceneManager == null)
            combatGridSceneManager = FindFirstObjectByType<CombatGridSceneManager>();

        if (combatGridSceneManager != null)
            combatGridSceneManager.OnCombatantsBuilt += HandleCombatantsBuilt;
    }

    private void OnDisable()
    {
        if (combatGridSceneManager != null)
            combatGridSceneManager.OnCombatantsBuilt -= HandleCombatantsBuilt;

        SetHighlightedEntity(null);
    }

    private void Start()
    {
        if (combatGridSceneManager != null && combatGridSceneManager.HasBuiltCombatants)
            StartCombatLoop();
        else if (combatGridSceneManager == null)
            StartCombatLoop();
    }

    private void HandleCombatantsBuilt()
    {
        ApplyHighlightSettingsToCombatants();
        StartCombatLoop();
    }

    private void StartCombatLoop()
    {
        if (hasStartedCombatLoop)
            return;

        hasStartedCombatLoop = true;
        ApplyHighlightSettingsToCombatants();

        CombatSessionData.CombatSessionSnapshot session = CombatSessionData.CurrentSession;
        if (session != null)
            startingTeam = session.InitiatingTeam;

        CurrentTurnTeam = startingTeam;
        BeginTurnPhase(CurrentTurnTeam);
    }

    private void Update()
    {
        if (IsCombatFinished || IsResolvingAction || CurrentTurnTeam != Team.Player)
            return;

        if (!TryEnsureActiveEntity())
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SelectNextAvailablePlayerEntity();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CompleteEntityAction(CurrentActiveEntity);
            return;
        }

        Vector2Int direction = ReadDirectionInput();
        if (direction != Vector2Int.zero)
            TryActWithEntity(CurrentActiveEntity, direction);
    }

    private Vector2Int ReadDirectionInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            return Vector2Int.right;
        return Vector2Int.zero;
    }

    private void BeginTurnPhase(Team team)
    {
        if (IsCombatFinished)
            return;

        CurrentTurnTeam = team;
        actedThisPhase.Clear();
        CurrentActiveEntity = null;
        SetHighlightedEntity(null);

        if (CheckCombatFinished())
            return;

        if (CurrentTurnTeam == Team.Player)
        {
            SelectNextAvailablePlayerEntity();
            return;
        }

        if (enemyTurnRoutine != null)
            StopCoroutine(enemyTurnRoutine);

        enemyTurnRoutine = StartCoroutine(RunEnemyTurnRoutine());
    }

    private bool TryEnsureActiveEntity()
    {
        if (CurrentActiveEntity != null &&
            !CurrentActiveEntity.IsDead &&
            CurrentActiveEntity.team == CurrentTurnTeam &&
            !actedThisPhase.Contains(CurrentActiveEntity))
        {
            SetHighlightedEntity(CurrentActiveEntity);
            return true;
        }

        CurrentActiveEntity = GetAvailableEntities(CurrentTurnTeam).FirstOrDefault();
        SetHighlightedEntity(CurrentActiveEntity);
        return CurrentActiveEntity != null;
    }

    private void SelectNextAvailablePlayerEntity()
    {
        List<Entity> availablePlayers = GetAvailableEntities(Team.Player);
        if (availablePlayers.Count == 0)
        {
            EndCurrentPhase();
            return;
        }

        if (CurrentActiveEntity == null || !availablePlayers.Contains(CurrentActiveEntity))
        {
            CurrentActiveEntity = availablePlayers[0];
            SetHighlightedEntity(CurrentActiveEntity);
            return;
        }

        int currentIndex = availablePlayers.IndexOf(CurrentActiveEntity);
        int nextIndex = (currentIndex + 1) % availablePlayers.Count;
        CurrentActiveEntity = availablePlayers[nextIndex];
        SetHighlightedEntity(CurrentActiveEntity);
    }

    private IEnumerator RunEnemyTurnRoutine()
    {
        yield return null;

        while (!IsCombatFinished && CurrentTurnTeam == Team.Enemy)
        {
            List<Entity> availableEnemies = GetAvailableEntities(Team.Enemy);
            if (availableEnemies.Count == 0)
            {
                EndCurrentPhase();
                yield break;
            }

            Entity actingEnemy = availableEnemies[0];
            CurrentActiveEntity = actingEnemy;
            SetHighlightedEntity(CurrentActiveEntity);

            Vector2Int? actionDirection = GetEnemyActionDirection(actingEnemy);
            if (actionDirection.HasValue)
                yield return StartCoroutine(ResolveActionRoutine(actingEnemy, actionDirection.Value));
            else
                CompleteEntityAction(actingEnemy);

            yield return new WaitForSeconds(delayBetweenEnemyActions);
        }
    }

    private Vector2Int? GetEnemyActionDirection(Entity enemy)
    {
        if (enemy == null || enemy.IsDead)
            return null;

        List<Entity> playerTargets = gridManager.GetEntitiesByTeam(Team.Player);
        if (playerTargets.Count == 0)
            return null;

        Entity nearestTarget = null;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < playerTargets.Count; i++)
        {
            Entity target = playerTargets[i];
            if (target == null || target.IsDead)
                continue;

            int distance = Manhattan(enemy.GridPosition, target.GridPosition);
            if (enemyRequiresLineOfSight && !gridManager.HasLineOfSight(enemy.GridPosition, target.GridPosition))
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestTarget = target;
            }
        }

        if (nearestTarget == null)
            return null;

        Vector2Int delta = nearestTarget.GridPosition - enemy.GridPosition;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && delta.x != 0)
        {
            Vector2Int horizontal = new Vector2Int(delta.x > 0 ? 1 : -1, 0);
            if (CanEnemyUseDirection(enemy, horizontal))
                return horizontal;
        }

        if (delta.y != 0)
        {
            Vector2Int vertical = new Vector2Int(0, delta.y > 0 ? 1 : -1);
            if (CanEnemyUseDirection(enemy, vertical))
                return vertical;
        }

        return null;
    }

    private bool CanEnemyUseDirection(Entity enemy, Vector2Int direction)
    {
        Vector2Int targetCell = enemy.GridPosition + direction;
        if (gridManager.IsCellBlocked(targetCell))
            return false;

        List<Entity> occupants = gridManager.GetEntitiesAtCell(targetCell);
        if (occupants.Count == 0)
            return true;

        bool hasEnemy = occupants.Any(entity => entity != null && !entity.IsDead && entity.team != enemy.team);
        bool hasAlly = occupants.Any(entity => entity != null && !entity.IsDead && entity.team == enemy.team);
        return hasEnemy || !hasAlly;
    }

    private void TryActWithEntity(Entity entity, Vector2Int direction)
    {
        if (entity == null || entity.IsDead || IsResolvingAction)
            return;

        StartCoroutine(ResolveActionRoutine(entity, direction));
    }

    private IEnumerator ResolveActionRoutine(Entity entity, Vector2Int direction)
    {
        if (entity == null || entity.IsDead)
            yield break;

        IsResolvingAction = true;

        Vector2Int targetCell = entity.GridPosition + direction;
        if (gridManager.IsCellBlocked(targetCell))
        {
            IsResolvingAction = false;
            yield break;
        }

        List<Entity> targetEntities = gridManager.GetEntitiesAtCell(targetCell);
        bool hasEnemy = targetEntities.Any(other => other != null && !other.IsDead && other.team != entity.team);
        bool hasAlly = targetEntities.Any(other => other != null && !other.IsDead && other.team == entity.team);

        if (hasAlly && !hasEnemy)
        {
            IsResolvingAction = false;
            yield break;
        }

        bool actionDone;
        if (hasEnemy)
        {
            actionDone = ExecuteAttack(entity, targetCell);
            if (actionDone && delayAfterAttack > 0f)
                yield return new WaitForSeconds(delayAfterAttack);
        }
        else
        {
            actionDone = gridManager.TryMoveGroupOrAttack(new List<Entity> { entity }, targetCell);
        }

        IsResolvingAction = false;

        if (actionDone)
            CompleteEntityAction(entity);
    }

    private bool ExecuteAttack(Entity attacker, Vector2Int targetCell)
    {
        if (attacker == null || attacker.IsDead)
            return false;

        List<Entity> defenders = gridManager.GetEntitiesAtCell(targetCell)
            .Where(entity => entity != null && !entity.IsDead && entity.team != attacker.team)
            .ToList();

        if (defenders.Count == 0)
            return false;

        Vector3 attackDirection = (gridManager.GetCellCenterWorld(targetCell) - gridManager.GetCellCenterWorld(attacker.GridPosition)).normalized;
        attacker.PlayAttackLunge(attackDirection);

        int damage = Mathf.Max(1, attacker.attackDamage);
        Entity primaryTarget = defenders[0];
        CharacterStats defenderStats = primaryTarget.GetStatsComponent();
        int finalDamage = defenderStats != null ? defenderStats.CalculateIncomingDamage(damage) : damage;

        primaryTarget.ReceiveDamage(finalDamage);
        return true;
    }

    private void CompleteEntityAction(Entity entity)
    {
        if (entity != null)
            actedThisPhase.Add(entity);

        if (CheckCombatFinished())
            return;

        List<Entity> remaining = GetAvailableEntities(CurrentTurnTeam);
        if (remaining.Count == 0)
        {
            EndCurrentPhase();
            return;
        }

        CurrentActiveEntity = CurrentTurnTeam == Team.Player ? remaining[0] : null;
        SetHighlightedEntity(CurrentActiveEntity);
    }

    private void EndCurrentPhase()
    {
        if (CheckCombatFinished())
            return;

        if (CurrentTurnTeam == Team.Player)
            BeginTurnPhase(Team.Enemy);
        else
        {
            RoundIndex += 1;
            BeginTurnPhase(Team.Player);
        }
    }

    private bool CheckCombatFinished()
    {
        bool hasPlayers = gridManager.GetEntitiesByTeam(Team.Player).Count > 0;
        bool hasEnemies = gridManager.GetEntitiesByTeam(Team.Enemy).Count > 0;

        if (hasPlayers && hasEnemies)
            return false;

        IsCombatFinished = true;
        SetHighlightedEntity(null);

        if (enemyTurnRoutine != null)
        {
            StopCoroutine(enemyTurnRoutine);
            enemyTurnRoutine = null;
        }

        Team winner = hasPlayers ? Team.Player : Team.Enemy;
        if (!returnTriggered)
        {
            returnTriggered = true;
            StartCoroutine(ReturnToExplorationRoutine(winner));
        }

        return true;
    }

    private List<Entity> GetAvailableEntities(Team team)
    {
        return gridManager.GetEntitiesByTeam(team)
            .Where(entity => entity != null && !entity.IsDead && !actedThisPhase.Contains(entity))
            .OrderBy(entity => entity.name)
            .ToList();
    }

    private void ApplyHighlightSettingsToCombatants()
    {
        if (gridManager == null)
            return;

        List<Entity> combatants = gridManager.GetEntitiesByTeam(Team.Player)
            .Concat(gridManager.GetEntitiesByTeam(Team.Enemy))
            .Where(entity => entity != null)
            .ToList();

        for (int i = 0; i < combatants.Count; i++)
            ApplyHighlightSettings(combatants[i]);
    }

    private void SetHighlightedEntity(Entity entity)
    {
        if (highlightedEntity == entity)
            return;

        ToggleEntityOutline(highlightedEntity, false);
        highlightedEntity = entity;
        ToggleEntityOutline(highlightedEntity, true);
    }

    private void ToggleEntityOutline(Entity entity, bool highlighted)
    {
        if (entity == null)
            return;

        CombatTurnOutline outline = entity.GetComponent<CombatTurnOutline>();
        if (outline == null)
            return;

        ApplyHighlightSettings(entity);
        outline.SetHighlighted(highlighted);
    }

    private void ApplyHighlightSettings(Entity entity)
    {
        if (entity == null)
            return;

        CombatTurnOutline outline = entity.GetComponent<CombatTurnOutline>();
        if (outline != null)
            outline.Configure(activeOutlineColor, activeOutlineOffset, activeOutlineSortingOrderOffset);
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private IEnumerator ReturnToExplorationRoutine(Team winner)
    {
        yield return new WaitForSeconds(0.4f);

        CombatSessionData.CombatSessionSnapshot session = CombatSessionData.CurrentSession;
        if (session == null)
            yield break;

        if (winner == Team.Player)
            PreparePlayerVictoryReturn(session);
        else
            CombatExplorationReturnData.Clear();

        string explorationSceneName = session.ExplorationSceneName;
        CombatSessionData.ClearSession();
        SceneManager.LoadScene(explorationSceneName, LoadSceneMode.Single);
    }

    private void PreparePlayerVictoryReturn(CombatSessionData.CombatSessionSnapshot session)
    {
        List<Entity> playerSurvivors = gridManager.GetEntitiesByTeam(Team.Player);
        List<CombatExplorationReturnData.PlayerReturnSnapshot> survivorSnapshots =
            new List<CombatExplorationReturnData.PlayerReturnSnapshot>();

        for (int i = 0; i < playerSurvivors.Count; i++)
        {
            Entity entity = playerSurvivors[i];
            if (entity == null || entity.IsDead)
                continue;

            CombatEntityRuntime runtime = entity.GetComponent<CombatEntityRuntime>();
            string originalName = runtime != null && !string.IsNullOrWhiteSpace(runtime.OriginalEntityName)
                ? runtime.OriginalEntityName
                : entity.name;
            string originalCharacterId = runtime != null && !string.IsNullOrWhiteSpace(runtime.OriginalCharacterId)
                ? runtime.OriginalCharacterId
                : CharacterIdentity.ResolveFromEntity(entity);
            string combatantId = runtime != null && !string.IsNullOrWhiteSpace(runtime.CombatantId)
                ? runtime.CombatantId
                : "";

            CombatSessionData.CombatParticipantSnapshot originalSnapshot =
                FindOriginalPlayerSnapshot(session, combatantId, originalCharacterId, originalName);
            CombatSessionData.EntityStateSnapshot currentState = new CombatSessionData.EntityStateSnapshot(entity);
            Vector2Int returnCell = session.InitiatingTeam == Team.Player ? session.DefenderCell : session.AttackerCell;

            survivorSnapshots.Add(new CombatExplorationReturnData.PlayerReturnSnapshot(
                originalName,
                combatantId,
                originalCharacterId,
                returnCell,
                currentState.CurrentHP,
                currentState.CurrentXP,
                currentState.UnspentStatPoints,
                currentState.Level,
                currentState.MoneyReward,
                currentState.QuestEnemyId,
                currentState.EnemyPrefabId,
                currentState.BaseStats,
                currentState.PointBonus,
                currentState.EquippedWeapon ?? originalSnapshot?.EquippedWeapon,
                currentState.EquippedArmor ?? originalSnapshot?.EquippedArmor,
                currentState.EquippedAccessory ?? originalSnapshot?.EquippedAccessory));
        }

        HashSet<string> survivingEnemyIds = new HashSet<string>(
            gridManager.GetEntitiesByTeam(Team.Enemy)
                .Select(entity => entity != null ? entity.GetComponent<CombatEntityRuntime>() : null)
                .Where(runtime => runtime != null && !string.IsNullOrWhiteSpace(runtime.CombatantId))
                .Select(runtime => runtime.CombatantId));

        List<CombatSessionData.CombatParticipantSnapshot> defeatedEnemySnapshots =
            session.Attackers.Where(snapshot => snapshot.Team == Team.Enemy && !survivingEnemyIds.Contains(snapshot.CombatantId))
            .Concat(session.Defenders.Where(snapshot => snapshot.Team == Team.Enemy && !survivingEnemyIds.Contains(snapshot.CombatantId)))
            .ToList();

        int initialEnemyCount = session.Attackers.Count(participant => participant.Team == Team.Enemy) +
            session.Defenders.Count(participant => participant.Team == Team.Enemy);
        int survivingEnemyCount = gridManager.GetEntitiesByTeam(Team.Enemy).Count;
        int defeatedEnemyCount = Mathf.Max(0, initialEnemyCount - survivingEnemyCount);
        int rewardMoney = defeatedEnemySnapshots.Sum(snapshot => snapshot.MoneyReward);
        rewardMoney += defeatedEnemySnapshots
            .Where(snapshot => snapshot != null && snapshot.IsDungeonBoss)
            .Sum(snapshot => snapshot.BossRewardMoney);

        List<string> defeatedBossKeys = defeatedEnemySnapshots
            .Where(snapshot => snapshot != null && snapshot.IsDungeonBoss && !string.IsNullOrWhiteSpace(snapshot.BossPersistenceKey))
            .Select(snapshot => snapshot.BossPersistenceKey)
            .Distinct()
            .ToList();

        for (int i = 0; i < defeatedBossKeys.Count; i++)
            DungeonBossPersistence.MarkBossDefeated(defeatedBossKeys[i]);

        List<CombatExplorationReturnData.EnemyReturnSnapshot> preservedEnemies = session.PreservedExplorationEnemies
            .Select(snapshot => new CombatExplorationReturnData.EnemyReturnSnapshot(
                snapshot.CombatantId,
                snapshot.CharacterId,
                snapshot.EntityName,
                snapshot.Cell,
                snapshot.CurrentHP,
                snapshot.CurrentXP,
                snapshot.UnspentStatPoints,
                snapshot.Level,
                snapshot.MoneyReward,
                snapshot.QuestEnemyId,
                snapshot.EnemyPrefabId,
                snapshot.BaseStats,
                snapshot.PointBonus,
                snapshot.EquippedWeapon,
                snapshot.EquippedArmor,
                snapshot.EquippedAccessory))
            .ToList();

        List<InventoryItemEntry> lootEntries = RollLootEntries(defeatedEnemySnapshots);
        AddBossRewardEntries(defeatedEnemySnapshots, lootEntries);
        Vector2Int finalReturnCell = session.InitiatingTeam == Team.Player ? session.DefenderCell : session.AttackerCell;
        string leaderCharacterId = PartyAnchorService.Instance != null && PartyAnchorService.Instance.GetLeader() != null
            ? CharacterIdentity.ResolveFromEntity(PartyAnchorService.Instance.GetLeader())
            : null;

        CombatExplorationReturnData.SetPendingReturn(
            new CombatExplorationReturnData.ExplorationReturnSnapshot(
                session.ExplorationSceneName,
                finalReturnCell,
                finalReturnCell,
                leaderCharacterId,
                defeatedEnemyCount,
                rewardMoney,
                defeatedBossKeys,
                survivorSnapshots,
                preservedEnemies,
                lootEntries,
                CloneInventoryEntries(session.PartyInventoryItems)));
    }

    private CombatSessionData.CombatParticipantSnapshot FindOriginalPlayerSnapshot(
        CombatSessionData.CombatSessionSnapshot session,
        string combatantId,
        string characterId,
        string originalName)
    {
        CombatSessionData.CombatParticipantSnapshot snapshot = session.Attackers
            .Concat(session.Defenders)
            .FirstOrDefault(candidate =>
                candidate.Team == Team.Player &&
                !string.IsNullOrWhiteSpace(combatantId) &&
                candidate.CombatantId == combatantId);

        if (snapshot != null)
            return snapshot;

        snapshot = session.Attackers
            .Concat(session.Defenders)
            .FirstOrDefault(candidate =>
                candidate.Team == Team.Player &&
                !string.IsNullOrWhiteSpace(characterId) &&
                candidate.CharacterId == characterId);

        if (snapshot != null)
            return snapshot;

        return session.Attackers
            .Concat(session.Defenders)
            .FirstOrDefault(candidate => candidate.Team == Team.Player && candidate.EntityName == originalName);
    }

    private List<InventoryItemEntry> RollLootEntries(List<CombatSessionData.CombatParticipantSnapshot> defeatedEnemySnapshots)
    {
        List<InventoryItemEntry> result = new List<InventoryItemEntry>();

        for (int i = 0; i < defeatedEnemySnapshots.Count; i++)
        {
            CombatSessionData.CombatParticipantSnapshot snapshot = defeatedEnemySnapshots[i];
            if (snapshot == null || snapshot.LootTable == null || snapshot.LootTable.Count == 0)
                continue;

            List<LootDropEntry> validDrops = new List<LootDropEntry>();
            for (int j = 0; j < snapshot.LootTable.Count; j++)
            {
                LootDropEntry entry = snapshot.LootTable[j];
                if (entry != null && entry.HasValidItemSource() && entry.RollDrop())
                    validDrops.Add(entry);
            }

            if (validDrops.Count == 0)
                continue;

            LootDropEntry chosen = validDrops[Random.Range(0, validDrops.Count)];
            if (chosen.staticItem != null)
            {
                result.Add(InventoryItemEntry.FromStatic(chosen.staticItem));
            }
            else if (chosen.generatedProfile != null)
            {
                GeneratedItemInstance generated = ItemGenerator.Generate(chosen.generatedProfile);
                if (generated != null)
                    result.Add(InventoryItemEntry.FromGenerated(generated));
            }
        }

        return result;
    }

    private void AddBossRewardEntries(
        List<CombatSessionData.CombatParticipantSnapshot> defeatedEnemySnapshots,
        List<InventoryItemEntry> lootEntries)
    {
        if (defeatedEnemySnapshots == null || lootEntries == null)
            return;

        for (int i = 0; i < defeatedEnemySnapshots.Count; i++)
        {
            CombatSessionData.CombatParticipantSnapshot snapshot = defeatedEnemySnapshots[i];
            if (snapshot == null || !snapshot.IsDungeonBoss || snapshot.BossRewardEntry == null || snapshot.BossRewardEntry.IsEmpty)
                continue;

            lootEntries.Add(snapshot.BossRewardEntry.Clone());
        }
    }

    private List<InventoryItemEntry> CloneInventoryEntries(IEnumerable<InventoryItemEntry> source)
    {
        if (source == null)
            return new List<InventoryItemEntry>();

        return source
            .Select(entry => entry != null ? entry.Clone() : new InventoryItemEntry())
            .ToList();
    }
}
