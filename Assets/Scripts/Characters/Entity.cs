using UnityEngine;
using System.Collections;

public class Entity : MonoBehaviour
{
    public Vector2Int gridPosition;

    [Header("Stats")]
    public int maxHP = 10;
    public int currentHP;

    public int attack = 2;
    public int defense = 1;

    public bool isDead = false;

    private DamageFlash flash;
    private AttackAnimation attackAnim;

    void Start()
    {
        flash = GetComponent<DamageFlash>();
        attackAnim = GetComponent<AttackAnimation>();

        currentHP = maxHP;

        gridPosition = WorldToGrid(transform.position);

        GridManager.Instance.RegisterEntity(gridPosition, this);

        UpdateWorldPosition();
    }

    Vector2Int WorldToGrid(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y)
        );
    }

    public void MoveTo(Vector2Int newPos)
    {
        if (isDead)
            return;

        GridManager.Instance.MoveEntity(gridPosition, newPos, this);

        gridPosition = newPos;

        UpdateWorldPosition();
    }

    void UpdateWorldPosition()
    {
        transform.position = new Vector3(
            gridPosition.x + 0.5f,
            gridPosition.y + 0.5f,
            0
        );
    }

    public void Attack(Entity target)
    {
        if (isDead)
            return;

        if (target == null)
            return;

        StartCoroutine(AttackRoutine(target));
    }

    IEnumerator AttackRoutine(Entity target)
    {
        if (attackAnim != null)
        {
            yield return StartCoroutine(
                attackAnim.PlayAttack(target.transform.position)
            );
        }

        if (target != null)
        {
            target.TakeDamage(attack);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        int finalDamage = Mathf.Max(damage - defense, 1);

        currentHP -= finalDamage;

        Debug.Log(gameObject.name + " took " + finalDamage + " damage. HP: " + currentHP);

        if (flash != null)
            flash.Flash();

        if (DamageNumberSpawner.Instance != null)
        {
            DamageNumberSpawner.Instance.SpawnDamageNumber(
                finalDamage,
                transform.position + Vector3.up * 0.6f
            );
        }

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GridManager.Instance.RemoveEntity(gridPosition);

        Debug.Log(gameObject.name + " died");

        Destroy(gameObject);
    }
}