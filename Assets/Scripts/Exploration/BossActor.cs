using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Entity))]
public class BossActor : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string bossId = "boss_001";
    [SerializeField] private string bossDisplayName = "Boss da Dungeon";

    [Header("Reward")]
    [SerializeField] private int bonusMoneyReward = 0;
    [SerializeField] private ItemData guaranteedStaticReward;
    [SerializeField] private ItemGenerationProfile guaranteedGeneratedReward;

    [Header("Persistence")]
    [SerializeField] private bool persistDefeat = true;

    [Header("Visual")]
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject defeatedVisual;

    private InventoryItemEntry cachedRewardEntry;

    public string BossId => string.IsNullOrWhiteSpace(bossId) ? "boss_unnamed" : bossId.Trim();
    public string BossDisplayName => string.IsNullOrWhiteSpace(bossDisplayName) ? gameObject.name : bossDisplayName;
    public int BonusMoneyReward => Mathf.Max(0, bonusMoneyReward);
    public string BossPersistenceKey => $"{SceneManager.GetActiveScene().name}::{BossId}";
    public bool IsDefeated => persistDefeat && DungeonBossPersistence.IsBossDefeated(BossPersistenceKey);

    private void Awake()
    {
        if (IsDefeated)
        {
            ApplyVisualState(false);
            Destroy(gameObject);
            return;
        }

        ApplyVisualState(true);
    }

    public InventoryItemEntry CreateRewardEntrySnapshot()
    {
        if (cachedRewardEntry != null && !cachedRewardEntry.IsEmpty)
            return cachedRewardEntry.Clone();

        if (guaranteedStaticReward != null)
        {
            cachedRewardEntry = InventoryItemEntry.FromStatic(guaranteedStaticReward);
            return cachedRewardEntry.Clone();
        }

        if (guaranteedGeneratedReward != null)
        {
            GeneratedItemInstance generated = ItemGenerator.Generate(guaranteedGeneratedReward);
            if (generated != null)
            {
                cachedRewardEntry = InventoryItemEntry.FromGenerated(generated);
                return cachedRewardEntry.Clone();
            }
        }

        return null;
    }

    private void ApplyVisualState(bool active)
    {
        if (activeVisual != null)
            activeVisual.SetActive(active);

        if (defeatedVisual != null)
            defeatedVisual.SetActive(!active);
    }
}
