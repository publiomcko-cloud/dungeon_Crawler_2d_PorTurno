using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatTransitionManager : MonoBehaviour
{
    public static CombatTransitionManager Instance;

    [Header("Combat Scene")]
    [SerializeField] private bool interceptCellCombat = true;
    [SerializeField] private string combatSceneName = "CombatGrid";
    [SerializeField] private bool clearPreviousSessionOnAwake = true;
    [SerializeField] private bool logSessionDetails = true;

    public bool IsTransitionInProgress { get; private set; }
    public string CombatSceneName => combatSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (clearPreviousSessionOnAwake)
            CombatSessionData.ClearSession();
    }

    public bool TryStartCombatTransition(
        List<Entity> attackers,
        List<Entity> defenders,
        Vector2Int attackerCell,
        Vector2Int defenderCell,
        Team initiatingTeam)
    {
        if (!interceptCellCombat)
            return false;

        if (IsTransitionInProgress)
            return true;

        List<Entity> validAttackers = attackers
            .Where(entity => entity != null && !entity.IsDead)
            .ToList();

        List<Entity> validDefenders = defenders
            .Where(entity => entity != null && !entity.IsDead)
            .ToList();

        if (validAttackers.Count == 0 || validDefenders.Count == 0)
            return false;

        if (string.IsNullOrWhiteSpace(combatSceneName))
        {
            Debug.LogWarning("CombatTransitionManager: combatSceneName is empty.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(combatSceneName))
        {
            Debug.LogWarning($"CombatTransitionManager: scene '{combatSceneName}' is not in Build Settings.");
            return false;
        }

        CombatSessionData.CreateSession(
            combatSceneName,
            initiatingTeam,
            attackerCell,
            defenderCell,
            validAttackers,
            validDefenders);

        IsTransitionInProgress = true;

        if (logSessionDetails)
            LogCurrentSession();

        SceneManager.LoadScene(combatSceneName, LoadSceneMode.Single);
        return true;
    }

    private void LogCurrentSession()
    {
        CombatSessionData.CombatSessionSnapshot session = CombatSessionData.CurrentSession;
        if (session == null)
            return;

        string attackers = string.Join(", ", session.Attackers.Select(participant => participant.EntityName));
        string defenders = string.Join(", ", session.Defenders.Select(participant => participant.EntityName));

        Debug.Log(
            $"CombatTransitionManager: loading '{session.CombatSceneName}' from '{session.ExplorationSceneName}'. " +
            $"Attackers [{attackers}] at {session.AttackerCell} vs defenders [{defenders}] at {session.DefenderCell}.");
    }
}
