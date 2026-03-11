using UnityEngine;

[RequireComponent(typeof(Entity))]
public class DamageNumberReceiver : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    private void OnEnable()
    {
        if (entity != null)
            entity.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (entity != null)
            entity.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(int amount, Vector3 worldPosition)
    {
        if (DamageNumberManager.Instance == null)
            return;

        DamageNumberManager.Instance.SpawnDamageNumber(amount, worldPosition);
    }
}