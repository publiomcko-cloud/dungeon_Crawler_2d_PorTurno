using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private Entity entity;
    private Entity player;

    void Awake()
    {
        entity = GetComponent<Entity>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.GetComponent<Entity>();
        }
    }

    public void TakeTurn()
    {
        if (entity == null || player == null)
            return;

        if (entity.isDead)
            return;

        Vector2Int direction =
            player.gridPosition - entity.gridPosition;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            direction = new Vector2Int(
                (int)Mathf.Sign(direction.x),
                0
            );
        }
        else
        {
            direction = new Vector2Int(
                0,
                (int)Mathf.Sign(direction.y)
            );
        }

        Vector2Int target = entity.gridPosition + direction;

        if (target == player.gridPosition)
        {
            entity.Attack(player);
            return;
        }

        if (GridManager.Instance.IsCellOccupied(target))
            return;

        entity.MoveTo(target);
    }
}