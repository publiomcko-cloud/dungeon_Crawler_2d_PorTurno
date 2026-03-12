using UnityEngine;

public class PlayerItemPickup : MonoBehaviour
{
    [SerializeField] private Team pickerTeam = Team.Player;
    [SerializeField] private float detectionRadius = 0.25f;
    [SerializeField] private bool autoOpenLootWindowOnEnter = false;

    private void Update()
    {
        if (!autoOpenLootWindowOnEnter)
            return;

        if (GridManager.Instance == null || LootWindowUI.Instance == null)
            return;

        Entity picker = FindFirstAlivePicker();
        if (picker == null)
            return;

        if (CellHasGroundItems(picker.GridPosition))
        {
            PlayerInventory inventory = picker.GetComponent<PlayerInventory>();
            if (inventory != null && !LootWindowUI.Instance.IsOpen)
                LootWindowUI.Instance.OpenForCell(picker, inventory, picker.GridPosition);
        }
    }

    private Entity FindFirstAlivePicker()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i] != null && !entities[i].IsDead && entities[i].team == pickerTeam)
                return entities[i];
        }

        return null;
    }

    private bool CellHasGroundItems(Vector2Int cell)
    {
        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponent<GroundItem>() != null)
                return true;
        }

        return false;
    }
}