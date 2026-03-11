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