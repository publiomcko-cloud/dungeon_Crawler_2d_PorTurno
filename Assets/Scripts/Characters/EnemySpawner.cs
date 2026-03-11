using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;

    [Header("Spawn Area")]
    public int minX = -8;
    public int maxX = 8;
    public int minY = -8;
    public int maxY = 8;

    [Header("Enemy Groups")]
    public int numberOfEnemyGroups = 4;
    public int minEnemiesPerGroup = 1;
    public int maxEnemiesPerGroup = 4;

    [Header("Block Player Start Cell")]
    public bool avoidPlayerCell = true;

    private void Start()
    {
        SpawnEnemyGroups();
    }

    private void SpawnEnemyGroups()
    {
        Vector2Int playerCell = Vector2Int.zero;
        List<Entity> players = GridManager.Instance.GetEntitiesByTeam(Team.Player);

        if (players.Count > 0)
            playerCell = players[0].GridPosition;

        for (int g = 0; g < numberOfEnemyGroups; g++)
        {
            Vector2Int spawnCell = GetFreeEnemyCell(playerCell);
            int count = Random.Range(minEnemiesPerGroup, maxEnemiesPerGroup + 1);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GridManager.Instance.GetCellCenterWorld(spawnCell);
                Instantiate(enemyPrefab, pos, Quaternion.identity);
            }
        }
    }

    private Vector2Int GetFreeEnemyCell(Vector2Int playerCell)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            Vector2Int cell = new Vector2Int(
                Random.Range(minX, maxX + 1),
                Random.Range(minY, maxY + 1)
            );

            if (avoidPlayerCell && cell == playerCell)
                continue;

            List<Entity> entities = GridManager.Instance.GetEntitiesAtCell(cell);

            bool hasPlayer = false;
            foreach (Entity e in entities)
            {
                if (e.team == Team.Player)
                {
                    hasPlayer = true;
                    break;
                }
            }

            if (hasPlayer)
                continue;

            if (entities.Count < GridManager.Instance.maxEntitiesPerCell)
                return cell;
        }

        return new Vector2Int(maxX, maxY);
    }
}