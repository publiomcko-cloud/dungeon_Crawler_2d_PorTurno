using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    private Dictionary<Vector2Int, Entity> grid =
        new Dictionary<Vector2Int, Entity>();

    void Awake()
    {
        Instance = this;
    }

    public bool IsCellOccupied(Vector2Int pos)
    {
        return grid.ContainsKey(pos);
    }

    public Entity GetEntityAt(Vector2Int pos)
    {
        if (grid.ContainsKey(pos))
            return grid[pos];

        return null;
    }

    public void RegisterEntity(Vector2Int pos, Entity entity)
    {
        grid[pos] = entity;
    }

    public void MoveEntity(Vector2Int oldPos, Vector2Int newPos, Entity entity)
    {
        grid.Remove(oldPos);
        grid[newPos] = entity;
    }

    public void RemoveEntity(Vector2Int pos)
    {
        grid.Remove(pos);
    }
}