using System.Collections.Generic;

public static class NpcRecruitmentPersistence
{
    private static readonly HashSet<string> recruitedNpcKeys = new HashSet<string>();

    public static bool IsNpcRecruited(string sceneKey, string npcId)
    {
        return recruitedNpcKeys.Contains(BuildKey(sceneKey, npcId));
    }

    public static void MarkNpcRecruited(string sceneKey, string npcId)
    {
        recruitedNpcKeys.Add(BuildKey(sceneKey, npcId));
    }

    public static void Clear()
    {
        recruitedNpcKeys.Clear();
    }

    private static string BuildKey(string sceneKey, string npcId)
    {
        string safeScene = string.IsNullOrWhiteSpace(sceneKey) ? "UnknownScene" : sceneKey.Trim();
        string safeNpc = string.IsNullOrWhiteSpace(npcId) ? "UnknownNpc" : npcId.Trim();
        return $"{safeScene}::{safeNpc}";
    }
}
