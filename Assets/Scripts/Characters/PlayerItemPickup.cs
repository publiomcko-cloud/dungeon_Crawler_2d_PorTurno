using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerItemPickup : MonoBehaviour
{
    [SerializeField] private Team pickerTeam = Team.Player;
    [SerializeField] private float pickupRadius = 0.25f;

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

            TryPickupAtEntityCell(picker);
        }
    }

    private void TryPickupAtEntityCell(Entity picker)
    {
        Vector2Int cell = picker.GridPosition;
        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, pickupRadius);

        if (hits == null || hits.Length == 0)
            return;

        List<GroundItem> items = new List<GroundItem>();

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem item = hits[i].GetComponent<GroundItem>();
            if (item != null)
                items.Add(item);
        }

        if (items.Count == 0)
            return;

        GroundItem firstItem = items.FirstOrDefault();
        if (firstItem == null)
            return;

        firstItem.TryAutoEquip(picker);
    }
}