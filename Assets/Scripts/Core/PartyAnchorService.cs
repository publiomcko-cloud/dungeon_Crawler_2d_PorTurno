using System.Collections.Generic;
using UnityEngine;

public class PartyAnchorService : MonoBehaviour
{
    public static PartyAnchorService Instance;

    [Header("Team")]
    [SerializeField] private Team controlledTeam = Team.Player;

    [Header("Leader")]
    [SerializeField] private Entity explicitLeader;
    [SerializeField] private bool fallbackToFirstAlive = true;

    public Entity CurrentLeader { get; private set; }

    public event System.Action<Entity> OnLeaderChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshLeader();
    }

    private void Update()
    {
        if (CurrentLeader == null || CurrentLeader.IsDead)
            RefreshLeader();
    }

    public Entity GetLeader()
    {
        if (CurrentLeader == null || CurrentLeader.IsDead)
            RefreshLeader();

        return CurrentLeader;
    }

    public void SetExplicitLeader(Entity leader)
    {
        explicitLeader = leader;
        RefreshLeader();
    }

    public List<Entity> GetAliveMembers()
    {
        List<Entity> members = new List<Entity>();
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entity == null)
                continue;

            if (entity.team != controlledTeam)
                continue;

            if (entity.IsDead)
                continue;

            members.Add(entity);
        }

        members.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return members;
    }

    public void RefreshLeader()
    {
        Entity next = null;

        if (explicitLeader != null && !explicitLeader.IsDead && explicitLeader.team == controlledTeam)
            next = explicitLeader;

        if (next == null && fallbackToFirstAlive)
        {
            List<Entity> alive = GetAliveMembers();
            if (alive.Count > 0)
                next = alive[0];
        }

        if (CurrentLeader == next)
            return;

        CurrentLeader = next;
        OnLeaderChanged?.Invoke(CurrentLeader);
    }
}
