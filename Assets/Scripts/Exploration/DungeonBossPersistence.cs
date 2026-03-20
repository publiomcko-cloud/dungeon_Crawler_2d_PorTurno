using System.Collections.Generic;

public static class DungeonBossPersistence
{
    private static readonly HashSet<string> defeatedBossKeys = new HashSet<string>();

    public static bool IsBossDefeated(string bossKey)
    {
        return !string.IsNullOrWhiteSpace(bossKey) && defeatedBossKeys.Contains(bossKey);
    }

    public static void MarkBossDefeated(string bossKey)
    {
        if (string.IsNullOrWhiteSpace(bossKey))
            return;

        defeatedBossKeys.Add(bossKey);
    }

    public static void Clear()
    {
        defeatedBossKeys.Clear();
    }
}
