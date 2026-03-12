using System;
using UnityEngine;

[Serializable]
public class LootDropEntry
{
    [Header("Source")]
    public ItemData staticItem;
    public ItemGenerationProfile generatedProfile;

    [Header("Chance")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;

    public bool HasValidItemSource()
    {
        return staticItem != null || generatedProfile != null;
    }

    public bool RollDrop()
    {
        return UnityEngine.Random.value <= dropChance;
    }
}