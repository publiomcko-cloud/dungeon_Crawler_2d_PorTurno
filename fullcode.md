# Dungeon Crawler 2D Turn-Based Game Code

This document contains all the C# scripts for the Unity project, organized by file. This is a 2D dungeon crawler game with turn-based movement, where the player moves on a grid, and enemies take turns after the player.


## GridManager.cs

```csharp

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
```


## GridDebug.cs

```csharp

using UnityEngine;

public class GridDebug : MonoBehaviour
{
    public int gridWidth = 20;
    public int gridHeight = 20;

    public float cellSize = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        for (int x = -gridWidth; x < gridWidth; x++)
        {
            for (int y = -gridHeight; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(
                    x + 0.5f,
                    y + 0.5f,
                    0
                );

                Gizmos.DrawSphere(pos, 0.05f);
            }
        }
    }
}
```


## TurnManager.cs

```csharp

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn
    }

    public TurnState currentState;

    private List<Entity> enemies = new List<Entity>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentState = TurnState.PlayerTurn;

        RegisterEnemies();
    }

    void RegisterEnemies()
    {
        enemies.Clear();

        EnemyAI[] foundEnemies = FindObjectsOfType<EnemyAI>();

        foreach (EnemyAI enemy in foundEnemies)
        {
            Entity e = enemy.GetComponent<Entity>();

            if (e != null)
                enemies.Add(e);
        }
    }

    public bool IsPlayerTurn()
    {
        return currentState == TurnState.PlayerTurn;
    }

    public void EndPlayerTurn()
    {
        currentState = TurnState.EnemyTurn;

        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        // Remove inimigos destruídos antes de iniciar turno
        enemies.RemoveAll(enemy => enemy == null);

        foreach (Entity enemy in enemies)
        {
            if (enemy == null)
                continue;

            EnemyAI ai = enemy.GetComponent<EnemyAI>();

            if (ai != null)
            {
                ai.TakeTurn();
            }

            yield return new WaitForSeconds(0.2f);
        }

        currentState = TurnState.PlayerTurn;
    }
}
```


## PlayerGridMovement.cs

```csharp

using UnityEngine;
using System.Collections;

public class PlayerGridMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Landing Effect")]
    public float bounceHeight = 0.08f;
    public float bounceDuration = 0.08f;

    private Entity entity;

    private bool isMoving = false;

    private Vector3 startWorldPosition;
    private Vector3 targetWorldPosition;

    private float moveProgress = 0f;

    private Vector2Int lastDirection = Vector2Int.down;

    void Start()
    {
        entity = GetComponent<Entity>();
        targetWorldPosition = transform.position;
    }

    void Update()
    {
        if (!TurnManager.Instance.IsPlayerTurn())
            return;

        if (isMoving)
        {
            AnimateMovement();
            return;
        }

        HandleInput();
    }

    void HandleInput()
    {
        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;

        if (Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;

        if (direction != Vector2Int.zero)
        {
            TryMove(direction);
        }
    }

    void TryMove(Vector2Int direction)
    {
        Vector2Int targetGrid = entity.gridPosition + direction;

        Entity targetEntity = GridManager.Instance.GetEntityAt(targetGrid);

        // EXISTE ALGO NA CÉLULA
        if (targetEntity != null)
        {
            // SE FOR INIMIGO → ATACA
            EnemyAI enemy = targetEntity.GetComponent<EnemyAI>();

            if (enemy != null)
            {
                entity.Attack(targetEntity);
                TurnManager.Instance.EndPlayerTurn();
                return;
            }

            // Se não for inimigo, não move
            return;
        }

        lastDirection = direction;

        startWorldPosition = transform.position;

        entity.MoveTo(targetGrid);

        targetWorldPosition = transform.position;

        transform.position = startWorldPosition;

        moveProgress = 0f;
        isMoving = true;

        UpdateAnimator(direction);

        if (animator != null)
            animator.SetBool("Moving", true);
    }

    void AnimateMovement()
    {
        moveProgress += Time.deltaTime * moveSpeed;

        transform.position = Vector3.Lerp(
            startWorldPosition,
            targetWorldPosition,
            moveProgress
        );

        if (moveProgress >= 1f)
        {
            transform.position = targetWorldPosition;
            isMoving = false;

            if (animator != null)
            {
                animator.SetBool("Moving", false);
                animator.SetTrigger("Idle");
            }

            StartCoroutine(BounceEffect());

            TurnManager.Instance.EndPlayerTurn();
        }
    }

    void UpdateAnimator(Vector2Int direction)
    {
        if (animator == null)
            return;

        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);

        if (spriteRenderer != null)
        {
            if (direction.x != 0)
                spriteRenderer.flipX = direction.x < 0;
        }
    }

    IEnumerator BounceEffect()
    {
        Vector3 original = transform.position;

        Vector3 up = original + Vector3.up * bounceHeight;

        float t = 0f;

        while (t < bounceDuration)
        {
            transform.position = Vector3.Lerp(original, up, t / bounceDuration);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;

        while (t < bounceDuration)
        {
            transform.position = Vector3.Lerp(up, original, t / bounceDuration);
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = original;
    }
}
```


## HealthBar.cs

```csharp

using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Entity target;
    public Image fillImage;

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        float hpPercent = (float)target.currentHP / target.maxHP;

        fillImage.fillAmount = hpPercent;

        transform.position = target.transform.position + Vector3.up * 0.8f;
    }
}
```


## Entity.cs

```csharp

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
```


## EnemyAI.cs

```csharp

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
```


## DamageNumberSpawner.cs

```csharp

using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance;

    public GameObject damageNumberPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnDamageNumber(int damage, Vector3 position)
    {
        GameObject obj = Instantiate(damageNumberPrefab, position, Quaternion.identity);

        DamageNumber number = obj.GetComponent<DamageNumber>();

        if (number != null)
        {
            number.Setup(damage);
        }
    }
}
```


## DamageNumber.cs

```csharp

using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float lifetime = 1f;

    private TextMeshProUGUI textMesh;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(int damage)
    {
        textMesh.text = "-" + damage.ToString();
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
```


## DamageFlash.cs

```csharp

using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    private SpriteRenderer sprite;

    public float flashTime = 0.12f;
    public Color flashColor = Color.red;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();

        if (sprite == null)
        {
            Debug.LogWarning("DamageFlash: SpriteRenderer não encontrado em " + gameObject.name);
        }
    }

    public void Flash()
    {
        if (sprite != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    IEnumerator FlashRoutine()
    {
        Color original = sprite.color;

        sprite.color = flashColor;

        yield return new WaitForSeconds(flashTime);

        sprite.color = original;
    }
}
```


## AttackAnimation.cs

```csharp

using UnityEngine;
using System.Collections;

public class AttackAnimation : MonoBehaviour
{
    public float lungeDistance = 0.2f;
    public float lungeSpeed = 8f;

    public IEnumerator PlayAttack(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        Vector3 direction = (targetPosition - startPosition).normalized;

        Vector3 attackPosition = startPosition + direction * lungeDistance;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * lungeSpeed;
            transform.position = Vector3.Lerp(startPosition, attackPosition, t);
            yield return null;
        }

        t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * lungeSpeed;
            transform.position = Vector3.Lerp(attackPosition, startPosition, t);
            yield return null;
        }
    }
}
```


