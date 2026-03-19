using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Turn Timing")]
    public float delayBetweenEnemyGroups = 0.15f;

    [Header("Exploration")]
    [SerializeField] private bool keepEnemiesStaticInExploration = true;
    [SerializeField] private bool allowAdjacentCombatTrigger = true;
    [SerializeField] private bool useOnlyOrthogonalAdjacency = true;

    [Header("Vision")]
    public int maxVisionRange = 6;
    public bool requireLineOfSight = true;

    public IEnumerator ExecuteEnemyTurn()
    {
        if (keepEnemiesStaticInExploration)
            yield break;

        List<Vector2Int> enemyCells = GridManager.Instance.GetOccupiedCellsByTeam(Team.Enemy);

        foreach (Vector2Int enemyCell in enemyCells)
        {
            List<Entity> enemiesInCell = GridManager.Instance.GetEntitiesAtCell(enemyCell)
                .Where(e => e.team == Team.Enemy)
                .ToList();

            if (enemiesInCell.Count == 0)
                continue;

            List<Vector2Int> playerCells = GridManager.Instance.GetOccupiedCellsByTeam(Team.Player);
            if (playerCells.Count == 0)
                yield break;

            Vector2Int? targetPlayerCell = GetVisibleNearestPlayerCell(enemyCell, playerCells);

            if (!targetPlayerCell.HasValue)
            {
                yield return new WaitForSeconds(delayBetweenEnemyGroups);
                continue;
            }

            Vector2Int playerCell = targetPlayerCell.Value;
            int distance = Manhattan(enemyCell, playerCell);

            if (distance == 1)
            {
                GridManager.Instance.TryMoveGroupOrAttack(enemiesInCell, playerCell);
            }
            else
            {
                Vector2Int step = GetStepTowards(enemyCell, playerCell);
                Vector2Int targetCell = enemyCell + step;
                GridManager.Instance.TryMoveGroupOrAttack(enemiesInCell, targetCell);
            }

            yield return new WaitForSeconds(delayBetweenEnemyGroups);
        }
    }

    public bool TryTriggerAdjacentCombat(List<Entity> defendingParty, Vector2Int partyCell)
    {
        if (!allowAdjacentCombatTrigger || GridManager.Instance == null || CombatTransitionManager.Instance == null)
            return false;

        List<Entity> validDefenders = defendingParty != null
            ? defendingParty.Where(entity => entity != null && !entity.IsDead && entity.team == Team.Player).ToList()
            : new List<Entity>();

        if (validDefenders.Count == 0)
            return false;

        List<Vector2Int> adjacentCells = GetAdjacentCells(partyCell);
        for (int i = 0; i < adjacentCells.Count; i++)
        {
            Vector2Int enemyCell = adjacentCells[i];
            List<Entity> enemiesInCell = GridManager.Instance.GetEntitiesAtCell(enemyCell)
                .Where(entity => entity != null && !entity.IsDead && entity.team == Team.Enemy)
                .ToList();

            if (enemiesInCell.Count == 0 || !ShouldTriggerCombat(enemiesInCell))
                continue;

            if (CombatTransitionManager.Instance.TryStartCombatTransition(
                enemiesInCell,
                validDefenders,
                enemyCell,
                partyCell,
                Team.Enemy))
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldTriggerCombat(List<Entity> enemiesInCell)
    {
        float highestChance = 0f;

        for (int i = 0; i < enemiesInCell.Count; i++)
        {
            Entity enemy = enemiesInCell[i];
            if (enemy == null || enemy.IsDead)
                continue;

            highestChance = Mathf.Max(highestChance, enemy.AdjacentCombatTriggerChance);
        }

        if (highestChance <= 0f)
            return false;

        return Random.value <= highestChance;
    }

    private List<Vector2Int> GetAdjacentCells(Vector2Int origin)
    {
        List<Vector2Int> result = new List<Vector2Int>
        {
            origin + Vector2Int.up,
            origin + Vector2Int.right,
            origin + Vector2Int.down,
            origin + Vector2Int.left
        };

        if (!useOnlyOrthogonalAdjacency)
        {
            result.Add(origin + new Vector2Int(1, 1));
            result.Add(origin + new Vector2Int(1, -1));
            result.Add(origin + new Vector2Int(-1, -1));
            result.Add(origin + new Vector2Int(-1, 1));
        }

        return result;
    }

    private Vector2Int? GetVisibleNearestPlayerCell(Vector2Int enemyCell, List<Vector2Int> playerCells)
    {
        Vector2Int? bestCell = null;
        int bestDistance = int.MaxValue;

        foreach (Vector2Int playerCell in playerCells)
        {
            int distance = Manhattan(enemyCell, playerCell);

            if (distance > maxVisionRange)
                continue;

            if (requireLineOfSight && !GridManager.Instance.HasLineOfSight(enemyCell, playerCell))
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCell = playerCell;
            }
        }

        return bestCell;
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int GetStepTowards(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        if (delta.y != 0)
            return new Vector2Int(0, delta.y > 0 ? 1 : -1);

        if (delta.x != 0)
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        return Vector2Int.zero;
    }
}
