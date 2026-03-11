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