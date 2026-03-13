using UnityEngine;

public class PlayerItemPickup : MonoBehaviour
{
    [SerializeField] private Team pickerTeam = Team.Player;
    [SerializeField] private float detectionRadius = 0.25f;
    [SerializeField] private bool autoOpenLootWindowOnEnter = false;

    private Vector2Int? lastOpenedCell;
    private Vector2Int? lastObservedCell;

    private void Update()
    {
        if (!autoOpenLootWindowOnEnter)
            return;

        if (GridManager.Instance == null || LootWindowUI.Instance == null)
            return;

        Entity picker = FindFirstAlivePicker();
        if (picker == null)
            return;

        Vector2Int currentCell = picker.GridPosition;

        if (!lastObservedCell.HasValue || lastObservedCell.Value != currentCell)
        {
            lastObservedCell = currentCell;

            bool hasGroundItems = CellHasGroundItems(currentCell);

            if (hasGroundItems)
            {
                if (!lastOpenedCell.HasValue || lastOpenedCell.Value != currentCell)
                {
                    LootWindowUI.Instance.OpenForCell(picker, currentCell);
                    lastOpenedCell = currentCell;
                }
            }
            else
            {
                if (lastOpenedCell.HasValue && lastOpenedCell.Value == currentCell)
                    lastOpenedCell = null;
            }
        }

        if (!CellHasGroundItems(currentCell))
        {
            if (lastOpenedCell.HasValue && lastOpenedCell.Value == currentCell)
                lastOpenedCell = null;
        }
    }

    private Entity FindFirstAlivePicker()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            if (entity == null)
                continue;

            if (entity.team != pickerTeam)
                continue;

            if (entity.IsDead)
                continue;

            return entity;
        }

        return null;
    }

    private bool CellHasGroundItems(Vector2Int cell)
    {
        if (GridManager.Instance == null)
            return false;

        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);

        if (hits == null || hits.Length == 0)
            return false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            GroundItem groundItem = hits[i].GetComponent<GroundItem>();
            if (groundItem != null)
                return true;
        }

        return false;
    }
}