using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatSessionData
{
    public class EntityStateSnapshot
    {
        public string CombatantId { get; private set; }
        public string CharacterId { get; private set; }
        public string EntityName { get; private set; }
        public Team Team { get; private set; }
        public Vector2Int Cell { get; private set; }
        public int CurrentHP { get; private set; }
        public int CurrentXP { get; private set; }
        public int UnspentStatPoints { get; private set; }
        public int Level { get; private set; }
        public int MoneyReward { get; private set; }
        public string QuestEnemyId { get; private set; }
        public string EnemyPrefabId { get; private set; }
        public bool IsDungeonBoss { get; private set; }
        public string BossPersistenceKey { get; private set; }
        public string BossDisplayName { get; private set; }
        public int BossRewardMoney { get; private set; }
        public InventoryItemEntry BossRewardEntry { get; private set; }
        public StatBlock BaseStats { get; private set; }
        public StatBlock PointBonus { get; private set; }
        public InventoryItemEntry EquippedWeapon { get; private set; }
        public InventoryItemEntry EquippedArmor { get; private set; }
        public InventoryItemEntry EquippedAccessory { get; private set; }

        public EntityStateSnapshot(Entity entity)
        {
            if (entity == null)
            {
                CombatantId = "";
                CharacterId = "Missing";
                EntityName = "Missing";
                Team = Team.Player;
                Cell = Vector2Int.zero;
                CurrentHP = 0;
                CurrentXP = 0;
                UnspentStatPoints = 0;
                Level = 1;
                MoneyReward = 0;
                QuestEnemyId = "";
                EnemyPrefabId = "";
                IsDungeonBoss = false;
                BossPersistenceKey = "";
                BossDisplayName = "";
                BossRewardMoney = 0;
                BossRewardEntry = null;
                BaseStats = new StatBlock();
                PointBonus = new StatBlock();
                return;
            }

            CharacterStats stats = entity.GetStatsComponent();
            BossActor bossActor = entity.GetComponent<BossActor>();

            CombatantId = entity.GetInstanceID().ToString();
            CharacterId = CharacterIdentity.ResolveFromEntity(entity);
            EntityName = entity.name;
            Team = entity.team;
            Cell = entity.GridPosition;
            CurrentHP = entity.CurrentHP;
            CurrentXP = entity.CurrentXP;
            UnspentStatPoints = entity.UnspentStatPoints;
            Level = entity.Level;
            MoneyReward = entity.moneyReward;
            QuestEnemyId = entity.QuestEnemyId;
            EnemyPrefabId = entity.EnemyPrefabId;
            IsDungeonBoss = bossActor != null;
            BossPersistenceKey = bossActor != null ? bossActor.BossPersistenceKey : "";
            BossDisplayName = bossActor != null ? bossActor.BossDisplayName : "";
            BossRewardMoney = bossActor != null ? bossActor.BonusMoneyReward : 0;
            BossRewardEntry = bossActor != null ? bossActor.CreateRewardEntrySnapshot() : null;
            BaseStats = stats != null && stats.BaseStats != null ? stats.BaseStats.Clone() : new StatBlock();
            PointBonus = stats != null && stats.PointBonus != null ? stats.PointBonus.Clone() : new StatBlock();
            EquippedWeapon = CreateEquipmentEntry(entity, EquipmentSlotType.Weapon);
            EquippedArmor = CreateEquipmentEntry(entity, EquipmentSlotType.Armor);
            EquippedAccessory = CreateEquipmentEntry(entity, EquipmentSlotType.Accessory);
        }

        private static InventoryItemEntry CreateEquipmentEntry(Entity entity, EquipmentSlotType slotType)
        {
            EquipmentSlots equipmentSlots = entity != null ? entity.GetEquipmentSlots() : null;
            if (equipmentSlots == null)
                return null;

            ItemData staticItem = equipmentSlots.GetItemInSlot(slotType);
            if (staticItem != null)
                return InventoryItemEntry.FromStatic(staticItem);

            GeneratedItemInstance generatedItem = equipmentSlots.GetGeneratedItemInSlot(slotType);
            if (generatedItem != null)
                return InventoryItemEntry.FromGenerated(generatedItem);

            return null;
        }
    }

    public sealed class EnemyExplorationSnapshot : EntityStateSnapshot
    {
        public EnemyExplorationSnapshot(Entity entity) : base(entity)
        {
        }
    }

    public sealed class CombatParticipantSnapshot : EntityStateSnapshot
    {
        public Vector2Int ExplorationCell { get; private set; }
        public IReadOnlyList<LootDropEntry> LootTable { get; private set; }

        public CombatParticipantSnapshot(Entity entity) : base(entity)
        {
            ExplorationCell = entity != null ? entity.GridPosition : Vector2Int.zero;
            LootTable = CreateLootTableSnapshot(entity);
        }

        private static List<LootDropEntry> CreateLootTableSnapshot(Entity entity)
        {
            LootDropper lootDropper = entity != null ? entity.GetComponent<LootDropper>() : null;
            if (lootDropper == null)
                return new List<LootDropEntry>();

            return lootDropper.GetLootTableSnapshot();
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
        public IReadOnlyList<EnemyExplorationSnapshot> PreservedExplorationEnemies { get; private set; }
        public IReadOnlyList<InventoryItemEntry> PartyInventoryItems { get; private set; }

        public CombatSessionSnapshot(
            string explorationSceneName,
            string combatSceneName,
            Team initiatingTeam,
            Vector2Int attackerCell,
            Vector2Int defenderCell,
            List<CombatParticipantSnapshot> attackers,
            List<CombatParticipantSnapshot> defenders,
            List<EnemyExplorationSnapshot> preservedExplorationEnemies,
            List<InventoryItemEntry> partyInventoryItems)
        {
            ExplorationSceneName = explorationSceneName;
            CombatSceneName = combatSceneName;
            InitiatingTeam = initiatingTeam;
            AttackerCell = attackerCell;
            DefenderCell = defenderCell;
            Attackers = attackers;
            Defenders = defenders;
            PreservedExplorationEnemies = preservedExplorationEnemies;
            PartyInventoryItems = partyInventoryItems;
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

        List<CombatParticipantSnapshot> attackerSnapshots = CreateParticipantSnapshots(attackers);
        List<CombatParticipantSnapshot> defenderSnapshots = CreateParticipantSnapshots(defenders);
        List<EnemyExplorationSnapshot> preservedExplorationEnemies = CreatePreservedEnemySnapshots(attackerSnapshots, defenderSnapshots);
        List<InventoryItemEntry> partyInventoryItems = CreatePartyInventorySnapshot();

        CurrentSession = new CombatSessionSnapshot(
            explorationSceneName,
            combatSceneName,
            initiatingTeam,
            attackerCell,
            defenderCell,
            attackerSnapshots,
            defenderSnapshots,
            preservedExplorationEnemies,
            partyInventoryItems);

        return CurrentSession;
    }

    public static void ClearSession()
    {
        CurrentSession = null;
    }

    private static List<CombatParticipantSnapshot> CreateParticipantSnapshots(List<Entity> entities)
    {
        if (entities == null)
            return new List<CombatParticipantSnapshot>();

        return entities
            .Where(entity => entity != null && !entity.IsDead)
            .OrderBy(entity => entity.name)
            .Select(entity => new CombatParticipantSnapshot(entity))
            .ToList();
    }

    private static List<EnemyExplorationSnapshot> CreatePreservedEnemySnapshots(
        List<CombatParticipantSnapshot> attackers,
        List<CombatParticipantSnapshot> defenders)
    {
        HashSet<string> engagedEnemyIds = new HashSet<string>(
            attackers.Where(snapshot => snapshot.Team == Team.Enemy).Select(snapshot => snapshot.CombatantId)
            .Concat(defenders.Where(snapshot => snapshot.Team == Team.Enemy).Select(snapshot => snapshot.CombatantId)));

        Entity[] entities = Object.FindObjectsByType<Entity>(FindObjectsSortMode.None);

        return entities
            .Where(entity => entity != null && !entity.IsDead && entity.team == Team.Enemy)
            .Where(entity => !engagedEnemyIds.Contains(entity.GetInstanceID().ToString()))
            .OrderBy(entity => entity.name)
            .Select(entity => new EnemyExplorationSnapshot(entity))
            .ToList();
    }

    private static List<InventoryItemEntry> CreatePartyInventorySnapshot()
    {
        PartyInventory partyInventory = Object.FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
            return new List<InventoryItemEntry>();

        return partyInventory.GetItemsSnapshot();
    }
}
