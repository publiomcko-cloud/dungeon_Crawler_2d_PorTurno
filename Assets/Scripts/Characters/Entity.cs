using UnityEngine;

public enum Team
{
    Player,
    Enemy
}

public class Entity : MonoBehaviour
{
    [Header("Identity")]
    public Team team = Team.Player;

    [Header("Stats")]
    public int maxHP = 10;
    public int attackDamage = 3;

    public int CurrentHP { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private void Start()
    {
        CurrentHP = maxHP;

        Vector2Int startCell = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );

        GridManager.Instance.RegisterEntity(this, startCell);
    }

    public void SetGridPosition(Vector2Int newGridPosition)
    {
        GridPosition = newGridPosition;
    }

    public void ReceiveDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP -= amount;

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Die();
        }
    }

    private void Die()
    {
        GridManager.Instance.RemoveEntity(this);
        Destroy(gameObject);
    }
}