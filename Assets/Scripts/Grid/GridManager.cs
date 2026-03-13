using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid")]
    public int maxEntitiesPerCell = 4;
    public float slotOffset = 0.18f;

    [Header("Walls")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckRadius = 0.2f;

    private readonly Dictionary<Vector2Int, List<Entity>> grid = new Dictionary<Vector2Int, List<Entity>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterEntity(Entity entity, Vector2Int cell)
    {
        if (entity == null) return;
        if (IsCellBlocked(cell))
        {
            Debug.LogWarning($"Tentativa de registrar entidade em célula bloqueada: {cell}");
            return;
        }

        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Entity>();

        CleanupCell(cell);

        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Entity>();

        if (grid[cell].Count >= maxEntitiesPerCell)
        {
            Debug.LogWarning($"Cell {cell} já está cheia.");
            return;
        }

        if (!grid[cell].Contains(entity))
            grid[cell].Add(entity);

        entity.SetGridPosition(cell);
        RefreshCellVisuals(cell, true);
    }

    public void RemoveEntity(Entity entity)
    {
        if (entity == null) return;

        Vector2Int cell = entity.GridPosition;

        if (!grid.ContainsKey(cell))
            return;

        grid[cell].Remove(entity);

        if (grid[cell].Count == 0)
            grid.Remove(cell);
        else
            RefreshCellVisuals(cell, false);
    }

    public List<Entity> GetEntitiesAtCell(Vector2Int cell)
    {
        if (!grid.ContainsKey(cell))
            return new List<Entity>();

        CleanupCell(cell);

        if (!grid.ContainsKey(cell))
            return new List<Entity>();

        return grid[cell].Where(e => e != null && !e.IsDead).ToList();
    }

    public List<Entity> GetEntitiesByTeam(Team team)
    {
        List<Entity> result = new List<Entity>();

        foreach (var cell in grid.Keys.ToList())
        {
            CleanupCell(cell);

            if (!grid.ContainsKey(cell))
                continue;

            foreach (var entity in grid[cell])
            {
                if (entity != null && !entity.IsDead && entity.team == team)
                    result.Add(entity);
            }
        }

        return result;
    }

    public List<Vector2Int> GetOccupiedCellsByTeam(Team team)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        foreach (var cell in grid.Keys.ToList())
        {
            CleanupCell(cell);

            if (!grid.ContainsKey(cell))
                continue;

            bool hasTeam = grid[cell].Any(e => e != null && !e.IsDead && e.team == team);
            if (hasTeam)
                result.Add(cell);
        }

        return result;
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        return GetEntitiesAtCell(cell).Count > 0;
    }

    public bool IsCellBlocked(Vector2Int cell)
    {
        Vector2 world = GetCellCenterWorld(cell);
        Collider2D hit = Physics2D.OverlapCircle(world, wallCheckRadius, wallLayer);
        return hit != null;
    }

    public bool HasLineOfSight(Vector2Int fromCell, Vector2Int toCell)
    {
        Vector2 from = GetCellCenterWorld(fromCell);
        Vector2 to = GetCellCenterWorld(toCell);

        RaycastHit2D hit = Physics2D.Linecast(from, to, wallLayer);
        return hit.collider == null;
    }

    public bool TryMoveGroupOrAttack(List<Entity> movers, Vector2Int targetCell)
    {
        movers = movers.Where(e => e != null && !e.IsDead).ToList();
        if (movers.Count == 0) return false;

        Team movingTeam = movers[0].team;
        Vector2Int sourceCell = movers[0].GridPosition;

        if (movers.Any(e => e.team != movingTeam))
            return false;

        if (movers.Any(e => e.GridPosition != sourceCell))
            return false;

        if (IsCellBlocked(targetCell))
            return false;

        List<Entity> targetEntities = GetEntitiesAtCell(targetCell);

        if (targetEntities.Count == 0)
        {
            MoveGroup(movers, sourceCell, targetCell);
            return true;
        }

        bool hasEnemy = targetEntities.Any(e => e.team != movingTeam);
        if (hasEnemy)
        {
            List<Entity> attackers = movers
                .Where(e => e != null && !e.IsDead && e.team == movingTeam)
                .ToList();

            List<Entity> defenders = targetEntities
                .Where(e => e != null && !e.IsDead && e.team != movingTeam)
                .ToList();

            if (CombatTransitionManager.Instance != null &&
                CombatTransitionManager.Instance.TryStartCombatTransition(
                    attackers,
                    defenders,
                    sourceCell,
                    targetCell,
                    movingTeam))
            {
                return true;
            }

            ResolveCellAttack(sourceCell, targetCell, movingTeam);
            return true;
        }

        int futureCount = targetEntities.Count + movers.Count;
        if (futureCount > maxEntitiesPerCell)
            return false;

        MoveGroup(movers, sourceCell, targetCell);
        return true;
    }

    public void ResolveCellAttack(Vector2Int attackerCell, Vector2Int defenderCell, Team attackerTeam)
    {
        List<Entity> attackers = GetEntitiesAtCell(attackerCell)
            .Where(e => e.team == attackerTeam)
            .ToList();

        List<Entity> defenders = GetEntitiesAtCell(defenderCell)
            .Where(e => e.team != attackerTeam)
            .ToList();

        if (attackers.Count == 0 || defenders.Count == 0)
            return;

        Vector3 attackDirection = (GetCellCenterWorld(defenderCell) - GetCellCenterWorld(attackerCell)).normalized;

        foreach (Entity attacker in attackers)
        {
            if (attacker != null && !attacker.IsDead)
                attacker.PlayAttackLunge(attackDirection);
        }

        int totalAtk = 0;
        foreach (Entity attacker in attackers)
        {
            CharacterStats attackerStats = attacker.GetStatsComponent();
            if (attackerStats != null)
                totalAtk += Mathf.Max(0, attackerStats.Atk);
        }

        if (totalAtk <= 0)
            totalAtk = attackers.Count;

        DistributeDamage(defenders, totalAtk);
    }

    private void DistributeDamage(List<Entity> defenders, int totalIncomingDamage)
    {
        defenders = defenders.Where(e => e != null && !e.IsDead).ToList();
        if (defenders.Count == 0 || totalIncomingDamage <= 0) return;

        int livingCount = defenders.Count;
        int baseShare = totalIncomingDamage / livingCount;
        int remainder = totalIncomingDamage % livingCount;

        for (int i = 0; i < defenders.Count; i++)
        {
            if (defenders[i] == null || defenders[i].IsDead)
                continue;

            int incomingDamage = baseShare;
            if (i < remainder)
                incomingDamage += 1;

            CharacterStats defenderStats = defenders[i].GetStatsComponent();
            int finalDamage = incomingDamage;

            if (defenderStats != null)
                finalDamage = defenderStats.CalculateIncomingDamage(incomingDamage);

            defenders[i].ReceiveDamage(finalDamage);
        }
    }

    private void MoveGroup(List<Entity> movers, Vector2Int sourceCell, Vector2Int targetCell)
    {
        if (!grid.ContainsKey(sourceCell))
            return;

        CleanupCell(sourceCell);

        if (!grid.ContainsKey(sourceCell))
            return;

        if (!grid.ContainsKey(targetCell))
            grid[targetCell] = new List<Entity>();

        foreach (Entity mover in movers)
        {
            if (grid.ContainsKey(sourceCell))
                grid[sourceCell].Remove(mover);

            if (!grid[targetCell].Contains(mover))
                grid[targetCell].Add(mover);

            mover.SetGridPosition(targetCell);
        }

        CleanupCell(sourceCell);
        CleanupCell(targetCell);

        if (grid.ContainsKey(sourceCell))
            RefreshCellVisuals(sourceCell, false);

        if (grid.ContainsKey(targetCell))
            RefreshCellVisuals(targetCell, false);
    }

    public Vector3 GetCellCenterWorld(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }

    private void RefreshCellVisuals(Vector2Int cell, bool snapImmediately)
    {
        if (!grid.ContainsKey(cell))
            return;

        CleanupCell(cell);

        if (!grid.ContainsKey(cell))
            return;

        List<Entity> entities = grid[cell].Where(e => e != null && !e.IsDead).ToList();

        for (int i = 0; i < entities.Count; i++)
        {
            Vector3 targetPos = GetSlotWorldPosition(cell, i);
            entities[i].SetVisualTarget(targetPos, snapImmediately);
        }
    }

    private Vector3 GetSlotWorldPosition(Vector2Int cell, int index)
    {
        Vector3 center = GetCellCenterWorld(cell);

        switch (index)
        {
            case 0: return center + new Vector3(-slotOffset,  slotOffset, 0f);
            case 1: return center + new Vector3( slotOffset,  slotOffset, 0f);
            case 2: return center + new Vector3(-slotOffset, -slotOffset, 0f);
            case 3: return center + new Vector3( slotOffset, -slotOffset, 0f);
            default: return center;
        }
    }

    private void CleanupCell(Vector2Int cell)
    {
        if (!grid.ContainsKey(cell))
            return;

        grid[cell].RemoveAll(e => e == null || e.IsDead);

        if (grid[cell].Count == 0)
            grid.Remove(cell);
    }
}
