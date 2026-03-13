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
    [SerializeField] private float dragAlpha = 0.8f;

    private Action normalClickAction;
    private Action shiftClickAction;
    private Action<ItemButtonUI> receiveDropAction;
    private InventoryItemEntry tooltipEntry;

    private bool canClick;
    private bool canDrag;

    private Transform originalParent;
    private int originalSiblingIndex;

    private RectTransform rectTransform;
    private Canvas rootCanvas;

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

        EnsureIcon();

        if (layoutElement != null)
        {
            layoutElement.minWidth = minWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 0f;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.transition = Selectable.Transition.ColorTint;
        }

        ApplyVisualState(false, false, true);
    }

    public void Setup(
        InventoryItemEntry entry,
        Action onNormalClick,
        Action onShiftClick = null,
        bool draggable = false,
        Action<ItemButtonUI> onReceiveDrop = null)
    {
        tooltipEntry = entry;
        normalClickAction = onNormalClick;
        shiftClickAction = onShiftClick;
        receiveDropAction = onReceiveDrop;

        bool isEmpty = entry == null || entry.IsEmpty;

        canClick = onNormalClick != null || onShiftClick != null;
        canDrag = draggable && !isEmpty;

        if (iconImage != null)
        {
            iconImage.sprite = isEmpty ? null : entry.Icon;
            iconImage.color = isEmpty || entry.Icon == null ? emptyIconColor : enabledIconColor;
            iconImage.enabled = !isEmpty && entry.Icon != null;
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

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || !HasItem)
            return;

        if (rectTransform != null)
            rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag || !HasItem)
            return;

        ItemButtonUI targetButton = null;

        if (eventData.pointerCurrentRaycast.gameObject != null)
            targetButton = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<ItemButtonUI>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (targetButton != null && targetButton != this && targetButton.CanReceiveDrop)
            targetButton.ReceiveDrop(this);

        if (this == null)
            return;

        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipEntry != null && !tooltipEntry.IsEmpty && ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Show(tooltipEntry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
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

    private void ApplyVisualState(bool clickable, bool draggable, bool isEmpty)
    {
        if (button != null)
            button.interactable = true;

        if (backgroundImage != null)
            backgroundImage.color = isEmpty ? emptyColor : normalColor;

        canClick = clickable;
        canDrag = draggable;
    }
}