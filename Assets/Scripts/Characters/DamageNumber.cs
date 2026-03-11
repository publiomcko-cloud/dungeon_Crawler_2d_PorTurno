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