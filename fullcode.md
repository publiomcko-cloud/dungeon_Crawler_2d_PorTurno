# Dungeon Crawler 2D Turn-Based Game Code

This document contains all the C# scripts for the Unity project, organized by file. This is a 2D dungeon crawler game with turn-based movement, where the player moves on a grid, and enemies take turns after the player.


using UnityEngine;

public class GridDebug : MonoBehaviour
{
    public int gridWidth = 20;
    public int gridHeight = 20;

    public float cellSize = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        for (int x = -gridWidth; x < gridWidth; x++)
        {
            for (int y = -gridHeight; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(
                    x + 0.5f,
                    y + 0.5f,
                    0
                );

                Gizmos.DrawSphere(pos, 0.05f);
            }
        }
    }
}


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid")]
    public int maxEntitiesPerCell = 4;
    public float slotOffset = 0.18f;

    [Header("Walls")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckRadius = 0.2f;

    private readonly Dictionary<Vector2Int, List<Entity>> grid = new Dictionary<Vector2Int, List<Entity>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterEntity(Entity entity, Vector2Int cell)
    {
        if (entity == null) return;
        if (IsCellBlocked(cell))
        {
            Debug.LogWarning($"Tentativa de registrar entidade em célula bloqueada: {cell}");
            return;
        }

        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Entity>();

        CleanupCell(cell);

        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Entity>();

        if (grid[cell].Count >= maxEntitiesPerCell)
        {
            Debug.LogWarning($"Cell {cell} já está cheia.");
            return;
        }

        if (!grid[cell].Contains(entity))
            grid[cell].Add(entity);

        entity.SetGridPosition(cell);
        RefreshCellVisuals(cell, true);
    }

    public void RemoveEntity(Entity entity)
    {
        if (entity == null) return;

        Vector2Int cell = entity.GridPosition;

        if (!grid.ContainsKey(cell))
            return;

        grid[cell].Remove(entity);

        if (grid[cell].Count == 0)
            grid.Remove(cell);
        else
            RefreshCellVisuals(cell, false);
    }

    public List<Entity> GetEntitiesAtCell(Vector2Int cell)
    {
        if (!grid.ContainsKey(cell))
            return new List<Entity>();

        CleanupCell(cell);

        if (!grid.ContainsKey(cell))
            return new List<Entity>();

        return grid[cell].Where(e => e != null && !e.IsDead).ToList();
    }

    public List<Entity> GetEntitiesByTeam(Team team)
    {
        List<Entity> result = new List<Entity>();

        foreach (var cell in grid.Keys.ToList())
        {
            CleanupCell(cell);

            if (!grid.ContainsKey(cell))
                continue;

            foreach (var entity in grid[cell])
            {
                if (entity != null && !entity.IsDead && entity.team == team)
                    result.Add(entity);
            }
        }

        return result;
    }

    public List<Vector2Int> GetOccupiedCellsByTeam(Team team)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        foreach (var cell in grid.Keys.ToList())
        {
            CleanupCell(cell);

            if (!grid.ContainsKey(cell))
                continue;

            bool hasTeam = grid[cell].Any(e => e != null && !e.IsDead && e.team == team);
            if (hasTeam)
                result.Add(cell);
        }

        return result;
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        return GetEntitiesAtCell(cell).Count > 0;
    }

    public bool IsCellBlocked(Vector2Int cell)
    {
        Vector2 world = GetCellCenterWorld(cell);
        Collider2D hit = Physics2D.OverlapCircle(world, wallCheckRadius, wallLayer);
        return hit != null;
    }

    public bool HasLineOfSight(Vector2Int fromCell, Vector2Int toCell)
    {
        Vector2 from = GetCellCenterWorld(fromCell);
        Vector2 to = GetCellCenterWorld(toCell);

        RaycastHit2D hit = Physics2D.Linecast(from, to, wallLayer);
        return hit.collider == null;
    }

    public bool TryMoveGroupOrAttack(List<Entity> movers, Vector2Int targetCell)
    {
        movers = movers.Where(e => e != null && !e.IsDead).ToList();
        if (movers.Count == 0) return false;

        Team movingTeam = movers[0].team;
        Vector2Int sourceCell = movers[0].GridPosition;

        if (movers.Any(e => e.team != movingTeam))
            return false;

        if (movers.Any(e => e.GridPosition != sourceCell))
            return false;

        if (IsCellBlocked(targetCell))
            return false;

        List<Entity> targetEntities = GetEntitiesAtCell(targetCell);

        if (targetEntities.Count == 0)
        {
            MoveGroup(movers, sourceCell, targetCell);
            return true;
        }

        bool hasEnemy = targetEntities.Any(e => e.team != movingTeam);
        if (hasEnemy)
        {
            ResolveCellAttack(sourceCell, targetCell, movingTeam);
            return true;
        }

        int futureCount = targetEntities.Count + movers.Count;
        if (futureCount > maxEntitiesPerCell)
            return false;

        MoveGroup(movers, sourceCell, targetCell);
        return true;
    }

    public void ResolveCellAttack(Vector2Int attackerCell, Vector2Int defenderCell, Team attackerTeam)
    {
        List<Entity> attackers = GetEntitiesAtCell(attackerCell)
            .Where(e => e.team == attackerTeam)
            .ToList();

        List<Entity> defenders = GetEntitiesAtCell(defenderCell)
            .Where(e => e.team != attackerTeam)
            .ToList();

        if (attackers.Count == 0 || defenders.Count == 0)
            return;

        Vector3 attackDirection = (GetCellCenterWorld(defenderCell) - GetCellCenterWorld(attackerCell)).normalized;

        foreach (Entity attacker in attackers)
        {
            if (attacker != null && !attacker.IsDead)
                attacker.PlayAttackLunge(attackDirection);
        }

        int totalAtk = 0;
        foreach (Entity attacker in attackers)
        {
            CharacterStats attackerStats = attacker.GetStatsComponent();
            if (attackerStats != null)
                totalAtk += Mathf.Max(0, attackerStats.Atk);
        }

        if (totalAtk <= 0)
            totalAtk = attackers.Count;

        DistributeDamage(defenders, totalAtk);
    }

    private void DistributeDamage(List<Entity> defenders, int totalIncomingDamage)
    {
        defenders = defenders.Where(e => e != null && !e.IsDead).ToList();
        if (defenders.Count == 0 || totalIncomingDamage <= 0) return;

        int livingCount = defenders.Count;
        int baseShare = totalIncomingDamage / livingCount;
        int remainder = totalIncomingDamage % livingCount;

        for (int i = 0; i < defenders.Count; i++)
        {
            if (defenders[i] == null || defenders[i].IsDead)
                continue;

            int incomingDamage = baseShare;
            if (i < remainder)
                incomingDamage += 1;

            CharacterStats defenderStats = defenders[i].GetStatsComponent();
            int finalDamage = incomingDamage;

            if (defenderStats != null)
                finalDamage = defenderStats.CalculateIncomingDamage(incomingDamage);

            defenders[i].ReceiveDamage(finalDamage);
        }
    }

    private void MoveGroup(List<Entity> movers, Vector2Int sourceCell, Vector2Int targetCell)
    {
        if (!grid.ContainsKey(sourceCell))
            return;

        CleanupCell(sourceCell);

        if (!grid.ContainsKey(sourceCell))
            return;

        if (!grid.ContainsKey(targetCell))
            grid[targetCell] = new List<Entity>();

        foreach (Entity mover in movers)
        {
            if (grid.ContainsKey(sourceCell))
                grid[sourceCell].Remove(mover);

            if (!grid[targetCell].Contains(mover))
                grid[targetCell].Add(mover);

            mover.SetGridPosition(targetCell);
        }

        CleanupCell(sourceCell);
        CleanupCell(targetCell);

        if (grid.ContainsKey(sourceCell))
            RefreshCellVisuals(sourceCell, false);

        if (grid.ContainsKey(targetCell))
            RefreshCellVisuals(targetCell, false);
    }

    public Vector3 GetCellCenterWorld(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }

    private void RefreshCellVisuals(Vector2Int cell, bool snapImmediately)
    {
        if (!grid.ContainsKey(cell))
            return;

        CleanupCell(cell);

        if (!grid.ContainsKey(cell))
            return;

        List<Entity> entities = grid[cell].Where(e => e != null && !e.IsDead).ToList();

        for (int i = 0; i < entities.Count; i++)
        {
            Vector3 targetPos = GetSlotWorldPosition(cell, i);
            entities[i].SetVisualTarget(targetPos, snapImmediately);
        }
    }

    private Vector3 GetSlotWorldPosition(Vector2Int cell, int index)
    {
        Vector3 center = GetCellCenterWorld(cell);

        switch (index)
        {
            case 0: return center + new Vector3(-slotOffset,  slotOffset, 0f);
            case 1: return center + new Vector3( slotOffset,  slotOffset, 0f);
            case 2: return center + new Vector3(-slotOffset, -slotOffset, 0f);
            case 3: return center + new Vector3( slotOffset, -slotOffset, 0f);
            default: return center;
        }
    }

    private void CleanupCell(Vector2Int cell)
    {
        if (!grid.ContainsKey(cell))
            return;

        grid[cell].RemoveAll(e => e == null || e.IsDead);

        if (grid[cell].Count == 0)
            grid.Remove(cell);
    }
}



using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [SerializeField] private EnemyAI enemyAI;

    public bool IsPlayerTurn { get; private set; } = true;
    private bool enemyTurnRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartEnemyTurn()
    {
        if (enemyTurnRunning) return;

        IsPlayerTurn = false;
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        enemyTurnRunning = true;

        if (enemyAI != null)
            yield return StartCoroutine(enemyAI.ExecuteEnemyTurn());

        enemyTurnRunning = false;
        IsPlayerTurn = true;
    }
}



public enum Team
{
    Player,
    Enemy
}


using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanelUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Entity targetEntity;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("Stat Texts")]
    [SerializeField] private TextMeshProUGUI hpValueText;
    [SerializeField] private TextMeshProUGUI atkValueText;
    [SerializeField] private TextMeshProUGUI defValueText;
    [SerializeField] private TextMeshProUGUI apValueText;
    [SerializeField] private TextMeshProUGUI critValueText;

    [Header("Buttons")]
    [SerializeField] private Button hpButton;
    [SerializeField] private Button atkButton;
    [SerializeField] private Button defButton;
    [SerializeField] private Button apButton;
    [SerializeField] private Button critButton;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;
    [SerializeField] private bool startHidden = true;

    private CharacterStats targetStats;
    private bool isOpen;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        isOpen = !startHidden;
        panelRoot.SetActive(isOpen);

        BindButtons();
    }

    private void Start()
    {
        ResolveTarget();
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOpen = !isOpen;
            panelRoot.SetActive(isOpen);

            if (isOpen)
                RefreshUI();
        }

        if (targetEntity == null || targetStats == null)
            ResolveTarget();

        if (isOpen)
            RefreshUI();
    }

    public void ConfigureReferences(
        GameObject newPanelRoot,
        TextMeshProUGUI newNameText,
        TextMeshProUGUI newLevelText,
        TextMeshProUGUI newXPText,
        TextMeshProUGUI newPointsText,
        TextMeshProUGUI newHPValueText,
        TextMeshProUGUI newATKValueText,
        TextMeshProUGUI newDEFValueText,
        TextMeshProUGUI newAPValueText,
        TextMeshProUGUI newCRITValueText,
        Button newHPButton,
        Button newATKButton,
        Button newDEFButton,
        Button newAPButton,
        Button newCRITButton)
    {
        panelRoot = newPanelRoot;

        nameText = newNameText;
        levelText = newLevelText;
        xpText = newXPText;
        pointsText = newPointsText;

        hpValueText = newHPValueText;
        atkValueText = newATKValueText;
        defValueText = newDEFValueText;
        apValueText = newAPValueText;
        critValueText = newCRITValueText;

        hpButton = newHPButton;
        atkButton = newATKButton;
        defButton = newDEFButton;
        apButton = newAPButton;
        critButton = newCRITButton;

        BindButtons();
        ResolveTarget();
        RefreshUI();
    }

    private void ResolveTarget()
    {
        if (targetEntity == null)
            targetEntity = FindFirstPlayerEntity();

        if (targetEntity != null)
            targetStats = targetEntity.GetStatsComponent();
    }

    private void BindButtons()
    {
        if (hpButton != null)
        {
            hpButton.onClick.RemoveAllListeners();
            hpButton.onClick.AddListener(OnClickHP);
        }

        if (atkButton != null)
        {
            atkButton.onClick.RemoveAllListeners();
            atkButton.onClick.AddListener(OnClickATK);
        }

        if (defButton != null)
        {
            defButton.onClick.RemoveAllListeners();
            defButton.onClick.AddListener(OnClickDEF);
        }

        if (apButton != null)
        {
            apButton.onClick.RemoveAllListeners();
            apButton.onClick.AddListener(OnClickAP);
        }

        if (critButton != null)
        {
            critButton.onClick.RemoveAllListeners();
            critButton.onClick.AddListener(OnClickCRIT);
        }
    }

    private void RefreshUI()
    {
        if (targetEntity == null || targetStats == null)
            return;

        StatBlock baseStats = targetStats.BaseStats;
        StatBlock levelBonus = targetStats.LevelBonus;
        StatBlock pointBonus = targetStats.PointBonus;
        StatBlock itemBonus = targetStats.ItemBonus;

        int finalHP = targetEntity.maxHP;
        int finalATK = targetEntity.attackDamage;
        int finalDEF = targetEntity.defense;
        int finalAP = targetEntity.actionPoints;
        float finalCRIT = targetEntity.critChance;

        if (nameText != null)
            nameText.text = targetEntity.name;

        if (levelText != null)
            levelText.text = $"Level: {targetEntity.Level}";

        if (xpText != null)
            xpText.text = $"XP: {targetEntity.CurrentXP} / {targetStats.GetXPToNextLevel()}";

        if (pointsText != null)
            pointsText.text = $"Points: {targetEntity.UnspentStatPoints}";

        if (hpValueText != null)
        {
            hpValueText.text =
                $"HP: {targetEntity.CurrentHP} / {finalHP}\n" +
                $"B{baseStats.hp} + L{levelBonus.hp} + P{pointBonus.hp} + I{itemBonus.hp}";
        }

        if (atkValueText != null)
        {
            atkValueText.text =
                $"ATK: {finalATK}\n" +
                $"B{baseStats.atk} + L{levelBonus.atk} + P{pointBonus.atk} + I{itemBonus.atk}";
        }

        if (defValueText != null)
        {
            defValueText.text =
                $"DEF: {finalDEF}\n" +
                $"B{baseStats.def} + L{levelBonus.def} + P{pointBonus.def} + I{itemBonus.def}";
        }

        if (apValueText != null)
        {
            apValueText.text =
                $"AP: {finalAP}\n" +
                $"B{baseStats.ap} + L{levelBonus.ap} + P{pointBonus.ap} + I{itemBonus.ap}";
        }

        if (critValueText != null)
        {
            critValueText.text =
                $"CRIT: {finalCRIT:0.#}%\n" +
                $"B{baseStats.crit:0.#} + L{levelBonus.crit:0.#} + P{pointBonus.crit:0.#} + I{itemBonus.crit:0.#}";
        }

        bool canSpend = targetEntity.UnspentStatPoints > 0;

        if (hpButton != null) hpButton.interactable = canSpend;
        if (atkButton != null) atkButton.interactable = canSpend;
        if (defButton != null) defButton.interactable = canSpend;
        if (apButton != null) apButton.interactable = canSpend;
        if (critButton != null) critButton.interactable = canSpend;
    }

    private void OnClickHP()
    {
        if (targetEntity == null) return;
        targetEntity.SpendPointOnHP(1);
        RefreshUI();
    }

    private void OnClickATK()
    {
        if (targetEntity == null) return;
        targetEntity.SpendPointOnATK(1);
        RefreshUI();
    }

    private void OnClickDEF()
    {
        if (targetEntity == null) return;
        targetEntity.SpendPointOnDEF(1);
        RefreshUI();
    }

    private void OnClickAP()
    {
        if (targetEntity == null) return;
        targetEntity.SpendPointOnAP(1);
        RefreshUI();
    }

    private void OnClickCRIT()
    {
        if (targetEntity == null) return;
        targetEntity.SpendPointOnCRIT(1f, 1);
        RefreshUI();
    }

    private Entity FindFirstPlayerEntity()
    {
        Entity[] allEntities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        foreach (Entity entity in allEntities)
        {
            if (entity != null && entity.team == Team.Player)
                return entity;
        }

        return null;
    }
}


using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class StatsPanelAutoBuilder : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform panelRoot;

    [Header("Controller")]
    [SerializeField] private StatsPanelUI statsPanelUI;

    [Header("Style")]
    [SerializeField] private Vector2 panelSize = new Vector2(320f, 420f);
    [SerializeField] private Vector2 panelPosition = new Vector2(180f, -220f);
    [SerializeField] private Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Vector2 rowSize = new Vector2(280f, 36f);
    [SerializeField] private Vector2 buttonSize = new Vector2(80f, 32f);
    [SerializeField] private float spacing = 8f;
    [SerializeField] private bool rebuildOnStart = true;

    private void Start()
    {
        if (rebuildOnStart)
            Build();
    }

    [ContextMenu("Build UI Layout")]
    public void Build()
    {
        if (panelRoot == null)
            panelRoot = GetComponent<RectTransform>();

        if (panelRoot == null)
            return;

        EnsurePanelVisual();
        ClearChildren(panelRoot);

        VerticalLayoutGroup rootLayout = EnsureComponent<VerticalLayoutGroup>(panelRoot.gameObject);
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.spacing = spacing;
        rootLayout.padding = new RectOffset(12, 12, 12, 12);

        ContentSizeFitter rootFitter = EnsureComponent<ContentSizeFitter>(panelRoot.gameObject);
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panelRoot.anchorMin = new Vector2(0f, 1f);
        panelRoot.anchorMax = new Vector2(0f, 1f);
        panelRoot.pivot = new Vector2(0.5f, 1f);
        panelRoot.anchoredPosition = panelPosition;
        panelRoot.sizeDelta = panelSize;

        TextMeshProUGUI nameText = CreateText(panelRoot, "NameText", "Character");
        TextMeshProUGUI levelText = CreateText(panelRoot, "LevelText", "Level: 1");
        TextMeshProUGUI xpText = CreateText(panelRoot, "XPText", "XP: 0 / 10");
        TextMeshProUGUI pointsText = CreateText(panelRoot, "PointsText", "Points: 0");

        StatRowRefs hp = CreateStatRow("HP", panelRoot);
        StatRowRefs atk = CreateStatRow("ATK", panelRoot);
        StatRowRefs def = CreateStatRow("DEF", panelRoot);
        StatRowRefs ap = CreateStatRow("AP", panelRoot);
        StatRowRefs crit = CreateStatRow("CRIT", panelRoot);

        WireController(
            nameText, levelText, xpText, pointsText,
            hp, atk, def, ap, crit
        );
    }

    private void WireController(
        TextMeshProUGUI nameText,
        TextMeshProUGUI levelText,
        TextMeshProUGUI xpText,
        TextMeshProUGUI pointsText,
        StatRowRefs hp,
        StatRowRefs atk,
        StatRowRefs def,
        StatRowRefs ap,
        StatRowRefs crit)
    {
        if (statsPanelUI == null)
            statsPanelUI = FindFirstObjectByType<StatsPanelUI>();

        if (statsPanelUI == null)
        {
            Debug.LogWarning("StatsPanelAutoBuilder: não encontrou StatsPanelUI na cena.");
            return;
        }

        statsPanelUI.ConfigureReferences(
            panelRoot.gameObject,
            nameText, levelText, xpText, pointsText,
            hp.valueText, atk.valueText, def.valueText, ap.valueText, crit.valueText,
            hp.button, atk.button, def.button, ap.button, crit.button
        );
    }

    private void EnsurePanelVisual()
    {
        Image image = EnsureComponent<Image>(panelRoot.gameObject);
        image.color = panelColor;
    }

    private StatRowRefs CreateStatRow(string statName, RectTransform parent)
    {
        GameObject row = new GameObject($"{statName}Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = rowSize;

        HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(row);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 2, 2);

        ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(row);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI valueText = CreateText(rowRect, $"{statName}ValueText", $"{statName}: 0");
        Button button = CreateButton(rowRect, $"{statName}Button", $"+ {statName}");

        return new StatRowRefs
        {
            valueText = valueText,
            button = button
        };
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string objectName, string textValue)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = rowSize;

        LayoutElement layout = EnsureComponent<LayoutElement>(go);
        layout.minHeight = 30f;
        layout.preferredHeight = 34f;
        layout.flexibleWidth = 1f;

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(go);
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return text;
    }

    private Button CreateButton(RectTransform parent, string objectName, string label)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = buttonSize;

        LayoutElement layout = EnsureComponent<LayoutElement>(go);
        layout.minWidth = buttonSize.x;
        layout.preferredWidth = buttonSize.x;
        layout.minHeight = buttonSize.y;
        layout.preferredHeight = buttonSize.y;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = EnsureComponent<TextMeshProUGUI>(textGO);
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private T EnsureComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    private class StatRowRefs
    {
        public TextMeshProUGUI valueText;
        public Button button;
    }
}


using System;
using UnityEngine;

[Serializable]
public class StatBlock
{
    [Header("Core")]
    public int hp;
    public int atk;
    public int def;
    public int ap;

    [Header("Combat")]
    [Range(0f, 100f)]
    public float crit;

    public StatBlock Clone()
    {
        return new StatBlock
        {
            hp = hp,
            atk = atk,
            def = def,
            ap = ap,
            crit = crit
        };
    }

    public static StatBlock Add(StatBlock a, StatBlock b)
    {
        return new StatBlock
        {
            hp = a.hp + b.hp,
            atk = a.atk + b.atk,
            def = a.def + b.def,
            ap = a.ap + b.ap,
            crit = a.crit + b.crit
        };
    }

    public void ClampAsFinalStats()
    {
        hp = Mathf.Max(1, hp);
        atk = Mathf.Max(0, atk);
        def = Mathf.Max(0, def);
        ap = Mathf.Max(0, ap);
        crit = Mathf.Clamp(crit, 0f, 100f);
    }
}


using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
[RequireComponent(typeof(EquipmentSlots))]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<InventoryItemEntry> items = new List<InventoryItemEntry>();

    public event Action OnInventoryChanged;

    private Entity entity;
    private EquipmentSlots equipmentSlots;

    public int InventorySize => inventorySize;
    public IReadOnlyList<InventoryItemEntry> Items => items;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        equipmentSlots = GetComponent<EquipmentSlots>();
        EnsureSize();
    }

    public InventoryItemEntry GetItem(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return items[index];
    }

    public bool HasEmptySlot()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return true;
        }

        return false;
    }

    public int GetFirstEmptySlotIndex()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return i;
        }

        return -1;
    }

    public bool AddStaticItem(ItemData item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromStatic(item));
    }

    public bool AddGeneratedItem(GeneratedItemInstance item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromGenerated(item));
    }

    public bool AddEntry(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        EnsureSize();

        int emptyIndex = GetFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        items[emptyIndex] = entry.Clone();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (!IsValidIndex(index))
            return false;

        if (items[index] == null || items[index].IsEmpty)
            return false;

        items[index] = new InventoryItemEntry();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool MoveItem(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            return false;

        if (fromIndex == toIndex)
            return false;

        EnsureSize();

        InventoryItemEntry from = items[fromIndex];
        InventoryItemEntry to = items[toIndex];

        items[toIndex] = from;
        items[fromIndex] = to;

        NormalizeSlot(fromIndex);
        NormalizeSlot(toIndex);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipFromInventory(int index)
    {
        if (!IsValidIndex(index))
            return false;

        InventoryItemEntry entry = items[index];

        if (entry == null || entry.IsEmpty)
            return false;

        if (entity == null)
            entity = GetComponent<Entity>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (entity == null || equipmentSlots == null)
            return false;

        if (entity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(entry.SlotType);

        if (equippedEntry != null && !equippedEntry.IsEmpty)
        {
            int emptyIndex = GetFirstEmptySlotIndex();
            if (emptyIndex < 0 || emptyIndex == index)
            {
                int swapIndex = index;
                items[swapIndex] = equippedEntry.Clone();
            }
            else
            {
                items[emptyIndex] = equippedEntry.Clone();
                NormalizeSlot(emptyIndex);
                items[index] = new InventoryItemEntry();
            }
        }
        else
        {
            items[index] = new InventoryItemEntry();
        }

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = entity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
        {
            items[index] = entry.Clone();
            OnInventoryChanged?.Invoke();
            return false;
        }

        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventory(EquipmentSlotType slotType)
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (equipmentSlots == null)
            return false;

        if (!HasEmptySlot())
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        bool added = AddEntry(equippedEntry);
        if (!added)
            return false;

        entity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventoryItemEntry GetEquippedEntry(EquipmentSlotType slotType)
    {
        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

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

    public bool TryEquipEntryDirectly(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        if (entity == null)
            entity = GetComponent<Entity>();

        if (equipmentSlots == null)
            equipmentSlots = GetComponent<EquipmentSlots>();

        if (entity == null || equipmentSlots == null)
            return false;

        if (entity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(entry.SlotType);

        if (equippedEntry != null && !equippedEntry.IsEmpty && !HasEmptySlot())
            return false;

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = entity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = entity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        if (equippedEntry != null && !equippedEntry.IsEmpty)
            AddEntry(equippedEntry);

        OnInventoryChanged?.Invoke();
        return true;
    }

    private void EnsureSize()
    {
        if (inventorySize < 1)
            inventorySize = 1;

        if (items == null)
            items = new List<InventoryItemEntry>();

        while (items.Count < inventorySize)
            items.Add(new InventoryItemEntry());

        while (items.Count > inventorySize)
            items.RemoveAt(items.Count - 1);

        for (int i = 0; i < items.Count; i++)
            NormalizeSlot(i);
    }

    private void NormalizeSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (items[index] == null)
            items[index] = new InventoryItemEntry();
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < items.Count;
    }
}


using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyInventory : MonoBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<InventoryItemEntry> items = new List<InventoryItemEntry>();

    public event Action OnInventoryChanged;

    public int InventorySize => inventorySize;
    public IReadOnlyList<InventoryItemEntry> Items => items;

    private void Awake()
    {
        EnsureSize();
    }

    public InventoryItemEntry GetItem(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return items[index];
    }

    public bool IsSlotEmpty(int index)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();
        return items[index] == null || items[index].IsEmpty;
    }

    public bool HasEmptySlot()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return true;
        }

        return false;
    }

    public int GetFirstEmptySlotIndex()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return i;
        }

        return -1;
    }

    public bool AddStaticItem(ItemData item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromStatic(item));
    }

    public bool AddGeneratedItem(GeneratedItemInstance item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromGenerated(item));
    }

    public bool AddEntry(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        EnsureSize();

        int emptyIndex = GetFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        items[emptyIndex] = entry.Clone();
        NormalizeSlot(emptyIndex);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool AddEntryToIndex(InventoryItemEntry entry, int index)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        if (items[index] != null && !items[index].IsEmpty)
            return false;

        items[index] = entry.Clone();
        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        if (items[index] == null || items[index].IsEmpty)
            return false;

        items[index] = new InventoryItemEntry();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool MoveItem(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            return false;

        EnsureSize();

        if (fromIndex == toIndex)
            return false;

        if (items[fromIndex] == null || items[fromIndex].IsEmpty)
            return false;

        InventoryItemEntry from = items[fromIndex];
        InventoryItemEntry to = items[toIndex];

        items[toIndex] = from;
        items[fromIndex] = to;

        NormalizeSlot(fromIndex);
        NormalizeSlot(toIndex);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventoryItemEntry GetEquippedEntry(Entity targetEntity, EquipmentSlotType slotType)
    {
        if (targetEntity == null)
            return null;

        EquipmentSlots equipmentSlots = targetEntity.GetComponent<EquipmentSlots>();
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

    public bool TryEquipFromInventory(Entity targetEntity, int index)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (targetEntity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(targetEntity, entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = targetEntity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = targetEntity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        items[index] = new InventoryItemEntry();

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = targetEntity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = targetEntity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                items[index] = entry.Clone();
                NormalizeSlot(index);

                if (!restored)
                    Debug.LogWarning("PartyInventory: falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipFromInventoryToSlot(Entity targetEntity, int index, EquipmentSlotType targetSlotType)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipFromInventory(targetEntity, index);
    }

    public bool UnequipToInventory(Entity targetEntity, EquipmentSlotType slotType)
    {
        if (targetEntity == null)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(targetEntity, slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        if (!AddEntry(equippedEntry))
            return false;

        targetEntity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventorySlot(Entity targetEntity, EquipmentSlotType slotType, int inventoryIndex)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(inventoryIndex))
            return false;

        EnsureSize();

        if (items[inventoryIndex] != null && !items[inventoryIndex].IsEmpty)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(targetEntity, slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        items[inventoryIndex] = equippedEntry.Clone();
        NormalizeSlot(inventoryIndex);

        targetEntity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectly(Entity targetEntity, InventoryItemEntry entry)
    {
        if (targetEntity == null || entry == null || entry.IsEmpty)
            return false;

        if (targetEntity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(targetEntity, entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = targetEntity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = targetEntity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = targetEntity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = targetEntity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                if (!restored)
                    Debug.LogWarning("PartyInventory: falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectlyToSlot(Entity targetEntity, InventoryItemEntry entry, EquipmentSlotType targetSlotType)
    {
        if (targetEntity == null || entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipEntryDirectly(targetEntity, entry);
    }

    private void EnsureSize()
    {
        if (inventorySize < 1)
            inventorySize = 1;

        if (items == null)
            items = new List<InventoryItemEntry>();

        while (items.Count < inventorySize)
            items.Add(new InventoryItemEntry());

        while (items.Count > inventorySize)
            items.RemoveAt(items.Count - 1);

        for (int i = 0; i < items.Count; i++)
            NormalizeSlot(i);
    }

    private void NormalizeSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (items[index] == null)
            items[index] = new InventoryItemEntry();
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < items.Count;
    }
}



using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootWindowUI : MonoBehaviour
{
    public static LootWindowUI Instance;

    [Header("Window")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private Transform equippedContentRoot;
    [SerializeField] private Transform inventoryContentRoot;
    [SerializeField] private Transform groundLootContentRoot;

    [Header("Prefab")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;

    [Header("Input")]
    [SerializeField] private KeyCode toggleLootKey = KeyCode.E;
    [SerializeField] private KeyCode closeLootKey = KeyCode.Escape;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 0.35f;

    private Entity currentEntity;
    private PlayerInventory currentInventory;
    private bool isOpen = false;

    private readonly List<GameObject> spawnedUI = new List<GameObject>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoFindReferences();
        BindCloseButton();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleLootKey))
        {
            if (isOpen)
            {
                CloseWindow();
            }
            else
            {
                TryOpenForFirstPlayer();
            }

            return;
        }

        if (Input.GetKeyDown(closeLootKey) && isOpen)
        {
            CloseWindow();
            return;
        }

        if (!isOpen)
            return;

        if (currentEntity == null || currentInventory == null || currentEntity.IsDead)
            CloseWindow();
    }

    public void ConfigureReferences(
        GameObject newWindowRoot,
        Button newCloseButton,
        Transform newEquippedContentRoot,
        Transform newInventoryContentRoot,
        Transform newGroundLootContentRoot,
        TMP_Text newTitleText,
        TMP_Text newHintText)
    {
        windowRoot = newWindowRoot;
        closeButton = newCloseButton;
        equippedContentRoot = newEquippedContentRoot;
        inventoryContentRoot = newInventoryContentRoot;
        groundLootContentRoot = newGroundLootContentRoot;
        titleText = newTitleText;
        hintText = newHintText;

        AutoFindReferences();
        BindCloseButton();
    }

    public void SetItemButtonPrefab(ItemButtonUI prefab)
    {
        itemButtonPrefab = prefab;
    }

    public void OpenForCell(Entity entity, PlayerInventory inventory, Vector2Int cell)
    {
        AutoFindReferences();

        if (entity == null || inventory == null)
            return;

        if (itemButtonPrefab == null)
            return;

        if (equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
            return;

        currentEntity = entity;
        currentInventory = inventory;
        isOpen = true;

        if (windowRoot != null)
            windowRoot.SetActive(true);

        RefreshUI();
    }

    public void CloseWindow()
    {
        isOpen = false;
        currentEntity = null;
        currentInventory = null;

        ClearSpawnedUI();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void TryOpenForFirstPlayer()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            if (entity == null || entity.IsDead || entity.team != Team.Player)
                continue;

            PlayerInventory inventory = entity.GetComponent<PlayerInventory>();
            if (inventory == null)
                continue;

            OpenForCell(entity, inventory, entity.GridPosition);
            return;
        }
    }

    private void AutoFindReferences()
    {
        if (closeButton == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "CloseButton");
            if (t != null)
                closeButton = t.GetComponent<Button>();
        }

        if (equippedContentRoot == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "EquippedContent");
            if (t != null)
                equippedContentRoot = t;
        }

        if (inventoryContentRoot == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "InventoryContent");
            if (t != null)
                inventoryContentRoot = t;
        }

        if (groundLootContentRoot == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "GroundLootContent");
            if (t != null)
                groundLootContentRoot = t;
        }

        if (titleText == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "TitleText");
            if (t != null)
                titleText = t.GetComponent<TMP_Text>();
        }

        if (hintText == null)
        {
            Transform t = FindDeepChild(windowRoot != null ? windowRoot.transform : null, "HintText");
            if (t != null)
                hintText = t.GetComponent<TMP_Text>();
        }
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseWindow);
    }

    private void RefreshUI()
    {
        if (!isOpen || currentEntity == null || currentInventory == null)
            return;

        AutoFindReferences();

        if (itemButtonPrefab == null)
            return;

        if (equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
            return;

        ClearSpawnedUI();

        if (titleText != null)
            titleText.text = $"Inventory - {currentEntity.name}";

        if (hintText != null)
            hintText.text = "Only icon in slot | Mouse hover shows tooltip | Click chão -> mochila | Shift+Click chão -> equipar";

        BuildEquippedSection();
        BuildInventorySection();
        BuildGroundLootSection();
    }

    private void BuildEquippedSection()
    {
        CreateEquippedButton(EquipmentSlotType.Weapon);
        CreateEquippedButton(EquipmentSlotType.Armor);
        CreateEquippedButton(EquipmentSlotType.Accessory);
    }

    private void CreateEquippedButton(EquipmentSlotType slotType)
    {
        InventoryItemEntry equipped = currentInventory.GetEquippedEntry(slotType);

        ItemButtonUI button = Instantiate(itemButtonPrefab, equippedContentRoot);

        button.Setup(
            equipped,
            equipped == null || equipped.IsEmpty
                ? null
                : () =>
                {
                    currentInventory.UnequipToInventory(slotType);
                    RefreshUI();
                }
        );

        spawnedUI.Add(button.gameObject);
    }

    private void BuildInventorySection()
    {
        IReadOnlyList<InventoryItemEntry> items = currentInventory.Items;

        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            InventoryItemEntry entry = items[index];

            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryContentRoot);

            if (entry == null || entry.IsEmpty)
            {
                button.Setup(null, null, null);
            }
            else
            {
                button.Setup(
                    entry,
                    () =>
                    {
                        currentInventory.TryEquipFromInventory(index);
                        RefreshUI();
                    },
                    () =>
                    {
                        currentInventory.TryEquipFromInventory(index);
                        RefreshUI();
                    }
                );
            }

            spawnedUI.Add(button.gameObject);
        }
    }

    private void BuildGroundLootSection()
    {
        List<GroundItem> items = GetGroundItemsInCell(currentEntity.GridPosition);

        int maxGroundSlots = 20;

        for (int i = 0; i < maxGroundSlots; i++)
        {
            if (i >= items.Count || items[i] == null)
            {
                ItemButtonUI emptyButton = Instantiate(itemButtonPrefab, groundLootContentRoot);
                emptyButton.Setup(null, null, null);
                spawnedUI.Add(emptyButton.gameObject);
                continue;
            }

            GroundItem groundItem = items[i];
            InventoryItemEntry entry = groundItem.ToInventoryEntry();

            ItemButtonUI button = Instantiate(itemButtonPrefab, groundLootContentRoot);

            button.Setup(
                entry,
                () =>
                {
                    bool moved = groundItem.TrySendToInventory(currentInventory);
                    if (moved)
                        RefreshUI();
                },
                () =>
                {
                    bool equipped = groundItem.TryEquipDirect(currentEntity, currentInventory);
                    if (equipped)
                        RefreshUI();
                }
            );

            spawnedUI.Add(button.gameObject);
        }
    }

    private List<GroundItem> GetGroundItemsInCell(Vector2Int cell)
    {
        List<GroundItem> result = new List<GroundItem>();

        if (GridManager.Instance == null)
            return result;

        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);

        if (hits == null)
            return result;

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem item = hits[i].GetComponent<GroundItem>();
            if (item != null)
                result.Add(item);
        }

        return result;
    }

    private void ClearSpawnedUI()
    {
        for (int i = 0; i < spawnedUI.Count; i++)
        {
            if (spawnedUI[i] != null)
                Destroy(spawnedUI[i]);
        }

        spawnedUI.Clear();
    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}


using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class LootWindowGridAutoBuilder : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform windowRoot;

    [Header("Controller")]
    [SerializeField] private LootWindowUI lootWindowUI;

    [Header("Prefab")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Window")]
    [SerializeField] private Vector2 windowSize = new Vector2(1080f, 720f);
    [SerializeField] private Color windowColor = new Color(0.08f, 0.08f, 0.08f, 0.94f);

    [Header("Panels")]
    [SerializeField] private Color panelColor = new Color(0.14f, 0.14f, 0.14f, 0.96f);

    [Header("Slots")]
    [SerializeField] private Vector2 slotSize = new Vector2(92f, 92f);
    [SerializeField] private Vector2 equipmentSlotSize = new Vector2(110f, 110f);
    [SerializeField] private Vector2 gridSpacing = new Vector2(8f, 8f);

    [Header("Text")]
    [SerializeField] private int titleFontSize = 30;
    [SerializeField] private int hintFontSize = 18;
    [SerializeField] private int headerFontSize = 22;

    [Header("Build")]
    [SerializeField] private bool rebuildOnStart = true;

    private void Start()
    {
        if (rebuildOnStart)
            Build();
    }

    [ContextMenu("Build Loot Grid Layout")]
    public void Build()
    {
        if (windowRoot == null)
            windowRoot = GetComponent<RectTransform>();

        if (lootWindowUI == null)
            lootWindowUI = GetComponent<LootWindowUI>();

        if (windowRoot == null)
            return;

        EnsureRootVisual();
        ClearChildren(windowRoot);

        windowRoot.anchorMin = new Vector2(0.5f, 0.5f);
        windowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        windowRoot.pivot = new Vector2(0.5f, 0.5f);
        windowRoot.anchoredPosition = Vector2.zero;
        windowRoot.sizeDelta = windowSize;

        TextMeshProUGUI titleText = CreateText(
            "TitleText",
            windowRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(20f, -10f),
            new Vector2(-80f, -52f),
            "Inventory",
            titleFontSize,
            TextAlignmentOptions.Left
        );

        TextMeshProUGUI hintText = CreateText(
            "HintText",
            windowRoot,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(20f, -56f),
            new Vector2(-20f, -92f),
            "E abre/fecha | Esc fecha | Click chão -> mochila | Shift+Click chão -> equipar | Click mochila -> equipar | Click equipado -> mochila",
            hintFontSize,
            TextAlignmentOptions.Left
        );

        Button closeButton = CreateButton(
            "CloseButton",
            windowRoot,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-60f, -14f),
            new Vector2(-14f, -58f),
            "X"
        );

        RectTransform equippedPanel = CreatePanel(
            "EquippedPanel",
            windowRoot,
            new Vector2(0f, 0f),
            new Vector2(0.22f, 1f),
            new Vector2(20f, 20f),
            new Vector2(-10f, -110f)
        );

        RectTransform inventoryPanel = CreatePanel(
            "InventoryPanel",
            windowRoot,
            new Vector2(0.22f, 0f),
            new Vector2(0.61f, 1f),
            new Vector2(10f, 20f),
            new Vector2(-10f, -110f)
        );

        RectTransform groundPanel = CreatePanel(
            "GroundLootPanel",
            windowRoot,
            new Vector2(0.61f, 0f),
            new Vector2(1f, 1f),
            new Vector2(10f, 20f),
            new Vector2(-20f, -110f)
        );

        CreateSectionHeader("EquippedHeader", equippedPanel, "Equipped");
        CreateSectionHeader("InventoryHeader", inventoryPanel, "Inventory");
        CreateSectionHeader("GroundHeader", groundPanel, "Ground Loot");

        RectTransform equippedContent = CreateEquipmentContent("EquippedContent", equippedPanel);
        RectTransform inventoryContent = CreateGridContent("InventoryContent", inventoryPanel, 4);
        RectTransform groundContent = CreateGridContent("GroundLootContent", groundPanel, 4);

        if (lootWindowUI != null)
        {
            lootWindowUI.ConfigureReferences(
                windowRoot.gameObject,
                closeButton,
                equippedContent,
                inventoryContent,
                groundContent,
                titleText,
                hintText
            );

            lootWindowUI.SetItemButtonPrefab(itemButtonPrefab);
        }
    }

    private void EnsureRootVisual()
    {
        Image image = GetOrAdd<Image>(windowRoot.gameObject);
        image.color = windowColor;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = go.GetComponent<Image>();
        image.color = panelColor;

        return rect;
    }

    private void CreateSectionHeader(string name, RectTransform parent, string text)
    {
        CreateText(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(10f, -10f),
            new Vector2(-10f, -40f),
            text,
            headerFontSize,
            TextAlignmentOptions.Left
        );
    }

    private RectTransform CreateEquipmentContent(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(12f, 12f);
        rect.offsetMax = new Vector2(-12f, -50f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(go);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(go);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    private RectTransform CreateGridContent(string name, RectTransform parent, int constraintCount)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(12f, 12f);
        rect.offsetMax = new Vector2(-12f, -50f);

        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(go);
        grid.cellSize = slotSize;
        grid.spacing = gridSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = constraintCount;

        return rect;
    }

    private TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string content,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(go);
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        return tmp;
    }

    private Button CreateButton(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(textGO);
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }
}


using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class LootDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject groundItemPrefab;
    [SerializeField] private List<LootDropEntry> lootTable = new List<LootDropEntry>();

    private Entity entity;
    private bool dropped = false;

    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    private void OnEnable()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (entity != null)
            entity.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (entity != null)
            entity.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (dropped)
            return;

        dropped = true;
        TryDropLoot();
    }

    private void TryDropLoot()
    {
        if (groundItemPrefab == null)
            return;

        if (lootTable == null || lootTable.Count == 0)
            return;

        List<LootDropEntry> validDrops = new List<LootDropEntry>();

        for (int i = 0; i < lootTable.Count; i++)
        {
            if (lootTable[i] == null)
                continue;

            if (!lootTable[i].HasValidItemSource())
                continue;

            if (lootTable[i].RollDrop())
                validDrops.Add(lootTable[i]);
        }

        if (validDrops.Count == 0)
            return;

        LootDropEntry chosen = validDrops[Random.Range(0, validDrops.Count)];
        SpawnGroundItem(chosen);
    }

    private void SpawnGroundItem(LootDropEntry entry)
    {
        if (entry == null)
            return;

        Vector2Int cell = entity != null ? entity.GridPosition : Vector2Int.zero;
        Vector3 worldPos = GridManager.Instance != null
            ? GridManager.Instance.GetCellCenterWorld(cell)
            : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

        GameObject instance = Instantiate(groundItemPrefab, worldPos, Quaternion.identity);

        GroundItem groundItem = instance.GetComponent<GroundItem>();
        if (groundItem == null)
        {
            Destroy(instance);
            return;
        }

        if (entry.staticItem != null)
        {
            groundItem.SetupStatic(entry.staticItem);
            return;
        }

        if (entry.generatedProfile != null)
        {
            GeneratedItemInstance generated = ItemGenerator.Generate(entry.generatedProfile);
            groundItem.SetupGenerated(generated);
        }
    }
}


using System;
using UnityEngine;

[Serializable]
public class LootDropEntry
{
    [Header("Source")]
    public ItemData staticItem;
    public ItemGenerationProfile generatedProfile;

    [Header("Chance")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;

    public bool HasValidItemSource()
    {
        return staticItem != null || generatedProfile != null;
    }

    public bool RollDrop()
    {
        return UnityEngine.Random.value <= dropChance;
    }
}


using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
            root = gameObject;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundImage = GetComponent<Image>();

        SetupVisuals();
        Hide();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf)
            return;

        Vector2 localPoint;
        RectTransform canvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;

        if (canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                out localPoint))
        {
            panelRect.anchoredPosition = localPoint + offset;
            panelRect.SetAsLastSibling();
        }
    }

    public void Show(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
        {
            Hide();
            return;
        }

        if (nameText != null)
            nameText.text = entry.ItemName;

        if (statsText != null)
            statsText.text = BuildStatsText(entry.StatBonus);

        SetupVisuals();
        root.SetActive(true);
        panelRect.SetAsLastSibling();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void SetupVisuals()
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        }

        if (nameText != null)
        {
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.margin = new Vector4(10f, 10f, 10f, 0f);
        }

        if (statsText != null)
        {
            statsText.color = Color.white;
            statsText.raycastTarget = false;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.textWrappingMode = TextWrappingModes.Normal;
            statsText.margin = new Vector4(10f, 30f, 10f, 10f);
        }
    }

    private string BuildStatsText(StatBlock stats)
    {
        if (stats == null)
            return "";

        string text = "";

        if (stats.hp != 0) text += $"HP: {stats.hp}\n";
        if (stats.atk != 0) text += $"ATK: {stats.atk}\n";
        if (stats.def != 0) text += $"DEF: {stats.def}\n";
        if (stats.ap != 0) text += $"AP: {stats.ap}\n";
        if (Mathf.Abs(stats.crit) > 0.001f) text += $"CRIT: {stats.crit:0.#}%\n";

        if (string.IsNullOrWhiteSpace(text))
            text = "No bonus";

        return text.TrimEnd();
    }
}


using UnityEngine;

public static class ItemGenerator
{
    public static GeneratedItemInstance Generate(ItemGenerationProfile profile)
    {
        if (profile == null)
            return null;

        StatBlock rolledStats = profile.RollStats();

        GeneratedItemInstance item = new GeneratedItemInstance
        {
            itemName = BuildName(profile, rolledStats),
            description = BuildDescription(profile, rolledStats),
            icon = profile.icon,
            slotType = profile.slotType,
            rarity = profile.rarity,
            requiredLevel = Mathf.Max(1, profile.requiredLevel),
            value = Mathf.Max(0, profile.value),
            statBonus = rolledStats
        };

        return item;
    }

    private static string BuildName(ItemGenerationProfile profile, StatBlock stats)
    {
        string suffix = GetPrimaryStatSuffix(stats);
        return $"{profile.generatedNamePrefix} {suffix}".Trim();
    }

    private static string BuildDescription(ItemGenerationProfile profile, StatBlock stats)
    {
        return $"{profile.rarity} {profile.slotType}";
    }

    private static string GetPrimaryStatSuffix(StatBlock stats)
    {
        int bestIntValue = stats.hp;
        string bestName = "Vitality";

        if (stats.atk > bestIntValue)
        {
            bestIntValue = stats.atk;
            bestName = "Power";
        }

        if (stats.def > bestIntValue)
        {
            bestIntValue = stats.def;
            bestName = "Guard";
        }

        if (stats.ap > bestIntValue)
        {
            bestIntValue = stats.ap;
            bestName = "Focus";
        }

        if (stats.crit > bestIntValue)
            bestName = "Precision";

        return bestName;
    }
}


using UnityEngine;

[CreateAssetMenu(fileName = "NewItemGenerationProfile", menuName = "RPG/Item Generation Profile")]
public class ItemGenerationProfile : ScriptableObject
{
    [Header("Identity")]
    public string generatedNamePrefix = "Generated";

    [Header("Visual")]
    public Sprite icon;

    [Header("Base")]
    public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
    public ItemRarity rarity = ItemRarity.Common;
    public int requiredLevel = 1;
    public int value = 10;

    [Header("Stat Ranges")]
    public Vector2Int hpRange = new Vector2Int(0, 0);
    public Vector2Int atkRange = new Vector2Int(0, 0);
    public Vector2Int defRange = new Vector2Int(0, 0);
    public Vector2Int apRange = new Vector2Int(0, 0);
    public Vector2 critRange = new Vector2(0f, 0f);

    public StatBlock RollStats()
    {
        return new StatBlock
        {
            hp = Random.Range(Mathf.Min(hpRange.x, hpRange.y), Mathf.Max(hpRange.x, hpRange.y) + 1),
            atk = Random.Range(Mathf.Min(atkRange.x, atkRange.y), Mathf.Max(atkRange.x, atkRange.y) + 1),
            def = Random.Range(Mathf.Min(defRange.x, defRange.y), Mathf.Max(defRange.x, defRange.y) + 1),
            ap = Random.Range(Mathf.Min(apRange.x, apRange.y), Mathf.Max(apRange.x, apRange.y) + 1),
            crit = Random.Range(Mathf.Min(critRange.x, critRange.y), Mathf.Max(critRange.x, critRange.y))
        };
    }
}



public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum EquipmentSlotType
{
    Weapon,
    Armor,
    Accessory
}


using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";
    [TextArea] public string description = "";

    [Header("Visual")]
    public Sprite icon;

    [Header("Classification")]
    public EquipmentSlotType slotType = EquipmentSlotType.Weapon;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Requirements")]
    public int requiredLevel = 1;

    [Header("Economy")]
    public int value = 10;

    [Header("Stat Bonus")]
    public StatBlock statBonus = new StatBlock();
}


using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class ItemButtonUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Style")]
    [SerializeField] private float preferredWidth = 92f;
    [SerializeField] private float preferredHeight = 92f;
    [SerializeField] private float minWidth = 92f;
    [SerializeField] private float minHeight = 92f;
    [SerializeField] private Color normalColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.14f, 0.14f, 0.14f, 1f);
    [SerializeField] private Color enabledIconColor = Color.white;
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);

    private Action normalClickAction;
    private Action shiftClickAction;
    private Action beginDragAction;
    private Action endDragAction;
    private InventoryItemEntry tooltipEntry;

    private bool interactable = true;
    private Transform originalParent;
    private int originalSiblingIndex;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureIcon();

        if (layoutElement != null)
        {
            layoutElement.minWidth = minWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 0f;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.transition = Selectable.Transition.ColorTint;
        }

        ApplyInteractableVisual(true, true);
    }

    public void Setup(
        InventoryItemEntry entry,
        Action onNormalClick,
        Action onShiftClick = null,
        Action onBeginDrag = null,
        Action onEndDrag = null)
    {
        tooltipEntry = entry;
        normalClickAction = onNormalClick;
        shiftClickAction = onShiftClick;
        beginDragAction = onBeginDrag;
        endDragAction = onEndDrag;

        bool isEmpty = entry == null || entry.IsEmpty;
        bool hasAnyAction = onNormalClick != null || onShiftClick != null || onBeginDrag != null;

        if (iconImage != null)
        {
            iconImage.sprite = isEmpty ? null : entry.Icon;
            iconImage.color = isEmpty || entry.Icon == null ? emptyIconColor : enabledIconColor;
            iconImage.enabled = !isEmpty && entry.Icon != null;
        }

        ApplyInteractableVisual(hasAnyAction, isEmpty);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable)
            return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shiftPressed && shiftClickAction != null)
        {
            shiftClickAction.Invoke();
            return;
        }

        normalClickAction?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactable || beginDragAction == null)
            return;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvasGroup.blocksRaycasts = false;
        beginDragAction.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactable || beginDragAction == null)
            return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!interactable || beginDragAction == null)
            return;

        canvasGroup.blocksRaycasts = true;

        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        endDragAction?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipEntry != null && !tooltipEntry.IsEmpty && ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Show(tooltipEntry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private void EnsureIcon()
    {
        if (iconImage != null)
            return;

        Transform existing = transform.Find("Icon");
        if (existing != null)
        {
            iconImage = existing.GetComponent<Image>();
            return;
        }

        GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);

        iconImage = go.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void ApplyInteractableVisual(bool canClick, bool isEmpty)
    {
        interactable = canClick;

        if (button != null)
            button.interactable = true;

        if (backgroundImage != null)
            backgroundImage.color = isEmpty ? emptyColor : normalColor;
    }
}


using System;
using UnityEngine;

[Serializable]
public class InventoryItemEntry
{
    [SerializeField] private ItemData staticItem;
    [SerializeField] private GeneratedItemInstance generatedItem;

    public ItemData StaticItem => staticItem;
    public GeneratedItemInstance GeneratedItem => generatedItem;

    public bool IsEmpty => staticItem == null && generatedItem == null;
    public bool IsStaticItem => staticItem != null;
    public bool IsGeneratedItem => generatedItem != null;

    public EquipmentSlotType SlotType
    {
        get
        {
            if (staticItem != null)
                return staticItem.slotType;

            if (generatedItem != null)
                return generatedItem.slotType;

            return EquipmentSlotType.Weapon;
        }
    }

    public string ItemName
    {
        get
        {
            if (staticItem != null)
                return staticItem.itemName;

            if (generatedItem != null)
                return generatedItem.itemName;

            return "";
        }
    }

    public Sprite Icon
    {
        get
        {
            if (staticItem != null)
                return staticItem.icon;

            if (generatedItem != null)
                return generatedItem.icon;

            return null;
        }
    }

    public int RequiredLevel
    {
        get
        {
            if (staticItem != null)
                return staticItem.requiredLevel;

            if (generatedItem != null)
                return generatedItem.requiredLevel;

            return 1;
        }
    }

    public StatBlock StatBonus
    {
        get
        {
            if (staticItem != null && staticItem.statBonus != null)
                return staticItem.statBonus;

            if (generatedItem != null && generatedItem.statBonus != null)
                return generatedItem.statBonus;

            return new StatBlock();
        }
    }

    public static InventoryItemEntry FromStatic(ItemData item)
    {
        if (item == null)
            return null;

        return new InventoryItemEntry
        {
            staticItem = item,
            generatedItem = null
        };
    }

    public static InventoryItemEntry FromGenerated(GeneratedItemInstance item)
    {
        if (item == null)
            return null;

        return new InventoryItemEntry
        {
            staticItem = null,
            generatedItem = item.Clone()
        };
    }

    public InventoryItemEntry Clone()
    {
        if (IsStaticItem)
            return FromStatic(staticItem);

        if (IsGeneratedItem)
            return FromGenerated(generatedItem);

        return new InventoryItemEntry();
    }

    public void Clear()
    {
        staticItem = null;
        generatedItem = null;
    }
}


using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GroundItem : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ItemData staticItem;
    [SerializeField] private GeneratedItemInstance generatedItem;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color staticItemColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private Color generatedItemColor = new Color(1f, 0.85f, 0.2f, 1f);

    private bool consumed = false;

    public bool HasStaticItem => staticItem != null;
    public bool HasGeneratedItem => generatedItem != null;

    public ItemData StaticItem => staticItem;
    public GeneratedItemInstance GeneratedItem => generatedItem;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        RefreshVisual();
    }

    public void SetupStatic(ItemData item)
    {
        staticItem = item;
        generatedItem = null;
        consumed = false;
        RefreshVisual();
    }

    public void SetupGenerated(GeneratedItemInstance item)
    {
        generatedItem = item != null ? item.Clone() : null;
        staticItem = null;
        consumed = false;
        RefreshVisual();
    }

    public string GetDisplayName()
    {
        if (staticItem != null)
            return staticItem.itemName;

        if (generatedItem != null)
            return generatedItem.itemName;

        return "Empty";
    }

    public InventoryItemEntry ToInventoryEntry()
    {
        if (staticItem != null)
            return InventoryItemEntry.FromStatic(staticItem);

        if (generatedItem != null)
            return InventoryItemEntry.FromGenerated(generatedItem);

        return null;
    }

    public bool TrySendToInventory(PlayerInventory inventory)
    {
        if (consumed || inventory == null)
            return false;

        bool added = false;

        if (staticItem != null)
            added = inventory.AddStaticItem(staticItem);
        else if (generatedItem != null)
            added = inventory.AddGeneratedItem(generatedItem);

        if (!added)
            return false;

        Consume();
        return true;
    }

    public bool TryEquipDirect(Entity entity, PlayerInventory inventory)
    {
        if (consumed || entity == null || inventory == null)
            return false;

        InventoryItemEntry entry = ToInventoryEntry();
        if (entry == null)
            return false;

        bool equipped = inventory.TryEquipEntryDirectly(entry);

        if (!equipped)
            return false;

        Consume();
        return true;
    }

    private void Consume()
    {
        if (consumed)
            return;

        consumed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
            return;

        if (generatedItem != null)
            spriteRenderer.color = generatedItemColor;
        else if (staticItem != null)
            spriteRenderer.color = staticItemColor;
    }
}


using System;
using UnityEngine;

[Serializable]
public class GeneratedItemInstance
{
    public string itemName;
    public string description;
    public Sprite icon;
    public EquipmentSlotType slotType;
    public ItemRarity rarity;
    public int requiredLevel;
    public int value;
    public StatBlock statBonus;

    public GeneratedItemInstance Clone()
    {
        return new GeneratedItemInstance
        {
            itemName = itemName,
            description = description,
            icon = icon,
            slotType = slotType,
            rarity = rarity,
            requiredLevel = requiredLevel,
            value = value,
            statBonus = statBonus != null ? statBonus.Clone() : new StatBlock()
        };
    }
}


using System;
using UnityEngine;

public class EquipmentSlots : MonoBehaviour
{
    [Header("Static Equipped Items")]
    [SerializeField] private ItemData weapon;
    [SerializeField] private ItemData armor;
    [SerializeField] private ItemData accessory;

    [Header("Generated Equipped Items")]
    [SerializeField] private GeneratedItemInstance generatedWeapon;
    [SerializeField] private GeneratedItemInstance generatedArmor;
    [SerializeField] private GeneratedItemInstance generatedAccessory;

    public event Action OnEquipmentChanged;

    public ItemData Weapon => weapon;
    public ItemData Armor => armor;
    public ItemData Accessory => accessory;

    public GeneratedItemInstance GeneratedWeapon => IsValidGeneratedItem(generatedWeapon) ? generatedWeapon : null;
    public GeneratedItemInstance GeneratedArmor => IsValidGeneratedItem(generatedArmor) ? generatedArmor : null;
    public GeneratedItemInstance GeneratedAccessory => IsValidGeneratedItem(generatedAccessory) ? generatedAccessory : null;

    private void Awake()
    {
        NormalizeGeneratedItems();
    }

    private void OnValidate()
    {
        NormalizeGeneratedItems();
    }

    public ItemData GetItemInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return weapon;
            case EquipmentSlotType.Armor: return armor;
            case EquipmentSlotType.Accessory: return accessory;
            default: return null;
        }
    }

    public GeneratedItemInstance GetGeneratedItemInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon: return GeneratedWeapon;
            case EquipmentSlotType.Armor: return GeneratedArmor;
            case EquipmentSlotType.Accessory: return GeneratedAccessory;
            default: return null;
        }
    }

    public bool Equip(ItemData item, int ownerLevel)
    {
        if (item == null)
            return false;

        if (ownerLevel < item.requiredLevel)
            return false;

        ClearSlot(item.slotType);

        switch (item.slotType)
        {
            case EquipmentSlotType.Weapon:
                weapon = item;
                break;

            case EquipmentSlotType.Armor:
                armor = item;
                break;

            case EquipmentSlotType.Accessory:
                accessory = item;
                break;

            default:
                return false;
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool EquipGenerated(GeneratedItemInstance item, int ownerLevel)
    {
        if (item == null)
            return false;

        if (ownerLevel < item.requiredLevel)
            return false;

        ClearSlot(item.slotType);

        GeneratedItemInstance clone = item.Clone();

        switch (item.slotType)
        {
            case EquipmentSlotType.Weapon:
                generatedWeapon = clone;
                break;

            case EquipmentSlotType.Armor:
                generatedArmor = clone;
                break;

            case EquipmentSlotType.Accessory:
                generatedAccessory = clone;
                break;

            default:
                return false;
        }

        NormalizeGeneratedItems();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public void Unequip(EquipmentSlotType slotType)
    {
        bool changed = HasAnythingInSlot(slotType);

        ClearSlot(slotType);

        if (changed)
            OnEquipmentChanged?.Invoke();
    }

    public void UnequipAll()
    {
        bool changed =
            weapon != null || armor != null || accessory != null ||
            IsValidGeneratedItem(generatedWeapon) ||
            IsValidGeneratedItem(generatedArmor) ||
            IsValidGeneratedItem(generatedAccessory);

        weapon = null;
        armor = null;
        accessory = null;

        generatedWeapon = null;
        generatedArmor = null;
        generatedAccessory = null;

        if (changed)
            OnEquipmentChanged?.Invoke();
    }

    public StatBlock GetTotalItemBonus()
    {
        NormalizeGeneratedItems();

        StatBlock total = new StatBlock
        {
            hp = 0,
            atk = 0,
            def = 0,
            ap = 0,
            crit = 0f
        };

        AddItemBonus(ref total, weapon);
        AddItemBonus(ref total, armor);
        AddItemBonus(ref total, accessory);

        AddGeneratedItemBonus(ref total, generatedWeapon);
        AddGeneratedItemBonus(ref total, generatedArmor);
        AddGeneratedItemBonus(ref total, generatedAccessory);

        return total;
    }

    private void AddItemBonus(ref StatBlock total, ItemData item)
    {
        if (item == null || item.statBonus == null)
            return;

        total.hp += item.statBonus.hp;
        total.atk += item.statBonus.atk;
        total.def += item.statBonus.def;
        total.ap += item.statBonus.ap;
        total.crit += item.statBonus.crit;
    }

    private void AddGeneratedItemBonus(ref StatBlock total, GeneratedItemInstance item)
    {
        if (!IsValidGeneratedItem(item))
            return;

        if (item.statBonus == null)
            return;

        total.hp += item.statBonus.hp;
        total.atk += item.statBonus.atk;
        total.def += item.statBonus.def;
        total.ap += item.statBonus.ap;
        total.crit += item.statBonus.crit;
    }

    private bool HasAnythingInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                return weapon != null || IsValidGeneratedItem(generatedWeapon);

            case EquipmentSlotType.Armor:
                return armor != null || IsValidGeneratedItem(generatedArmor);

            case EquipmentSlotType.Accessory:
                return accessory != null || IsValidGeneratedItem(generatedAccessory);

            default:
                return false;
        }
    }

    private void ClearSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                weapon = null;
                generatedWeapon = null;
                break;

            case EquipmentSlotType.Armor:
                armor = null;
                generatedArmor = null;
                break;

            case EquipmentSlotType.Accessory:
                accessory = null;
                generatedAccessory = null;
                break;
        }
    }

    private void NormalizeGeneratedItems()
    {
        if (!IsValidGeneratedItem(generatedWeapon))
            generatedWeapon = null;

        if (!IsValidGeneratedItem(generatedArmor))
            generatedArmor = null;

        if (!IsValidGeneratedItem(generatedAccessory))
            generatedAccessory = null;
    }

    private bool IsValidGeneratedItem(GeneratedItemInstance item)
    {
        if (item == null)
            return false;

        bool hasName = !string.IsNullOrWhiteSpace(item.itemName);
        bool hasDescription = !string.IsNullOrWhiteSpace(item.description);
        bool hasLevel = item.requiredLevel > 0;
        bool hasValue = item.value > 0;
        bool hasStats =
            item.statBonus != null &&
            (item.statBonus.hp != 0 ||
             item.statBonus.atk != 0 ||
             item.statBonus.def != 0 ||
             item.statBonus.ap != 0 ||
             Mathf.Abs(item.statBonus.crit) > 0.001f);

        return hasName || hasDescription || hasLevel || hasValue || hasStats;
    }
}


using UnityEngine;

[RequireComponent(typeof(Entity))]
public class EquipmentDebugWatcher : MonoBehaviour
{
    private Entity entity;
    private EquipmentSlots slots;

    private string lastSnapshot = "";

    private void Awake()
    {
        entity = GetComponent<Entity>();
        slots = GetComponent<EquipmentSlots>();
    }

    private void Start()
    {
        DebugSnapshot("START");
        CheckGroundItemsOnMyCell();
    }

    private void Update()
    {
        string current = BuildSnapshot();

        if (current != lastSnapshot)
        {
            lastSnapshot = current;
            Debug.Log($"[EquipmentDebugWatcher] {gameObject.name} equipment changed:\n{current}", this);
        }
    }

    private void DebugSnapshot(string label)
    {
        string snapshot = BuildSnapshot();
        lastSnapshot = snapshot;
        Debug.Log($"[EquipmentDebugWatcher] {label} snapshot for {gameObject.name}:\n{snapshot}", this);
    }

    private string BuildSnapshot()
    {
        if (slots == null)
            return "No EquipmentSlots found.";

        string staticWeapon = slots.Weapon != null ? slots.Weapon.itemName : "null";
        string staticArmor = slots.Armor != null ? slots.Armor.itemName : "null";
        string staticAccessory = slots.Accessory != null ? slots.Accessory.itemName : "null";

        string generatedWeapon = slots.GeneratedWeapon != null ? slots.GeneratedWeapon.itemName : "null";
        string generatedArmor = slots.GeneratedArmor != null ? slots.GeneratedArmor.itemName : "null";
        string generatedAccessory = slots.GeneratedAccessory != null ? slots.GeneratedAccessory.itemName : "null";

        return
            $"Static Weapon: {staticWeapon}\n" +
            $"Static Armor: {staticArmor}\n" +
            $"Static Accessory: {staticAccessory}\n" +
            $"Generated Weapon: {generatedWeapon}\n" +
            $"Generated Armor: {generatedArmor}\n" +
            $"Generated Accessory: {generatedAccessory}";
    }

    private void CheckGroundItemsOnMyCell()
    {
        if (entity == null || GridManager.Instance == null)
            return;

        Vector3 center = GridManager.Instance.GetCellCenterWorld(entity.GridPosition);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.3f);

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem groundItem = hits[i].GetComponent<GroundItem>();
            if (groundItem != null)
            {
                Debug.LogWarning(
                    $"[EquipmentDebugWatcher] GroundItem found on {gameObject.name} start cell. " +
                    $"This can auto-equip immediately. Object: {groundItem.gameObject.name}",
                    groundItem
                );
            }
        }
    }
}




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


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPartyController : MonoBehaviour
{
    private void Update()
    {
        if (!TurnManager.Instance.IsPlayerTurn)
            return;

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;

        if (direction == Vector2Int.zero)
            return;

        TryMoveParty(direction);
    }

    private void TryMoveParty(Vector2Int direction)
    {
        List<Entity> party = GridManager.Instance.GetEntitiesByTeam(Team.Player);

        if (party.Count == 0)
            return;

        Vector2Int sourceCell = party[0].GridPosition;

        party = party
            .Where(e => e.GridPosition == sourceCell)
            .ToList();

        Vector2Int targetCell = sourceCell + direction;

        bool actionDone = GridManager.Instance.TryMoveGroupOrAttack(party, targetCell);

        if (actionDone)
            TurnManager.Instance.StartEnemyTurn();
    }
}


using UnityEngine;

public class PlayerItemPickup : MonoBehaviour
{
    [SerializeField] private Team pickerTeam = Team.Player;
    [SerializeField] private float detectionRadius = 0.25f;
    [SerializeField] private bool autoOpenLootWindowOnEnter = false;

    private void Update()
    {
        if (!autoOpenLootWindowOnEnter)
            return;

        if (GridManager.Instance == null || LootWindowUI.Instance == null)
            return;

        Entity picker = FindFirstAlivePicker();
        if (picker == null)
            return;

        if (CellHasGroundItems(picker.GridPosition))
        {
            PlayerInventory inventory = picker.GetComponent<PlayerInventory>();
            if (inventory != null && !LootWindowUI.Instance.IsOpen)
                LootWindowUI.Instance.OpenForCell(picker, inventory, picker.GridPosition);
        }
    }

    private Entity FindFirstAlivePicker()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i] != null && !entities[i].IsDead && entities[i].team == pickerTeam)
                return entities[i];
        }

        return null;
    }

    private bool CellHasGroundItems(Vector2Int cell)
    {
        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponent<GroundItem>() != null)
                return true;
        }

        return false;
    }
}


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerGridMovement : MonoBehaviour
{
    private void Update()
    {
        if (TurnManager.Instance == null) return;
        if (!TurnManager.Instance.IsPlayerTurn) return;

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;

        if (direction == Vector2Int.zero)
            return;

        TryMoveParty(direction);
    }

    private void TryMoveParty(Vector2Int direction)
    {
        if (GridManager.Instance == null) return;

        List<Entity> party = GridManager.Instance.GetEntitiesByTeam(Team.Player);

        if (party.Count == 0)
            return;

        Vector2Int sourceCell = party[0].GridPosition;

        party = party
            .Where(e => e != null && !e.IsDead && e.GridPosition == sourceCell)
            .ToList();

        if (party.Count == 0)
            return;

        Vector2Int targetCell = sourceCell + direction;

        bool actionDone = GridManager.Instance.TryMoveGroupOrAttack(party, targetCell);

        if (actionDone)
            TurnManager.Instance.StartEnemyTurn();
    }
}


using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Entity entity;
    [SerializeField] private Image fillImage;
    [SerializeField] private bool hideWhenFull = false;

    private void Awake()
    {
        if (entity == null)
            entity = GetComponentInParent<Entity>();
    }

    private void OnEnable()
    {
        if (entity != null)
        {
            entity.OnHealthChanged += HandleHealthChanged;
            entity.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (entity != null)
        {
            entity.OnHealthChanged -= HandleHealthChanged;
            entity.OnDied -= HandleDied;
        }
    }

    private void Start()
    {
        RefreshNow();
    }

    private void HandleHealthChanged(int currentHP, int maxHP)
    {
        UpdateFill(currentHP, maxHP);
        RefreshVisibility(currentHP, maxHP);
    }

    private void HandleDied()
    {
        UpdateFill(0, entity != null ? entity.maxHP : 1);
        RefreshVisibility(0, entity != null ? entity.maxHP : 1);
    }

    private void RefreshNow()
    {
        if (entity == null || fillImage == null)
            return;

        UpdateFill(entity.CurrentHP, entity.maxHP);
        RefreshVisibility(entity.CurrentHP, entity.maxHP);
    }

    private void UpdateFill(int currentHP, int maxHP)
    {
        if (fillImage == null)
            return;

        float value = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        fillImage.fillAmount = Mathf.Clamp01(value);
    }

    private void RefreshVisibility(int currentHP, int maxHP)
    {
        if (!hideWhenFull)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            return;
        }

        bool shouldShow = currentHP > 0 && currentHP < maxHP;

        if (gameObject.activeSelf != shouldShow)
            gameObject.SetActive(shouldShow);
    }
}


using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class Entity : MonoBehaviour
{
    [Header("Identity")]
    public Team team = Team.Player;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Attack Animation")]
    public float attackLungeDistance = 0.22f;
    public float attackLungeDuration = 0.08f;
    public bool isAnimatingAttack = false;

    [Header("Damage Number")]
    public Transform damageNumberAnchor;

    [Header("Rewards")]
    public int xpReward = 5;

    public int CurrentHP => stats != null ? stats.CurrentHP : 0;
    public int maxHP => stats != null ? stats.MaxHP : 1;
    public int attackDamage => stats != null ? stats.Atk : 0;
    public int defense => stats != null ? stats.Def : 0;
    public int actionPoints => stats != null ? stats.Ap : 0;
    public float critChance => stats != null ? stats.Crit : 0f;

    public int Level => stats != null ? stats.Level : 1;
    public int CurrentXP => stats != null ? stats.CurrentXP : 0;
    public int UnspentStatPoints => stats != null ? stats.UnspentStatPoints : 0;

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
    private bool targetInitialized = false;

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

        Vector2Int startCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );

        if (GridManager.Instance != null)
            GridManager.Instance.RegisterEntity(this, startCell);
    }

    private void Update()
    {
        if (!targetInitialized) return;
        if (isAnimatingAttack) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );
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

    public bool EquipItem(ItemData item)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots == null || item == null)
            return false;

        return slots.Equip(item, Level);
    }

    public bool EquipGeneratedItem(GeneratedItemInstance item)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots == null || item == null)
            return false;

        return slots.EquipGenerated(item, Level);
    }

    public void UnequipItem(EquipmentSlotType slotType)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots == null)
            return;

        slots.Unequip(slotType);
    }

    public void AddXP(int amount)
    {
        if (stats == null) return;
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
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(AttackLungeRoutine(attackDirection.normalized));
    }

    private IEnumerator AttackLungeRoutine(Vector3 attackDirection)
    {
        if (isAnimatingAttack) yield break;

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

    public void ReceiveDamage(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;
        if (stats == null) return;

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
        if (IsDead) return;
        if (amount <= 0) return;
        if (stats == null) return;

        stats.Heal(amount);
    }

    private void Die()
    {
        if (!IsDead)
            return;

        GrantXPToOppositeTeam();

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


using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class Entity : MonoBehaviour
{
    [Header("Identity")]
    public Team team = Team.Player;

    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Attack Animation")]
    public float attackLungeDistance = 0.22f;
    public float attackLungeDuration = 0.08f;
    public bool isAnimatingAttack = false;

    [Header("Damage Number")]
    public Transform damageNumberAnchor;

    [Header("Rewards")]
    public int xpReward = 5;

    public int CurrentHP => stats != null ? stats.CurrentHP : 0;
    public int maxHP => stats != null ? stats.MaxHP : 1;
    public int attackDamage => stats != null ? stats.Atk : 0;
    public int defense => stats != null ? stats.Def : 0;
    public int actionPoints => stats != null ? stats.Ap : 0;
    public float critChance => stats != null ? stats.Crit : 0f;

    public int Level => stats != null ? stats.Level : 1;
    public int CurrentXP => stats != null ? stats.CurrentXP : 0;
    public int UnspentStatPoints => stats != null ? stats.UnspentStatPoints : 0;

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
    private bool targetInitialized = false;

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

        Vector2Int startCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );

        if (GridManager.Instance != null)
            GridManager.Instance.RegisterEntity(this, startCell);
    }

    private void Update()
    {
        if (!targetInitialized) return;
        if (isAnimatingAttack) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWorldPosition,
            moveSpeed * Time.deltaTime
        );
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

    public bool EquipItem(ItemData item)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots == null || item == null)
            return false;

        return slots.Equip(item, Level);
    }

    public bool EquipGeneratedItem(GeneratedItemInstance item)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots == null || item == null)
            return false;

        return slots.EquipGenerated(item, Level);
    }

    public void UnequipItem(EquipmentSlotType slotType)
    {
        EquipmentSlots slots = GetEquipmentSlots();
        if (slots == null)
            return;

        slots.Unequip(slotType);
    }

    public void AddXP(int amount)
    {
        if (stats == null) return;
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
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(AttackLungeRoutine(attackDirection.normalized));
    }

    private IEnumerator AttackLungeRoutine(Vector3 attackDirection)
    {
        if (isAnimatingAttack) yield break;

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

    public void ReceiveDamage(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;
        if (stats == null) return;

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
        if (IsDead) return;
        if (amount <= 0) return;
        if (stats == null) return;

        stats.Heal(amount);
    }

    private void Die()
    {
        if (!IsDead)
            return;

        GrantXPToOppositeTeam();

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


using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;

    [Header("Spawn Area")]
    public int minX = -8;
    public int maxX = 8;
    public int minY = -8;
    public int maxY = 8;

    [Header("Enemy Groups")]
    public int numberOfEnemyGroups = 4;
    public int minEnemiesPerGroup = 1;
    public int maxEnemiesPerGroup = 4;

    [Header("Block Player Start Cell")]
    public bool avoidPlayerCell = true;

    private void Start()
    {
        SpawnEnemyGroups();
    }

    private void SpawnEnemyGroups()
    {
        Vector2Int playerCell = Vector2Int.zero;
        List<Entity> players = GridManager.Instance.GetEntitiesByTeam(Team.Player);

        if (players.Count > 0)
            playerCell = players[0].GridPosition;

        for (int g = 0; g < numberOfEnemyGroups; g++)
        {
            Vector2Int spawnCell = GetFreeEnemyCell(playerCell);
            int count = Random.Range(minEnemiesPerGroup, maxEnemiesPerGroup + 1);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GridManager.Instance.GetCellCenterWorld(spawnCell);
                Instantiate(enemyPrefab, pos, Quaternion.identity);
            }
        }
    }

    private Vector2Int GetFreeEnemyCell(Vector2Int playerCell)
    {
        for (int tries = 0; tries < 200; tries++)
        {
            Vector2Int cell = new Vector2Int(
                Random.Range(minX, maxX + 1),
                Random.Range(minY, maxY + 1)
            );

            if (avoidPlayerCell && cell == playerCell)
                continue;

            List<Entity> entities = GridManager.Instance.GetEntitiesAtCell(cell);

            bool hasPlayer = false;
            foreach (Entity e in entities)
            {
                if (e.team == Team.Player)
                {
                    hasPlayer = true;
                    break;
                }
            }

            if (hasPlayer)
                continue;

            if (entities.Count < GridManager.Instance.maxEntitiesPerCell)
                return cell;
        }

        return new Vector2Int(maxX, maxY);
    }
}



using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Turn Timing")]
    public float delayBetweenEnemyGroups = 0.15f;

    [Header("Vision")]
    public int maxVisionRange = 6;
    public bool requireLineOfSight = true;

    public IEnumerator ExecuteEnemyTurn()
    {
        List<Vector2Int> enemyCells = GridManager.Instance.GetOccupiedCellsByTeam(Team.Enemy);

        foreach (Vector2Int enemyCell in enemyCells)
        {
            List<Entity> enemiesInCell = GridManager.Instance.GetEntitiesAtCell(enemyCell)
                .Where(e => e.team == Team.Enemy)
                .ToList();

            if (enemiesInCell.Count == 0)
                continue;

            List<Vector2Int> playerCells = GridManager.Instance.GetOccupiedCellsByTeam(Team.Player);
            if (playerCells.Count == 0)
                yield break;

            Vector2Int? targetPlayerCell = GetVisibleNearestPlayerCell(enemyCell, playerCells);

            if (!targetPlayerCell.HasValue)
            {
                yield return new WaitForSeconds(delayBetweenEnemyGroups);
                continue;
            }

            Vector2Int playerCell = targetPlayerCell.Value;
            int distance = Manhattan(enemyCell, playerCell);

            if (distance == 1)
            {
                GridManager.Instance.ResolveCellAttack(enemyCell, playerCell, Team.Enemy);
            }
            else
            {
                Vector2Int step = GetStepTowards(enemyCell, playerCell);
                Vector2Int targetCell = enemyCell + step;
                GridManager.Instance.TryMoveGroupOrAttack(enemiesInCell, targetCell);
            }

            yield return new WaitForSeconds(delayBetweenEnemyGroups);
        }
    }

    private Vector2Int? GetVisibleNearestPlayerCell(Vector2Int enemyCell, List<Vector2Int> playerCells)
    {
        Vector2Int? bestCell = null;
        int bestDistance = int.MaxValue;

        foreach (Vector2Int playerCell in playerCells)
        {
            int distance = Manhattan(enemyCell, playerCell);

            if (distance > maxVisionRange)
                continue;

            if (requireLineOfSight && !GridManager.Instance.HasLineOfSight(enemyCell, playerCell))
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCell = playerCell;
            }
        }

        return bestCell;
    }

    private int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector2Int GetStepTowards(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        if (delta.y != 0)
            return new Vector2Int(0, delta.y > 0 ? 1 : -1);

        if (delta.x != 0)
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        return Vector2Int.zero;
    }
}



using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance;

    public GameObject damageNumberPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnDamageNumber(int damage, Vector3 position)
    {
        GameObject obj = Instantiate(damageNumberPrefab, position, Quaternion.identity);

        DamageNumber number = obj.GetComponent<DamageNumber>();

        if (number != null)
        {
            number.Setup(damage);
        }
    }
}



using UnityEngine;

[RequireComponent(typeof(Entity))]
public class DamageNumberReceiver : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    private void OnEnable()
    {
        if (entity != null)
            entity.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (entity != null)
            entity.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(int amount, Vector3 worldPosition)
    {
        if (DamageNumberManager.Instance == null)
            return;

        DamageNumberManager.Instance.SpawnDamageNumber(amount, worldPosition);
    }
}



using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance;

    [SerializeField] private DamageNumber damageNumberPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SpawnDamageNumber(int amount, Vector3 worldPosition)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("DamageNumber prefab não configurado no DamageNumberManager.");
            return;
        }

        DamageNumber numberInstance = Instantiate(
            damageNumberPrefab,
            worldPosition,
            Quaternion.identity
        );

        numberInstance.Setup(amount);
    }
}



using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float lifetime = 1f;

    private TextMeshProUGUI textMesh;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(int damage)
    {
        textMesh.text = "-" + damage.ToString();
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}



using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class DamageFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Entity entity;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private int flashCount = 1;

    private Coroutine flashCoroutine;
    private Color[] originalColors;

    private void Awake()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        CacheOriginalColors();
    }

    private void Start()
    {
        CacheOriginalColors();
        RestoreOriginalColors();
    }

    private void OnEnable()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (entity != null)
            entity.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (entity != null)
            entity.OnDamageTaken -= HandleDamageTaken;

        RestoreOriginalColors();
    }

    private void HandleDamageTaken(int amount, Vector3 worldPosition)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        CacheOriginalColors();

        int totalFlashes = Mathf.Max(1, flashCount);
        float halfDuration = Mathf.Max(0.01f, flashDuration * 0.5f);

        for (int flashIndex = 0; flashIndex < totalFlashes; flashIndex++)
        {
            ApplyColor(flashColor);
            yield return new WaitForSeconds(halfDuration);

            RestoreOriginalColors();
            yield return new WaitForSeconds(halfDuration);
        }

        flashCoroutine = null;
    }

    private void CacheOriginalColors()
    {
        if (spriteRenderers == null)
            return;

        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                originalColors[i] = spriteRenderers[i].color;
        }
    }

    private void ApplyColor(Color color)
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = color;
        }
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers == null || originalColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
                spriteRenderers[i].color = originalColors[i];
        }
    }
}



using UnityEngine;
using System.Collections;

public class AttackAnimation : MonoBehaviour
{
    public float lungeDistance = 0.2f;
    public float lungeSpeed = 8f;

    public IEnumerator PlayAttack(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        Vector3 direction = (targetPosition - startPosition).normalized;

        Vector3 attackPosition = startPosition + direction * lungeDistance;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * lungeSpeed;
            transform.position = Vector3.Lerp(startPosition, attackPosition, t);
            yield return null;
        }

        t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * lungeSpeed;
            transform.position = Vector3.Lerp(attackPosition, startPosition, t);
            yield return null;
        }
    }
}

# --- LATEST CHECKPOINT OVERRIDES (March 13 2026) ---
# The script versions below supersede earlier copies in this document.

using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyInventory : MonoBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<InventoryItemEntry> items = new List<InventoryItemEntry>();

    public event Action OnInventoryChanged;

    public int InventorySize => inventorySize;
    public IReadOnlyList<InventoryItemEntry> Items => items;

    private void Awake()
    {
        EnsureSize();
    }

    public InventoryItemEntry GetItem(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return items[index];
    }

    public bool IsSlotEmpty(int index)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();
        return items[index] == null || items[index].IsEmpty;
    }

    public bool HasEmptySlot()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return true;
        }

        return false;
    }

    public int GetFirstEmptySlotIndex()
    {
        EnsureSize();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
                return i;
        }

        return -1;
    }

    public bool AddStaticItem(ItemData item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromStatic(item));
    }

    public bool AddGeneratedItem(GeneratedItemInstance item)
    {
        if (item == null)
            return false;

        return AddEntry(InventoryItemEntry.FromGenerated(item));
    }

    public bool AddEntry(InventoryItemEntry entry)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        EnsureSize();

        int emptyIndex = GetFirstEmptySlotIndex();
        if (emptyIndex < 0)
            return false;

        items[emptyIndex] = entry.Clone();
        NormalizeSlot(emptyIndex);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool AddEntryToIndex(InventoryItemEntry entry, int index)
    {
        if (entry == null || entry.IsEmpty)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        if (items[index] != null && !items[index].IsEmpty)
            return false;

        items[index] = entry.Clone();
        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        if (items[index] == null || items[index].IsEmpty)
            return false;

        items[index] = new InventoryItemEntry();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool MoveItem(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            return false;

        EnsureSize();

        if (fromIndex == toIndex)
            return false;

        if (items[fromIndex] == null || items[fromIndex].IsEmpty)
            return false;

        InventoryItemEntry from = items[fromIndex];
        InventoryItemEntry to = items[toIndex];

        items[toIndex] = from;
        items[fromIndex] = to;

        NormalizeSlot(fromIndex);
        NormalizeSlot(toIndex);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventoryItemEntry GetEquippedEntry(Entity targetEntity, EquipmentSlotType slotType)
    {
        if (targetEntity == null)
            return null;

        EquipmentSlots equipmentSlots = targetEntity.GetComponent<EquipmentSlots>();
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

    public bool TryEquipFromInventory(Entity targetEntity, int index)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (targetEntity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(targetEntity, entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = targetEntity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = targetEntity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        items[index] = new InventoryItemEntry();

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = targetEntity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = targetEntity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                items[index] = entry.Clone();
                NormalizeSlot(index);

                if (!restored)
                    Debug.LogWarning("PartyInventory: falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        NormalizeSlot(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipFromInventoryToSlot(Entity targetEntity, int index, EquipmentSlotType targetSlotType)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(index))
            return false;

        EnsureSize();

        InventoryItemEntry entry = items[index];
        if (entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipFromInventory(targetEntity, index);
    }

    public bool UnequipToInventory(Entity targetEntity, EquipmentSlotType slotType)
    {
        if (targetEntity == null)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(targetEntity, slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        if (!AddEntry(equippedEntry))
            return false;

        targetEntity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipToInventorySlot(Entity targetEntity, EquipmentSlotType slotType, int inventoryIndex)
    {
        if (targetEntity == null)
            return false;

        if (!IsValidIndex(inventoryIndex))
            return false;

        EnsureSize();

        if (items[inventoryIndex] != null && !items[inventoryIndex].IsEmpty)
            return false;

        InventoryItemEntry equippedEntry = GetEquippedEntry(targetEntity, slotType);
        if (equippedEntry == null || equippedEntry.IsEmpty)
            return false;

        items[inventoryIndex] = equippedEntry.Clone();
        NormalizeSlot(inventoryIndex);

        targetEntity.UnequipItem(slotType);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectly(Entity targetEntity, InventoryItemEntry entry)
    {
        if (targetEntity == null || entry == null || entry.IsEmpty)
            return false;

        if (targetEntity.Level < entry.RequiredLevel)
            return false;

        InventoryItemEntry oldEquipped = GetEquippedEntry(targetEntity, entry.SlotType);

        bool equipped = false;

        if (entry.IsStaticItem)
            equipped = targetEntity.EquipItem(entry.StaticItem);
        else if (entry.IsGeneratedItem)
            equipped = targetEntity.EquipGeneratedItem(entry.GeneratedItem);

        if (!equipped)
            return false;

        if (oldEquipped != null && !oldEquipped.IsEmpty)
        {
            if (!AddEntry(oldEquipped))
            {
                bool restored = false;

                if (oldEquipped.IsStaticItem)
                    restored = targetEntity.EquipItem(oldEquipped.StaticItem);
                else if (oldEquipped.IsGeneratedItem)
                    restored = targetEntity.EquipGeneratedItem(oldEquipped.GeneratedItem);

                if (!restored)
                    Debug.LogWarning("PartyInventory: falha ao restaurar item equipado anterior.");

                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryEquipEntryDirectlyToSlot(Entity targetEntity, InventoryItemEntry entry, EquipmentSlotType targetSlotType)
    {
        if (targetEntity == null || entry == null || entry.IsEmpty)
            return false;

        if (entry.SlotType != targetSlotType)
            return false;

        return TryEquipEntryDirectly(targetEntity, entry);
    }

    private void EnsureSize()
    {
        if (inventorySize < 1)
            inventorySize = 1;

        if (items == null)
            items = new List<InventoryItemEntry>();

        while (items.Count < inventorySize)
            items.Add(new InventoryItemEntry());

        while (items.Count > inventorySize)
            items.RemoveAt(items.Count - 1);

        for (int i = 0; i < items.Count; i++)
            NormalizeSlot(i);
    }

    private void NormalizeSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (items[index] == null)
            items[index] = new InventoryItemEntry();
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < items.Count;
    }
}



using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootWindowUI : MonoBehaviour
{
    public static LootWindowUI Instance;

    [Header("External References")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private LootWindowGridAutoBuilder windowBuilder;
    [SerializeField] private PartyInventory partyInventory;

    [Header("Window")]
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private Transform selectorContentRoot;
    [SerializeField] private Transform equippedContentRoot;
    [SerializeField] private Transform inventoryContentRoot;
    [SerializeField] private Transform groundLootContentRoot;

    [Header("Prefab")]
    [SerializeField] private ItemButtonUI itemButtonPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;

    [Header("Input")]
    [SerializeField] private KeyCode toggleLootKey = KeyCode.E;
    [SerializeField] private KeyCode closeLootKey = KeyCode.Escape;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 0.35f;

    [Header("Selector Style")]
    [SerializeField] private Vector2 selectorButtonSize = new Vector2(52f, 74f);
    [SerializeField] private Color selectorNormalColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color selectorSelectedColor = new Color(0.25f, 0.45f, 0.90f, 1f);
    [SerializeField] private Color selectorTextColor = Color.white;
    [SerializeField] private int selectorFontSize = 14;

    private Entity currentEntity;
    private bool isOpen = false;

    private readonly List<GameObject> spawnedUI = new List<GameObject>();
    private readonly List<Entity> cachedPlayers = new List<Entity>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveExternalReferences();
        AutoFindReferences();
        BindCloseButton();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleLootKey))
        {
            if (isOpen)
                CloseWindow();
            else
                TryOpenForFirstPlayer();

            return;
        }

        if (Input.GetKeyDown(closeLootKey) && isOpen)
        {
            CloseWindow();
            return;
        }

        if (!isOpen)
            return;

        if (currentEntity == null || currentEntity.IsDead)
        {
            Entity fallback = FindFirstAlivePlayer();
            if (fallback != null)
                SelectPlayer(fallback);
            else
                CloseWindow();
        }
    }

    public void ConfigureReferences(
        GameObject newWindowRoot,
        Button newCloseButton,
        Transform newSelectorContentRoot,
        Transform newEquippedContentRoot,
        Transform newInventoryContentRoot,
        Transform newGroundLootContentRoot,
        TMP_Text newTitleText,
        TMP_Text newHintText)
    {
        windowRoot = newWindowRoot;
        closeButton = newCloseButton;
        selectorContentRoot = newSelectorContentRoot;
        equippedContentRoot = newEquippedContentRoot;
        inventoryContentRoot = newInventoryContentRoot;
        groundLootContentRoot = newGroundLootContentRoot;
        titleText = newTitleText;
        hintText = newHintText;

        AutoFindReferences();
        BindCloseButton();
    }

    public void SetItemButtonPrefab(ItemButtonUI prefab)
    {
        itemButtonPrefab = prefab;
    }

    public void OpenForCell(Entity entity, Vector2Int cell)
    {
        EnsureWindowReady();

        if (entity == null || partyInventory == null)
            return;

        if (itemButtonPrefab == null)
        {
            Debug.LogWarning("LootWindowUI: ItemButtonPrefab está vazio.");
            return;
        }

        if (selectorContentRoot == null || equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
        {
            Debug.LogWarning("LootWindowUI: content roots ausentes.");
            return;
        }

        currentEntity = entity;
        isOpen = true;

        if (windowRoot != null)
            windowRoot.SetActive(true);

        RefreshUI();
    }

    public void CloseWindow()
    {
        HideTooltip();

        isOpen = false;
        currentEntity = null;

        ClearSpawnedUI();

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private void TryOpenForFirstPlayer()
    {
        Entity firstPlayer = FindFirstAlivePlayer();
        if (firstPlayer == null)
            return;

        OpenForCell(firstPlayer, firstPlayer.GridPosition);
    }

    private void EnsureWindowReady()
    {
        ResolveExternalReferences();

        if (windowBuilder != null)
        {
            windowBuilder.SetLootWindowUI(this);

            if (!windowBuilder.IsBuilt)
                windowBuilder.Build();
        }

        AutoFindReferences();
        BindCloseButton();
    }

    private void ResolveExternalReferences()
    {
        if (windowBuilder == null)
            windowBuilder = FindFirstObjectByType<LootWindowGridAutoBuilder>();

        if (windowRoot == null && windowBuilder != null)
            windowRoot = windowBuilder.gameObject;

        if (partyInventory == null)
            partyInventory = FindFirstObjectByType<PartyInventory>();
    }

    private Entity FindFirstAlivePlayer()
    {
        List<Entity> players = GetAvailablePlayers();
        if (players.Count == 0)
            return null;

        return players[0];
    }

    private List<Entity> GetAvailablePlayers()
    {
        cachedPlayers.Clear();

        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            if (entity == null)
                continue;

            if (entity.team != Team.Player)
                continue;

            if (entity.IsDead)
                continue;

            cachedPlayers.Add(entity);
        }

        cachedPlayers.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return cachedPlayers;
    }

    private void SelectPlayer(Entity entity)
    {
        if (entity == null)
            return;

        currentEntity = entity;
        HideTooltip();
        RefreshUI();
    }

    private void AutoFindReferences()
    {
        Transform searchRoot = windowRoot != null ? windowRoot.transform : null;

        if (closeButton == null)
        {
            Transform t = FindDeepChild(searchRoot, "CloseButton");
            if (t != null)
                closeButton = t.GetComponent<Button>();
        }

        if (selectorContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "SelectorContent");
            if (t != null)
                selectorContentRoot = t;
        }

        if (equippedContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "EquippedContent");
            if (t != null)
                equippedContentRoot = t;
        }

        if (inventoryContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "InventoryContent");
            if (t != null)
                inventoryContentRoot = t;
        }

        if (groundLootContentRoot == null)
        {
            Transform t = FindDeepChild(searchRoot, "GroundLootContent");
            if (t != null)
                groundLootContentRoot = t;
        }

        if (titleText == null)
        {
            Transform t = FindDeepChild(searchRoot, "TitleText");
            if (t != null)
                titleText = t.GetComponent<TMP_Text>();
        }

        if (hintText == null)
        {
            Transform t = FindDeepChild(searchRoot, "HintText");
            if (t != null)
                hintText = t.GetComponent<TMP_Text>();
        }
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseWindow);
    }

    private void RefreshUI()
    {
        if (!isOpen || currentEntity == null || partyInventory == null)
            return;

        EnsureWindowReady();

        if (itemButtonPrefab == null)
            return;

        if (selectorContentRoot == null || equippedContentRoot == null || inventoryContentRoot == null || groundLootContentRoot == null)
            return;

        ClearSpawnedUI();

        if (titleText != null)
            titleText.text = $"Party Inventory - Equipando: {currentEntity.name}";

        if (hintText != null)
            hintText.text = "Party shared bag | seletor troca só equipamentos | chão e mochila são únicos";

        BuildSelectorSection();
        BuildEquippedSection();
        BuildInventorySection();
        BuildGroundLootSection();
    }

    private void BuildSelectorSection()
    {
        List<Entity> players = GetAvailablePlayers();

        for (int i = 0; i < players.Count; i++)
        {
            Entity player = players[i];
            int displayIndex = i + 1;

            GameObject buttonGO = CreateSelectorButton(player, displayIndex, player == currentEntity);
            buttonGO.transform.SetParent(selectorContentRoot, false);
            spawnedUI.Add(buttonGO);
        }
    }

    private GameObject CreateSelectorButton(Entity player, int displayIndex, bool selected)
    {
        GameObject root = new GameObject(
            $"PlayerSelector_{displayIndex}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = selectorButtonSize;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minWidth = selectorButtonSize.x;
        layout.preferredWidth = selectorButtonSize.x;
        layout.minHeight = selectorButtonSize.y;
        layout.preferredHeight = selectorButtonSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image bg = root.GetComponent<Image>();
        bg.color = selected ? selectorSelectedColor : selectorNormalColor;

        Button button = root.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            SelectPlayer(player);
        });

        GameObject iconGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(root.transform, false);

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        iconRect.anchoredPosition = new Vector2(0f, -6f);

        Image icon = iconGO.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = GetEntityPortrait(player);
        icon.color = icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(root.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, 22f);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = displayIndex.ToString();
        label.fontSize = selectorFontSize;
        label.color = selectorTextColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        return root;
    }

    private Sprite GetEntityPortrait(Entity entity)
    {
        if (entity == null)
            return null;

        SpriteRenderer spriteRenderer = entity.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            return null;

        return spriteRenderer.sprite;
    }

    private void BuildEquippedSection()
    {
        CreateEquippedButton(EquipmentSlotType.Weapon);
        CreateEquippedButton(EquipmentSlotType.Armor);
        CreateEquippedButton(EquipmentSlotType.Accessory);
    }

    private void CreateEquippedButton(EquipmentSlotType slotType)
    {
        InventoryItemEntry equipped = partyInventory.GetEquippedEntry(currentEntity, slotType);

        ItemButtonUI button = Instantiate(itemButtonPrefab, equippedContentRoot);
        button.ConfigureAsEquippedSlot(slotType);

        button.Setup(
            equipped,
            equipped == null || equipped.IsEmpty
                ? null
                : () =>
                {
                    bool moved = partyInventory.UnequipToInventory(currentEntity, slotType);
                    if (moved)
                        RefreshUI();
                },
            null,
            equipped != null && !equipped.IsEmpty,
            (sourceButton) =>
            {
                HandleDropOnEquippedSlot(slotType, sourceButton);
            },
            null
        );

        spawnedUI.Add(button.gameObject);
    }

    private void BuildInventorySection()
    {
        IReadOnlyList<InventoryItemEntry> items = partyInventory.Items;

        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            InventoryItemEntry entry = items[index];
            InventoryItemEntry equippedCompare = null;

            if (entry != null && !entry.IsEmpty)
                equippedCompare = partyInventory.GetEquippedEntry(currentEntity, entry.SlotType);

            ItemButtonUI button = Instantiate(itemButtonPrefab, inventoryContentRoot);
            button.ConfigureAsInventorySlot(index);

            bool hasItem = entry != null && !entry.IsEmpty;

            button.Setup(
                hasItem ? entry : null,
                hasItem
                    ? () =>
                    {
                        bool equipped = partyInventory.TryEquipFromInventory(currentEntity, index);
                        if (equipped)
                            RefreshUI();
                    }
                    : null,
                hasItem
                    ? () =>
                    {
                        bool equipped = partyInventory.TryEquipFromInventory(currentEntity, index);
                        if (equipped)
                            RefreshUI();
                    }
                    : null,
                hasItem,
                (sourceButton) =>
                {
                    HandleDropOnInventorySlot(index, sourceButton);
                },
                equippedCompare
            );

            spawnedUI.Add(button.gameObject);
        }
    }

    private void BuildGroundLootSection()
    {
        Vector2Int anchorCell = GetPartyAnchorCell();
        List<GroundItem> items = GetGroundItemsInCell(anchorCell);
        int maxGroundSlots = 20;

        for (int i = 0; i < maxGroundSlots; i++)
        {
            ItemButtonUI button = Instantiate(itemButtonPrefab, groundLootContentRoot);

            if (i >= items.Count || items[i] == null)
            {
                button.ClearContext();
                button.Setup(null, null, null, false, null, null);
                spawnedUI.Add(button.gameObject);
                continue;
            }

            GroundItem groundItem = items[i];
            InventoryItemEntry entry = groundItem.ToInventoryEntry();
            InventoryItemEntry equippedCompare = null;

            if (entry != null && !entry.IsEmpty)
                equippedCompare = partyInventory.GetEquippedEntry(currentEntity, entry.SlotType);

            button.ConfigureAsGroundSlot(groundItem);

            button.Setup(
                entry,
                () =>
                {
                    bool moved = groundItem.TrySendToPartyInventory(partyInventory);
                    if (moved)
                        RefreshUI();
                },
                () =>
                {
                    bool equipped = groundItem.TryEquipDirectToParty(currentEntity, partyInventory);
                    if (equipped)
                        RefreshUI();
                },
                true,
                null,
                equippedCompare
            );

            spawnedUI.Add(button.gameObject);
        }
    }

    private Vector2Int GetPartyAnchorCell()
    {
        List<Entity> players = GetAvailablePlayers();
        if (players.Count > 0)
            return players[0].GridPosition;

        return currentEntity != null ? currentEntity.GridPosition : Vector2Int.zero;
    }

    private void HandleDropOnInventorySlot(int targetIndex, ItemButtonUI sourceButton)
    {
        if (partyInventory == null || sourceButton == null)
            return;

        bool changed = false;

        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory:
                changed = partyInventory.MoveItem(sourceButton.InventoryIndex, targetIndex);
                break;

            case ItemButtonSlotKind.Equipped:
                changed = partyInventory.UnequipToInventorySlot(currentEntity, sourceButton.EquippedSlotType, targetIndex);
                break;

            case ItemButtonSlotKind.Ground:
                if (sourceButton.GroundItemRef != null)
                    changed = sourceButton.GroundItemRef.TrySendToPartyInventorySlot(partyInventory, targetIndex);
                break;
        }

        if (changed)
            RefreshUI();
    }

    private void HandleDropOnEquippedSlot(EquipmentSlotType targetSlotType, ItemButtonUI sourceButton)
    {
        if (partyInventory == null || currentEntity == null || sourceButton == null)
            return;

        bool changed = false;

        switch (sourceButton.SlotKind)
        {
            case ItemButtonSlotKind.Inventory:
                changed = partyInventory.TryEquipFromInventoryToSlot(currentEntity, sourceButton.InventoryIndex, targetSlotType);
                break;

            case ItemButtonSlotKind.Ground:
                if (sourceButton.GroundItemRef != null)
                    changed = sourceButton.GroundItemRef.TryEquipDirectToPartySlot(currentEntity, partyInventory, targetSlotType);
                break;
        }

        if (changed)
            RefreshUI();
    }

    private List<GroundItem> GetGroundItemsInCell(Vector2Int cell)
    {
        List<GroundItem> result = new List<GroundItem>();

        if (GridManager.Instance == null)
            return result;

        Vector3 center = GridManager.Instance.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectionRadius);

        if (hits == null)
            return result;

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem item = hits[i].GetComponent<GroundItem>();
            if (item != null)
                result.Add(item);
        }

        return result;
    }

    private void ClearSpawnedUI()
    {
        for (int i = 0; i < spawnedUI.Count; i++)
        {
            if (spawnedUI[i] != null)
                Destroy(spawnedUI[i]);
        }

        spawnedUI.Clear();
    }

    private void HideTooltip()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private Transform FindDeepChild(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}

