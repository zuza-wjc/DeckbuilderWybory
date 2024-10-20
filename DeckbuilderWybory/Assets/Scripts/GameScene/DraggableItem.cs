using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image image;
    public Image backgroundOverlay;

    [HideInInspector] public Transform parentAfterDrag;

    private Vector2 startTouchPosition;
    private bool isDrag = false;
    public float tapThreshold = 10f;

    private bool isEnlarged = false;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private Vector3 centerScreenPosition;

    private Canvas canvas;

    private void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;

        centerScreenPosition = new Vector3(Screen.width / 2, Screen.height / 2, originalPosition.z);

        if (backgroundOverlay != null)
        {
            backgroundOverlay.gameObject.SetActive(false);
        }

        canvas = GetComponentInParent<Canvas>();
    }

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
        if (isEnlarged)
        {
            transform.localScale = originalScale;
            transform.position = originalPosition;
            isEnlarged = false;

            transform.SetParent(parentAfterDrag);

            if (backgroundOverlay != null)
            {
                backgroundOverlay.gameObject.SetActive(false);
            }
        }
        else
        {
            originalScale = transform.localScale;
            originalPosition = transform.position;

            transform.localScale = originalScale * 2f;
            transform.position = centerScreenPosition;

            parentAfterDrag = transform.parent;
            transform.SetParent(canvas.transform);

            isEnlarged = true;

            if (backgroundOverlay != null)
            {
                backgroundOverlay.gameObject.SetActive(true);
            }
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
