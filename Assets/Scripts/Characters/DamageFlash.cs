using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    private SpriteRenderer sprite;

    public float flashTime = 0.12f;
    public Color flashColor = Color.red;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();

        if (sprite == null)
        {
            Debug.LogWarning("DamageFlash: SpriteRenderer não encontrado em " + gameObject.name);
        }
    }

    public void Flash()
    {
        if (sprite != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    IEnumerator FlashRoutine()
    {
        Color original = sprite.color;

        sprite.color = flashColor;

        yield return new WaitForSeconds(flashTime);

        sprite.color = original;
    }
}