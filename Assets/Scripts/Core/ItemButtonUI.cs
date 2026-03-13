using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ItemButtonSlotKind
{
    None,
    Inventory,
    Equipped,
    Ground
}

public class ItemButtonUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Style")]
    [SerializeField] private float preferredWidth = 92f;
    [SerializeField] private float preferredHeight = 92f;
    [SerializeField] private float minWidth = 92f;
    [SerializeField] private float minHeight = 92f;
    [SerializeField] private Color normalColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.14f, 0.14f, 0.14f, 1f);
    [SerializeField] private Color enabledIconColor = Color.white;
    [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float ghostAlpha = 0.9f;

    [Header("Rarity Tint")]
    [SerializeField] private bool tintSlotByRarity = true;
    [SerializeField] private float rarityTintStrength = 0.45f;

    private Action normalClickAction;
    private Action shiftClickAction;
    private Action<ItemButtonUI> receiveDropAction;

    private InventoryItemEntry tooltipEntry;
    private InventoryItemEntry compareEntry;

    private bool canClick;
    private bool canDrag;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera canvasCamera;

    private GameObject dragGhostObject;
    private RectTransform dragGhostRect;
    private Image dragGhostImage;

    public ItemButtonSlotKind SlotKind { get; private set; } = ItemButtonSlotKind.None;
    public int InventoryIndex { get; private set; } = -1;
    public EquipmentSlotType EquippedSlotType { get; private set; } = EquipmentSlotType.Weapon;
    public GroundItem GroundItemRef { get; private set; }

    public bool HasItem => tooltipEntry != null && !tooltipEntry.IsEmpty;
    public InventoryItemEntry Entry => tooltipEntry;
    public bool CanReceiveDrop => receiveDropAction != null;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = rootCanvas.worldCamera;

        EnsureIcon();

        if (layoutElement != null)
        {
            layoutElement.minWidth = minWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        if (backgroundImage != null)
            backgroundImage.raycastTarget = true;

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = backgroundImage;
        }

        ResetVisualCompletely();
    }

    public void Setup(
        InventoryItemEntry entry,
        Action onNormalClick,
        Action onShiftClick = null,
        bool draggable = false,
        Action<ItemButtonUI> onReceiveDrop = null,
        InventoryItemEntry compareTarget = null)
    {
        tooltipEntry = entry;
        compareEntry = compareTarget;
        normalClickAction = onNormalClick;
        shiftClickAction = onShiftClick;
        receiveDropAction = onReceiveDrop;

        bool isEmpty = entry == null || entry.IsEmpty;

        canClick = onNormalClick != null || onShiftClick != null;
        canDrag = draggable && !isEmpty;

        ResetVisualCompletely();

        if (iconImage != null)
        {
            if (isEmpty || entry.Icon == null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                iconImage.color = emptyIconColor;
            }
            else
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = true;
                iconImage.color = enabledIconColor;
            }
        }

        ApplyVisualState(canClick, canDrag, isEmpty);
    }

    public void ConfigureAsInventorySlot(int inventoryIndex)
    {
        SlotKind = ItemButtonSlotKind.Inventory;
        InventoryIndex = inventoryIndex;
        GroundItemRef = null;
    }

    public void ConfigureAsEquippedSlot(EquipmentSlotType slotType)
    {
        SlotKind = ItemButtonSlotKind.Equipped;
        EquippedSlotType = slotType;
        InventoryIndex = -1;
        GroundItemRef = null;
    }

    public void ConfigureAsGroundSlot(GroundItem groundItem)
    {
        SlotKind = ItemButtonSlotKind.Ground;
        GroundItemRef = groundItem;
        InventoryIndex = -1;
    }

    public void ClearContext()
    {
        SlotKind = ItemButtonSlotKind.None;
        InventoryIndex = -1;
        GroundItemRef = null;
    }

    public void ReceiveDrop(ItemButtonUI sourceButton)
    {
        if (receiveDropAction == null || sourceButton == null)
            return;

        receiveDropAction.Invoke(sourceButton);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick)
            return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (shiftPressed && shiftClickAction != null)
        {
            shiftClickAction.Invoke();
            return;
        }

        normalClickAction?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag || !HasItem)
            return;

        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();

        CreateDragGhost();
        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || !HasItem)
            return;

        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag || !HasItem)
            return;

        ItemButtonUI targetButton = null;

        if (eventData.pointerCurrentRaycast.gameObject != null)
            targetButton = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<ItemButtonUI>();

        DestroyDragGhost();

        if (targetButton != null && targetButton != this && targetButton.CanReceiveDrop)
            targetButton.ReceiveDrop(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!HasItem || ItemTooltipUI.Instance == null)
            return;

        ItemTooltipUI.Instance.Show(tooltipEntry, compareEntry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private void CreateDragGhost()
    {
        DestroyDragGhost();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null || tooltipEntry == null || tooltipEntry.IsEmpty || tooltipEntry.Icon == null)
            return;

        dragGhostObject = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragGhostObject.transform.SetParent(rootCanvas.transform, false);
        dragGhostObject.transform.SetAsLastSibling();

        dragGhostRect = dragGhostObject.GetComponent<RectTransform>();
        dragGhostImage = dragGhostObject.GetComponent<Image>();
        CanvasGroup ghostGroup = dragGhostObject.GetComponent<CanvasGroup>();

        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;
        ghostGroup.alpha = ghostAlpha;

        dragGhostImage.sprite = tooltipEntry.Icon;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.raycastTarget = false;
        dragGhostImage.color = enabledIconColor;

        Vector2 size = rectTransform != null ? rectTransform.rect.size : new Vector2(preferredWidth, preferredHeight);
        dragGhostRect.sizeDelta = size;
        dragGhostRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragGhostRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (dragGhostRect == null || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
            return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasCamera,
            out localPoint))
        {
            dragGhostRect.anchoredPosition = localPoint;
        }
    }

    private void DestroyDragGhost()
    {
        if (dragGhostObject != null)
            Destroy(dragGhostObject);

        dragGhostObject = null;
        dragGhostRect = null;
        dragGhostImage = null;
    }

    private void EnsureIcon()
    {
        if (iconImage != null)
            return;

        Transform existing = transform.Find("Icon");
        if (existing != null)
        {
            iconImage = existing.GetComponent<Image>();
            return;
        }

        GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);

        iconImage = go.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void ResetVisualCompletely()
    {
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.interactable = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = emptyColor;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.color = emptyIconColor;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ApplyVisualState(bool clickable, bool draggable, bool isEmpty)
    {
        if (backgroundImage != null)
        {
            if (isEmpty)
            {
                backgroundImage.color = emptyColor;
            }
            else
            {
                Color finalColor = normalColor;

                if (tintSlotByRarity && tooltipEntry != null && !tooltipEntry.IsEmpty)
                {
                    Color rarityColor = GetRarityColor(tooltipEntry.Rarity);
                    finalColor = Color.Lerp(normalColor, rarityColor, rarityTintStrength);
                    finalColor.a = normalColor.a;
                }

                backgroundImage.color = finalColor;
            }
        }

        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.interactable = clickable;
        }

        canClick = clickable;
        canDrag = draggable;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return new Color(0.75f, 0.75f, 0.75f, 1f);
            case ItemRarity.Uncommon:
                return new Color(0.35f, 0.85f, 0.35f, 1f);
            case ItemRarity.Rare:
                return new Color(0.3f, 0.55f, 1f, 1f);
            case ItemRarity.Epic:
                return new Color(0.75f, 0.35f, 0.95f, 1f);
            case ItemRarity.Legendary:
                return new Color(1f, 0.65f, 0.15f, 1f);
            default:
                return Color.white;
        }
    }
}