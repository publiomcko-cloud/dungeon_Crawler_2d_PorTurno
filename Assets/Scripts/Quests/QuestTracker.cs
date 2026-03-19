using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestTracker : MonoBehaviour
{
    public static QuestTracker Instance;

    [SerializeField] private bool persistAcrossScenes = true;

    public event Action OnQuestStateChanged;

    public QuestDefinition ActiveQuest { get; private set; }
    public int ActiveQuestProgress { get; private set; }
    public bool HasActiveQuest => ActiveQuest != null;
    public bool IsActiveQuestComplete => ActiveQuest != null && ActiveQuestProgress >= ActiveQuest.RequiredKillCount;

    private readonly HashSet<string> completedQuestIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    public bool IsQuestCompleted(QuestDefinition definition)
    {
        return definition != null && completedQuestIds.Contains(definition.QuestId);
    }

    public bool IsQuestActive(QuestDefinition definition)
    {
        return definition != null && ActiveQuest != null && ActiveQuest.QuestId == definition.QuestId;
    }

    public bool CanAcceptQuest(QuestDefinition definition, out string message)
    {
        if (definition == null)
        {
            message = "Quest invalida.";
            return false;
        }

        if (HasActiveQuest)
        {
            message = $"Voce ja possui uma quest ativa: {ActiveQuest.DisplayName}.";
            return false;
        }

        if (IsQuestCompleted(definition))
        {
            message = "Essa quest ja foi concluida.";
            return false;
        }

        Entity leader = PartyAnchorService.Instance != null ? PartyAnchorService.Instance.GetLeader() : null;
        int leaderLevel = leader != null ? leader.Level : 1;
        if (leaderLevel < definition.MinimumLeaderLevel)
        {
            message = $"Nivel do lider insuficiente. Necessario: {definition.MinimumLeaderLevel}.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public bool TryAcceptQuest(QuestDefinition definition, out string message)
    {
        if (!CanAcceptQuest(definition, out message))
            return false;

        ActiveQuest = definition;
        ActiveQuestProgress = 0;
        OnQuestStateChanged?.Invoke();
        message = $"Quest aceita: {definition.DisplayName}.";
        return true;
    }

    public bool TryClaimActiveQuest(out string message)
    {
        if (!HasActiveQuest)
        {
            message = "Nenhuma quest ativa.";
            return false;
        }

        if (!IsActiveQuestComplete)
        {
            message = "A quest ativa ainda nao foi concluida.";
            return false;
        }

        QuestDefinition completedQuest = ActiveQuest;
        if (PartyCurrency.Instance != null && completedQuest.RewardMoney > 0)
            PartyCurrency.Instance.AddMoney(completedQuest.RewardMoney);

        completedQuestIds.Add(completedQuest.QuestId);
        ActiveQuest = null;
        ActiveQuestProgress = 0;

        OnQuestStateChanged?.Invoke();
        message = $"{completedQuest.DisplayName} concluida. Recompensa: {completedQuest.RewardMoney}.";
        return true;
    }

    public void NotifyEnemyDefeated(Entity enemy)
    {
        if (enemy == null || !HasActiveQuest)
            return;

        if (!ActiveQuest.MatchesEnemy(enemy))
            return;

        ActiveQuestProgress = Mathf.Clamp(ActiveQuestProgress + 1, 0, ActiveQuest.RequiredKillCount);
        OnQuestStateChanged?.Invoke();
    }

    public string BuildActiveQuestProgressText()
    {
        if (!HasActiveQuest)
            return "Nenhuma quest ativa.";

        return $"{ActiveQuestProgress}/{ActiveQuest.RequiredKillCount}";
    }
}
