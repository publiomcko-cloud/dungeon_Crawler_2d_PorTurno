using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(LayoutElement))]
public class ItemButtonUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Style")]
    [SerializeField] private float preferredHeight = 42f;
    [SerializeField] private float minHeight = 42f;
    [SerializeField] private Color normalColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.14f, 0.14f, 0.14f, 1f);
    [SerializeField] private Color disabledTextColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color enabledTextColor = Color.white;

    private Action normalClickAction;
    private Action shiftClickAction;
    private Action beginDragAction;
    private Action endDragAction;

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

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (layoutElement != null)
        {
            layoutElement.minHeight = minHeight;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.transition = Selectable.Transition.ColorTint;
        }

        ApplyInteractableVisual(true, false);
    }

    public void Setup(
        string text,
        Action onNormalClick,
        Action onShiftClick = null,
        Action onBeginDrag = null,
        Action onEndDrag = null)
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        normalClickAction = onNormalClick;
        shiftClickAction = onShiftClick;
        beginDragAction = onBeginDrag;
        endDragAction = onEndDrag;

        if (label != null)
            label.text = text;

        bool isEmpty = string.IsNullOrWhiteSpace(text) || text.Contains("Empty");
        bool hasAnyAction = onNormalClick != null || onShiftClick != null || onBeginDrag != null;

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

    private void ApplyInteractableVisual(bool canClick, bool isEmpty)
    {
        interactable = canClick;

        if (button != null)
            button.interactable = true;

        if (backgroundImage != null)
            backgroundImage.color = isEmpty ? emptyColor : normalColor;

        if (label != null)
            label.color = canClick ? enabledTextColor : disabledTextColor;
    }
}