using UnityEngine;

public class CombatEntityRuntime : MonoBehaviour
{
    public string CombatantId { get; private set; }
    public string OriginalEntityName { get; private set; }
    public Vector2Int OriginalExplorationCell { get; private set; }
    public int SpawnIndex { get; private set; }
    public bool SuppressWorldDropOnDeath { get; private set; }

    public void Setup(
        string combatantId,
        string originalEntityName,
        Vector2Int originalExplorationCell,
        int spawnIndex,
        bool suppressWorldDropOnDeath = true)
    {
        CombatantId = combatantId;
        OriginalEntityName = originalEntityName;
        OriginalExplorationCell = originalExplorationCell;
        SpawnIndex = spawnIndex;
        SuppressWorldDropOnDeath = suppressWorldDropOnDeath;
    }
}
