using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image image;
    [HideInInspector] public Transform parentAfterDrag;
    private Vector2 startTouchPosition;
    private bool isDrag = false;
    public float tapThreshold = 10f;
    private bool isEnlarged = false;
    [HideInInspector] public string instanceId;
    [HideInInspector] public string cardId;
    public GameObject cardPanel;
    public Image panelImage;
    public Button closeButton;

    public void OnPointerDown(PointerEventData eventData)
    {
        startTouchPosition = Input.GetTouch(0).position;
        isDrag = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrag)
        {
            OnTap();
        }
    }

    private void OnTap()
    {
        if (cardPanel == null || panelImage == null || closeButton == null)
        {
            Debug.LogError("Panel UI or its components are not assigned in DraggableItem!");
            return;
        }

        if (image == null)
        {
            Debug.LogError("Image reference is missing in DraggableItem!");
            return;
        }

        panelImage.sprite = image.sprite;
        cardPanel.SetActive(true);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ClosePanel);
    }

    private void ClosePanel()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isEnlarged)
        {
            float distance = Vector2.Distance(startTouchPosition, Input.GetTouch(0).position);
            if (distance > tapThreshold)
            {
                isDrag = true;
                parentAfterDrag = transform.parent;
                transform.SetParent(transform.root);
                transform.SetAsLastSibling();
                image.raycastTarget = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDrag)
        {
            transform.position = Input.GetTouch(0).position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDrag)
        {
            transform.SetParent(parentAfterDrag);
            image.raycastTarget = true;
        }
    }
}
