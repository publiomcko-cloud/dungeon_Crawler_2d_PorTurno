using System.Collections.Generic;
using UnityEngine;

public class PlayerItemPickup : MonoBehaviour
{
    [SerializeField] private Team pickerTeam = Team.Player;
    [SerializeField] private float pickupRadius = 0.25f;

    private readonly Dictionary<Entity, Vector2Int> lastKnownCells = new Dictionary<Entity, Vector2Int>();

    private void Update()
    {
        if (GridManager.Instance == null)
            return;

        List<Entity> pickers = GridManager.Instance.GetEntitiesByTeam(pickerTeam);

        for (int i = 0; i < pickers.Count; i++)
        {
            Entity picker = pickers[i];

            if (picker == null || picker.IsDead)
                continue;

            Vector2Int currentCell = picker.GridPosition;

            if (!lastKnownCells.ContainsKey(picker))
            {
                lastKnownCells[picker] = currentCell;
                continue;
            }

            if (lastKnownCells[picker] != currentCell)
            {
                lastKnownCells[picker] = currentCell;
                TryPickupAtEntityCell(picker);
            }
        }

        CleanupDeadEntities();
    }

    private void TryPickupAtEntityCell(Entity picker)
    {
        Vector2Int cell = picker.GridPosition;
        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, pickupRadius);

        if (hits == null || hits.Length == 0)
            return;

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem item = hits[i].GetComponent<GroundItem>();

            if (item == null)
                continue;

            bool equipped = item.TryAutoEquip(picker);

            if (equipped)
            {
                Debug.Log($"[PlayerItemPickup] {picker.name} picked and equipped item at cell {cell}.", picker);
                break;
            }
        }
    }

    private void CleanupDeadEntities()
    {
        List<Entity> toRemove = new List<Entity>();

        foreach (var pair in lastKnownCells)
        {
            if (pair.Key == null || pair.Key.IsDead)
                toRemove.Add(pair.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
            lastKnownCells.Remove(toRemove[i]);
    }
}