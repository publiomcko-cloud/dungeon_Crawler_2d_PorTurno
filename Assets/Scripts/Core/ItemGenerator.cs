using UnityEngine;

public static class ItemGenerator
{
    public static GeneratedItemInstance Generate(ItemGenerationProfile profile)
    {
        if (profile == null)
            return null;

        StatBlock rolledStats = profile.RollStats();

        GeneratedItemInstance item = new GeneratedItemInstance
        {
            itemName = BuildName(profile, rolledStats),
            description = BuildDescription(profile, rolledStats),
            icon = profile.icon,
            slotType = profile.slotType,
            rarity = profile.rarity,
            requiredLevel = Mathf.Max(1, profile.requiredLevel),
            value = Mathf.Max(0, profile.value),
            statBonus = rolledStats
        };

        return item;
    }

    private static string BuildName(ItemGenerationProfile profile, StatBlock stats)
    {
        string suffix = GetPrimaryStatSuffix(stats);
        return $"{profile.generatedNamePrefix} {suffix}".Trim();
    }

    private static string BuildDescription(ItemGenerationProfile profile, StatBlock stats)
    {
        return $"{profile.rarity} {profile.slotType}";
    }

    private static string GetPrimaryStatSuffix(StatBlock stats)
    {
        int bestIntValue = stats.hp;
        string bestName = "Vitality";

        if (stats.atk > bestIntValue)
        {
            bestIntValue = stats.atk;
            bestName = "Power";
        }

        if (stats.def > bestIntValue)
        {
            bestIntValue = stats.def;
            bestName = "Guard";
        }

        if (stats.ap > bestIntValue)
        {
            bestIntValue = stats.ap;
            bestName = "Focus";
        }

        if (stats.crit > bestIntValue)
            bestName = "Precision";

        return bestName;
    }
}