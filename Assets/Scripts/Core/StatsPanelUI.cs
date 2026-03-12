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

        if (nameText != null)
            nameText.text = targetEntity.name;

        if (levelText != null)
            levelText.text = $"Level: {targetEntity.Level}";

        if (xpText != null)
            xpText.text = $"XP: {targetEntity.CurrentXP} / {targetStats.GetXPToNextLevel()}";

        if (pointsText != null)
            pointsText.text = $"Points: {targetEntity.UnspentStatPoints}";

        if (hpValueText != null)
            hpValueText.text = $"HP: {targetEntity.CurrentHP} / {targetEntity.maxHP}";

        if (atkValueText != null)
            atkValueText.text = $"ATK: {targetEntity.attackDamage}";

        if (defValueText != null)
            defValueText.text = $"DEF: {targetEntity.defense}";

        if (apValueText != null)
            apValueText.text = $"AP: {targetEntity.actionPoints}";

        if (critValueText != null)
            critValueText.text = $"CRIT: {targetEntity.critChance:0.#}%";

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