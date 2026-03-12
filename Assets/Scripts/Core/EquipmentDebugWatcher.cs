using UnityEngine;

[RequireComponent(typeof(Entity))]
public class EquipmentDebugWatcher : MonoBehaviour
{
    private Entity entity;
    private EquipmentSlots slots;

    private string lastSnapshot = "";

    private void Awake()
    {
        entity = GetComponent<Entity>();
        slots = GetComponent<EquipmentSlots>();
    }

    private void Start()
    {
        DebugSnapshot("START");
        CheckGroundItemsOnMyCell();
    }

    private void Update()
    {
        string current = BuildSnapshot();

        if (current != lastSnapshot)
        {
            lastSnapshot = current;
            Debug.Log($"[EquipmentDebugWatcher] {gameObject.name} equipment changed:\n{current}", this);
        }
    }

    private void DebugSnapshot(string label)
    {
        string snapshot = BuildSnapshot();
        lastSnapshot = snapshot;
        Debug.Log($"[EquipmentDebugWatcher] {label} snapshot for {gameObject.name}:\n{snapshot}", this);
    }

    private string BuildSnapshot()
    {
        if (slots == null)
            return "No EquipmentSlots found.";

        string staticWeapon = slots.Weapon != null ? slots.Weapon.itemName : "null";
        string staticArmor = slots.Armor != null ? slots.Armor.itemName : "null";
        string staticAccessory = slots.Accessory != null ? slots.Accessory.itemName : "null";

        string generatedWeapon = slots.GeneratedWeapon != null ? slots.GeneratedWeapon.itemName : "null";
        string generatedArmor = slots.GeneratedArmor != null ? slots.GeneratedArmor.itemName : "null";
        string generatedAccessory = slots.GeneratedAccessory != null ? slots.GeneratedAccessory.itemName : "null";

        return
            $"Static Weapon: {staticWeapon}\n" +
            $"Static Armor: {staticArmor}\n" +
            $"Static Accessory: {staticAccessory}\n" +
            $"Generated Weapon: {generatedWeapon}\n" +
            $"Generated Armor: {generatedArmor}\n" +
            $"Generated Accessory: {generatedAccessory}";
    }

    private void CheckGroundItemsOnMyCell()
    {
        if (entity == null || GridManager.Instance == null)
            return;

        Vector3 center = GridManager.Instance.GetCellCenterWorld(entity.GridPosition);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.3f);

        for (int i = 0; i < hits.Length; i++)
        {
            GroundItem groundItem = hits[i].GetComponent<GroundItem>();
            if (groundItem != null)
            {
                Debug.LogWarning(
                    $"[EquipmentDebugWatcher] GroundItem found on {gameObject.name} start cell. " +
                    $"This can auto-equip immediately. Object: {groundItem.gameObject.name}",
                    groundItem
                );
            }
        }
    }
}