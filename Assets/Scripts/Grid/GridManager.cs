using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid")]
    public int maxEntitiesPerCell = 4;
    public float slotOffset = 0.22f;

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
        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Entity>();

        if (grid[cell].Count >= maxEntitiesPerCell)
        {
            Debug.LogWarning($"Cell {cell} já está cheia.");
            return;
        }

        grid[cell].Add(entity);
        entity.SetGridPosition(cell);
        RefreshCellVisuals(cell);
    }

    public void RemoveEntity(Entity entity)
    {
        Vector2Int cell = entity.GridPosition;

        if (!grid.ContainsKey(cell))
            return;

        grid[cell].Remove(entity);

        if (grid[cell].Count == 0)
            grid.Remove(cell);
        else
            RefreshCellVisuals(cell);
    }

    public List<Entity> GetEntitiesAtCell(Vector2Int cell)
    {
        if (!grid.ContainsKey(cell))
            return new List<Entity>();

        return grid[cell].Where(e => e != null && !e.IsDead).ToList();
    }

    public List<Entity> GetEntitiesByTeam(Team team)
    {
        List<Entity> result = new List<Entity>();

        foreach (var pair in grid)
        {
            foreach (var entity in pair.Value)
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

        foreach (var pair in grid)
        {
            bool hasTeam = pair.Value.Any(e => e != null && !e.IsDead && e.team == team);
            if (hasTeam)
                result.Add(pair.Key);
        }

        return result;
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        return grid.ContainsKey(cell) && GetEntitiesAtCell(cell).Count > 0;
    }

    public bool IsEnemyInCell(Vector2Int cell, Team team)
    {
        return GetEntitiesAtCell(cell).Any(e => e.team != team);
    }

    public bool IsFriendlyOnlyCell(Vector2Int cell, Team team)
    {
        List<Entity> entities = GetEntitiesAtCell(cell);
        return entities.Count > 0 && entities.All(e => e.team == team);
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

        List<Entity> targetEntities = GetEntitiesAtCell(targetCell);

        if (targetEntities.Count == 0)
        {
            MoveGroup(movers, sourceCell, targetCell);
            return true;
        }

        bool hasEnemy = targetEntities.Any(e => e.team != movingTeam);
        if (hasEnemy)
        {
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

        int totalDamage = attackers.Sum(a => a.attackDamage);
        DistributeDamage(defenders, totalDamage);
    }

    private void DistributeDamage(List<Entity> defenders, int totalDamage)
    {
        defenders = defenders.Where(e => e != null && !e.IsDead).ToList();
        if (defenders.Count == 0 || totalDamage <= 0) return;

        int livingCount = defenders.Count;
        int baseDamage = totalDamage / livingCount;
        int remainder = totalDamage % livingCount;

        for (int i = 0; i < defenders.Count; i++)
        {
            if (defenders[i] == null || defenders[i].IsDead) continue;

            int damage = baseDamage;
            if (i < remainder) damage += 1;

            if (damage > 0)
                defenders[i].ReceiveDamage(damage);
        }
    }

    private void MoveGroup(List<Entity> movers, Vector2Int sourceCell, Vector2Int targetCell)
    {
        if (!grid.ContainsKey(sourceCell))
            return;

        if (!grid.ContainsKey(targetCell))
            grid[targetCell] = new List<Entity>();

        foreach (Entity mover in movers)
        {
            grid[sourceCell].Remove(mover);
            grid[targetCell].Add(mover);
            mover.SetGridPosition(targetCell);
        }

        if (grid.ContainsKey(sourceCell))
        {
            if (grid[sourceCell].Count == 0)
                grid.Remove(sourceCell);
            else
                RefreshCellVisuals(sourceCell);
        }

        RefreshCellVisuals(targetCell);
    }

    public Vector3 GetCellCenterWorld(Vector2Int cell)
    {
        return new Vector3(cell.x, cell.y, 0f);
    }

    private void RefreshCellVisuals(Vector2Int cell)
    {
        if (!grid.ContainsKey(cell))
            return;

        List<Entity> entities = grid[cell].Where(e => e != null).ToList();
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            entities[i].transform.position = GetSlotWorldPosition(cell, i);
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
}