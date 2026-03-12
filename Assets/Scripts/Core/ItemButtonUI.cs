using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class ItemButtonUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
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

    private Action normalClickAction;
    private Action shiftClickAction;
    private Action beginDragAction;
    private Action endDragAction;
    private InventoryItemEntry tooltipEntry;

    private bool interactable = true;
    private Transform originalParent;
    private int originalSiblingIndex;

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

        ApplyInteractableVisual(true, true);
    }

    public void Setup(
        InventoryItemEntry entry,
        Action onNormalClick,
        Action onShiftClick = null,
        Action onBeginDrag = null,
        Action onEndDrag = null)
    {
        tooltipEntry = entry;
        normalClickAction = onNormalClick;
        shiftClickAction = onShiftClick;
        beginDragAction = onBeginDrag;
        endDragAction = onEndDrag;

        bool isEmpty = entry == null || entry.IsEmpty;
        bool hasAnyAction = onNormalClick != null || onShiftClick != null || onBeginDrag != null;

        if (iconImage != null)
        {
            iconImage.sprite = isEmpty ? null : entry.Icon;
            iconImage.color = isEmpty || entry.Icon == null ? emptyIconColor : enabledIconColor;
            iconImage.enabled = !isEmpty && entry.Icon != null;
        }

        ApplyInteractableVisual(hasAnyAction, isEmpty);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable)
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
        if (!interactable || beginDragAction == null)
            return;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvasGroup.blocksRaycasts = false;
        beginDragAction.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactable || beginDragAction == null)
            return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!interactable || beginDragAction == null)
            return;

        canvasGroup.blocksRaycasts = true;

        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        endDragAction?.Invoke();
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

    private void ApplyInteractableVisual(bool canClick, bool isEmpty)
    {
        interactable = canClick;

        if (button != null)
            button.interactable = true;

        if (backgroundImage != null)
            backgroundImage.color = isEmpty ? emptyColor : normalColor;
    }
}