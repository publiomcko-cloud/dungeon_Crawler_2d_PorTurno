using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChestActor : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string chestId = "chest_001";

    [Header("Rewards")]
    [SerializeField] private List<ItemData> staticItemRewards = new List<ItemData>();
    [SerializeField] private List<ItemGenerationProfile> generatedItemRewards = new List<ItemGenerationProfile>();
    [SerializeField] private int moneyReward = 0;

    [Header("Persistence")]
    [SerializeField] private bool openOnlyOnce = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer chestSpriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;
    [SerializeField] private bool disableActorWhenOpened = false;

    private ChestStoredState runtimeState;

    public string ChestId => chestId;
    public bool IsOpened => EnsureState().IsOpened;

    private void Awake()
    {
        EnsureState();
        ApplyVisualState(runtimeState.IsOpened);
    }

    public int GetStoredItemCount()
    {
        return EnsureState().Items.Count;
    }

    public InventoryItemEntry GetStoredItemAt(int index)
    {
        ChestStoredState state = EnsureState();
        if (index < 0 || index >= state.Items.Count)
            return null;

        InventoryItemEntry entry = state.Items[index];
        return entry != null ? entry.Clone() : new InventoryItemEntry();
    }

    public bool TryOpenInteraction(Entity interactor, out string message)
    {
        if (interactor == null)
        {
            message = "Interacao invalida.";
            return false;
        }

        if (disableActorWhenOpened && IsOpened)
        {
            message = "Esse bau ja foi aberto.";
            return false;
        }

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
        {
            message = "PartyInventory nao encontrado.";
            return false;
        }

        ChestStoredState state = EnsureState();
        bool firstOpen = !state.IsOpened;

        if (firstOpen)
        {
            state.IsOpened = true;
            if (openOnlyOnce)
                ChestPersistence.MarkChestOpened(GetPersistenceKey());

            ApplyVisualState(true);
        }

        int grantedMoney = TryGrantPendingMoney();
        message = grantedMoney > 0
            ? $"Bau aberto. Recebeu {grantedMoney} de dinheiro."
            : (firstOpen ? "Bau aberto." : "Bau aberto novamente.");

        ChestInteractionUI chestUi = ChestInteractionUI.GetOrCreateInstance();
        chestUi.Open(this, interactor, message);
        return true;
    }

    public bool TryMoveStoredItemToParty(int chestIndex, out string message)
    {
        ChestStoredState state = EnsureState();
        if (chestIndex < 0 || chestIndex >= state.Items.Count)
        {
            message = "Slot do bau invalido.";
            return false;
        }

        InventoryItemEntry entry = state.Items[chestIndex];
        if (entry == null || entry.IsEmpty)
        {
            message = "Esse slot do bau esta vazio.";
            return false;
        }

        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
        {
            message = "PartyInventory nao encontrado.";
            return false;
        }

        if (!partyInventory.AddEntry(entry))
        {
            message = "A mochila da party esta cheia.";
            return false;
        }

        state.Items.RemoveAt(chestIndex);
        message = $"{entry.ItemName} foi para a mochila da party.";
        return true;
    }

    public bool TryMovePartyItemToChest(int inventoryIndex, out string message)
    {
        PartyInventory partyInventory = FindFirstObjectByType<PartyInventory>();
        if (partyInventory == null)
        {
            message = "PartyInventory nao encontrado.";
            return false;
        }

        InventoryItemEntry entry = partyInventory.GetItem(inventoryIndex);
        if (entry == null || entry.IsEmpty)
        {
            message = "Esse slot da mochila esta vazio.";
            return false;
        }

        ChestStoredState state = EnsureState();
        state.Items.Add(entry.Clone());

        if (!partyInventory.RemoveAt(inventoryIndex))
        {
            state.Items.RemoveAt(state.Items.Count - 1);
            message = "Nao foi possivel mover o item para o bau.";
            return false;
        }

        message = $"{entry.ItemName} foi guardado no bau.";
        return true;
    }

    public static bool TryInteractAtCell(Vector2Int cell, Entity interactor)
    {
        ChestActor[] chests = FindObjectsByType<ChestActor>(FindObjectsSortMode.None);

        for (int i = 0; i < chests.Length; i++)
        {
            ChestActor chest = chests[i];
            if (chest == null || !chest.gameObject.activeInHierarchy)
                continue;

            if (chest.disableActorWhenOpened && chest.IsOpened)
                continue;

            if (chest.GetChestCell() != cell)
                continue;

            return chest.TryOpenInteraction(interactor, out _);
        }

        return false;
    }

    private ChestStoredState EnsureState()
    {
        if (runtimeState != null)
            return runtimeState;

        string key = GetPersistenceKey();
        runtimeState = ChestPersistence.GetOrCreateState(key, CreateInitialState);
        return runtimeState;
    }

    private ChestStoredState CreateInitialState()
    {
        List<InventoryItemEntry> items = new List<InventoryItemEntry>();

        for (int i = 0; i < staticItemRewards.Count; i++)
        {
            ItemData item = staticItemRewards[i];
            if (item != null)
                items.Add(InventoryItemEntry.FromStatic(item));
        }

        for (int i = 0; i < generatedItemRewards.Count; i++)
        {
            ItemGenerationProfile profile = generatedItemRewards[i];
            if (profile == null)
                continue;

            GeneratedItemInstance generated = ItemGenerator.Generate(profile);
            if (generated != null)
                items.Add(InventoryItemEntry.FromGenerated(generated));
        }

        bool alreadyOpened = openOnlyOnce && ChestPersistence.IsChestOpened(GetPersistenceKey());
        int pendingMoney = alreadyOpened ? 0 : Mathf.Max(0, moneyReward);

        return new ChestStoredState
        {
            IsOpened = alreadyOpened,
            PendingMoney = pendingMoney,
            Items = items
        };
    }

    private int TryGrantPendingMoney()
    {
        ChestStoredState state = EnsureState();
        int grantedMoney = Mathf.Max(0, state.PendingMoney);
        if (grantedMoney <= 0)
            return 0;

        if (PartyCurrency.Instance != null)
            PartyCurrency.Instance.AddMoney(grantedMoney);

        state.PendingMoney = 0;
        return grantedMoney;
    }

    private void ApplyVisualState(bool opened)
    {
        if (closedVisual != null)
            closedVisual.SetActive(!opened);

        if (openedVisual != null)
            openedVisual.SetActive(opened);

        if (chestSpriteRenderer != null)
        {
            if (opened && openedSprite != null)
                chestSpriteRenderer.sprite = openedSprite;
            else if (!opened && closedSprite != null)
                chestSpriteRenderer.sprite = closedSprite;
        }

        if (opened && disableActorWhenOpened)
            gameObject.SetActive(false);
    }

    private Vector2Int GetChestCell()
    {
        return new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y));
    }

    private string GetPersistenceKey()
    {
        return $"{GetSceneKey()}::{chestId}";
    }

    private string GetSceneKey()
    {
        return SceneManager.GetActiveScene().name;
    }
}
