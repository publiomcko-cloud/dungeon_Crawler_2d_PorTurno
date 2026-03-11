using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [SerializeField] private EnemyAI enemyAI;

    public bool IsPlayerTurn { get; private set; } = true;
    private bool enemyTurnRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartEnemyTurn()
    {
        if (enemyTurnRunning) return;

        IsPlayerTurn = false;
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        enemyTurnRunning = true;

        if (enemyAI != null)
            yield return StartCoroutine(enemyAI.ExecuteEnemyTurn());

        enemyTurnRunning = false;
        IsPlayerTurn = true;
    }
}