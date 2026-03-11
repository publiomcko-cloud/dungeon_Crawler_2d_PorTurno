# Dungeon Crawler 2D Turn-Based Game Code

This document contains all the C# scripts for the Unity project, organized by file. This is a 2D dungeon crawler game with turn-based movement, where the player moves on a grid, and enemies take turns after the player.

## Entity.cs

```csharp
using UnityEngine;

public class Entity : MonoBehaviour
{
    public Vector2Int gridPosition;

    public int maxHP = 10;
    public int currentHP;

    public int attack = 2;
    public int defense = 1;

    void Start()
    {
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

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - defense, 1);

        currentHP -= finalDamage;

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        GridManager.Instance.RemoveEntity(gridPosition);

        Destroy(gameObject);
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

        if (GridManager.Instance.IsCellOccupied(targetGrid))
            return;

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
        if (player == null || entity == null)
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

        // NÃO deixa entrar na célula do player
        if (target == player.gridPosition)
        {
            Debug.Log("Enemy atacaria o player aqui");
            return;
        }

        // verifica ocupação do grid
        if (GridManager.Instance.IsCellOccupied(target))
            return;

        entity.MoveTo(target);
    }
}
```

## GridManager.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    private Dictionary<Vector2Int, Entity> grid =
        new Dictionary<Vector2Int, Entity>();

    void Awake()
    {
        Instance = this;
    }

    public bool IsCellOccupied(Vector2Int pos)
    {
        return grid.ContainsKey(pos);
    }

    public Entity GetEntityAt(Vector2Int pos)
    {
        if (grid.ContainsKey(pos))
            return grid[pos];

        return null;
    }

    public void RegisterEntity(Vector2Int pos, Entity entity)
    {
        grid[pos] = entity;
    }

    public void MoveEntity(Vector2Int oldPos, Vector2Int newPos, Entity entity)
    {
        grid.Remove(oldPos);
        grid[newPos] = entity;
    }

    public void RemoveEntity(Vector2Int pos)
    {
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
        EnemyAI[] foundEnemies = FindObjectsOfType<EnemyAI>();

        foreach (EnemyAI enemy in foundEnemies)
        {
            enemies.Add(enemy.GetComponent<Entity>());
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

    System.Collections.IEnumerator EnemyTurn()
    {
        foreach (Entity enemy in enemies)
        {
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