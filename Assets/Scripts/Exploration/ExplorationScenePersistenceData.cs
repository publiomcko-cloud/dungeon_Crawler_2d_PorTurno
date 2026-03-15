using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExplorationScenePersistenceData
{
    public class EntityStateSnapshot
    {
        public string CharacterId { get; private set; }
        public string EntityName { get; private set; }
        public Vector2Int Cell { get; private set; }
        public int CurrentHP { get; private set; }
        public int CurrentXP { get; private set; }
        public int UnspentStatPoints { get; private set; }
        public int Level { get; private set; }
        public StatBlock BaseStats { get; private set; }
        public StatBlock PointBonus { get; private set; }
        public InventoryItemEntry EquippedWeapon { get; private set; }
        public InventoryItemEntry EquippedArmor { get; private set; }
        public InventoryItemEntry EquippedAccessory { get; private set; }

        public EntityStateSnapshot(Entity entity)
        {
            if (entity == null)
            {
                CharacterId = "Missing";
                EntityName = "Missing";
                Cell = Vector2Int.zero;
                CurrentHP = 0;
                CurrentXP = 0;
                UnspentStatPoints = 0;
                Level = 1;
                BaseStats = new StatBlock();
                PointBonus = new StatBlock();
                return;
            }

            CharacterStats stats = entity.GetStatsComponent();

            CharacterId = CharacterIdentity.ResolveFromEntity(entity);
            EntityName = entity.name;
            Cell = entity.GridPosition;
            CurrentHP = entity.CurrentHP;
            CurrentXP = entity.CurrentXP;
            UnspentStatPoints = entity.UnspentStatPoints;
            Level = entity.Level;
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

    public sealed class PartyMemberSnapshot : EntityStateSnapshot
    {
        public PartyMemberSnapshot(Entity entity) : base(entity)
        {
        }
    }

    public sealed class EnemyStateSnapshot : EntityStateSnapshot
    {
        public EnemyStateSnapshot(Entity entity) : base(entity)
        {
        }
    }

    public sealed class GroundItemSnapshot
    {
        public Vector2Int Cell { get; private set; }
        public InventoryItemEntry ItemEntry { get; private set; }

        public GroundItemSnapshot(GroundItem groundItem)
        {
            if (groundItem == null)
            {
                Cell = Vector2Int.zero;
                ItemEntry = null;
                return;
            }

            Vector3 position = groundItem.transform.position;
            Cell = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));

            InventoryItemEntry entry = groundItem.ToInventoryEntry();
            ItemEntry = entry != null ? entry.Clone() : null;
        }
    }

    public sealed class SceneStateSnapshot
    {
        public string SceneName { get; private set; }
        public List<EnemyStateSnapshot> Enemies { get; private set; }
        public List<GroundItemSnapshot> GroundItems { get; private set; }

        public SceneStateSnapshot(
            string sceneName,
            List<EnemyStateSnapshot> enemies,
            List<GroundItemSnapshot> groundItems)
        {
            SceneName = sceneName;
            Enemies = enemies ?? new List<EnemyStateSnapshot>();
            GroundItems = groundItems ?? new List<GroundItemSnapshot>();
        }
    }

    public sealed class PendingSceneTransition
    {
        public string SourceSceneName { get; private set; }
        public string TargetSceneName { get; private set; }
        public string SourcePortalId { get; private set; }
        public string TargetPortalId { get; private set; }
        public string LeaderCharacterId { get; private set; }
        public List<PartyMemberSnapshot> PartyMembers { get; private set; }
        public List<InventoryItemEntry> PartyInventoryItems { get; private set; }

        public PendingSceneTransition(
            string sourceSceneName,
            string targetSceneName,
            string sourcePortalId,
            string targetPortalId,
            string leaderCharacterId,
            List<PartyMemberSnapshot> partyMembers,
            List<InventoryItemEntry> partyInventoryItems)
        {
            SourceSceneName = sourceSceneName;
            TargetSceneName = targetSceneName;
            SourcePortalId = sourcePortalId;
            TargetPortalId = targetPortalId;
            LeaderCharacterId = leaderCharacterId;
            PartyMembers = partyMembers ?? new List<PartyMemberSnapshot>();
            PartyInventoryItems = partyInventoryItems ?? new List<InventoryItemEntry>();
        }
    }

    private static readonly Dictionary<string, SceneStateSnapshot> sceneStates = new Dictionary<string, SceneStateSnapshot>();

    public static PendingSceneTransition CurrentTransition { get; private set; }
    public static bool HasPendingTransition => CurrentTransition != null;

    public static bool HasPendingTransitionToScene(string sceneName)
    {
        return CurrentTransition != null && CurrentTransition.TargetSceneName == sceneName;
    }

    public static bool ShouldSuppressPlayerAutoRegistration(string sceneName)
    {
        return HasPendingTransitionToScene(sceneName);
    }

    public static bool HasSavedSceneState(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && sceneStates.ContainsKey(sceneName);
    }

    public static SceneStateSnapshot GetSavedSceneState(string sceneName)
    {
        if (!HasSavedSceneState(sceneName))
            return null;

        return sceneStates[sceneName];
    }

    public static bool PrepareTransition(ScenePortal sourcePortal)
    {
        if (sourcePortal == null)
            return false;

        string sourceSceneName = SceneManager.GetActiveScene().name;
        string targetSceneName = sourcePortal.TargetSceneName;

        if (string.IsNullOrWhiteSpace(sourceSceneName) || string.IsNullOrWhiteSpace(targetSceneName))
            return false;

        SaveSceneState(CaptureCurrentSceneState(sourceSceneName));

        string leaderCharacterId = null;
        if (PartyAnchorService.Instance != null && PartyAnchorService.Instance.GetLeader() != null)
            leaderCharacterId = CharacterIdentity.ResolveFromEntity(PartyAnchorService.Instance.GetLeader());

        CurrentTransition = new PendingSceneTransition(
            sourceSceneName,
            targetSceneName,
            sourcePortal.PortalId,
            sourcePortal.TargetPortalId,
            leaderCharacterId,
            CapturePartyMembers(),
            CapturePartyInventory());

        return true;
    }

    public static void ClearPendingTransition()
    {
        CurrentTransition = null;
    }

    private static void SaveSceneState(SceneStateSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.SceneName))
            return;

        sceneStates[snapshot.SceneName] = snapshot;
    }

    private static SceneStateSnapshot CaptureCurrentSceneState(string sceneName)
    {
        List<EnemyStateSnapshot> enemies = Object.FindObjectsByType<Entity>(FindObjectsSortMode.None)
            .Where(entity => entity != null && !entity.IsDead && entity.team == Team.Enemy)
            .OrderBy(entity => entity.name)
            .Select(entity => new EnemyStateSnapshot(entity))
            .ToList();

        List<GroundItemSnapshot> groundItems = Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None)
            .Where(item => item != null && item.gameObject.activeInHierarchy && item.ToInventoryEntry() != null)
            .Select(item => new GroundItemSnapshot(item))
            .ToList();

        return new SceneStateSnapshot(sceneName, enemies, groundItems);
    }

    private static List<PartyMemberSnapshot> CapturePartyMembers()
    {
        return Object.FindObjectsByType<Entity>(FindObjectsSortMode.None)
            .Where(entity => entity != null && !entity.IsDead && entity.team == Team.Player)
            .OrderBy(entity => entity.name)
            .Select(entity => new PartyMemberSnapshot(entity))
            .ToList();
    }

    private static List<InventoryItemEntry> CapturePartyInventory()
    {
        PartyInventory partyInventory = Object.FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
            return new List<InventoryItemEntry>();

        return partyInventory.GetItemsSnapshot();
    }
}
