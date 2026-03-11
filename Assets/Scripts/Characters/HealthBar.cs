using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Entity entity;
    [SerializeField] private Slider slider;

    private void Awake()
    {
        if (entity == null)
            entity = GetComponentInParent<Entity>();

        if (slider == null)
            slider = GetComponent<Slider>();
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (entity == null || slider == null)
            return;

        slider.maxValue = entity.maxHP;
        slider.value = entity.CurrentHP;
    }
}