using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterStats))]
public class Entity : MonoBehaviour
{
    [Header("Identity")]
    public Team team = Team.Player;
    [SerializeField] private string questEnemyId = "";
    [SerializeField] private string enemyPrefabId = "";

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Attack Animation")]
    public float attackLungeDistance = 0.22f;
    public float attackLungeDuration = 0.08f;
    public bool isAnimatingAttack;

    [Header("Damage Number")]
    public Transform damageNumberAnchor;

    [Header("Rewards")]
    public int xpReward = 5;
    public int moneyReward = 0;

    [Header("Exploration Trigger")]
    [Range(0f, 1f)]
    [SerializeField] private float adjacentCombatTriggerChance = 0.25f;

    public int CurrentHP => stats != null ? stats.CurrentHP : 0;
    public int maxHP => stats != null ? stats.MaxHP : 1;
    public int attackDamage => stats != null ? stats.Atk : 0;
    public int defense => stats != null ? stats.Def : 0;
    public int actionPoints => stats != null ? stats.Ap : 0;
    public float critChance => stats != null ? stats.Crit : 0f;
    public int Level => stats != null ? stats.Level : 1;
    public int CurrentXP => stats != null ? stats.CurrentXP : 0;
    public int UnspentStatPoints => stats != null ? stats.UnspentStatPoints : 0;
    public string QuestEnemyId => ResolveQuestEnemyId();
    public string EnemyPrefabId => string.IsNullOrWhiteSpace(enemyPrefabId) ? gameObject.name : enemyPrefabId.Trim();
    public float AdjacentCombatTriggerChance => Mathf.Clamp01(adjacentCombatTriggerChance);

    public Vector2Int GridPosition { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;
    public event Action<int, Vector3> OnDamageTaken;
    public event Action OnStatsChanged;
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnXPChanged;

    private CharacterStats stats;
    private EquipmentSlots equipmentSlots;
    private Vector3 targetWorldPosition;
    private bool targetInitialized;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        equipmentSlots = GetComponent<EquipmentSlots>();
    }

    private void OnEnable()
    {
        if (stats == null)
            stats = GetComponent<CharacterStats>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            stats.OnStatsChanged += HandleStatsChanged;
            stats.OnLevelUp += HandleLevelUp;
            stats.OnXPChanged += HandleXPChanged;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
            stats.OnStatsChanged -= HandleStatsChanged;
            stats.OnLevelUp -= HandleLevelUp;
            stats.OnXPChanged -= HandleXPChanged;
        }
    }

    private void Start()
    {
        if (stats == null)
            stats = GetComponent<CharacterStats>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (stats != null)
            stats.Initialize();

        if (ShouldSkipAutoRegistration())
            return;

        Vector2Int startCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y));

        if (GridManager.Instance != null)
            GridManager.Instance.RegisterEntity(this, startCell);
    }

    private void Update()
    {
        if (!targetInitialized || isAnimatingAttack)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime);
    }

    public CharacterStats GetStatsComponent()
    {
        return stats;
    }

    public EquipmentSlots GetEquipmentSlots()
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        return equipmentSlots;
    }

    public void SetQuestEnemyId(string value)
    {
        questEnemyId = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    public void SetEnemyPrefabId(string value)
    {
        enemyPrefabId = string.IsNullOrWhiteSpace(value) ? gameObject.name : value.Trim();
    }

    public void SetMoneyReward(int value)
    {
        moneyReward = Mathf.Max(0, value);
    }

    public bool EquipItem(ItemData item)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        return slots != null && item != null && slots.Equip(item, Level);
    }

    public bool EquipGeneratedItem(GeneratedItemInstance item)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        return slots != null && item != null && slots.EquipGenerated(item, Level);
    }

    public void UnequipItem(EquipmentSlotType slotType)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots != null)
            slots.Unequip(slotType);
    }

    public void AddXP(int amount)
    {
        if (stats != null)
            stats.AddXP(amount);
    }

    public bool SpendPointOnHP(int amount = 1)
    {
        return stats != null && stats.SpendPointOnHP(amount);
    }

    public bool SpendPointOnATK(int amount = 1)
    {
        return stats != null && stats.SpendPointOnATK(amount);
    }

    public bool SpendPointOnDEF(int amount = 1)
    {
        return stats != null && stats.SpendPointOnDEF(amount);
    }

    public bool SpendPointOnAP(int amount = 1)
    {
        return stats != null && stats.SpendPointOnAP(amount);
    }

    public bool SpendPointOnCRIT(float amount = 1f, int pointCost = 1)
    {
        return stats != null && stats.SpendPointOnCRIT(amount, pointCost);
    }

    public void SetGridPosition(Vector2Int newGridPosition)
    {
        GridPosition = newGridPosition;
    }

    public void SetVisualTarget(Vector3 worldPosition, bool snapImmediately = false)
    {
        targetWorldPosition = worldPosition;
        targetInitialized = true;

        if (snapImmediately)
            transform.position = targetWorldPosition;
    }

    public Vector3 GetVisualTarget()
    {
        return targetWorldPosition;
    }

    public void PlayAttackLunge(Vector3 attackDirection)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(AttackLungeRoutine(attackDirection.normalized));
    }

    public void ReceiveDamage(int amount)
    {
        if (IsDead || amount <= 0 || stats == null)
            return;

        stats.ReceiveRawDamage(amount);

        Vector3 popupPosition = damageNumberAnchor != null
            ? damageNumberAnchor.position
            : transform.position + Vector3.up * 0.6f;

        OnDamageTaken?.Invoke(amount, popupPosition);

        if (CurrentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (!IsDead && amount > 0 && stats != null)
            stats.Heal(amount);
    }

    private IEnumerator AttackLungeRoutine(Vector3 attackDirection)
    {
        if (isAnimatingAttack)
            yield break;

        isAnimatingAttack = true;

        Vector3 basePosition = targetWorldPosition;
        Vector3 forwardPosition = basePosition + attackDirection * attackLungeDistance;

        float timer = 0f;
        while (timer < attackLungeDuration)
        {
            timer += Time.deltaTime;
            float t = attackLungeDuration > 0f ? timer / attackLungeDuration : 1f;
            transform.position = Vector3.Lerp(basePosition, forwardPosition, t);
            yield return null;
        }

        timer = 0f;
        while (timer < attackLungeDuration)
        {
            timer += Time.deltaTime;
            float t = attackLungeDuration > 0f ? timer / attackLungeDuration : 1f;
            transform.position = Vector3.Lerp(forwardPosition, basePosition, t);
            yield return null;
        }

        transform.position = basePosition;
        isAnimatingAttack = false;
    }

    private void Die()
    {
        if (!IsDead)
            return;

        GrantXPToOppositeTeam();
        NotifyQuestTracker();

        OnDied?.Invoke();

        if (GridManager.Instance != null)
            GridManager.Instance.RemoveEntity(this);

        Destroy(gameObject);
    }

    private void GrantXPToOppositeTeam()
    {
        if (GridManager.Instance == null)
            return;

        Team receiverTeam = team == Team.Player ? Team.Enemy : Team.Player;
        var receivers = GridManager.Instance.GetEntitiesByTeam(receiverTeam);

        foreach (Entity receiver in receivers)
        {
            if (receiver != null && !receiver.IsDead)
                receiver.AddXP(xpReward);
        }
    }

    private void NotifyQuestTracker()
    {
        if (team == Team.Enemy && QuestTracker.Instance != null)
            QuestTracker.Instance.NotifyEnemyDefeated(this);
    }

    private bool ShouldSkipAutoRegistration()
    {
        if (team != Team.Player)
            return false;

        if (CombatExplorationReturnData.HasPendingReturn)
            return true;

        string activeSceneName = SceneManager.GetActiveScene().name;
        return ExplorationScenePersistenceData.ShouldSuppressPlayerAutoRegistration(activeSceneName);
    }

    private string ResolveQuestEnemyId()
    {
        if (!string.IsNullOrWhiteSpace(questEnemyId))
            return questEnemyId.Trim();

        return CharacterIdentity.ResolveFromEntity(this);
    }

    private void HandleHealthChanged(int currentHP, int maxHPValue)
    {
        OnHealthChanged?.Invoke(currentHP, maxHPValue);
    }

    private void HandleStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }

    private void HandleLevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);
    }

    private void HandleXPChanged(int current, int needed)
    {
        OnXPChanged?.Invoke(current, needed);
    }
}
