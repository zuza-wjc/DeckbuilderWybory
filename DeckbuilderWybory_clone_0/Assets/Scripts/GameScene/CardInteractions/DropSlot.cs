using Firebase.Database;
using Firebase;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public GameObject playerListPanel;
    public GameObject mapPanel;

    DatabaseReference dbRef;
    string playerId;
    string lobbyId;
    string cardId;

    private void Awake()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        playerListPanel.SetActive(false);
        mapPanel.SetActive(false);

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck");
    }
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();

        Debug.Log("Card dropped: "+ draggableItem.tag);

        if (draggableItem != null)
        {
            cardId = draggableItem.tag;
            dbRef.Child(cardId).Child("played").SetValueAsync(true);

            if (int.Parse(cardId) == 0)
            {
                CardTypeOnMe cardTypeOnMe = FindObjectOfType<CardTypeOnMe>();
                if (cardTypeOnMe != null)
                {
                    cardTypeOnMe.OnCardDropped(cardId);
                }
                else
                {
                    Debug.LogError("CardTypeOnMe component not found in the scene!");
                }
            }
            if (int.Parse(cardId) == 1 || int.Parse(cardId) == 3)
            {
                playerListPanel.SetActive(true);

                PlayerListManager playerListManager = FindObjectOfType<PlayerListManager>();
                if (playerListManager != null)
                {
                    playerListManager.SetCardIdOnEnemy(cardId);
                }
            }
            if (int.Parse(cardId) == 2)
            {
                mapPanel.SetActive(true);

                MapManager mapManager = FindObjectOfType<MapManager>();
                if (mapManager != null)
                {
                    mapManager.FetchDataFromDatabase();
                    mapManager.SetCardIdMap(cardId);
                }
            }
        }                                  
        
    }
}


