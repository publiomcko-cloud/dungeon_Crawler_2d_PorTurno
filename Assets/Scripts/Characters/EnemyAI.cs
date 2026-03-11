using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float delayBetweenEnemyGroups = 0.15f;

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

            Vector2Int nearestPlayerCell = GetNearestCell(enemyCell, playerCells);
            int distance = Manhattan(enemyCell, nearestPlayerCell);

            if (distance == 1)
            {
                GridManager.Instance.ResolveCellAttack(enemyCell, nearestPlayerCell, Team.Enemy);
            }
            else
            {
                Vector2Int step = GetStepTowards(enemyCell, nearestPlayerCell);
                Vector2Int targetCell = enemyCell + step;

                GridManager.Instance.TryMoveGroupOrAttack(enemiesInCell, targetCell);
            }

            yield return new WaitForSeconds(delayBetweenEnemyGroups);
        }
    }

    private Vector2Int GetNearestCell(Vector2Int origin, List<Vector2Int> cells)
    {
        Vector2Int best = cells[0];
        int bestDistance = Manhattan(origin, best);

        for (int i = 1; i < cells.Count; i++)
        {
            int dist = Manhattan(origin, cells[i]);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                best = cells[i];
            }
        }

        return best;
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