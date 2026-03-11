using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    // agora cada célula contém uma LISTA de entidades
    private Dictionary<Vector2Int, List<Entity>> grid =
        new Dictionary<Vector2Int, List<Entity>>();

    void Awake()
    {
        Instance = this;
    }

    public bool IsCellOccupied(Vector2Int pos)
    {
        if (!grid.ContainsKey(pos))
            return false;

        return grid[pos].Count > 0;
    }

    // mantém compatibilidade com o sistema antigo
    public Entity GetEntityAt(Vector2Int pos)
    {
        if (!grid.ContainsKey(pos))
            return null;

        if (grid[pos].Count == 0)
            return null;

        return grid[pos][0];
    }

    // NOVO: retorna todas entidades da célula
    public List<Entity> GetEntitiesAt(Vector2Int pos)
    {
        if (!grid.ContainsKey(pos))
            return new List<Entity>();

        return grid[pos];
    }

    public void RegisterEntity(Vector2Int pos, Entity entity)
    {
        if (!grid.ContainsKey(pos))
        {
            grid[pos] = new List<Entity>();
        }

        grid[pos].Add(entity);
    }

    public void MoveEntity(Vector2Int oldPos, Vector2Int newPos, Entity entity)
    {
        if (grid.ContainsKey(oldPos))
        {
            grid[oldPos].Remove(entity);

            if (grid[oldPos].Count == 0)
                grid.Remove(oldPos);
        }

        if (!grid.ContainsKey(newPos))
        {
            grid[newPos] = new List<Entity>();
        }

        grid[newPos].Add(entity);
    }

    public void RemoveEntity(Vector2Int pos)
    {
        if (!grid.ContainsKey(pos))
            return;

        // remove todas entidades destruídas da lista
        grid[pos].RemoveAll(e => e == null);

        if (grid[pos].Count == 0)
            grid.Remove(pos);
    }
}