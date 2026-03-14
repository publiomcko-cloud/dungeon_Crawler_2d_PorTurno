using UnityEngine;

[DisallowMultipleComponent]
public class CombatTurnOutline : MonoBehaviour
{
    [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0.15f, 0.95f);
    [SerializeField] private float outlineOffset = 0.06f;
    [SerializeField] private int sortingOrderOffset = -1;

    private SpriteRenderer targetRenderer;
    private readonly SpriteRenderer[] outlineRenderers = new SpriteRenderer[4];
    private bool built;

    private void Awake()
    {
        TryBuild();
        SetHighlighted(false);
    }

    private void LateUpdate()
    {
        if (!built || targetRenderer == null)
            return;

        SyncOutlineSprites();
    }

    public void Configure(Color color, float offset, int orderOffset)
    {
        outlineColor = color;
        outlineOffset = Mathf.Max(0f, offset);
        sortingOrderOffset = orderOffset;

        TryBuild();
        ApplyVisualSettings();
    }

    public void SetHighlighted(bool highlighted)
    {
        TryBuild();

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            if (outlineRenderers[i] != null)
                outlineRenderers[i].enabled = highlighted;
        }
    }

    private void TryBuild()
    {
        if (built)
            return;

        targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
            return;

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            GameObject child = new GameObject($"TurnOutline_{i}");
            child.transform.SetParent(transform, false);

            SpriteRenderer outlineRenderer = child.AddComponent<SpriteRenderer>();
            outlineRenderers[i] = outlineRenderer;
        }

        built = true;
        ApplyVisualSettings();
        SyncOutlineSprites();
    }

    private void ApplyVisualSettings()
    {
        if (!built)
            return;

        Vector3[] offsets =
        {
            new Vector3(outlineOffset, 0f, 0f),
            new Vector3(-outlineOffset, 0f, 0f),
            new Vector3(0f, outlineOffset, 0f),
            new Vector3(0f, -outlineOffset, 0f)
        };

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            SpriteRenderer outlineRenderer = outlineRenderers[i];
            if (outlineRenderer == null)
                continue;

            outlineRenderer.transform.localPosition = offsets[i];
            outlineRenderer.color = outlineColor;
            outlineRenderer.sortingLayerID = targetRenderer != null ? targetRenderer.sortingLayerID : 0;
            outlineRenderer.sortingOrder = targetRenderer != null ? targetRenderer.sortingOrder + sortingOrderOffset : sortingOrderOffset;
        }
    }

    private void SyncOutlineSprites()
    {
        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            SpriteRenderer outlineRenderer = outlineRenderers[i];
            if (outlineRenderer == null)
                continue;

            outlineRenderer.sprite = targetRenderer.sprite;
            outlineRenderer.flipX = targetRenderer.flipX;
            outlineRenderer.flipY = targetRenderer.flipY;
            outlineRenderer.drawMode = targetRenderer.drawMode;
            outlineRenderer.size = targetRenderer.size;
            outlineRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = targetRenderer.sortingOrder + sortingOrderOffset;
        }
    }
}
