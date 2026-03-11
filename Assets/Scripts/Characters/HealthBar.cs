using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Entity target;
    public Image fillImage;

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        float hpPercent = (float)target.currentHP / target.maxHP;

        fillImage.fillAmount = hpPercent;

        transform.position = target.transform.position + Vector3.up * 0.8f;
    }
}