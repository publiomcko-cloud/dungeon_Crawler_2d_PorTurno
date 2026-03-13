using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatSessionData
{
    public sealed class CombatParticipantSnapshot
    {
        public string EntityName { get; private set; }
        public Team Team { get; private set; }
        public Vector2Int ExplorationCell { get; private set; }
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }
        public int ActionPoints { get; private set; }
        public int Level { get; private set; }

        public CombatParticipantSnapshot(Entity entity)
        {
            if (entity == null)
            {
                EntityName = "Missing";
                Team = Team.Player;
                ExplorationCell = Vector2Int.zero;
                CurrentHP = 0;
                MaxHP = 0;
                Attack = 0;
                Defense = 0;
                ActionPoints = 0;
                Level = 1;
                return;
            }

            EntityName = entity.name;
            Team = entity.team;
            ExplorationCell = entity.GridPosition;
            CurrentHP = entity.CurrentHP;
            MaxHP = entity.maxHP;
            Attack = entity.attackDamage;
            Defense = entity.defense;
            ActionPoints = entity.actionPoints;
            Level = entity.Level;
        }
    }

    public sealed class CombatSessionSnapshot
    {
        public string ExplorationSceneName { get; private set; }
        public string CombatSceneName { get; private set; }
        public Team InitiatingTeam { get; private set; }
        public Vector2Int AttackerCell { get; private set; }
        public Vector2Int DefenderCell { get; private set; }
        public IReadOnlyList<CombatParticipantSnapshot> Attackers { get; private set; }
        public IReadOnlyList<CombatParticipantSnapshot> Defenders { get; private set; }

        public CombatSessionSnapshot(
            string explorationSceneName,
            string combatSceneName,
            Team initiatingTeam,
            Vector2Int attackerCell,
            Vector2Int defenderCell,
            List<CombatParticipantSnapshot> attackers,
            List<CombatParticipantSnapshot> defenders)
        {
            ExplorationSceneName = explorationSceneName;
            CombatSceneName = combatSceneName;
            InitiatingTeam = initiatingTeam;
            AttackerCell = attackerCell;
            DefenderCell = defenderCell;
            Attackers = attackers;
            Defenders = defenders;
        }
    }

    public static CombatSessionSnapshot CurrentSession { get; private set; }
    public static bool HasActiveSession => CurrentSession != null;

    public static CombatSessionSnapshot CreateSession(
        string combatSceneName,
        Team initiatingTeam,
        Vector2Int attackerCell,
        Vector2Int defenderCell,
        List<Entity> attackers,
        List<Entity> defenders)
    {
        string explorationSceneName = SceneManager.GetActiveScene().name;

        List<CombatParticipantSnapshot> attackerSnapshots = CreateSnapshots(attackers);
        List<CombatParticipantSnapshot> defenderSnapshots = CreateSnapshots(defenders);

        CurrentSession = new CombatSessionSnapshot(
            explorationSceneName,
            combatSceneName,
            initiatingTeam,
            attackerCell,
            defenderCell,
            attackerSnapshots,
            defenderSnapshots);

        return CurrentSession;
    }

    public static void ClearSession()
    {
        CurrentSession = null;
    }

    private static List<CombatParticipantSnapshot> CreateSnapshots(List<Entity> entities)
    {
        if (entities == null)
            return new List<CombatParticipantSnapshot>();

        return entities
            .Where(entity => entity != null && !entity.IsDead)
            .OrderBy(entity => entity.name)
            .Select(entity => new CombatParticipantSnapshot(entity))
            .ToList();
    }
}
