using System;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private StatBlock baseStats = new StatBlock();

    [Header("Bonus Stats")]
    [SerializeField] private StatBlock levelBonus = new StatBlock();
    [SerializeField] private StatBlock pointBonus = new StatBlock();

    [Header("Level System")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int baseXPToNextLevel = 10;
    [SerializeField] private int xpGrowthPerLevel = 5;
    [SerializeField] private int statPointsPerLevel = 3;

    [Header("Level Bonus Per Level")]
    [SerializeField] private int hpPerLevel = 2;
    [SerializeField] private int atkPerLevel = 1;
    [SerializeField] private int defPerLevel = 0;
    [SerializeField] private int apPerLevel = 0;
    [SerializeField] private float critPerLevel = 0f;

    [Header("Available Points")]
    [SerializeField] private int unspentStatPoints = 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnStatsChanged;
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnXPChanged;

    public StatBlock BaseStats => baseStats;
    public StatBlock LevelBonus => levelBonus;
    public StatBlock PointBonus => pointBonus;
    public StatBlock ItemBonus => GetRuntimeItemBonus();

    public int Level => level;
    public int CurrentXP => currentXP;
    public int UnspentStatPoints => unspentStatPoints;

    public int CurrentHP { get; private set; }

    public int MaxHP => GetFinalStats().hp;
    public int Atk => GetFinalStats().atk;
    public int Def => GetFinalStats().def;
    public int Ap => GetFinalStats().ap;
    public float Crit => GetFinalStats().crit;

    private bool initialized = false;
    private EquipmentSlots equipmentSlots;

    private void Awake()
    {
        equipmentSlots = GetComponent<EquipmentSlots>();
    }

    private void OnEnable()
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (equipmentSlots != null)
            equipmentSlots.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void OnDisable()
    {
        if (equipmentSlots != null)
            equipmentSlots.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    public void Initialize()
    {
        if (initialized)
            return;

        SanitizeReferences();
        RebuildLevelBonus();

        CurrentHP = MaxHP;
        initialized = true;

        OnStatsChanged?.Invoke();
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        OnXPChanged?.Invoke(currentXP, GetXPToNextLevel());
    }

    public StatBlock GetFinalStats()
    {
        SanitizeReferences();

        StatBlock total = StatBlock.Add(baseStats, levelBonus);
        total = StatBlock.Add(total, pointBonus);
        total = StatBlock.Add(total, GetRuntimeItemBonus());
        total.ClampAsFinalStats();
        return total;
    }

    public void RecalculateStats(bool preserveHealthPercent = true)
    {
        int oldMaxHP = Mathf.Max(1, MaxHP);
        float healthPercent = oldMaxHP > 0 ? (float)CurrentHP / oldMaxHP : 1f;

        SanitizeReferences();

        int newMaxHP = MaxHP;

        if (preserveHealthPercent)
            CurrentHP = Mathf.Clamp(Mathf.RoundToInt(newMaxHP * healthPercent), 0, newMaxHP);
        else
            CurrentHP = Mathf.Clamp(CurrentHP, 0, newMaxHP);

        OnStatsChanged?.Invoke();
        OnHealthChanged?.Invoke(CurrentHP, newMaxHP);
    }

    public void SetCurrentHPToMax()
    {
        CurrentHP = MaxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void ReceiveRawDamage(int amount)
    {
        if (!initialized)
            Initialize();

        if (amount <= 0)
            return;

        CurrentHP -= amount;
        if (CurrentHP < 0)
            CurrentHP = 0;

        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void Heal(int amount)
    {
        if (!initialized)
            Initialize();

        if (amount <= 0)
            return;

        CurrentHP += amount;
        if (CurrentHP > MaxHP)
            CurrentHP = MaxHP;

        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    public int CalculateIncomingDamage(int incomingDamage)
    {
        int reducedDamage = incomingDamage - Mathf.Max(0, Def);
        return Mathf.Max(1, reducedDamage);
    }

    public bool RollCrit()
    {
        float roll = UnityEngine.Random.Range(0f, 100f);
        return roll < Crit;
    }

    public int GetXPToNextLevel()
    {
        int xpNeeded = baseXPToNextLevel + (level - 1) * xpGrowthPerLevel;
        return Mathf.Max(1, xpNeeded);
    }

    public void AddXP(int amount)
    {
        if (!initialized)
            Initialize();

        if (amount <= 0)
            return;

        currentXP += amount;

        bool leveledUp = false;

        while (currentXP >= GetXPToNextLevel())
        {
            currentXP -= GetXPToNextLevel();
            LevelUp();
            leveledUp = true;
        }

        if (!leveledUp)
            OnXPChanged?.Invoke(currentXP, GetXPToNextLevel());
    }

    private void LevelUp()
    {
        level += 1;
        unspentStatPoints += statPointsPerLevel;

        RebuildLevelBonus();
        RecalculateStats(true);

        OnLevelUp?.Invoke(level);
        OnXPChanged?.Invoke(currentXP, GetXPToNextLevel());
    }

    private void RebuildLevelBonus()
    {
        int extraLevels = Mathf.Max(0, level - 1);

        levelBonus = new StatBlock
        {
            hp = hpPerLevel * extraLevels,
            atk = atkPerLevel * extraLevels,
            def = defPerLevel * extraLevels,
            ap = apPerLevel * extraLevels,
            crit = critPerLevel * extraLevels
        };
    }

    private StatBlock GetRuntimeItemBonus()
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (equipmentSlots == null)
        {
            return new StatBlock
            {
                hp = 0,
                atk = 0,
                def = 0,
                ap = 0,
                crit = 0f
            };
        }

        StatBlock total = equipmentSlots.GetTotalItemBonus();

        if (total == null)
        {
            return new StatBlock
            {
                hp = 0,
                atk = 0,
                def = 0,
                ap = 0,
                crit = 0f
            };
        }

        return new StatBlock
        {
            hp = total.hp,
            atk = total.atk,
            def = total.def,
            ap = total.ap,
            crit = total.crit
        };
    }

    public bool SpendPointOnHP(int amount = 1)
    {
        if (!CanSpendPoints(amount)) return false;
        unspentStatPoints -= amount;
        pointBonus.hp += amount;
        RecalculateStats(false);
        return true;
    }

    public bool SpendPointOnATK(int amount = 1)
    {
        if (!CanSpendPoints(amount)) return false;
        unspentStatPoints -= amount;
        pointBonus.atk += amount;
        RecalculateStats(false);
        return true;
    }

    public bool SpendPointOnDEF(int amount = 1)
    {
        if (!CanSpendPoints(amount)) return false;
        unspentStatPoints -= amount;
        pointBonus.def += amount;
        RecalculateStats(false);
        return true;
    }

    public bool SpendPointOnAP(int amount = 1)
    {
        if (!CanSpendPoints(amount)) return false;
        unspentStatPoints -= amount;
        pointBonus.ap += amount;
        RecalculateStats(false);
        return true;
    }

    public bool SpendPointOnCRIT(float amount = 1f, int pointCost = 1)
    {
        if (!CanSpendPoints(pointCost)) return false;
        unspentStatPoints -= pointCost;
        pointBonus.crit += amount;
        RecalculateStats(false);
        return true;
    }

    private bool CanSpendPoints(int amount)
    {
        return amount > 0 && unspentStatPoints >= amount;
    }

    public void SetBaseStats(StatBlock newBaseStats, bool preserveHealthPercent = true)
    {
        if (newBaseStats == null)
            return;

        baseStats = newBaseStats.Clone();
        RecalculateStats(preserveHealthPercent);
    }

    public void SetLevelBonus(StatBlock newLevelBonus, bool preserveHealthPercent = true)
    {
        if (newLevelBonus == null)
            return;

        levelBonus = newLevelBonus.Clone();
        RecalculateStats(preserveHealthPercent);
    }

    public void SetPointBonus(StatBlock newPointBonus, bool preserveHealthPercent = true)
    {
        if (newPointBonus == null)
            return;

        pointBonus = newPointBonus.Clone();
        RecalculateStats(preserveHealthPercent);
    }

    public void SetProgressionData(int newLevel, int newXP, int newUnspentStatPoints)
    {
        level = Mathf.Max(1, newLevel);
        currentXP = Mathf.Max(0, newXP);
        unspentStatPoints = Mathf.Max(0, newUnspentStatPoints);

        RebuildLevelBonus();
        RecalculateStats(true);
        OnXPChanged?.Invoke(currentXP, GetXPToNextLevel());
    }

    private void HandleEquipmentChanged()
    {
        RecalculateStats(true);
    }

    private void SanitizeReferences()
    {
        if (baseStats == null) baseStats = new StatBlock();
        if (levelBonus == null) levelBonus = new StatBlock();
        if (pointBonus == null) pointBonus = new StatBlock();

        level = Mathf.Max(1, level);
        currentXP = Mathf.Max(0, currentXP);
        unspentStatPoints = Mathf.Max(0, unspentStatPoints);
        baseXPToNextLevel = Mathf.Max(1, baseXPToNextLevel);
        xpGrowthPerLevel = Mathf.Max(0, xpGrowthPerLevel);
        statPointsPerLevel = Mathf.Max(0, statPointsPerLevel);
    }
}