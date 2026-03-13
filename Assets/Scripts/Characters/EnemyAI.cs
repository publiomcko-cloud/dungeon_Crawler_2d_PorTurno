using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Turn Timing")]
    public float delayBetweenEnemyGroups = 0.15f;

    [Header("Vision")]
    public int maxVisionRange = 6;
    public bool requireLineOfSight = true;

    public IEnumerator ExecuteEnemyTurn()
    {
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
