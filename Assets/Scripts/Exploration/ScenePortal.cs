using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Portal Identity")]
    [SerializeField] private string portalId = "Portal_A";

    [Header("Destination")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private string targetPortalId = "";
    [SerializeField] private Vector2Int arrivalOffset = Vector2Int.zero;

    [Header("Flow")]
    [SerializeField] private bool allowTravel = true;

    public string PortalId => portalId;
    public string TargetSceneName => targetSceneName;
    public string TargetPortalId => targetPortalId;

    public Vector2Int GetPortalCell()
    {
        return new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y));
    }

    public Vector2Int GetArrivalCell()
    {
        return GetPortalCell() + arrivalOffset;
    }

    public bool TryTriggerTransition()
    {
        if (!allowTravel)
            return false;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning($"ScenePortal '{name}': Target Scene Name is empty.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"ScenePortal '{name}': scene '{targetSceneName}' is not in Build Settings.");
            return false;
        }

        if (!ExplorationScenePersistenceData.PrepareTransition(this))
            return false;

        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        return true;
    }

    public static bool TryTriggerPortalAtCell(Vector2Int cell)
    {
        ScenePortal[] portals = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None);

        for (int i = 0; i < portals.Length; i++)
        {
            ScenePortal portal = portals[i];
            if (portal == null)
                continue;

            if (portal.GetPortalCell() != cell)
                continue;

            return portal.TryTriggerTransition();
        }

        return false;
    }
}
