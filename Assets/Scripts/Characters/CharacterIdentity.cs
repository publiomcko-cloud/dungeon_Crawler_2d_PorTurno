using UnityEngine;

[DisallowMultipleComponent]
public class CharacterIdentity : MonoBehaviour
{
    [SerializeField] private string characterId = "";

    public string CharacterId => NormalizeCharacterId(characterId, gameObject.name);

    public void SetCharacterId(string value)
    {
        characterId = NormalizeCharacterId(value, gameObject.name);
    }

    public static string ResolveFromEntity(Entity entity)
    {
        if (entity == null)
            return "";

        CharacterIdentity identity = entity.GetComponent<CharacterIdentity>();
        if (identity != null)
            return identity.CharacterId;

        return NormalizeCharacterId("", entity.name);
    }

    private static string NormalizeCharacterId(string value, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        if (!string.IsNullOrWhiteSpace(fallback))
            return fallback.Trim();

        return "UnknownCharacter";
    }
}
