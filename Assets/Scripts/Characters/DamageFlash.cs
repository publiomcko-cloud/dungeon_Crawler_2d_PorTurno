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