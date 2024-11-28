using Firebase;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public GameObject playerListPanel;
    public GameObject mapPanel;
    public CardTypeManager cardTypeManager;

    private void Awake()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        playerListPanel.SetActive(false);
        mapPanel.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();

        if (draggableItem != null)
        {
            cardTypeManager.OnCardDropped(draggableItem.instanceId, draggableItem.cardId, false);
        }
    }
}
