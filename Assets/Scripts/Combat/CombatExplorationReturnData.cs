using System.Collections.Generic;
using UnityEngine;

public static class CombatExplorationReturnData
{
    public class EntityReturnSnapshot
    {
        public string CombatantId { get; private set; }
        public string CharacterId { get; private set; }
        public string EntityName { get; private set; }
        public Vector2Int Cell { get; private set; }
        public int CurrentHP { get; private set; }
        public int CurrentXP { get; private set; }
        public int UnspentStatPoints { get; private set; }
        public int Level { get; private set; }
        public int MoneyReward { get; private set; }
        public string QuestEnemyId { get; private set; }
        public string EnemyPrefabId { get; private set; }
        public StatBlock BaseStats { get; private set; }
        public StatBlock PointBonus { get; private set; }
        public InventoryItemEntry EquippedWeapon { get; private set; }
        public InventoryItemEntry EquippedArmor { get; private set; }
        public InventoryItemEntry EquippedAccessory { get; private set; }

        public EntityReturnSnapshot(
            string combatantId,
            string characterId,
            string entityName,
            Vector2Int cell,
            int currentHP,
            int currentXP,
            int unspentStatPoints,
            int level,
            int moneyReward,
            string questEnemyId,
            string enemyPrefabId,
            StatBlock baseStats,
            StatBlock pointBonus,
            InventoryItemEntry equippedWeapon,
            InventoryItemEntry equippedArmor,
            InventoryItemEntry equippedAccessory)
        {
            CombatantId = combatantId;
            CharacterId = string.IsNullOrWhiteSpace(characterId) ? entityName : characterId;
            EntityName = entityName;
            Cell = cell;
            CurrentHP = Mathf.Max(0, currentHP);
            CurrentXP = Mathf.Max(0, currentXP);
            UnspentStatPoints = Mathf.Max(0, unspentStatPoints);
            Level = Mathf.Max(1, level);
            MoneyReward = Mathf.Max(0, moneyReward);
            QuestEnemyId = string.IsNullOrWhiteSpace(questEnemyId) ? "" : questEnemyId.Trim();
            EnemyPrefabId = string.IsNullOrWhiteSpace(enemyPrefabId) ? "" : enemyPrefabId.Trim();
            BaseStats = baseStats != null ? baseStats.Clone() : new StatBlock();
            PointBonus = pointBonus != null ? pointBonus.Clone() : new StatBlock();
            EquippedWeapon = equippedWeapon != null ? equippedWeapon.Clone() : null;
            EquippedArmor = equippedArmor != null ? equippedArmor.Clone() : null;
            EquippedAccessory = equippedAccessory != null ? equippedAccessory.Clone() : null;
        }
    }

    public sealed class EnemyReturnSnapshot : EntityReturnSnapshot
    {
        public EnemyReturnSnapshot(
            string combatantId,
            string characterId,
            string entityName,
            Vector2Int cell,
            int currentHP,
            int currentXP,
            int unspentStatPoints,
            int level,
            int moneyReward,
            string questEnemyId,
            string enemyPrefabId,
            StatBlock baseStats,
            StatBlock pointBonus,
            InventoryItemEntry equippedWeapon,
            InventoryItemEntry equippedArmor,
            InventoryItemEntry equippedAccessory)
            : base(
                combatantId,
                characterId,
                entityName,
                cell,
                currentHP,
                currentXP,
                unspentStatPoints,
                level,
                moneyReward,
                questEnemyId,
                enemyPrefabId,
                baseStats,
                pointBonus,
                equippedWeapon,
                equippedArmor,
                equippedAccessory)
        {
        }
    }

    public sealed class PlayerReturnSnapshot : EntityReturnSnapshot
    {
        public string OriginalEntityName { get; private set; }

        public PlayerReturnSnapshot(
            string originalEntityName,
            string combatantId,
            string characterId,
            Vector2Int cell,
            int currentHP,
            int currentXP,
            int unspentStatPoints,
            int level,
            int moneyReward,
            string questEnemyId,
            string enemyPrefabId,
            StatBlock baseStats,
            StatBlock pointBonus,
            InventoryItemEntry equippedWeapon,
            InventoryItemEntry equippedArmor,
            InventoryItemEntry equippedAccessory)
            : base(
                combatantId,
                characterId,
                originalEntityName,
                cell,
                currentHP,
                currentXP,
                unspentStatPoints,
                level,
                moneyReward,
                questEnemyId,
                enemyPrefabId,
                baseStats,
                pointBonus,
                equippedWeapon,
                equippedArmor,
                equippedAccessory)
        {
            OriginalEntityName = originalEntityName;
        }
    }

    public sealed class ExplorationReturnSnapshot
    {
        public string ExplorationSceneName { get; private set; }
        public Vector2Int ReturnCell { get; private set; }
        public Vector2Int LootCell { get; private set; }
        public string LeaderCharacterId { get; private set; }
        public int DefeatedEnemyCount { get; private set; }
        public int RewardMoney { get; private set; }
        public List<string> DefeatedBossKeys { get; private set; }
        public List<PlayerReturnSnapshot> PlayerSurvivors { get; private set; }
        public List<EnemyReturnSnapshot> PreservedEnemies { get; private set; }
        public List<InventoryItemEntry> LootEntries { get; private set; }
        public List<InventoryItemEntry> PartyInventoryItems { get; private set; }

        public ExplorationReturnSnapshot(
            string explorationSceneName,
            Vector2Int returnCell,
            Vector2Int lootCell,
            string leaderCharacterId,
            int defeatedEnemyCount,
            int rewardMoney,
            List<string> defeatedBossKeys,
            List<PlayerReturnSnapshot> playerSurvivors,
            List<EnemyReturnSnapshot> preservedEnemies,
            List<InventoryItemEntry> lootEntries,
            List<InventoryItemEntry> partyInventoryItems)
        {
            ExplorationSceneName = explorationSceneName;
            ReturnCell = returnCell;
            LootCell = lootCell;
            LeaderCharacterId = leaderCharacterId;
            DefeatedEnemyCount = Mathf.Max(0, defeatedEnemyCount);
            RewardMoney = Mathf.Max(0, rewardMoney);
            DefeatedBossKeys = defeatedBossKeys ?? new List<string>();
            PlayerSurvivors = playerSurvivors ?? new List<PlayerReturnSnapshot>();
            PreservedEnemies = preservedEnemies ?? new List<EnemyReturnSnapshot>();
            LootEntries = lootEntries ?? new List<InventoryItemEntry>();
            PartyInventoryItems = partyInventoryItems ?? new List<InventoryItemEntry>();
        }
    }

    public static ExplorationReturnSnapshot PendingReturn { get; private set; }
    public static bool HasPendingReturn => PendingReturn != null;

    public static void SetPendingReturn(ExplorationReturnSnapshot pendingReturn)
    {
        PendingReturn = pendingReturn;
    }

    public static void Clear()
    {
        PendingReturn = null;
    }
}
