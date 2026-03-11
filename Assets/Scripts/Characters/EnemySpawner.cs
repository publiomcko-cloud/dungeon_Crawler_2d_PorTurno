using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemyCount = 5;

    public int mapWidth = 10;
    public int mapHeight = 10;

    void Start()
    {
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2Int pos = GetRandomFreeCell();

            Vector3 worldPos = new Vector3(
                pos.x + 0.5f,
                pos.y + 0.5f,
                0
            );

            Instantiate(enemyPrefab, worldPos, Quaternion.identity);
        }
    }

    Vector2Int GetRandomFreeCell()
    {
        for (int i = 0; i < 100; i++)
        {
            Vector2Int pos = new Vector2Int(
                Random.Range(-mapWidth, mapWidth),
                Random.Range(-mapHeight, mapHeight)
            );

            if (!GridManager.Instance.IsCellOccupied(pos))
                return pos;
        }

        return Vector2Int.zero;
    }
}