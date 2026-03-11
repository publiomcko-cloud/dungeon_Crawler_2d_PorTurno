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