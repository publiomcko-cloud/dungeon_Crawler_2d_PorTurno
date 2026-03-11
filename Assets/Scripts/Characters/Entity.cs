using System;
using System.Collections;
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

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Attack Animation")]
    public float attackLungeDistance = 0.22f;
    public float attackLungeDuration = 0.08f;
    public bool isAnimatingAttack = false;

    [Header("Damage Number")]
    public Transform damageNumberAnchor;

    public int CurrentHP { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    public event Action<int, Vector3> OnDamageTaken;

    private Vector3 targetWorldPosition;
    private bool targetInitialized = false;

    private void Start()
    {
        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        Vector2Int startCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );

        if (GridManager.Instance != null)
            GridManager.Instance.RegisterEntity(this, startCell);
    }

    private void Update()
    {
        if (!targetInitialized) return;
        if (isAnimatingAttack) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );
    }

    public void SetGridPosition(Vector2Int newGridPosition)
    {
        GridPosition = newGridPosition;
    }

    public void SetVisualTarget(Vector3 worldPosition, bool snapImmediately = false)
    {
        targetWorldPosition = worldPosition;
        targetInitialized = true;

        if (snapImmediately)
            transform.position = targetWorldPosition;
    }

    public Vector3 GetVisualTarget()
    {
        return targetWorldPosition;
    }

    public void PlayAttackLunge(Vector3 attackDirection)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(AttackLungeRoutine(attackDirection.normalized));
    }

    private IEnumerator AttackLungeRoutine(Vector3 attackDirection)
    {
        if (isAnimatingAttack) yield break;

        isAnimatingAttack = true;

        Vector3 basePosition = targetWorldPosition;
        Vector3 forwardPosition = basePosition + attackDirection * attackLungeDistance;

        float timer = 0f;
        while (timer < attackLungeDuration)
        {
            timer += Time.deltaTime;
            float t = attackLungeDuration > 0f ? timer / attackLungeDuration : 1f;
            transform.position = Vector3.Lerp(basePosition, forwardPosition, t);
            yield return null;
        }

        timer = 0f;
        while (timer < attackLungeDuration)
        {
            timer += Time.deltaTime;
            float t = attackLungeDuration > 0f ? timer / attackLungeDuration : 1f;
            transform.position = Vector3.Lerp(forwardPosition, basePosition, t);
            yield return null;
        }

        transform.position = basePosition;
        isAnimatingAttack = false;
    }

    public void ReceiveDamage(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        CurrentHP -= amount;

        if (CurrentHP < 0)
            CurrentHP = 0;

        Vector3 popupPosition = damageNumberAnchor != null
            ? damageNumberAnchor.position
            : transform.position + Vector3.up * 0.6f;

        OnDamageTaken?.Invoke(amount, popupPosition);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        CurrentHP += amount;

        if (CurrentHP > maxHP)
            CurrentHP = maxHP;

        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private void Die()
    {
        if (!IsDead)
            return;

        OnDied?.Invoke();

        if (GridManager.Instance != null)
            GridManager.Instance.RemoveEntity(this);

        Destroy(gameObject);
    }
}