using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootWindowUIDebugProbe : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private LootWindowUI lootWindowUI;
    [SerializeField] private LootWindowGridAutoBuilder gridAutoBuilder;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode dumpStateKey = KeyCode.F8;
    [SerializeField] private KeyCode spawnMarkersKey = KeyCode.F9;
    [SerializeField] private KeyCode clearMarkersKey = KeyCode.F10;
    [SerializeField] private KeyCode rebuildKey = KeyCode.F11;

    [Header("Marker Visual")]
    [SerializeField] private Vector2 markerSize = new Vector2(32f, 32f);
    [SerializeField] private Color selectorMarkerColor = new Color(1f, 0.5f, 0.2f, 0.9f);
    [SerializeField] private Color equippedMarkerColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    [SerializeField] private Color inventoryMarkerColor = new Color(0.2f, 1f, 0.35f, 0.9f);
    [SerializeField] private Color groundMarkerColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    private readonly List<GameObject> debugMarkers = new List<GameObject>();

    private void Awake()
    {
        if (lootWindowUI == null)
            lootWindowUI = GetComponent<LootWindowUI>();

        if (gridAutoBuilder == null)
            gridAutoBuilder = GetComponent<LootWindowGridAutoBuilder>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(dumpStateKey))
            DumpState();

        if (Input.GetKeyDown(spawnMarkersKey))
            SpawnMarkers();

        if (Input.GetKeyDown(clearMarkersKey))
            ClearMarkers();

        if (Input.GetKeyDown(rebuildKey))
            RebuildAndDump();
    }

    [ContextMenu("Debug Dump State")]
    public void DumpState()
    {
        Debug.Log("========== LOOT WINDOW DEBUG BEGIN ==========");

        ResolveTargets();

        Debug.Log($"[Probe] LootWindowUI found: {lootWindowUI != null}");
        Debug.Log($"[Probe] LootWindowGridAutoBuilder found: {gridAutoBuilder != null}");

        if (lootWindowUI == null)
        {
            Debug.LogWarning("[Probe] LootWindowUI não encontrado.");
            Debug.Log("========== LOOT WINDOW DEBUG END ==========");
            return;
        }

        System.Type uiType = lootWindowUI.GetType();
        Debug.Log($"[Probe] LootWindowUI runtime type: {uiType.FullName}");

        bool hasSelectorField = HasField(uiType, "selectorContentRoot");
        Debug.Log($"[Probe] Has selectorContentRoot field: {hasSelectorField}");

        GameObject windowRoot = GetPrivateField<GameObject>(lootWindowUI, "windowRoot");
        Button closeButton = GetPrivateField<Button>(lootWindowUI, "closeButton");
        Transform selectorContentRoot = GetPrivateField<Transform>(lootWindowUI, "selectorContentRoot");
        Transform equippedContentRoot = GetPrivateField<Transform>(lootWindowUI, "equippedContentRoot");
        Transform inventoryContentRoot = GetPrivateField<Transform>(lootWindowUI, "inventoryContentRoot");
        Transform groundLootContentRoot = GetPrivateField<Transform>(lootWindowUI, "groundLootContentRoot");
        ItemButtonUI itemButtonPrefab = GetPrivateField<ItemButtonUI>(lootWindowUI, "itemButtonPrefab");
        TMP_Text titleText = GetPrivateField<TMP_Text>(lootWindowUI, "titleText");
        TMP_Text hintText = GetPrivateField<TMP_Text>(lootWindowUI, "hintText");
        Entity currentEntity = GetPrivateField<Entity>(lootWindowUI, "currentEntity");
        PlayerInventory currentInventory = GetPrivateField<PlayerInventory>(lootWindowUI, "currentInventory");
        bool isOpen = GetPrivateField<bool>(lootWindowUI, "isOpen");
        List<GameObject> spawnedUI = GetPrivateField<List<GameObject>>(lootWindowUI, "spawnedUI");

        Debug.Log($"[Probe] windowRoot: {DescribeObject(windowRoot)}");
        Debug.Log($"[Probe] closeButton: {DescribeObject(closeButton)}");
        Debug.Log($"[Probe] selectorContentRoot: {DescribeTransform(selectorContentRoot)}");
        Debug.Log($"[Probe] equippedContentRoot: {DescribeTransform(equippedContentRoot)}");
        Debug.Log($"[Probe] inventoryContentRoot: {DescribeTransform(inventoryContentRoot)}");
        Debug.Log($"[Probe] groundLootContentRoot: {DescribeTransform(groundLootContentRoot)}");
        Debug.Log($"[Probe] itemButtonPrefab: {DescribeObject(itemButtonPrefab)}");
        Debug.Log($"[Probe] titleText: {DescribeObject(titleText)}");
        Debug.Log($"[Probe] hintText: {DescribeObject(hintText)}");
        Debug.Log($"[Probe] isOpen: {isOpen}");
        Debug.Log($"[Probe] currentEntity: {DescribeEntity(currentEntity)}");
        Debug.Log($"[Probe] currentInventory: {DescribeObject(currentInventory)}");
        Debug.Log($"[Probe] spawnedUI count(list): {(spawnedUI != null ? spawnedUI.Count : -1)}");

        if (equippedContentRoot != null)
            Debug.Log($"[Probe] equippedContentRoot childCount: {equippedContentRoot.childCount}");

        if (inventoryContentRoot != null)
            Debug.Log($"[Probe] inventoryContentRoot childCount: {inventoryContentRoot.childCount}");

        if (groundLootContentRoot != null)
            Debug.Log($"[Probe] groundLootContentRoot childCount: {groundLootContentRoot.childCount}");

        if (selectorContentRoot != null)
            Debug.Log($"[Probe] selectorContentRoot childCount: {selectorContentRoot.childCount}");

        DumpRect("windowRoot", windowRoot != null ? windowRoot.GetComponent<RectTransform>() : null);
        DumpRect("selectorContentRoot", selectorContentRoot as RectTransform);
        DumpRect("equippedContentRoot", equippedContentRoot as RectTransform);
        DumpRect("inventoryContentRoot", inventoryContentRoot as RectTransform);
        DumpRect("groundLootContentRoot", groundLootContentRoot as RectTransform);

        if (gridAutoBuilder != null)
            DumpBuilderState();

        DumpPlayersState();

        Debug.Log("=========== LOOT WINDOW DEBUG END ===========");
    }

    [ContextMenu("Debug Spawn Markers")]
    public void SpawnMarkers()
    {
        ResolveTargets();
        ClearMarkers();

        if (lootWindowUI == null)
        {
            Debug.LogWarning("[Probe] Não foi possível spawnar markers. LootWindowUI nulo.");
            return;
        }

        Transform selectorContentRoot = GetPrivateField<Transform>(lootWindowUI, "selectorContentRoot");
        Transform equippedContentRoot = GetPrivateField<Transform>(lootWindowUI, "equippedContentRoot");
        Transform inventoryContentRoot = GetPrivateField<Transform>(lootWindowUI, "inventoryContentRoot");
        Transform groundLootContentRoot = GetPrivateField<Transform>(lootWindowUI, "groundLootContentRoot");

        if (selectorContentRoot != null)
            CreateMarker(selectorContentRoot, "SelectorMarker", selectorMarkerColor);

        if (equippedContentRoot != null)
        {
            CreateMarker(equippedContentRoot, "EquipMarker_1", equippedMarkerColor);
            CreateMarker(equippedContentRoot, "EquipMarker_2", equippedMarkerColor);
            CreateMarker(equippedContentRoot, "EquipMarker_3", equippedMarkerColor);
        }

        if (inventoryContentRoot != null)
        {
            for (int i = 0; i < 8; i++)
                CreateMarker(inventoryContentRoot, $"InventoryMarker_{i}", inventoryMarkerColor);
        }

        if (groundLootContentRoot != null)
        {
            for (int i = 0; i < 8; i++)
                CreateMarker(groundLootContentRoot, $"GroundMarker_{i}", groundMarkerColor);
        }

        Debug.Log($"[Probe] Markers criados: {debugMarkers.Count}");
        DumpState();
    }

    [ContextMenu("Debug Clear Markers")]
    public void ClearMarkers()
    {
        for (int i = debugMarkers.Count - 1; i >= 0; i--)
        {
            if (debugMarkers[i] != null)
                DestroyImmediate(debugMarkers[i]);
        }

        debugMarkers.Clear();
        Debug.Log("[Probe] Markers removidos.");
    }

    [ContextMenu("Debug Rebuild And Dump")]
    public void RebuildAndDump()
    {
        ResolveTargets();

        if (gridAutoBuilder != null)
        {
            Debug.Log("[Probe] Executando Build() no LootWindowGridAutoBuilder.");
            gridAutoBuilder.Build();
        }
        else
        {
            Debug.LogWarning("[Probe] LootWindowGridAutoBuilder não encontrado.");
        }

        DumpState();
    }

    private void DumpBuilderState()
    {
        if (gridAutoBuilder == null)
            return;

        Debug.Log($"[Probe] Builder type: {gridAutoBuilder.GetType().FullName}");

        RectTransform windowRoot = GetPrivateField<RectTransform>(gridAutoBuilder, "windowRoot");
        LootWindowUI builderLootWindowUI = GetPrivateField<LootWindowUI>(gridAutoBuilder, "lootWindowUI");
        ItemButtonUI builderItemButtonPrefab = GetPrivateField<ItemButtonUI>(gridAutoBuilder, "itemButtonPrefab");

        Debug.Log($"[Probe] Builder.windowRoot: {DescribeObject(windowRoot)}");
        Debug.Log($"[Probe] Builder.lootWindowUI: {DescribeObject(builderLootWindowUI)}");
        Debug.Log($"[Probe] Builder.itemButtonPrefab: {DescribeObject(builderItemButtonPrefab)}");

        DumpRect("Builder.windowRoot Rect", windowRoot);
    }

    private void DumpPlayersState()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
        Debug.Log($"[Probe] Total Entity objects found: {entities.Length}");

        int validPlayers = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity e = entities[i];
            if (e == null)
                continue;

            PlayerInventory inventory = e.GetComponent<PlayerInventory>();
            bool isPlayer = e.team == Team.Player;
            bool alive = !e.IsDead;

            Debug.Log($"[Probe] Entity[{i}] name={e.name} | team={e.team} | alive={alive} | hasInventory={inventory != null} | grid={e.GridPosition}");

            if (isPlayer && alive && inventory != null)
                validPlayers++;
        }

        Debug.Log($"[Probe] Valid players for multi-inventory selector: {validPlayers}");
    }

    private void CreateMarker(Transform parent, string markerName, Color color)
    {
        GameObject marker = new GameObject(markerName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        marker.transform.SetParent(parent, false);

        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.sizeDelta = markerSize;

        Image image = marker.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        LayoutElement layout = marker.GetComponent<LayoutElement>();
        layout.minWidth = markerSize.x;
        layout.preferredWidth = markerSize.x;
        layout.minHeight = markerSize.y;
        layout.preferredHeight = markerSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        debugMarkers.Add(marker);
    }

    private void ResolveTargets()
    {
        if (lootWindowUI == null)
            lootWindowUI = GetComponent<LootWindowUI>();

        if (lootWindowUI == null)
            lootWindowUI = FindFirstObjectByType<LootWindowUI>();

        if (gridAutoBuilder == null)
            gridAutoBuilder = GetComponent<LootWindowGridAutoBuilder>();

        if (gridAutoBuilder == null)
            gridAutoBuilder = FindFirstObjectByType<LootWindowGridAutoBuilder>();
    }

    private bool HasField(System.Type type, string fieldName)
    {
        return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) != null;
    }

    private T GetPrivateField<T>(object target, string fieldName)
    {
        if (target == null)
            return default;

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
            return default;

        object value = field.GetValue(target);
        if (value == null)
            return default;

        return (T)value;
    }

    private string DescribeObject(Object obj)
    {
        return obj == null ? "NULL" : $"{obj.name} ({obj.GetType().Name})";
    }

    private string DescribeTransform(Transform t)
    {
        if (t == null)
            return "NULL";

        RectTransform rt = t as RectTransform;
        if (rt == null)
            return $"{t.name} (Transform)";

        return $"{t.name} (RectTransform, active={t.gameObject.activeInHierarchy}, children={t.childCount})";
    }

    private string DescribeEntity(Entity entity)
    {
        if (entity == null)
            return "NULL";

        return $"{entity.name} | team={entity.team} | dead={entity.IsDead} | grid={entity.GridPosition}";
    }

    private void DumpRect(string label, RectTransform rect)
    {
        if (rect == null)
        {
            Debug.Log($"[Probe] {label}: NULL");
            return;
        }

        Debug.Log(
            $"[Probe] {label}: " +
            $"anchoredPos={rect.anchoredPosition} | " +
            $"sizeDelta={rect.sizeDelta} | " +
            $"rectSize={rect.rect.size} | " +
            $"anchorMin={rect.anchorMin} | " +
            $"anchorMax={rect.anchorMax} | " +
            $"offsetMin={rect.offsetMin} | " +
            $"offsetMax={rect.offsetMax} | " +
            $"active={rect.gameObject.activeInHierarchy}"
        );
    }
}