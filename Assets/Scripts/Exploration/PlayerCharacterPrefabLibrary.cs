using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterPrefabLibrary : MonoBehaviour
{
    [Serializable]
    private sealed class CharacterPrefabEntry
    {
        public string characterId;
        public GameObject prefab;
    }

    [Header("Validation")]
    [SerializeField] private bool enableInspectorWarnings = true;

    [SerializeField] private GameObject defaultPlayerPrefab;
    [SerializeField] private List<CharacterPrefabEntry> entries = new List<CharacterPrefabEntry>();

    private void Awake()
    {
        ValidateInspectorConfiguration();
    }

    public GameObject ResolvePrefab(string characterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                CharacterPrefabEntry entry = entries[i];
                if (entry == null || entry.prefab == null)
                    continue;

                if (string.Equals(entry.characterId, characterId, StringComparison.OrdinalIgnoreCase))
                    return entry.prefab;
            }
        }

        return defaultPlayerPrefab;
    }

    private void ValidateInspectorConfiguration()
    {
        if (!enableInspectorWarnings)
            return;

        if (defaultPlayerPrefab == null)
            Debug.LogWarning("PlayerCharacterPrefabLibrary: 'Default Player Prefab' nao esta preenchido.", this);
        else
            ValidatePlayerPrefab(defaultPlayerPrefab, "Default Player Prefab");

        HashSet<string> seenCharacterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<GameObject, string> prefabOwners = new Dictionary<GameObject, string>();

        for (int i = 0; i < entries.Count; i++)
        {
            CharacterPrefabEntry entry = entries[i];
            if (entry == null)
            {
                Debug.LogWarning($"PlayerCharacterPrefabLibrary: entry {i} esta nula.", this);
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.characterId))
                Debug.LogWarning($"PlayerCharacterPrefabLibrary: entry {i} esta sem 'Character Id'.", this);

            if (entry.prefab == null)
            {
                Debug.LogWarning($"PlayerCharacterPrefabLibrary: entry '{entry.characterId}' esta sem prefab.", this);
                continue;
            }

            ValidatePlayerPrefab(entry.prefab, $"Entry '{entry.characterId}'");

            if (!string.IsNullOrWhiteSpace(entry.characterId) && !seenCharacterIds.Add(entry.characterId))
                Debug.LogWarning($"PlayerCharacterPrefabLibrary: 'Character Id' duplicado: '{entry.characterId}'.", this);

            if (prefabOwners.TryGetValue(entry.prefab, out string existingOwner))
            {
                Debug.LogWarning(
                    $"PlayerCharacterPrefabLibrary: o mesmo prefab '{entry.prefab.name}' esta sendo usado por '{existingOwner}' e '{entry.characterId}'.",
                    this);
            }
            else
            {
                prefabOwners.Add(entry.prefab, entry.characterId);
            }

            CharacterIdentity identity = entry.prefab.GetComponent<CharacterIdentity>();
            if (identity == null)
            {
                Debug.LogWarning(
                    $"PlayerCharacterPrefabLibrary: prefab '{entry.prefab.name}' nao possui CharacterIdentity.",
                    this);
            }
        }
    }

    private void ValidatePlayerPrefab(GameObject prefab, string label)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponent<Entity>() == null)
            Debug.LogWarning($"PlayerCharacterPrefabLibrary: {label} '{prefab.name}' nao possui Entity.", this);

        if (prefab.GetComponent<CharacterStats>() == null)
            Debug.LogWarning($"PlayerCharacterPrefabLibrary: {label} '{prefab.name}' nao possui CharacterStats.", this);

        if (prefab.GetComponent<CharacterIdentity>() == null)
            Debug.LogWarning($"PlayerCharacterPrefabLibrary: {label} '{prefab.name}' nao possui CharacterIdentity.", this);
    }
}
