using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [SerializeField] private EnemyAI enemyAI;

    public bool IsPlayerTurn { get; private set; } = true;

    private bool enemyTurnRunning;

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
        if (enemyTurnRunning)
            return;

        IsPlayerTurn = false;
        StartCoroutine(EnemyTurnRoutine());
    }

    public bool TryTriggerAdjacentEnemyCombat(List<Entity> defendingParty, Vector2Int partyCell)
    {
        if (enemyAI == null)
            enemyAI = FindFirstObjectByType<EnemyAI>();

        return enemyAI != null && enemyAI.TryTriggerAdjacentCombat(defendingParty, partyCell);
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
