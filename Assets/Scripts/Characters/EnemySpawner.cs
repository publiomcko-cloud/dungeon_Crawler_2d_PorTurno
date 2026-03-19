using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class EnemySpawnAreaDefinition
{
    public string areaName = "Spawn Area";
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public int minX = -8;
    public int maxX = 8;
    public int minY = -8;
    public int maxY = 8;
    public int numberOfEnemyGroups = 2;
    public int minEnemiesPerGroup = 1;
    public int maxEnemiesPerGroup = 3;
    public bool avoidPlayerCell = true;
    public Color gizmoColor = new Color(1f, 0.35f, 0.15f, 0.85f);
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject fallbackEnemyPrefab;

    [Header("Spawn Areas")]
    [SerializeField] private List<EnemySpawnAreaDefinition> spawnAreas = new List<EnemySpawnAreaDefinition>();

    [Header("Debug")]
    [SerializeField] private bool drawSpawnAreaGizmos = true;

    public GameObject enemyPrefab => fallbackEnemyPrefab;
    public GameObject FallbackEnemyPrefab => fallbackEnemyPrefab;

    private void Start()
    {
        if (CombatExplorationReturnData.HasPendingReturn)
            return;

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (ExplorationScenePersistenceData.HasPendingTransitionToScene(activeSceneName) &&
            ExplorationScenePersistenceData.HasSavedSceneState(activeSceneName))
        {
            return;
        }

        SpawnEnemyGroups();
    }

    public GameObject ResolveEnemyPrefab(string enemyPrefabId)
    {
        if (!string.IsNullOrWhiteSpace(enemyPrefabId))
        {
            for (int i = 0; i < spawnAreas.Count; i++)
            {
                EnemySpawnAreaDefinition area = spawnAreas[i];
                if (area == null || area.enemyPrefabs == null)
                    continue;

                for (int j = 0; j < area.enemyPrefabs.Count; j++)
                {
                    GameObject prefab = area.enemyPrefabs[j];
                    if (prefab == null)
                        continue;

                    if (string.Equals(prefab.name, enemyPrefabId, StringComparison.OrdinalIgnoreCase))
                        return prefab;
                }
            }
        }

        return fallbackEnemyPrefab;
    }

    public void ApplySpawnMetadata(Entity enemyEntity, GameObject sourcePrefab)
    {
        if (enemyEntity == null)
            return;

        enemyEntity.team = Team.Enemy;
        enemyEntity.SetEnemyPrefabId(sourcePrefab != null ? sourcePrefab.name : enemyEntity.name);
    }

    private void SpawnEnemyGroups()
    {
        if (GridManager.Instance == null)
            return;

        Vector2Int playerCell = Vector2Int.zero;
        List<Entity> players = GridManager.Instance.GetEntitiesByTeam(Team.Player);
        if (players.Count > 0)
            playerCell = players[0].GridPosition;

        for (int areaIndex = 0; areaIndex < spawnAreas.Count; areaIndex++)
        {
            EnemySpawnAreaDefinition area = spawnAreas[areaIndex];
            if (area == null)
                continue;

            int groups = Mathf.Max(0, area.numberOfEnemyGroups);
            for (int groupIndex = 0; groupIndex < groups; groupIndex++)
            {
                Vector2Int spawnCell = GetFreeEnemyCell(area, playerCell);
                int enemyCount = UnityEngine.Random.Range(
                    Mathf.Max(1, area.minEnemiesPerGroup),
                    Mathf.Max(Mathf.Max(1, area.minEnemiesPerGroup), area.maxEnemiesPerGroup) + 1);

                for (int enemyIndex = 0; enemyIndex < enemyCount; enemyIndex++)
                {
                    GameObject prefab = ResolveAreaEnemyPrefab(area);
                    if (prefab == null)
                        continue;

                    Vector3 spawnPosition = GridManager.Instance.GetCellCenterWorld(spawnCell);
                    GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
                    Entity entity = instance.GetComponent<Entity>();
                    if (entity != null)
                        ApplySpawnMetadata(entity, prefab);
                }
            }
        }
    }

    private GameObject ResolveAreaEnemyPrefab(EnemySpawnAreaDefinition area)
    {
        if (area != null && area.enemyPrefabs != null)
        {
            List<GameObject> validPrefabs = new List<GameObject>();
            for (int i = 0; i < area.enemyPrefabs.Count; i++)
            {
                if (area.enemyPrefabs[i] != null)
                    validPrefabs.Add(area.enemyPrefabs[i]);
            }

            if (validPrefabs.Count > 0)
                return validPrefabs[UnityEngine.Random.Range(0, validPrefabs.Count)];
        }

        return fallbackEnemyPrefab;
    }

    private Vector2Int GetFreeEnemyCell(EnemySpawnAreaDefinition area, Vector2Int playerCell)
    {
        int fallbackX = area != null ? area.maxX : 0;
        int fallbackY = area != null ? area.maxY : 0;

        for (int tries = 0; tries < 200; tries++)
        {
            Vector2Int cell = new Vector2Int(
                UnityEngine.Random.Range(area.minX, area.maxX + 1),
                UnityEngine.Random.Range(area.minY, area.maxY + 1));

            if (area.avoidPlayerCell && cell == playerCell)
                continue;

            List<Entity> entities = GridManager.Instance.GetEntitiesAtCell(cell);
            bool hasPlayer = false;
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i] != null && entities[i].team == Team.Player)
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

        return new Vector2Int(fallbackX, fallbackY);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnAreaGizmos || spawnAreas == null)
            return;

        for (int i = 0; i < spawnAreas.Count; i++)
        {
            EnemySpawnAreaDefinition area = spawnAreas[i];
            if (area == null)
                continue;

            float width = (area.maxX - area.minX) + 1f;
            float height = (area.maxY - area.minY) + 1f;
            Vector3 center = new Vector3(
                (area.minX + area.maxX + 1f) * 0.5f,
                (area.minY + area.maxY + 1f) * 0.5f,
                0f);

            Gizmos.color = area.gizmoColor;
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
