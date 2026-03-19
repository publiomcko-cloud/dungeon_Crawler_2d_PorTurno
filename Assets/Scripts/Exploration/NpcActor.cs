using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NpcType
{
    Recruit,
    Quest,
    Merchant
}

public class NpcActor : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string npcId = "npc_001";
    [SerializeField] private string displayName = "NPC";
    [SerializeField] private NpcType npcType = NpcType.Recruit;

    [Header("Recruit UI")]
    [TextArea]
    [SerializeField] private string greetingText = "Quer se juntar ao grupo?";
    [SerializeField] private string confirmButtonLabel = "Contratar";
    [SerializeField] private string cancelButtonLabel = "Cancelar";

    [Header("Recruitment")]
    [SerializeField] private int recruitmentCost = 25;
    [SerializeField] private int maxPartySize = 4;
    [SerializeField] private GameObject recruitPartyMemberPrefab;
    [SerializeField] private bool removeNpcAfterRecruitment = true;

    [Header("Quest")]
    [TextArea]
    [SerializeField] private string questGreetingText = "Tem trabalho para voce.";
    [SerializeField] private List<QuestDefinition> questOffers = new List<QuestDefinition>();

    [Header("Merchant")]
    [TextArea]
    [SerializeField] private string merchantGreetingText = "Quer comprar ou vender?";
    [SerializeField] private List<ItemData> merchantStock = new List<ItemData>();

    private readonly List<ItemData> runtimeMerchantStock = new List<ItemData>();

    public string NpcId => npcId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public NpcType Type => npcType;
    public string GreetingText => greetingText;
    public string QuestGreetingText => string.IsNullOrWhiteSpace(questGreetingText) ? greetingText : questGreetingText;
    public string MerchantGreetingText => string.IsNullOrWhiteSpace(merchantGreetingText) ? greetingText : merchantGreetingText;
    public string ConfirmButtonLabel => string.IsNullOrWhiteSpace(confirmButtonLabel) ? "Confirmar" : confirmButtonLabel;
    public string CancelButtonLabel => string.IsNullOrWhiteSpace(cancelButtonLabel) ? "Cancelar" : cancelButtonLabel;
    public int RecruitmentCost => Mathf.Max(0, recruitmentCost);
    public bool IsRecruited => NpcRecruitmentPersistence.IsNpcRecruited(GetSceneKey(), npcId);

    private void Awake()
    {
        BuildRuntimeMerchantStock();

        if (IsRecruited && removeNpcAfterRecruitment)
            Destroy(gameObject);
    }

    public bool TryOpenInteraction(Entity interactor)
    {
        if (interactor == null || !gameObject.activeInHierarchy)
            return false;

        NpcInteractionUI ui = NpcInteractionUI.GetOrCreateInstance();
        if (ui == null)
            return false;

        ui.Open(this, interactor);
        return true;
    }

    public bool TryRecruit(Entity interactor, out string message)
    {
        if (npcType != NpcType.Recruit)
        {
            message = "Esse NPC nao e recrutavel.";
            return false;
        }

        if (IsRecruited)
        {
            message = "Esse NPC ja faz parte da party.";
            return false;
        }

        if (recruitPartyMemberPrefab == null)
        {
            message = "Prefab do recruta nao configurado.";
            return false;
        }

        if (PartyCurrency.Instance == null)
        {
            message = "PartyCurrency nao encontrado.";
            return false;
        }

        int aliveMembers = CountAlivePartyMembers();
        if (aliveMembers >= Mathf.Max(1, maxPartySize))
        {
            message = $"A party ja esta no limite de {Mathf.Max(1, maxPartySize)} membros.";
            return false;
        }

        if (!PartyCurrency.Instance.CanAfford(RecruitmentCost))
        {
            message = $"Dinheiro insuficiente. Necessario: {RecruitmentCost}.";
            return false;
        }

        GridManager gridManager = GridManager.Instance != null ? GridManager.Instance : FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            message = "GridManager nao encontrado.";
            return false;
        }

        Entity leader = PartyAnchorService.Instance != null ? PartyAnchorService.Instance.GetLeader() : null;
        Vector2Int spawnCell = leader != null ? leader.GridPosition : interactor.GridPosition;

        List<Entity> occupants = gridManager.GetEntitiesAtCell(spawnCell);
        if (occupants.Count >= gridManager.maxEntitiesPerCell)
        {
            message = "A celula atual da party esta cheia.";
            return false;
        }

        if (!PartyCurrency.Instance.TrySpend(RecruitmentCost))
        {
            message = $"Dinheiro insuficiente. Necessario: {RecruitmentCost}.";
            return false;
        }

        Vector3 spawnPosition = gridManager.GetCellCenterWorld(spawnCell);
        GameObject instance = Instantiate(recruitPartyMemberPrefab, spawnPosition, Quaternion.identity);
        Entity recruitedEntity = instance.GetComponent<Entity>();

        if (recruitedEntity == null)
        {
            Destroy(instance);
            PartyCurrency.Instance.AddMoney(RecruitmentCost);
            message = "O prefab do recruta precisa de Entity.";
            return false;
        }

        recruitedEntity.team = Team.Player;
        gridManager.RegisterEntity(recruitedEntity, spawnCell);

        NpcRecruitmentPersistence.MarkNpcRecruited(GetSceneKey(), npcId);

        if (removeNpcAfterRecruitment)
            Destroy(gameObject);

        if (PartyAnchorService.Instance != null)
            PartyAnchorService.Instance.RefreshLeader();

        message = $"{DisplayName} entrou para a party.";
        return true;
    }

    public bool TryAcceptQuest(QuestDefinition questDefinition, out string message)
    {
        if (npcType != NpcType.Quest)
        {
            message = "Esse NPC nao oferece quests.";
            return false;
        }

        if (questDefinition == null || !questOffers.Contains(questDefinition))
        {
            message = "Quest nao encontrada na lista desse NPC.";
            return false;
        }

        if (QuestTracker.Instance == null)
        {
            message = "QuestTracker nao encontrado.";
            return false;
        }

        return QuestTracker.Instance.TryAcceptQuest(questDefinition, out message);
    }

    public bool TryClaimQuestReward(out string message)
    {
        if (npcType != NpcType.Quest)
        {
            message = "Esse NPC nao entrega quests.";
            return false;
        }

        if (QuestTracker.Instance == null)
        {
            message = "QuestTracker nao encontrado.";
            return false;
        }

        return QuestTracker.Instance.TryClaimActiveQuest(out message);
    }

    public int GetAvailableQuestCount()
    {
        return GetAvailableQuestDefinitions().Count;
    }

    public QuestDefinition GetAvailableQuestAt(int index)
    {
        List<QuestDefinition> available = GetAvailableQuestDefinitions();
        if (index < 0 || index >= available.Count)
            return null;

        return available[index];
    }

    public int GetMerchantStockCount()
    {
        return runtimeMerchantStock.Count(item => item != null);
    }

    public InventoryItemEntry GetMerchantStockEntry(int index)
    {
        List<ItemData> validStock = runtimeMerchantStock.Where(item => item != null).ToList();
        if (index < 0 || index >= validStock.Count)
            return null;

        return InventoryItemEntry.FromStatic(validStock[index]);
    }

    public bool TryBuyMerchantItem(int stockIndex, out string message)
    {
        if (npcType != NpcType.Merchant)
        {
            message = "Esse NPC nao vende itens.";
            return false;
        }

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
        {
            message = "PartyInventory nao encontrado.";
            return false;
        }

        InventoryItemEntry entry = GetMerchantStockEntry(stockIndex);
        if (entry == null || entry.IsEmpty)
        {
            message = "Item invalido.";
            return false;
        }

        if (partyInventory.GetFirstEmptySlotIndex() < 0)
        {
            message = "A mochila da party esta cheia.";
            return false;
        }

        if (PartyCurrency.Instance == null)
        {
            message = "PartyCurrency nao encontrado.";
            return false;
        }

        if (!PartyCurrency.Instance.CanAfford(entry.Value))
        {
            message = $"Dinheiro insuficiente. Necessario: {entry.Value}.";
            return false;
        }

        if (!PartyCurrency.Instance.TrySpend(entry.Value))
        {
            message = $"Dinheiro insuficiente. Necessario: {entry.Value}.";
            return false;
        }

        if (!partyInventory.AddEntry(entry))
        {
            PartyCurrency.Instance.AddMoney(entry.Value);
            message = "Nao foi possivel adicionar o item na mochila.";
            return false;
        }

        RemoveRuntimeMerchantStockAt(stockIndex);

        message = $"Comprou {entry.ItemName} por {entry.Value}.";
        return true;
    }

    public bool TrySellInventoryItem(int inventoryIndex, out string message)
    {
        if (npcType != NpcType.Merchant)
        {
            message = "Esse NPC nao compra itens.";
            return false;
        }

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
        {
            message = "PartyInventory nao encontrado.";
            return false;
        }

        InventoryItemEntry entry = partyInventory.GetItem(inventoryIndex);
        if (entry == null || entry.IsEmpty)
        {
            message = "Nenhum item nessa posicao.";
            return false;
        }

        int sellValue = Mathf.Max(0, entry.Value);
        if (!partyInventory.RemoveAt(inventoryIndex))
        {
            message = "Nao foi possivel vender o item.";
            return false;
        }

        if (PartyCurrency.Instance != null && sellValue > 0)
            PartyCurrency.Instance.AddMoney(sellValue);

        message = $"Vendeu {entry.ItemName} por {sellValue}.";
        return true;
    }

    public static bool TryInteractAtCell(Vector2Int cell, Entity interactor)
    {
        NpcActor[] npcs = FindObjectsByType<NpcActor>(FindObjectsSortMode.None);

        for (int i = 0; i < npcs.Length; i++)
        {
            NpcActor npc = npcs[i];
            if (npc == null || !npc.gameObject.activeInHierarchy)
                continue;

            Vector2Int npcCell = new Vector2Int(
                Mathf.FloorToInt(npc.transform.position.x),
                Mathf.FloorToInt(npc.transform.position.y));

            if (npcCell != cell)
                continue;

            return npc.TryOpenInteraction(interactor);
        }

        return false;
    }

    private List<QuestDefinition> GetAvailableQuestDefinitions()
    {
        List<QuestDefinition> result = new List<QuestDefinition>();
        Entity leader = PartyAnchorService.Instance != null ? PartyAnchorService.Instance.GetLeader() : null;
        int leaderLevel = leader != null ? leader.Level : 1;

        for (int i = 0; i < questOffers.Count; i++)
        {
            QuestDefinition quest = questOffers[i];
            if (quest == null)
                continue;

            if (leaderLevel < quest.MinimumLeaderLevel)
                continue;

            if (QuestTracker.Instance != null && QuestTracker.Instance.IsQuestCompleted(quest))
                continue;

            result.Add(quest);
        }

        return result;
    }

    private int CountAlivePartyMembers()
    {
        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entity != null && entity.team == Team.Player && !entity.IsDead)
                count += 1;
        }

        return count;
    }

    private string GetSceneKey()
    {
        return SceneManager.GetActiveScene().name;
    }

    private void BuildRuntimeMerchantStock()
    {
        runtimeMerchantStock.Clear();

        for (int i = 0; i < merchantStock.Count; i++)
        {
            if (merchantStock[i] != null)
                runtimeMerchantStock.Add(merchantStock[i]);
        }
    }

    private void RemoveRuntimeMerchantStockAt(int stockIndex)
    {
        if (stockIndex < 0 || stockIndex >= runtimeMerchantStock.Count)
            return;

        runtimeMerchantStock.RemoveAt(stockIndex);
    }
}
