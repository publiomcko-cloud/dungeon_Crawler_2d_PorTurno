using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    private void Update()
    {
        if (TurnManager.Instance == null) return;
        if (!TurnManager.Instance.IsPlayerTurn) return;

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;

        if (direction == Vector2Int.zero)
            return;

        TryMoveParty(direction);
    }

    private void TryMoveParty(Vector2Int direction)
    {
        if (GridManager.Instance == null) return;

        List<Entity> party = GridManager.Instance.GetEntitiesByTeam(Team.Player);

        if (party.Count == 0)
            return;

        Entity leader = PartyAnchorService.Instance != null ? PartyAnchorService.Instance.GetLeader() : null;
        Vector2Int sourceCell = leader != null ? leader.GridPosition : party[0].GridPosition;

        party = party
            .Where(e => e != null && !e.IsDead && e.GridPosition == sourceCell)
            .ToList();

        if (party.Count == 0)
            return;

        Vector2Int targetCell = sourceCell + direction;

        bool actionDone = GridManager.Instance.TryMoveGroupOrAttack(party, targetCell);

        bool isTransitioningToCombat = CombatTransitionManager.Instance != null &&
            CombatTransitionManager.Instance.IsTransitionInProgress;

        if (actionDone && !isTransitioningToCombat)
            TurnManager.Instance.StartEnemyTurn();
    }
}
