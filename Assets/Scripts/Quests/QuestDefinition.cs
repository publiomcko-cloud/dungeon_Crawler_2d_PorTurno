using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestDefinition", menuName = "RPG/Quest Definition")]
public class QuestDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string questId = "quest_001";
    [SerializeField] private string displayName = "Nova Quest";
    [TextArea]
    [SerializeField] private string description = "Derrote inimigos para receber uma recompensa.";

    [Header("Offer Rules")]
    [SerializeField] private int minimumLeaderLevel = 1;

    [Header("Target")]
    [SerializeField] private string targetEnemyQuestId = "";
    [SerializeField] private int minimumEnemyLevel = 1;
    [SerializeField] private int requiredKillCount = 5;

    [Header("Reward")]
    [SerializeField] private int rewardMoney = 25;

    public string QuestId => BuildQuestKey(questId, name);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description ?? "";
    public int MinimumLeaderLevel => Mathf.Max(1, minimumLeaderLevel);
    public string TargetEnemyQuestId => string.IsNullOrWhiteSpace(targetEnemyQuestId) ? "" : targetEnemyQuestId.Trim();
    public int MinimumEnemyLevel => Mathf.Max(1, minimumEnemyLevel);
    public int RequiredKillCount => Mathf.Max(1, requiredKillCount);
    public int RewardMoney => Mathf.Max(0, rewardMoney);

    public bool MatchesEnemy(Entity enemy)
    {
        if (enemy == null || enemy.team != Team.Enemy)
            return false;

        if (!string.IsNullOrWhiteSpace(TargetEnemyQuestId) && enemy.QuestEnemyId != TargetEnemyQuestId)
            return false;

        return enemy.Level >= MinimumEnemyLevel;
    }

    public string BuildTargetSummary()
    {
        string enemyLabel = string.IsNullOrWhiteSpace(TargetEnemyQuestId)
            ? "Qualquer inimigo"
            : TargetEnemyQuestId;

        return $"Alvo: {enemyLabel} | Nivel minimo do alvo: {MinimumEnemyLevel}";
    }

    private static string BuildQuestKey(string rawQuestId, string assetName)
    {
        string sanitizedAssetName = string.IsNullOrWhiteSpace(assetName) ? "unnamed_quest" : assetName.Trim();
        string sanitizedQuestId = string.IsNullOrWhiteSpace(rawQuestId) ? "" : rawQuestId.Trim();

        if (string.IsNullOrWhiteSpace(sanitizedQuestId) || sanitizedQuestId == "quest_001")
            return sanitizedAssetName;

        return $"{sanitizedQuestId}::{sanitizedAssetName}";
    }
}
