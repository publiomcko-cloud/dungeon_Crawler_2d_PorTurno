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