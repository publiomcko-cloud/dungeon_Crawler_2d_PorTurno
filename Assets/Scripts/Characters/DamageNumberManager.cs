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